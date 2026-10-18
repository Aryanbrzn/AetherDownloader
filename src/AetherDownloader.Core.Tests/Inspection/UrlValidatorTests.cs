using AetherDownloader.Core.Inspection;
using FluentAssertions;

namespace AetherDownloader.Core.Tests.Inspection;

public sealed class UrlValidatorTests
{
    [Fact]
    public void Validate_EmptyUrl_ThrowsUrlValidationException()
    {
        var act = () => UrlValidator.Validate(string.Empty);
        act.Should().Throw<UrlValidationException>().WithMessage("*required*");
    }

    [Fact]
    public void Validate_WhitespaceUrl_ThrowsUrlValidationException()
    {
        var act = () => UrlValidator.Validate("   ");
        act.Should().Throw<UrlValidationException>();
    }

    [Fact]
    public void Validate_TooLongUrl_ThrowsUrlValidationException()
    {
        var longUrl = "https://example.com/" + new string('a', 2048);
        var act = () => UrlValidator.Validate(longUrl);
        act.Should().Throw<UrlValidationException>().WithMessage("*maximum length*");
    }

    [Fact]
    public void Validate_FtpScheme_ThrowsUrlValidationException()
    {
        var act = () => UrlValidator.Validate("ftp://example.com/file.mp4");
        act.Should().Throw<UrlValidationException>().WithMessage("*HTTP and HTTPS*");
    }

    [Fact]
    public void Validate_ValidHttpsUrl_ReturnsNormalizedUrl()
    {
        var result = UrlValidator.Validate("https://youtube.com/watch?v=abc");

        result.Original.Should().Be("https://youtube.com/watch?v=abc");
        result.Normalized.Should().StartWith("https://");
    }
}
