using AetherDownloader.Core.Inspection;
using AetherDownloader.Core.Process;
using FluentAssertions;

namespace AetherDownloader.Core.Tests.Inspection;

public sealed class YtDlpCookieRetryPolicyTests
{
    [Fact]
    public void ShouldRetryWithoutCookies_ReturnsFalseWhenNoCookiePath()
    {
        var result = new ProcessRunResult
        {
            ExitCode = 1,
            StandardOutput = [],
            StandardError = ["ERROR: generic failure"],
        };

        YtDlpCookieRetryPolicy.ShouldRetryWithoutCookies(result, cookiePath: null).Should().BeFalse();
    }

    [Fact]
    public void ShouldRetryWithoutCookies_ReturnsFalseForInstagramAuthErrors()
    {
        var result = new ProcessRunResult
        {
            ExitCode = 1,
            StandardOutput = ["null"],
            StandardError =
            [
                "ERROR: [Instagram] abc: Instagram sent an empty media response. use --cookies",
            ],
        };

        YtDlpCookieRetryPolicy.ShouldRetryWithoutCookies(result, cookiePath: @"C:\cookies.txt")
            .Should()
            .BeFalse();
    }

    [Fact]
    public void ShouldRetryWithoutCookies_ReturnsTrueForGenericFailureWithCookies()
    {
        var result = new ProcessRunResult
        {
            ExitCode = 1,
            StandardOutput = [],
            StandardError = ["ERROR: [youtube] abc: Video unavailable"],
        };

        YtDlpCookieRetryPolicy.ShouldRetryWithoutCookies(result, cookiePath: @"C:\cookies.txt")
            .Should()
            .BeTrue();
    }
}
