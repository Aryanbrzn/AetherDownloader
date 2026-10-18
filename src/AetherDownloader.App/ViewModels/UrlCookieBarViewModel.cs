using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using AetherDownloader.App.Services;
using AetherDownloader.Core.Abstractions;
using AetherDownloader.Core.Cookies;
using AetherDownloader.Core.Inspection;
using AetherDownloader.Core.Models;
using AetherDownloader.Core.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Reactive.Linq;

namespace AetherDownloader.App.ViewModels;

public partial class UrlCookieBarViewModel : ViewModelBase, IDisposable
{
    private readonly IDownloadEngine _downloadEngine;
    private readonly ICookieStore _cookieStore;
    private readonly AppSettingsHolder _settings;
    private readonly NavigationService _navigation;
    private readonly ToastNotificationService _toast;
    private readonly ClipboardMonitorService _clipboardMonitor;
    private readonly IDisposable _healthSubscription;
    private bool _isRefreshingProfiles;

    public UrlCookieBarViewModel(
        IDownloadEngine downloadEngine,
        ICookieStore cookieStore,
        AppSettingsHolder settings,
        NavigationService navigation,
        ToastNotificationService toast,
        ClipboardMonitorService clipboardMonitor)
    {
        _downloadEngine = downloadEngine;
        _cookieStore = cookieStore;
        _settings = settings;
        _navigation = navigation;
        _toast = toast;
        _clipboardMonitor = clipboardMonitor;

        _clipboardMonitor.UrlDetected += OnUrlDetected;

        _healthSubscription = cookieStore.HealthEvents
            .Where(static e => e.Severity >= CookieHealthSeverity.High)
            .Subscribe(OnHighSeverityHealthEvent);

        _ = RefreshProfilesAsync(CancellationToken.None);
    }

    public event Action<MediaMetadata>? InspectCompleted;

    public event Action<string>? InspectFailed;

    [ObservableProperty]
    private string _urlInput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InspectCommand))]
    private bool _isInspecting;

    [ObservableProperty]
    private CookieStatus _cookieStatus = CookieStatus.Missing;

    [ObservableProperty]
    private string _cookieStatusText = "Cookie Missing";

    [ObservableProperty]
    private CookieProfile? _activeCookieProfile;

    [ObservableProperty]
    private ObservableCollection<CookieProfile> _cookieProfiles = [];

    public bool HasCookieProfiles => CookieProfiles.Count > 0;

    public bool CanInspect => !IsInspecting && IsValidUrl(UrlInput);

    partial void OnUrlInputChanged(string value)
    {
        OnPropertyChanged(nameof(CanInspect));
        InspectCommand.NotifyCanExecuteChanged();
    }

    partial void OnActiveCookieProfileChanged(CookieProfile? value)
    {
        if (!_isRefreshingProfiles)
        {
            _ = SetActiveProfileAsync(value);
        }
    }

    partial void OnCookieProfilesChanged(ObservableCollection<CookieProfile> value) =>
        OnPropertyChanged(nameof(HasCookieProfiles));

    [RelayCommand(CanExecute = nameof(CanInspect))]
    private async Task InspectAsync()
    {
        IsInspecting = true;
        try
        {
            var inspectOptions = new InspectOptions
            {
                CookieProfileId = _settings.Current.ActiveCookieProfileId,
            };

            var metadata = await _downloadEngine
                .InspectAsync(UrlInput, inspectOptions, CancellationToken.None)
                .ConfigureAwait(true);
            InspectCompleted?.Invoke(metadata);
        }
        catch (Exception ex)
        {
            InspectFailed?.Invoke(ex.Message);
            _toast.ShowError("Inspect failed", ex.Message);
        }
        finally
        {
            IsInspecting = false;
        }
    }

    [RelayCommand]
    private void OpenCookieGuide() => _navigation.ShowCookieGuide(initialStep: 1);

    [RelayCommand]
    private async Task ImportCookieFileAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            var validation = NetscapeCookieValidator.Validate(filePath);
            if (!validation.IsValid)
            {
                _toast.ShowError("Invalid cookie file", validation.ErrorMessage ?? "Unsupported format.");
                return;
            }

            await using var stream = File.OpenRead(filePath);
            var profileName = Path.GetFileNameWithoutExtension(filePath);
            var profile = await _cookieStore.ImportAsync(stream, profileName, CancellationToken.None)
                .ConfigureAwait(true);
            await _cookieStore.SetActiveProfileAsync(profile.Id, CancellationToken.None).ConfigureAwait(true);
            await RefreshProfilesAsync(CancellationToken.None).ConfigureAwait(true);

            _toast.ShowSuccess(
                $"{validation.CookieCount} cookies imported from {validation.Domains.Count} domain(s)");
        }
        catch (CookieValidationException ex)
        {
            _toast.ShowError("Invalid cookie file", ex.Message);
        }
        catch (IOException ex)
        {
            _toast.ShowError("Import failed", ex.Message);
        }
    }

    public async Task RefreshProfilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var profiles = await _cookieStore.ListAsync(cancellationToken).ConfigureAwait(true);
            var active = await _cookieStore.GetActiveProfileAsync(cancellationToken).ConfigureAwait(true);

            _isRefreshingProfiles = true;
            try
            {
                CookieProfiles = new ObservableCollection<CookieProfile>(profiles);
                ActiveCookieProfile = active;
                UpdateCookieStatus(active);
                OnPropertyChanged(nameof(HasCookieProfiles));
            }
            finally
            {
                _isRefreshingProfiles = false;
            }
        }
        catch (Exception ex)
        {
            _toast.ShowError("Cookie profiles", ex.Message);
        }
    }

    private async Task SetActiveProfileAsync(CookieProfile? profile)
    {
        if (profile is null)
        {
            return;
        }

        try
        {
            await _cookieStore.SetActiveProfileAsync(profile.Id, CancellationToken.None).ConfigureAwait(true);
            UpdateCookieStatus(profile);
        }
        catch (Exception ex)
        {
            _toast.ShowError("Profile selection", ex.Message);
        }
    }

    private void OnUrlDetected(object? sender, string url)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            UrlInput = url;
            if (CanInspect)
            {
                _ = InspectAsync();
            }
        });
    }

    private void OnHighSeverityHealthEvent(CookieHealthEvent healthEvent)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CookieStatus = CookieStatus.Expired;
            CookieStatusText = "Cookie may have expired";
            _navigation.ShowCookieGuide(initialStep: 3);
            _toast.ShowError("Cookie issue", "Your cookie may have expired.");
        });
    }

    private void UpdateCookieStatus(CookieProfile? profile)
    {
        if (profile is null)
        {
            CookieStatus = CookieStatus.Missing;
            CookieStatusText = "Cookie Missing";
            return;
        }

        CookieStatus = CookieStatus.Valid;
        var primaryDomain = profile.DomainSummary.FirstOrDefault() ?? profile.Name;
        var ago = FormatRelativeTime(profile.ImportedAt);
        CookieStatusText = $"Cookie Connected — {primaryDomain} ({profile.DomainSummary.Count} domains) · Imported {ago}";
    }

    private static string FormatRelativeTime(DateTimeOffset importedAt)
    {
        var delta = DateTimeOffset.UtcNow - importedAt;
        if (delta.TotalMinutes < 60)
        {
            return $"{Math.Max(1, (int)delta.TotalMinutes)}m ago";
        }

        if (delta.TotalHours < 24)
        {
            return $"{(int)delta.TotalHours}h ago";
        }

        return $"{(int)delta.TotalDays}d ago";
    }

    private static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        try
        {
            UrlValidator.Validate(url);
            return true;
        }
        catch (UrlValidationException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        _clipboardMonitor.UrlDetected -= OnUrlDetected;
        _healthSubscription.Dispose();
    }
}
