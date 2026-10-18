using AetherDownloader.Core.Inspection;
using AetherDownloader.Core.Process;
using FluentAssertions;

namespace AetherDownloader.Core.Tests.Inspection;

public sealed class YtDlpOutputHelperTests
{
    [Fact]
    public void GetJsonPayload_UsesLastJsonLine()
    {
        var result = new ProcessRunResult
        {
            ExitCode = 0,
            StandardOutput = ["WARNING: ignored", "{\"id\":\"abc\"}"],
            StandardError = [],
        };

        YtDlpOutputHelper.GetJsonPayload(result).Should().Be("{\"id\":\"abc\"}");
    }

    [Fact]
    public void GetFailureMessage_StripsErrorPrefix()
    {
        var result = new ProcessRunResult
        {
            ExitCode = 1,
            StandardOutput = [],
            StandardError = ["ERROR: [youtube] abc123: The page needs to be reloaded."],
        };

        YtDlpOutputHelper.GetFailureMessage(result, "fallback")
            .Should()
            .Be("[youtube] abc123: The page needs to be reloaded.");
    }

    [Fact]
    public void GetFailureMessage_UsesFallbackWhenStderrEmpty()
    {
        var result = new ProcessRunResult
        {
            ExitCode = 1,
            StandardOutput = [],
            StandardError = [],
        };

        YtDlpOutputHelper.GetFailureMessage(result, "Metadata extraction failed.")
            .Should()
            .Be("Metadata extraction failed.");
    }

    [Fact]
    public void IsUsableJsonPayload_RejectsNullLiteralAndEmpty()
    {
        YtDlpOutputHelper.IsUsableJsonPayload("null").Should().BeFalse();
        YtDlpOutputHelper.IsUsableJsonPayload("").Should().BeFalse();
        YtDlpOutputHelper.IsUsableJsonPayload("{\"id\":\"abc\"}").Should().BeTrue();
    }

    [Fact]
    public void RequireUsableJsonPayload_ThrowsWithStderrMessageForNullOutput()
    {
        var result = new ProcessRunResult
        {
            ExitCode = 0,
            StandardOutput = ["null"],
            StandardError =
            [
                "ERROR: [Instagram] abc: Instagram sent an empty media response. use --cookies",
            ],
        };

        var act = () => YtDlpOutputHelper.RequireUsableJsonPayload(result, "Format discovery failed.");

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*empty media response*");
    }
}
