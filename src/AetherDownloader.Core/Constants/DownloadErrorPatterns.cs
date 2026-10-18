using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Constants;

public static class DownloadErrorPatterns
{
    public static readonly (ErrorCode Code, string Pattern, RecoveryAction PrimaryAction)[] Rules =
    [
        (ErrorCode.DlCookie,
            @"sign.?in.*required|login.*required|use.*--cookies|authentication|confirm your age|empty media response",
            RecoveryAction.CookieGuide),
        (ErrorCode.DlFormat,
            @"requested format|format not available|no video formats|unsupported url",
            RecoveryAction.ReInspect),
        (ErrorCode.DlDisk,
            @"no space left|disk full|not enough space",
            RecoveryAction.ChangePath),
        (ErrorCode.DlFfmpeg,
            @"ffmpeg|ffprobe|executable.*not found.*ffmpeg",
            RecoveryAction.Settings),
        (ErrorCode.DlGeo,
            @"HTTP Error 403|403 Forbidden",
            RecoveryAction.Settings),
        (ErrorCode.DlNetwork,
            @"HTTP Error 429|too many requests",
            RecoveryAction.Retry),
        (ErrorCode.DlGeo,
            @"not available in your country|geo.?restrict|region|blocked in your",
            RecoveryAction.Settings),
        (ErrorCode.DlNetwork,
            @"unable to download|network|connection reset|timed out|timeout|name resolution|getaddrinfo",
            RecoveryAction.Retry),
    ];
}
