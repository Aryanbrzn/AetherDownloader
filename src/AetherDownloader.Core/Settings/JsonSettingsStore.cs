using System.Text.Json;
using AetherDownloader.Core.Abstractions;
using AetherDownloader.Core.Models;
using Microsoft.Extensions.Logging;

namespace AetherDownloader.Core.Settings;

public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _settingsPath;
    private readonly AppSettingsHolder _holder;
    private readonly ILogger<JsonSettingsStore> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public JsonSettingsStore(AppSettingsHolder holder, ILogger<JsonSettingsStore> logger)
    {
        _holder = holder;
        _logger = logger;
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AetherDownloader");
        Directory.CreateDirectory(appData);
        _settingsPath = Path.Combine(appData, "settings.json");
    }

    internal JsonSettingsStore(AppSettingsHolder holder, ILogger<JsonSettingsStore> logger, string settingsPath)
    {
        _holder = holder;
        _logger = logger;
        _settingsPath = settingsPath;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _holder.Update(new AppSettings());
                return _holder.Current;
            }

            await using var stream = File.OpenRead(_settingsPath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false)
                ?? new AppSettings();
            settings = Normalize(settings);
            _holder.Update(settings);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to load settings; using defaults");
            _holder.Update(new AppSettings());
            return _holder.Current;
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        var normalized = Normalize(settings);
        _holder.Update(normalized);
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, normalized, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        var parallel = Math.Clamp(settings.MaxParallelDownloads, 1, 4);
        if (parallel == settings.MaxParallelDownloads && settings.Language == AppLanguage.En)
        {
            return settings;
        }

        return settings with
        {
            MaxParallelDownloads = parallel,
            Language = AppLanguage.En,
        };
    }
}
