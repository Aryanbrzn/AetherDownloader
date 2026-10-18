using AetherDownloader.Core.Errors;
using AetherDownloader.Core.Models;
using FluentAssertions;

namespace AetherDownloader.Core.Tests.Errors;

public sealed class YtDlpErrorClassifierTests
{
    private readonly YtDlpErrorClassifier _classifier = new();

    [Theory]
    [InlineData("Unable to download video: timeout", ErrorCode.DlNetwork)]
    [InlineData("not available in your country", ErrorCode.DlGeo)]
    [InlineData("Sign in required to confirm", ErrorCode.DlCookie)]
    [InlineData("Requested format is not available", ErrorCode.DlFormat)]
    [InlineData("No space left on device", ErrorCode.DlDisk)]
    [InlineData("ffmpeg not found", ErrorCode.DlFfmpeg)]
    [InlineData("HTTP Error 429", ErrorCode.DlNetwork)]
    [InlineData("HTTP Error 403 Forbidden", ErrorCode.DlGeo)]
    public void Classify_KnownPatterns_ReturnsExpectedCode(string stderr, ErrorCode expected)
    {
        var code = _classifier.Classify(-1, [stderr]);
        code.Should().Be(expected);
    }

    [Fact]
    public void Classify_UnknownPattern_ReturnsDlUnknown()
    {
        var code = _classifier.Classify(-1, ["Something completely unexpected happened"]);
        code.Should().Be(ErrorCode.DlUnknown);
    }

    [Fact]
    public void Classify_FixtureLines_MatchesExpectedCodes()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "stderr-lines.txt");
        var lines = File.ReadAllLines(path).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

        var expected = new Dictionary<string, ErrorCode>
        {
            ["Sign in required"] = ErrorCode.DlCookie,
            ["403"] = ErrorCode.DlGeo,
            ["not available in your country"] = ErrorCode.DlGeo,
            ["No space left"] = ErrorCode.DlDisk,
            ["ffmpeg not found"] = ErrorCode.DlFfmpeg,
            ["Requested format"] = ErrorCode.DlFormat,
            ["429"] = ErrorCode.DlNetwork,
            ["Connection reset"] = ErrorCode.DlNetwork,
            ["unexpected"] = ErrorCode.DlUnknown,
        };

        foreach (var line in lines)
        {
            var code = _classifier.Classify(-1, [line]);
            var match = expected.First(kvp => line.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase));
            code.Should().Be(match.Value, because: $"line was: {line}");
        }
    }
}
