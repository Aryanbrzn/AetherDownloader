using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AetherDownloader.App.Services;
using AetherDownloader.App.ViewModels;
using AetherDownloader.Core.Abstractions;

namespace AetherDownloader.App.Views;

public partial class MainWindow : Window
{
    private readonly DownloadOrchestrator _orchestrator;
    private readonly MainViewModel _viewModel;
    private readonly IToolBootstrapper _toolBootstrapper;
    private readonly IDialogService _dialogService;
    private readonly AppStatusBarViewModel _statusBar;
    private bool _toolPromptShown;

    public MainWindow(
        MainViewModel viewModel,
        DownloadOrchestrator orchestrator,
        IToolBootstrapper toolBootstrapper,
        IDialogService dialogService,
        AppStatusBarViewModel statusBar)
    {
        _viewModel = viewModel;
        _orchestrator = orchestrator;
        _toolBootstrapper = toolBootstrapper;
        _dialogService = dialogService;
        _statusBar = statusBar;
        DataContext = viewModel;
        InitializeComponent();
        SizeChanged += OnSizeChanged;
        Closing += MainWindow_Closing;
        Loaded += OnLoaded;
    }

    public Border ToastHost => ToastHostBorder;

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) =>
        _viewModel.UpdateLayoutWidth(e.NewSize.Width);

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.UpdateLayoutWidth(ActualWidth);
        await PromptForMissingToolsAsync().ConfigureAwait(true);
    }

    private async Task PromptForMissingToolsAsync()
    {
        if (_toolPromptShown)
        {
            return;
        }

        _toolPromptShown = true;
        var availability = _toolBootstrapper.CheckAvailability();
        if (availability.YtDlpAvailable && availability.FfmpegAvailable)
        {
            return;
        }

        var missing = new List<string>();
        if (!availability.YtDlpAvailable)
        {
            missing.Add("yt-dlp");
        }

        if (!availability.FfmpegAvailable)
        {
            missing.Add("FFmpeg");
        }

        var result = _dialogService.ShowConfirm(
            "Missing tools",
            $"{string.Join(" and ", missing)} not found.{Environment.NewLine}{Environment.NewLine}"
            + $"Download the official builds now into LocalAppData?{Environment.NewLine}"
            + "(You can also install them later from Settings.)",
            MessageBoxButton.YesNo);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            if (!availability.YtDlpAvailable)
            {
                await _toolBootstrapper.EnsureYtDlpAsync(cancellationToken: CancellationToken.None)
                    .ConfigureAwait(true);
            }

            if (!availability.FfmpegAvailable)
            {
                await _toolBootstrapper.EnsureFfmpegAsync(cancellationToken: CancellationToken.None)
                    .ConfigureAwait(true);
            }

            await _statusBar.RefreshToolVersionsAsync().ConfigureAwait(true);
            _dialogService.ShowInfo("Tools", "Required tools are ready.");
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(
                "Tool download failed",
                $"{ex.Message}{Environment.NewLine}{Environment.NewLine}Open Settings to set paths manually or retry Download missing tools.");
        }
    }

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_orchestrator.ActiveDownloadCount == 0)
        {
            return;
        }

        e.Cancel = true;

        var result = MessageBox.Show(
            "Downloads are in progress. Exit anyway?",
            "AetherDownloader",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _orchestrator.CancelAll();
        await _orchestrator.WaitForDrainAsync(TimeSpan.FromSeconds(10), CancellationToken.None)
            .ConfigureAwait(true);
        Application.Current.Shutdown();
    }
}
