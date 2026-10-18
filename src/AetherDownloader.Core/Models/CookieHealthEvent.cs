namespace AetherDownloader.Core.Models;

public sealed record CookieHealthEvent
{
    public required CookieHealthSeverity Severity { get; init; }
    public required string MatchedPattern { get; init; }
    public required string RawLine { get; init; }
    public RecoveryAction SuggestedAction { get; init; }
}
