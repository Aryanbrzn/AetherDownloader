using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using AetherDownloader.App.Services;
using AetherDownloader.Core.Models;
using AetherDownloader.Core.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AetherDownloader.App.ViewModels;

public partial class DownloadItemViewModel : ViewModelBase
{
    private readonly DownloadOrchestrator _orchestrator;
    private readonly NavigationService _navigation;
    private AppLanguage _language;

    public DownloadItemViewModel(
        DownloadOrchestrator orchestrator,
        NavigationService navigation,
        AppSettingsHolder settings)
    {
        _orchestrator = orchestrator;
        _navigation = navigation;
        _language = settings.Current.Language;
    }

    public Guid JobId { get; init; }

    public required string Url { get; init; }

    public required FormatSelection FormatSelection { get; init; }

    public bool DownloadPlaylist { get; init; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _formatLabel = string.Empty;

    [ObservableProperty]
    private double _percent;

    [ObservableProperty]
    private string? _speed;

    [ObservableProperty]
    private string? _eta;

    [ObservableProperty]
    private string? _sizeProgress;

    [ObservableProperty]
    private DownloadJobState _status;

    [ObservableProperty]
    private DownloadPhase _phase;

    [ObservableProperty]
    private UserFacingError? _userFacingError;

    [ObservableProperty]
    private string? _outputFilePath;

    public CancellationTokenSource Cancellation { get; } = new();

    public string DisplayError => UserFacingErrorExtensions.GetMessage(UserFacingError, _language);

    public string DisplayErrorTitle => UserFacingErrorExtensions.GetTitle(UserFacingError, _language);

    public bool IsIndeterminate => Phase is DownloadPhase.Merging or DownloadPhase.Extracting;

    public bool ShowErrorBanner => Status == DownloadJobState.Failed && UserFacingError is not null;

    public IEnumerable<RecoveryAction> RecoveryActions => UserFacingError?.RecoveryActions ?? [];

    partial void OnStatusChanged(DownloadJobState value) => OnPropertyChanged(nameof(ShowErrorBanner));

    partial void OnUserFacingErrorChanged(UserFacingError? value)
    {
        OnPropertyChanged(nameof(DisplayError));
        OnPropertyChanged(nameof(DisplayErrorTitle));
        OnPropertyChanged(nameof(RecoveryActions));
        OnPropertyChanged(nameof(ShowErrorBanner));
    }

    partial void OnPhaseChanged(DownloadPhase value) => OnPropertyChanged(nameof(IsIndeterminate));

    public void RefreshLanguage(AppLanguage language)
    {
        _language = language;
        OnPropertyChanged(nameof(DisplayError));
        OnPropertyChanged(nameof(DisplayErrorTitle));
    }

    [RelayCommand]
    private void Cancel()
    {
        if (Status is DownloadJobState.Downloading or DownloadJobState.Pending)
        {
            Cancellation.Cancel();
        }
    }

    [RelayCommand]
    private void Remove() => _orchestrator.RemoveItem(this);

    [RelayCommand]
    private void OpenFolder()
    {
        if (string.IsNullOrEmpty(OutputFilePath))
        {
            return;
        }

        var folder = Path.GetDirectoryName(OutputFilePath);
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
        {
            return;
        }

        Process.Start(new ProcessStartInfo("explorer.exe", folder) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task RetryAsync()
    {
        UserFacingError = null;
        Percent = 0;
        Speed = null;
        Eta = null;
        SizeProgress = null;
        Phase = DownloadPhase.Unknown;
        OutputFilePath = null;

        if (Cancellation.IsCancellationRequested)
        {
            return;
        }

        await _orchestrator.StartDownloadAsync(this, FormatSelection, DownloadPlaylist).ConfigureAwait(true);
    }

    [RelayCommand]
    private void UpdateCookie() => _navigation.ShowCookieGuide(initialStep: 3);

    [RelayCommand]
    private void OpenSettings() => _navigation.ShowSettings();

    [RelayCommand]
    private void CopyLog()
    {
        if (!string.IsNullOrEmpty(UserFacingError?.TechnicalDetail))
        {
            Clipboard.SetText(UserFacingError.TechnicalDetail);
        }
    }
}
