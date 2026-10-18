using AetherDownloader.App.Services;
using AetherDownloader.Core.Abstractions;
using AetherDownloader.Core.Models;
using AetherDownloader.Core.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AetherDownloader.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsStore _settingsStore;
    private readonly AppSettingsHolder _settingsHolder;
    private readonly DownloadOrchestrator _orchestrator;
    private readonly DownloadQueueViewModel _downloadQueue;
    private readonly AppStatusBarViewModel _statusBar;
    private readonly IToolBootstrapper _toolBootstrapper;
    private readonly IDialogService _dialogService;

    public SettingsViewModel(
        ISettingsStore settingsStore,
        AppSettingsHolder settingsHolder,
        DownloadOrchestrator orchestrator,
        DownloadQueueViewModel downloadQueue,
        AppStatusBarViewModel statusBar,
        IToolBootstrapper toolBootstrapper,
        IDialogService dialogService)
    {
        _settingsStore = settingsStore;
        _settingsHolder = settingsHolder;
        _orchestrator = orchestrator;
        _downloadQueue = downloadQueue;
        _statusBar = statusBar;
        _toolBootstrapper = toolBootstrapper;
        _dialogService = dialogService;
        LoadFrom(_settingsHolder.Current);
    }

    [ObservableProperty]
    private string _defaultOutputPath = string.Empty;

    [ObservableProperty]
    private string? _ytDlpPath;

    [ObservableProperty]
    private string? _ffmpegPath;

    [ObservableProperty]
    private int _maxParallelDownloads = 2;

    [ObservableProperty]
    private bool _clipboardMonitoring = true;

    [ObservableProperty]
    private AppTheme _theme = AppTheme.Dark;

    [ObservableProperty]
    private bool _isDownloadingTools;

    [ObservableProperty]
    private string _toolsStatus = string.Empty;

    [ObservableProperty]
    private double _toolsProgressPercent;

    [ObservableProperty]
    private bool _toolsProgressIsIndeterminate;

    public AppTheme[] Themes { get; } = [AppTheme.Dark, AppTheme.Light];

    [RelayCommand]
    private void BrowseOutputFolder()
    {
        var dialog = new OpenFolderDialog
        {
            InitialDirectory = DefaultOutputPath,
        };

        if (dialog.ShowDialog() == true)
        {
            DefaultOutputPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void BrowseYtDlpPath()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
            FileName = YtDlpPath ?? string.Empty,
        };

        if (dialog.ShowDialog() == true)
        {
            YtDlpPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void BrowseFfmpegPath()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
            FileName = FfmpegPath ?? string.Empty,
        };

        if (dialog.ShowDialog() == true)
        {
            FfmpegPath = dialog.FileName;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDownloadTools))]
    private async Task DownloadToolsAsync()
    {
        IsDownloadingTools = true;
        ToolsStatus = "Starting…";
        ToolsProgressPercent = 0;
        ToolsProgressIsIndeterminate = true;

        var progress = new Progress<ToolDownloadProgress>(OnToolProgress);

        try
        {
            await _toolBootstrapper.EnsureYtDlpAsync(progress, CancellationToken.None).ConfigureAwait(true);
            await _toolBootstrapper.EnsureFfmpegAsync(progress, CancellationToken.None).ConfigureAwait(true);
            ToolsStatus = "Tools ready.";
            ToolsProgressPercent = 100;
            ToolsProgressIsIndeterminate = false;
            await _statusBar.RefreshToolVersionsAsync().ConfigureAwait(true);
            _dialogService.ShowInfo(
                "Tools",
                "yt-dlp and FFmpeg are available. Binaries were installed under %LOCALAPPDATA%\\AetherDownloader\\tools when a download was required.");
        }
        catch (Exception ex)
        {
            ToolsStatus = "Download failed.";
            ToolsProgressIsIndeterminate = false;
            _dialogService.ShowError(
                "Tool download failed",
                $"{ex.Message}{Environment.NewLine}{Environment.NewLine}You can also install the tools manually and set paths above.");
        }
        finally
        {
            IsDownloadingTools = false;
            DownloadToolsCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnToolProgress(ToolDownloadProgress report)
    {
        ToolsStatus = report.Message;
        if (report.PercentComplete is null)
        {
            ToolsProgressIsIndeterminate = true;
            return;
        }

        ToolsProgressIsIndeterminate = false;
        ToolsProgressPercent = report.PercentComplete.Value;
    }

    private bool CanDownloadTools() => !IsDownloadingTools;

    partial void OnIsDownloadingToolsChanged(bool value) => DownloadToolsCommand.NotifyCanExecuteChanged();

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        var settings = new AppSettings
        {
            DefaultOutputPath = DefaultOutputPath,
            YtDlpPath = string.IsNullOrWhiteSpace(YtDlpPath) ? null : YtDlpPath,
            FfmpegPath = string.IsNullOrWhiteSpace(FfmpegPath) ? null : FfmpegPath,
            MaxParallelDownloads = MaxParallelDownloads,
            ClipboardMonitoring = ClipboardMonitoring,
            Language = AppLanguage.En,
            Theme = Theme,
            ActiveCookieProfileId = _settingsHolder.Current.ActiveCookieProfileId,
        };

        await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(true);
        _orchestrator.RebuildQueue();
        _statusBar.ClipboardMonitoring = ClipboardMonitoring;
        _downloadQueue.RefreshErrorMessages();
        await _statusBar.RefreshToolVersionsAsync().ConfigureAwait(true);
    }

    private void LoadFrom(AppSettings settings)
    {
        DefaultOutputPath = settings.DefaultOutputPath;
        YtDlpPath = settings.YtDlpPath;
        FfmpegPath = settings.FfmpegPath;
        MaxParallelDownloads = settings.MaxParallelDownloads;
        ClipboardMonitoring = settings.ClipboardMonitoring;
        Theme = settings.Theme;
    }
}
