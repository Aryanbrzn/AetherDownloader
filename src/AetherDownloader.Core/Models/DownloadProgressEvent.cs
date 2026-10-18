namespace AetherDownloader.Core.Models;

public sealed record DownloadProgressEvent
{
    public required Guid JobId { get; init; }
    public double? PercentComplete { get; init; }
    public string? Speed { get; init; }
    public string? Eta { get; init; }
    public DownloadPhase Phase { get; init; } = DownloadPhase.Unknown;
    public long? BytesDownloaded { get; init; }
    public long? TotalBytes { get; init; }
    public string? Message { get; init; }
}
