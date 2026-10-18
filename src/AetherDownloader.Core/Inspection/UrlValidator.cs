using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Inspection;

public static class UrlValidator
{
    private const int MaxLength = 2048;

    public static ValidatedUrl Validate(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new UrlValidationException("URL is required.");
        }

        var trimmed = url.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new UrlValidationException("URL exceeds maximum length.");
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            throw new UrlValidationException("Invalid URL format.");
        }

        if (uri.Scheme is not "http" and not "https")
        {
            throw new UrlValidationException("Only HTTP and HTTPS URLs are supported.");
        }

        return new ValidatedUrl { Original = url, Normalized = uri.ToString() };
    }
}
