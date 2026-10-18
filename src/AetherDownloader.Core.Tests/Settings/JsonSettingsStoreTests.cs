using AetherDownloader.Core.Models;
using AetherDownloader.Core.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherDownloader.Core.Tests.Settings;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _settingsPath;
    private readonly AppSettingsHolder _holder;
    private readonly JsonSettingsStore _store;

    public JsonSettingsStoreTests()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"aether-settings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        _settingsPath = Path.Combine(tempDir, "settings.json");
        _holder = new AppSettingsHolder();
        _store = new JsonSettingsStore(_holder, NullLogger<JsonSettingsStore>.Instance, _settingsPath);
    }

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(_settingsPath);
        if (dir is not null && Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsSettings()
    {
        var profileId = Guid.NewGuid();
        var settings = new AppSettings
        {
            DefaultOutputPath = @"D:\Downloads\Test",
            YtDlpPath = @"C:\tools\yt-dlp.exe",
            FfmpegPath = @"C:\tools\ffmpeg.exe",
            MaxParallelDownloads = 3,
            ClipboardMonitoring = false,
            Language = AppLanguage.En,
            Theme = AppTheme.Light,
            ActiveCookieProfileId = profileId,
        };

        await _store.SaveAsync(settings, CancellationToken.None);
        _holder.Current.Should().BeEquivalentTo(settings);

        var holder2 = new AppSettingsHolder();
        var store2 = new JsonSettingsStore(holder2, NullLogger<JsonSettingsStore>.Instance, _settingsPath);
        var loaded = await store2.LoadAsync(CancellationToken.None);

        loaded.Should().BeEquivalentTo(settings);
        holder2.Current.Should().BeEquivalentTo(settings);
    }

    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsDefaults()
    {
        var loaded = await _store.LoadAsync(CancellationToken.None);

        loaded.MaxParallelDownloads.Should().Be(2);
        loaded.Language.Should().Be(AppLanguage.Fa);
        loaded.Theme.Should().Be(AppTheme.Dark);
        File.Exists(_settingsPath).Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ClampedParallelDownloads_EnforcesRange()
    {
        await _store.SaveAsync(new AppSettings { MaxParallelDownloads = 99 }, CancellationToken.None);

        var holder2 = new AppSettingsHolder();
        var store2 = new JsonSettingsStore(holder2, NullLogger<JsonSettingsStore>.Instance, _settingsPath);
        var loaded = await store2.LoadAsync(CancellationToken.None);

        loaded.MaxParallelDownloads.Should().Be(4);
    }
}
