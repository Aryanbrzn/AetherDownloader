using System.Windows;
using AetherDownloader.App.Services;
using AetherDownloader.App.ViewModels;
using AetherDownloader.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AetherDownloader.App.Services;

public sealed class NavigationService
{
    private readonly IServiceProvider _services;
    private readonly IDialogService _dialogService;

    public NavigationService(IServiceProvider services, IDialogService dialogService)
    {
        _services = services;
        _dialogService = dialogService;
    }

    public void ShowCookieGuide(int initialStep = 1)
    {
        var viewModel = _services.GetRequiredService<CookieGuideViewModel>();
        viewModel.Reset(initialStep);

        var dialog = new CookieGuideModal(viewModel)
        {
            Owner = Application.Current.MainWindow,
        };

        _dialogService.ShowDialog(dialog);
    }

    public void ShowSettings()
    {
        var viewModel = _services.GetRequiredService<SettingsViewModel>();
        var dialog = new SettingsWindow(viewModel)
        {
            Owner = Application.Current.MainWindow,
        };

        _dialogService.ShowDialog(dialog);
    }

    public void ShowAbout()
    {
        var dialog = new AboutWindow
        {
            Owner = Application.Current.MainWindow,
        };

        _dialogService.ShowDialog(dialog);
    }
}
