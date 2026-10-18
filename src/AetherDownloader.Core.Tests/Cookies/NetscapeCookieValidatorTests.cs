using AetherDownloader.Core.Constants;
using AetherDownloader.Core.Cookies;
using AetherDownloader.Core.Models;
using FluentAssertions;

namespace AetherDownloader.Core.Tests.Cookies;

public sealed class NetscapeCookieValidatorTests
{
    private static string FixturePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);

    [Fact]
    public void Validate_ValidFixture_ReturnsSuccessWithDomains()
    {
        var result = NetscapeCookieValidator.Validate(FixturePath("cookies-valid.txt"));

        result.IsValid.Should().BeTrue();
        result.CookieCount.Should().BeGreaterThanOrEqualTo(1);
        result.Domains.Should().Contain(".example.com");
    }

    [Fact]
    public void Validate_EmptyFile_ReturnsFailureMentioningEmpty()
    {
        var result = NetscapeCookieValidator.Validate(FixturePath("cookies-empty.txt"));

        result.IsValid.Should().BeFalse();
        result.ErrorMessage!.Should().Contain("empty", because: "empty files should be rejected");
    }

    [Fact]
    public void Validate_JsonFormat_ReturnsFailureMentioningNetscape()
    {
        var result = NetscapeCookieValidator.Validate(FixturePath("cookies-mozilla-json.txt"));

        result.IsValid.Should().BeFalse();
        result.ErrorMessage!.Should().Contain("Netscape", because: "JSON format should mention Netscape export");
    }
}
