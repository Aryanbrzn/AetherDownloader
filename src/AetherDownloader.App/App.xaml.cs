using System.IO;
using System.Windows;
using AetherDownloader.App.Services;
using AetherDownloader.App.ViewModels;
using AetherDownloader.App.Views;
using AetherDownloader.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AetherDownloader.App;

public partial class App : Application
{
    private IHost? _host;

    public static IServiceProvider Services =>
        ((App)Current)._host?.Services
        ?? throw new InvalidOperationException("Application host is not initialized.");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AetherDownloader",
            "logs",
            "aether-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .CreateLogger();

        try
        {
            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices(ConfigureServices)
                .Build();

            await _host.StartAsync().ConfigureAwait(true);

            var toastService = _host.Services.GetRequiredService<ToastNotificationService>();
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            toastService.Attach(mainWindow);
            MainWindow = mainWindow;
            mainWindow.Show();
            mainWindow.Activate();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application startup failed");
            try
            {
                MessageBox.Show(
                    $"Startup failed:{Environment.NewLine}{ex.Message}",
                    "AetherDownloader",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // Ignore secondary UI failures during crash reporting.
            }

            Shutdown(1);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging();
        services.AddAetherDownloaderCore();

        services.AddSingleton<ThumbnailLoaderService>();
        services.AddSingleton<DownloadOrchestrator>();
        services.AddSingleton<DownloadQueueViewModel>();
        services.AddSingleton<MetadataCardViewModel>();
        services.AddSingleton<AppStatusBarViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<UrlCookieBarViewModel>();
        services.AddSingleton<CookieGuideViewModel>();
        services.AddSingleton<ClipboardMonitorService>();
        services.AddSingleton<ToastNotificationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<MainWindow>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync().ConfigureAwait(true);
            _host.Dispose();
        }

        await Log.CloseAndFlushAsync().ConfigureAwait(true);
        base.OnExit(e);
    }
}
