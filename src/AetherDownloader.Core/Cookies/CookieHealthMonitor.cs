using System.Text.RegularExpressions;
using AetherDownloader.Core.Constants;
using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Cookies;

public sealed class CookieHealthMonitor
{
    public CookieHealthEvent? AnalyzeLine(string line)
    {
        foreach (var (code, pattern, severity, action) in ErrorPatterns.CookieHealth)
        {
            if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                return new CookieHealthEvent
                {
                    Severity = severity,
                    MatchedPattern = code,
                    RawLine = line,
                    SuggestedAction = action,
                };
            }
        }

        return null;
    }
}
