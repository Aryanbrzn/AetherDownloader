using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AetherDownloader.Core.Abstractions;
using AetherDownloader.Core.Cookies;
using AetherDownloader.Core.Models;

namespace AetherDownloader.App.ViewModels;

public partial class CookieGuideViewModel : ViewModelBase
{
    private readonly ICookieStore _cookieStore;
    private readonly UrlCookieBarViewModel _urlCookieBarViewModel;

    public CookieGuideViewModel(ICookieStore cookieStore, UrlCookieBarViewModel urlCookieBarViewModel)
    {
        _cookieStore = cookieStore;
        _urlCookieBarViewModel = urlCookieBarViewModel;
    }

    public event Action? CloseRequested;

    [ObservableProperty]
    private int _currentStep = 1;

    [ObservableProperty]
    private bool _cookieImportSucceeded;

    [ObservableProperty]
    private string _importResultText = string.Empty;

    public bool CanGoNext => CurrentStep switch
    {
        1 or 2 => true,
        3 => CookieImportSucceeded,
        _ => false,
    };

    public bool IsLastStep => CurrentStep >= 3;

    public string NextButtonText => IsLastStep ? "Done" : "Next →";

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(NextButtonText));
        NextCommand.NotifyCanExecuteChanged();
    }

    partial void OnCookieImportSucceededChanged(bool value)
    {
        OnPropertyChanged(nameof(CanGoNext));
        NextCommand.NotifyCanExecuteChanged();
    }

    public void Reset(int initialStep = 1)
    {
        CurrentStep = initialStep;
        CookieImportSucceeded = false;
        ImportResultText = string.Empty;
    }

    [RelayCommand]
    private void Back()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        if (CurrentStep < 3)
        {
            CurrentStep++;
            return;
        }

        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Skip() => CloseRequested?.Invoke();

    [RelayCommand]
    private async Task ImportDroppedFileAsync(string? filePath)
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
                ImportResultText = validation.ErrorMessage ?? "Invalid cookie file.";
                CookieImportSucceeded = false;
                return;
            }

            await using var stream = File.OpenRead(filePath);
            var profileName = Path.GetFileNameWithoutExtension(filePath);
            var profile = await _cookieStore.ImportAsync(stream, profileName, CancellationToken.None)
                .ConfigureAwait(true);
            await _cookieStore.SetActiveProfileAsync(profile.Id, CancellationToken.None).ConfigureAwait(true);

            CookieImportSucceeded = true;
            var domainText = profile.DomainSummary.Count > 0
                ? string.Join(", ", profile.DomainSummary)
                : "unknown domains";
            ImportResultText =
                $"Import successful — {validation.CookieCount} cookies loaded from {domainText}";

            await _urlCookieBarViewModel.RefreshProfilesAsync(CancellationToken.None).ConfigureAwait(true);
        }
        catch (CookieValidationException ex)
        {
            ImportResultText = ex.Message;
            CookieImportSucceeded = false;
        }
        catch (IOException ex)
        {
            ImportResultText = $"Unable to read file: {ex.Message}";
            CookieImportSucceeded = false;
        }
    }

    [RelayCommand]
    private void OpenBrowserExtension(string browser)
    {
        var url = browser switch
        {
            "Chrome" => "https://chromewebstore.google.com/detail/get-cookiestxt-locally/cclelndahbckbenkjhflpdbgdldlbecc",
            "Edge" => "https://microsoftedge.microsoft.com/addons/detail/get-cookiestxt-locally/",
            "Firefox" => "https://addons.mozilla.org/en-US/firefox/addon/cookies-txt/",
            _ => "https://chromewebstore.google.com/detail/get-cookiestxt-locally/cclelndahbckbenkjhflpdbgdldlbecc",
        };

        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
