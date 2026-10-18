namespace AetherDownloader.Core.Models;

public sealed record FormatSelection
{
    public required FormatSelectionMode Mode { get; init; }
    public string? FormatId { get; init; }
    public int? TargetQualityHeight { get; init; }
}
