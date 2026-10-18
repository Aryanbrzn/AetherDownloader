using AetherDownloader.Core.Constants;
using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Download;

public sealed class ProgressParser
{
    private string? _lastOutputPath;

    public string? LastOutputPath => _lastOutputPath;

    public DownloadProgressEvent? ParseLine(Guid jobId, string line)
    {
        if (ProgressPatterns.Destination.IsMatch(line))
        {
            var destinationMatch = ProgressPatterns.Destination.Match(line);
            _lastOutputPath = destinationMatch.Groups[1].Value.Trim().Trim('"');
        }

        if (ProgressPatterns.Merger.IsMatch(line))
        {
            return new DownloadProgressEvent
            {
                JobId = jobId,
                Phase = DownloadPhase.Merging,
                Message = line.Trim(),
            };
        }

        if (ProgressPatterns.ExtractAudio.IsMatch(line))
        {
            return new DownloadProgressEvent
            {
                JobId = jobId,
                Phase = DownloadPhase.Extracting,
                Message = line.Trim(),
            };
        }

        if (!ProgressPatterns.PercentComplete.IsMatch(line))
        {
            return null;
        }

        var percentMatch = ProgressPatterns.PercentComplete.Match(line);
        var percent = double.Parse(percentMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

        string? speed = null;
        if (ProgressPatterns.Speed.IsMatch(line))
        {
            speed = ProgressPatterns.Speed.Match(line).Groups[1].Value;
        }

        string? eta = null;
        if (ProgressPatterns.Eta.IsMatch(line))
        {
            eta = ProgressPatterns.Eta.Match(line).Groups[1].Value;
        }

        return new DownloadProgressEvent
        {
            JobId = jobId,
            PercentComplete = percent,
            Speed = speed,
            Eta = eta,
            Phase = DownloadPhase.Downloading,
            Message = line.Trim(),
        };
    }
}
