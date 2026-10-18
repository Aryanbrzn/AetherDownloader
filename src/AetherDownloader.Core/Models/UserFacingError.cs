namespace AetherDownloader.Core.Models;

public sealed record UserFacingError
{
    public required ErrorCode Code { get; init; }
    public required string TitleFa { get; init; }
    public required string MessageFa { get; init; }
    public required string TitleEn { get; init; }
    public required string MessageEn { get; init; }
    public string? TechnicalDetail { get; init; }
    public required IReadOnlyList<RecoveryAction> RecoveryActions { get; init; }
}
