using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Constants;

public static class ErrorPatterns
{
    public static readonly (string Code, string Pattern, CookieHealthSeverity Severity, RecoveryAction Action)[] CookieHealth =
    [
        (CookieErrorPatternCodes.CookieExpiredSignIn,
            @"sign.?in.*required|login.*required|use.*--cookies",
            CookieHealthSeverity.High, RecoveryAction.CookieGuide),
        (CookieErrorPatternCodes.BotDetection,
            @"bot.*detect|confirm.*human|captcha|rate.?limit",
            CookieHealthSeverity.High, RecoveryAction.Retry),
        (CookieErrorPatternCodes.Http403,
            @"HTTP Error 403|403 Forbidden|access.*denied",
            CookieHealthSeverity.Medium, RecoveryAction.Settings),
        (CookieErrorPatternCodes.Http429,
            @"HTTP Error 429|too many requests",
            CookieHealthSeverity.Medium, RecoveryAction.Retry),
        (CookieErrorPatternCodes.PrivateContent,
            @"private video|members.?only|subscription",
            CookieHealthSeverity.Low, RecoveryAction.ReInspect),
    ];
}
