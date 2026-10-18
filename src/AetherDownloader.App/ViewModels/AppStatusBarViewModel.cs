using AetherDownloader.App.Services;
using AetherDownloader.Core.Abstractions;
using AetherDownloader.Core.Models;
using AetherDownloader.Core.Process;
using AetherDownloader.Core.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AetherDownloader.App.ViewModels;

public partial class AppStatusBarViewModel : ViewModelBase
{
    private readonly ClipboardMonitorService _clipboard;
    private readonly NavigationService _navigation;
    private readonly IProcessRunner _runner;
    private readonly IYtDlpLocator _ytDlpLocator;
    private readonly IFfmpegLocator _ffmpegLocator;

    public AppStatusBarViewModel(
        IYtDlpLocator ytDlpLocator,
        IFfmpegLocator ffmpegLocator,
        IProcessRunner runner,
        ClipboardMonitorService clipboard,
        AppSettingsHolder settings,
        NavigationService navigation)
    {
        _clipboard = clipboard;
        _navigation = navigation;
        _runner = runner;
        _ytDlpLocator = ytDlpLocator;
        _ffmpegLocator = ffmpegLocator;
        ClipboardMonitoring = settings.Current.ClipboardMonitoring;
        _ = RefreshToolVersionsAsync();
    }

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _ytDlpVersion = "yt-dlp …";

    [ObservableProperty]
    private string _ffmpegStatus = "FFmpeg ✗";

    [ObservableProperty]
    private bool _clipboardMonitoring;

    partial void OnClipboardMonitoringChanged(bool value) => _clipboard.SetEnabled(value);

    [RelayCommand]
    private void OpenSettings() => _navigation.ShowSettings();

    [RelayCommand]
    private void OpenAbout() => _navigation.ShowAbout();

    public async Task RefreshToolVersionsAsync()
    {
        try
        {
            var result = await _runner.RunAsync(
                new ProcessRunOptions
                {
                    ExecutablePath = _ytDlpLocator.ResolveExecutable(),
                    Arguments = ["--version"],
                    TimeoutSeconds = 10,
                },
                CancellationToken.None).ConfigureAwait(true);

            var version = result.StandardOutput.FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(version))
            {
                YtDlpVersion = $"yt-dlp {version}";
            }
        }
        catch
        {
            YtDlpVersion = "yt-dlp unavailable";
        }

        FfmpegStatus = _ffmpegLocator.TryResolve(out _)
            ? "FFmpeg ✓"
            : "FFmpeg ✗";
    }
}
