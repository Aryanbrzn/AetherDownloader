using AetherDownloader.Core.Inspection;
using AetherDownloader.Core.Models;
using FluentAssertions;

namespace AetherDownloader.Core.Tests.Inspection;

public sealed class FormatGrouperTests
{
    [Fact]
    public void Group_RawFormats_ContainsBestSyntheticEntry()
    {
        var raw = CreateSampleFormats();
        var grouped = FormatGrouper.Group(raw);

        grouped.Should().Contain(f =>
            f.FormatId == "best"
            && f.GroupLabel == FormatGrouper.GroupBest
            && f.Note == "Auto merge video+audio via FFmpeg");
    }

    [Fact]
    public void Group_SingleProgressiveFile_UsesOriginalFileBestNote()
    {
        var raw = new List<FormatOption>
        {
            new()
            {
                FormatId = "mp4",
                Extension = "mp4",
            },
        };

        var grouped = FormatGrouper.Group(raw);

        grouped.Should().Contain(f =>
            f.FormatId == "best"
            && f.Note == "Best available (original file)"
            && f.Extension == "mp4");
    }

    [Fact]
    public void Group_RawFormats_ContainsAudioGroup()
    {
        var raw = CreateSampleFormats();
        var grouped = FormatGrouper.Group(raw);

        grouped.Should().Contain(f =>
            f.FormatId == "140"
            && f.GroupLabel == FormatGrouper.GroupAudio);
    }

    [Fact]
    public void Group_RawFormats_ContainsVideoTiers()
    {
        var raw = CreateSampleFormats();
        var grouped = FormatGrouper.Group(raw);

        grouped.Should().Contain(f => f.FormatId == "137" && f.GroupLabel == "1080p");
        grouped.Should().Contain(f => f.FormatId == "136" && f.GroupLabel == "720p");
    }

    [Fact]
    public void Group_UnmatchedFormat_GoesToAdvanced()
    {
        var raw = new List<FormatOption>
        {
            new()
            {
                FormatId = "999",
                Extension = "webm",
                Resolution = "640x350",
                VideoCodec = "vp9",
                AudioCodec = "none",
            },
        };

        var grouped = FormatGrouper.Group(raw);

        grouped.Should().Contain(f =>
            f.FormatId == "999"
            && f.GroupLabel == FormatGrouper.GroupAdvanced);
    }

    private static List<FormatOption> CreateSampleFormats() =>
    [
        new()
        {
            FormatId = "137",
            Extension = "mp4",
            Resolution = "1920x1080",
            VideoCodec = "avc1",
            AudioCodec = "none",
        },
        new()
        {
            FormatId = "136",
            Extension = "mp4",
            Resolution = "1280x720",
            VideoCodec = "avc1",
            AudioCodec = "none",
        },
        new()
        {
            FormatId = "140",
            Extension = "m4a",
            Resolution = "audio only",
            VideoCodec = "none",
            AudioCodec = "mp4a.40.2",
        },
    ];
}
