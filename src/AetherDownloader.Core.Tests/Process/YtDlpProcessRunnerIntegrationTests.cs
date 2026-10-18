using AetherDownloader.Core.Process;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherDownloader.Core.Tests.Process;

public sealed class YtDlpProcessRunnerIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_VersionFlag_ReturnsExitZero()
    {
        var bundledPath = Path.Combine(AppContext.BaseDirectory, "tools", "yt-dlp", "yt-dlp.exe");
        if (!File.Exists(bundledPath))
        {
            return;
        }

        var runner = new YtDlpProcessRunner(NullLogger<YtDlpProcessRunner>.Instance);
        var result = await runner.RunAsync(
            new ProcessRunOptions
            {
                ExecutablePath = bundledPath,
                Arguments = ["--version"],
            },
            CancellationToken.None);

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().NotBeEmpty();
        result.WasCancelled.Should().BeFalse();
    }
}
