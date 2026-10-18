using AetherDownloader.Core.Inspection;
using FluentAssertions;

namespace AetherDownloader.Core.Tests.Inspection;

public sealed class YtDlpJsonParserTests
{
    private static string FixturePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);

    [Fact]
    public void ParseMetadata_VideoFixture_ReturnsExpectedFields()
    {
        var json = File.ReadAllText(FixturePath("ytdlp-video.json"));
        var metadata = YtDlpJsonParser.ParseMetadata(json, "https://youtube.com/watch?v=abc123xyz");

        metadata.Id.Should().Be("abc123xyz");
        metadata.Title.Should().Be("Sample Test Video");
        metadata.Uploader.Should().Be("Test Channel");
        metadata.DurationSeconds.Should().Be(245);
        metadata.IsLive.Should().BeFalse();
        metadata.IsPlaylist.Should().BeFalse();
        metadata.SourceUrl.Should().Be("https://youtube.com/watch?v=abc123xyz");
    }

    [Fact]
    public void ParseMetadata_PlaylistFixture_SetsIsPlaylistTrue()
    {
        var json = File.ReadAllText(FixturePath("ytdlp-playlist.json"));
        var metadata = YtDlpJsonParser.ParseMetadata(json, "https://youtube.com/playlist?list=PLtest");

        metadata.IsPlaylist.Should().BeTrue();
        metadata.Title.Should().Be("Sample Test Playlist");
    }

    [Fact]
    public void ParseMetadata_LiveStream_SetsIsLiveTrue()
    {
        var json = File.ReadAllText(FixturePath("ytdlp-video.json"))
            .Replace("\"is_live\": false", "\"is_live\": true", StringComparison.Ordinal);

        var metadata = YtDlpJsonParser.ParseMetadata(json, "https://youtube.com/live/abc");

        metadata.IsLive.Should().BeTrue();
    }

    [Fact]
    public void ParseFormats_StoryboardFractionalFps_ParsesSuccessfully()
    {
        const string json = """
            {
              "title": "Sample",
              "formats": [
                {
                  "format_id": "sb3",
                  "ext": "mhtml",
                  "resolution": "48x27",
                  "fps": 0.13831258644536654,
                  "vcodec": "none",
                  "acodec": "none",
                  "format_note": "storyboard"
                },
                {
                  "format_id": "137",
                  "ext": "mp4",
                  "resolution": "1920x1080",
                  "fps": 30,
                  "vcodec": "avc1",
                  "acodec": "none"
                }
              ]
            }
            """;

        var formats = YtDlpJsonParser.ParseFormats(json);

        formats.Should().HaveCount(2);
        formats.Should().Contain(f => f.FormatId == "sb3" && f.Fps == 0);
        formats.Should().Contain(f => f.FormatId == "137" && f.Fps == 30);
    }

    [Fact]
    public void ParseMetadata_IgnoresNestedFormatsWithFractionalFps()
    {
        const string json = """
            {
              "_type": "video",
              "id": "wb4kn7xDvQM",
              "title": "Sample Video",
              "uploader": "Channel",
              "duration": 723,
              "extractor": "youtube",
              "formats": [
                {
                  "format_id": "sb3",
                  "ext": "mhtml",
                  "fps": 0.13831258644536654
                }
              ]
            }
            """;

        var metadata = YtDlpJsonParser.ParseMetadata(json, "https://youtu.be/wb4kn7xDvQM");

        metadata.Id.Should().Be("wb4kn7xDvQM");
        metadata.Title.Should().Be("Sample Video");
        metadata.DurationSeconds.Should().Be(723);
    }

    [Fact]
    public void ParseFormats_VideoFixture_ReturnsMultipleFormats()
    {
        var json = File.ReadAllText(FixturePath("ytdlp-video.json"));
        var formats = YtDlpJsonParser.ParseFormats(json);

        formats.Should().HaveCountGreaterThanOrEqualTo(3);
        formats.Should().Contain(f => f.FormatId == "137" && f.Resolution == "1920x1080");
        formats.Should().Contain(f => f.FormatId == "140" && f.AudioCodec == "mp4a.40.2");
    }
}
