namespace AetherDownloader.Core.Models;

public sealed record ValidatedUrl
{
    public required string Original { get; init; }
    public required string Normalized { get; init; }
}
