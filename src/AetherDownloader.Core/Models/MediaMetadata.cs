namespace AetherDownloader.Core.Models;

public sealed record MediaMetadata
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Uploader { get; init; }
    public int? DurationSeconds { get; init; }
    public required string ThumbnailUrl { get; init; }
    public string? Description { get; init; }
    public required string Extractor { get; init; }
    public bool IsLive { get; init; }
    public bool IsPlaylist { get; init; }
    public required string SourceUrl { get; init; }
}
