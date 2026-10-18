namespace AetherDownloader.Core.Models;

public sealed record FormatOption
{
    public required string FormatId { get; init; }
    public required string Extension { get; init; }
    public string? Resolution { get; init; }
    public int? Fps { get; init; }
    public string? VideoCodec { get; init; }
    public string? AudioCodec { get; init; }
    public long? FileSizeApproxBytes { get; init; }
    public string? Note { get; init; }
    public string? GroupLabel { get; init; }
}
