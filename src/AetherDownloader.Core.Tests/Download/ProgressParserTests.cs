using AetherDownloader.Core.Download;
using AetherDownloader.Core.Models;
using FluentAssertions;

namespace AetherDownloader.Core.Tests.Download;

public sealed class ProgressParserTests
{
    private static IEnumerable<string> LoadProgressLines()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "progress-lines.txt");
        return File.ReadAllLines(path).Where(line => !string.IsNullOrWhiteSpace(line));
    }

    [Fact]
    public void ParseLine_AllFixtureLines_RecognizesAtLeastTenProgressEvents()
    {
        var parser = new ProgressParser();
        var jobId = Guid.NewGuid();
        var events = new List<DownloadProgressEvent>();

        foreach (var line in LoadProgressLines())
        {
            var parsed = parser.ParseLine(jobId, line);
            if (parsed is not null)
            {
                events.Add(parsed);
            }
        }

        events.Should().HaveCountGreaterThanOrEqualTo(10);
        events.Should().Contain(e => e.Phase == DownloadPhase.Downloading && e.PercentComplete == 100.0);
        events.Should().Contain(e => e.Phase == DownloadPhase.Merging);
        events.Should().Contain(e => e.Phase == DownloadPhase.Extracting);
    }

    [Fact]
    public void ParseLine_DestinationLine_SetsLastOutputPath()
    {
        var parser = new ProgressParser();
        var jobId = Guid.NewGuid();

        parser.ParseLine(jobId, "[download] Destination: C:\\Downloads\\video.mp4");

        parser.LastOutputPath.Should().Be("C:\\Downloads\\video.mp4");
    }

    [Fact]
    public void ParseLine_ProgressLine_ParsesPercentSpeedAndEta()
    {
        var parser = new ProgressParser();
        var jobId = Guid.NewGuid();

        var result = parser.ParseLine(jobId, "[download]  50.0% of 100.00MiB at  3.80MiB/s ETA 00:13");

        result.Should().NotBeNull();
        result!.PercentComplete.Should().Be(50.0);
        result.Speed.Should().Be("3.80MiB/s");
        result.Eta.Should().Be("00:13");
        result.Phase.Should().Be(DownloadPhase.Downloading);
    }
}
