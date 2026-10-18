using AetherDownloader.Core.Abstractions;
using AetherDownloader.Core.Cookies;
using AetherDownloader.Core.Download;
using AetherDownloader.Core.Errors;
using AetherDownloader.Core.Inspection;
using AetherDownloader.Core.Process;
using AetherDownloader.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace AetherDownloader.Core.DependencyInjection;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddAetherDownloaderCore(this IServiceCollection services)
    {
        services.AddSingleton<AppSettingsHolder>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<SettingsBootstrap>();

        services.AddSingleton<IYtDlpLocator>(sp =>
        {
            var holder = sp.GetRequiredService<AppSettingsHolder>();
            return new YtDlpLocator(() => holder.Current.YtDlpPath);
        });
        services.AddSingleton<IFfmpegLocator>(sp =>
        {
            var holder = sp.GetRequiredService<AppSettingsHolder>();
            return new FfmpegLocator(() => holder.Current.FfmpegPath);
        });

        services.AddSingleton<IProcessRunner, YtDlpProcessRunner>();
        services.AddSingleton<IToolBootstrapper, ToolBootstrapper>();
        services.AddSingleton<CookieStore>();
        services.AddSingleton<ICookieStore>(sp => sp.GetRequiredService<CookieStore>());
        services.AddSingleton<UrlInspector>();
        services.AddSingleton<FormatDiscoveryService>();
        services.AddSingleton<YtDlpErrorClassifier>();
        services.AddSingleton<UserMessageTranslator>();
        services.AddSingleton<IDownloadEngine, DownloadEngine>();

        return services;
    }
}
