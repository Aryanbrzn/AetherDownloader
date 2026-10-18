namespace AetherDownloader.Core.Models;

public sealed record DownloadRequest
{
    public required Guid JobId { get; init; }
    public required string Url { get; init; }
    public required FormatSelection FormatSelection { get; init; }
    public required string OutputDirectory { get; init; }
    public required string FileNameTemplate { get; init; }
    public Guid? CookieProfileId { get; init; }
    public bool DownloadPlaylist { get; init; }
}
