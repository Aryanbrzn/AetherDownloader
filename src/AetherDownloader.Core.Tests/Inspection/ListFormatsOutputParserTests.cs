using AetherDownloader.Core.Inspection;
using FluentAssertions;

namespace AetherDownloader.Core.Tests.Inspection;

public sealed class ListFormatsOutputParserTests
{
    private static readonly string[] SampleOutput =
    [
        "[info] Available formats for abc123:",
        "ID  EXT   RESOLUTION FPS CH | FILESIZE  TBR PROTO | VCODEC VBR ACODEC ABR ASR MORE INFO",
        "----------------------------------------------------------------------------------------",
        "139 m4a   audio only      2 |  1.23MiB  49k https | audio only       mp4a.40.5 49k 22k low, m4a_dash",
        "140 m4a   audio only      2 |  2.45MiB  129k https | audio only       mp4a.40.2 129k 44k medium, m4a_dash",
        "18  mp4   640x360     30  2 |  5.67MiB  300k https | avc1.42001E 300k video only          360p",
        "22  mp4   1280x720    30  2 | 15.00MiB  800k https | avc1.64001F 800k video only          720p",
    ];

    [Fact]
    public void Parse_TabularOutput_ReturnsVideoAndAudioFormats()
    {
        var formats = ListFormatsOutputParser.Parse(SampleOutput);

        formats.Should().HaveCount(4);
        formats.Should().Contain(f =>
            f.FormatId == "18"
            && f.Resolution != null
            && f.Resolution.Contains("640x360")
            && f.Extension == "mp4");
        formats.Should().Contain(f => f.FormatId == "140" && f.Extension == "m4a");
        formats.Select(f => f.FormatId).Should().NotContain(["audio", "video", "storyboard"]);
    }

    [Fact]
    public void Parse_LineWithNote_ExtractsNoteAfterPipe()
    {
        var lines = new[]
        {
            "ID  EXT   RESOLUTION FPS CH | FILESIZE  TBR PROTO | VCODEC VBR ACODEC ABR ASR MORE INFO",
            "22  mp4   1280x720    30  2 | 15.00MiB  800k https | avc1.64001F 800k video only          720p",
        };

        var formats = ListFormatsOutputParser.Parse(lines);

        formats.Should().ContainSingle();
        formats[0].Note.Should().Contain("720p");
    }

    [Fact]
    public void Parse_BeforeHeader_ReturnsEmpty()
    {
        var lines = new[] { "loading...", "please wait" };

        ListFormatsOutputParser.Parse(lines).Should().BeEmpty();
    }
}
