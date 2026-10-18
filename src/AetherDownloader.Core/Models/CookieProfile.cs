namespace AetherDownloader.Core.Models;

public sealed record CookieProfile
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required DateTimeOffset ImportedAt { get; init; }
    public required bool IsValid { get; init; }
    public required IReadOnlyList<string> DomainSummary { get; init; }
}
