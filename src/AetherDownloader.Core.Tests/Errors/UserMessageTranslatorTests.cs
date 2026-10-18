using AetherDownloader.Core.Errors;
using AetherDownloader.Core.Models;
using AetherDownloader.Core.Process;
using FluentAssertions;

namespace AetherDownloader.Core.Tests.Errors;

public sealed class UserMessageTranslatorTests
{
    private readonly UserMessageTranslator _translator = new();

    [Fact]
    public void Translate_CookieError_ReturnsPersianMessageAndCookieGuide()
    {
        var classifier = new YtDlpErrorClassifier();
        var code = classifier.Classify(-1, ["Sign in required"]);
        var error = _translator.Translate(code, [RecoveryAction.CookieGuide], "detail");

        code.Should().Be(ErrorCode.DlCookie);
        error.TitleFa.Should().Be("نیاز به ورود");
        error.MessageFa.Should().Contain("کوکی");
        error.RecoveryActions.Should().Contain(RecoveryAction.CookieGuide);
    }

    [Fact]
    public void FromProcessResult_SignInRequired_SetsTechnicalDetailFromStderr()
    {
        var classifier = new YtDlpErrorClassifier();
        var result = new ProcessRunResult
        {
            ExitCode = 1,
            StandardError = ["line1", "Sign in required to confirm your age"],
            StandardOutput = [],
            Duration = TimeSpan.FromSeconds(1),
            WasCancelled = false,
        };

        var error = _translator.FromProcessResult(result, classifier);

        error.Code.Should().Be(ErrorCode.DlCookie);
        error.TechnicalDetail.Should().Contain("Sign in required");
        error.MessageEn.Should().Contain("Cookie expired");
    }

    [Theory]
    [InlineData(ErrorCode.DlNetwork, "Network error")]
    [InlineData(ErrorCode.DlGeo, "Regional restriction")]
    [InlineData(ErrorCode.DlFormat, "Invalid format")]
    [InlineData(ErrorCode.DlDisk, "Disk space")]
    [InlineData(ErrorCode.DlFfmpeg, "FFmpeg not found")]
    [InlineData(ErrorCode.DlUnknown, "Unknown error")]
    public void Translate_AllCodes_LoadEnglishTitle(ErrorCode code, string expectedTitleEn)
    {
        var error = _translator.Translate(code, [RecoveryAction.Retry]);
        error.TitleEn.Should().Be(expectedTitleEn);
    }
}
