using AetherDownloader.Core.Constants;
using AetherDownloader.Core.Cookies;
using AetherDownloader.Core.Models;
using FluentAssertions;

namespace AetherDownloader.Core.Tests.Cookies;

public sealed class CookieHealthMonitorTests
{
    [Fact]
    public void AnalyzeLine_SignInRequired_ReturnsHighSeverityEvent()
    {
        var monitor = new CookieHealthMonitor();
        var line = "Sign in required to confirm your age";

        var result = monitor.AnalyzeLine(line);

        result.Should().NotBeNull();
        result!.Severity.Should().Be(CookieHealthSeverity.High);
        result.MatchedPattern.Should().Be(CookieErrorPatternCodes.CookieExpiredSignIn);
    }
}
