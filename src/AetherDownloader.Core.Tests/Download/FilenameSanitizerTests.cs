using AetherDownloader.Core.Download;
using FluentAssertions;

namespace AetherDownloader.Core.Tests.Download;

public sealed class FilenameSanitizerTests
{
    [Fact]
    public void Sanitize_InvalidChars_StripsThem()
    {
        var result = FilenameSanitizer.Sanitize("video<file>:name|test?.mp4");

        result.Should().NotContain("<");
        result.Should().NotContain(">");
        result.Should().NotContain(":");
        result.Should().NotContain("|");
        result.Should().NotContain("?");
    }

    [Fact]
    public void Sanitize_ParentDirectoryReference_RemovesDotDot()
    {
        var result = FilenameSanitizer.Sanitize("..\\..\\secret");

        result.Should().NotContain("..");
    }

    [Fact]
    public void Sanitize_ExceedsMaxLength_TruncatesWithHashSuffix()
    {
        var longTitle = new string('A', 200);
        var result = FilenameSanitizer.Sanitize(longTitle);

        result.Length.Should().BeLessThanOrEqualTo(180);
        result.Should().MatchRegex(@"^A+_[0-9A-F]{8}$");
    }

    [Fact]
    public void Sanitize_WithinMaxLength_ReturnsUnchanged()
    {
        var title = "Normal Video Title";
        FilenameSanitizer.Sanitize(title).Should().Be(title);
    }
}
