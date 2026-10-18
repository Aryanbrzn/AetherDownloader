using AetherDownloader.Core.Process;

namespace AetherDownloader.Core.Inspection;

internal static class YtDlpCookieRetryPolicy
{
    private static readonly string[] AuthRequiredPatterns =
    [
        "use --cookies",
        "empty media response",
        "login",
        "sign in",
        "sign-in",
        "authentication",
        "confirm your age",
        "private video",
        "members-only",
        "HTTP Error 401",
        "HTTP Error 403",
    ];

    public static bool ShouldRetryWithoutCookies(ProcessRunResult result, string? cookiePath)
    {
        if (string.IsNullOrEmpty(cookiePath))
        {
            return false;
        }

        var combined = string.Join('\n', result.StandardError);
        if (result.StandardOutput.Count > 0)
        {
            combined += '\n' + string.Join('\n', result.StandardOutput);
        }

        foreach (var pattern in AuthRequiredPatterns)
        {
            if (combined.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
