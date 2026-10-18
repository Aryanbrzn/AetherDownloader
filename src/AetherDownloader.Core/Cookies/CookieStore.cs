using AetherDownloader.Core.Abstractions;
using AetherDownloader.Core.DependencyInjection;
using AetherDownloader.Core.Models;
using AetherDownloader.Core.Process;
using AetherDownloader.Core.Settings;
using Microsoft.Extensions.Logging;
using System.Reactive.Subjects;

namespace AetherDownloader.Core.Cookies;

public sealed class CookieStore : ICookieStore, IDisposable
{
    private readonly Subject<CookieHealthEvent> _healthSubject = new();
    private readonly CookieFileStorage _storage;
    private readonly CookieProfileManifest _manifest;
    private readonly CookieHealthMonitor _healthMonitor = new();
    private readonly ILogger<CookieStore> _logger;
    private readonly AppSettingsHolder? _settingsHolder;
    private readonly ISettingsStore? _settingsStore;
    private Guid? _activeProfileId;

    public CookieStore(
        ILogger<CookieStore> logger,
        AppSettingsHolder settingsHolder,
        ISettingsStore settingsStore,
        SettingsBootstrap _)
        : this(logger, baseDirectoryOverride: null, settingsHolder, settingsStore)
    {
    }

    internal CookieStore(ILogger<CookieStore> logger, string? baseDirectoryOverride)
        : this(logger, baseDirectoryOverride, settingsHolder: null, settingsStore: null)
    {
    }

    internal CookieStore(
        ILogger<CookieStore> logger,
        string? baseDirectoryOverride,
        AppSettingsHolder? settingsHolder,
        ISettingsStore? settingsStore)
    {
        _logger = logger;
        _settingsHolder = settingsHolder;
        _settingsStore = settingsStore;
        _storage = new CookieFileStorage(baseDirectoryOverride);
        _manifest = new CookieProfileManifest(_storage);
        _activeProfileId = settingsHolder?.Current.ActiveCookieProfileId;
    }

    public IObservable<CookieHealthEvent> HealthEvents => _healthSubject;

    public async Task<CookieProfile> ImportAsync(
        Stream netscapeFile,
        string name,
        CancellationToken cancellationToken)
    {
        var validation = NetscapeCookieValidator.Validate(netscapeFile);
        if (!validation.IsValid)
        {
            throw new CookieValidationException(validation.ErrorMessage ?? "Invalid cookie file.");
        }

        if (!netscapeFile.CanSeek)
        {
            throw new CookieValidationException("Cookie stream must be seekable for import.");
        }

        netscapeFile.Position = 0;
        var profileId = Guid.NewGuid();
        await _storage.CopyToStoreAsync(netscapeFile, profileId, cancellationToken).ConfigureAwait(false);

        var entry = new CookieProfileEntry
        {
            Id = profileId,
            Name = name,
            ImportedAt = DateTimeOffset.UtcNow,
            DomainSummary = validation.Domains,
        };
        await _manifest.AddAsync(entry, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Cookie profile imported: {ProfileId}, domains: {DomainCount}",
            profileId,
            validation.Domains.Count);

        return ToProfile(entry);
    }

    public async Task<IReadOnlyList<CookieProfile>> ListAsync(CancellationToken cancellationToken)
    {
        var entries = await _manifest.LoadAsync(cancellationToken).ConfigureAwait(false);
        var profiles = new List<CookieProfile>();

        foreach (var entry in entries)
        {
            if (!_storage.CookieFileExists(entry.Id))
            {
                continue;
            }

            profiles.Add(ToProfile(entry));
        }

        return profiles;
    }

    public async Task SetActiveProfileAsync(Guid? profileId, CancellationToken cancellationToken)
    {
        if (profileId is Guid id && !_storage.CookieFileExists(id))
        {
            throw new CookieValidationException("Selected cookie profile no longer exists.");
        }

        _activeProfileId = profileId;
        _logger.LogDebug("Active cookie profile set: {ProfileId}", profileId);

        if (_settingsHolder is not null && _settingsStore is not null)
        {
            var settings = _settingsHolder.Current with { ActiveCookieProfileId = profileId };
            await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<CookieProfile?> GetActiveProfileAsync(CancellationToken cancellationToken)
    {
        if (_activeProfileId is not Guid activeId)
        {
            return null;
        }

        var entries = await _manifest.LoadAsync(cancellationToken).ConfigureAwait(false);
        var entry = entries.FirstOrDefault(e => e.Id == activeId);
        if (entry is null || !_storage.CookieFileExists(entry.Id))
        {
            _activeProfileId = null;
            return null;
        }

        return ToProfile(entry);
    }

    public void OnProcessOutputLine(ProcessOutputLine line)
    {
        if (!line.IsError)
        {
            return;
        }

        var healthEvent = _healthMonitor.AnalyzeLine(line.Text);
        if (healthEvent is not null)
        {
            _healthSubject.OnNext(healthEvent);
        }
    }

    public void Dispose() => _healthSubject.Dispose();

    private CookieProfile ToProfile(CookieProfileEntry entry) =>
        new()
        {
            Id = entry.Id,
            Name = entry.Name,
            FilePath = _storage.GetCookieFilePath(entry.Id),
            ImportedAt = entry.ImportedAt,
            IsValid = true,
            DomainSummary = entry.DomainSummary,
        };
}
