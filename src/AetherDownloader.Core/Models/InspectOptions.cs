namespace AetherDownloader.Core.Models;

public sealed record InspectOptions
{
    public Guid? CookieProfileId { get; init; }
    public int TimeoutSeconds { get; init; } = 60;
}
