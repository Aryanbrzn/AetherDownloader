using AetherDownloader.Core.Abstractions;
using AetherDownloader.Core.DependencyInjection;
using AetherDownloader.Core.Download;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AetherDownloader.Core.Tests.DependencyInjection;

public sealed class CoreServiceCollectionTests
{
    [Fact]
    public void AddAetherDownloaderCore_ResolvesDownloadEngine()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAetherDownloaderCore();

        using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IDownloadEngine>();

        engine.Should().BeOfType<DownloadEngine>();
    }

    [Fact]
    public void AddAetherDownloaderCore_ResolvesCoreServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAetherDownloaderCore();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICookieStore>().Should().NotBeNull();
        provider.GetRequiredService<IYtDlpLocator>().Should().NotBeNull();
        provider.GetRequiredService<IFfmpegLocator>().Should().NotBeNull();
        provider.GetRequiredService<IProcessRunner>().Should().NotBeNull();
    }
}
