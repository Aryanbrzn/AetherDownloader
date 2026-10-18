namespace AetherDownloader.Core.Cookies;

public sealed record CookieValidationResult
{
    public required bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public int CookieCount { get; init; }
    public required IReadOnlyList<string> Domains { get; init; }

    public static CookieValidationResult Success(int count, IReadOnlyList<string> domains) =>
        new() { IsValid = true, CookieCount = count, Domains = domains };

    public static CookieValidationResult Failure(string message) =>
        new() { IsValid = false, ErrorMessage = message, Domains = [] };
}
