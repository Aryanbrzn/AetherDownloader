namespace AetherDownloader.Core.Abstractions;

/// <summary>
/// Resolves or downloads third-party CLI tools at runtime (never bundled in source control).
/// </summary>
public interface IToolBootstrapper
{
    ToolAvailability CheckAvailability();

    /// <summary>
    /// Ensures yt-dlp is available; downloads the official Windows release when missing.
    /// </summary>
    Task<string> EnsureYtDlpAsync(
        IProgress<ToolDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures ffmpeg is available; downloads an official Windows essentials build when missing.
    /// </summary>
    Task<string> EnsureFfmpegAsync(
        IProgress<ToolDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed record ToolAvailability(bool YtDlpAvailable, bool FfmpegAvailable, string? YtDlpPath, string? FfmpegPath);

/// <param name="Message">Status text for the UI.</param>
/// <param name="PercentComplete">0–100 when known; null means indeterminate.</param>
public sealed record ToolDownloadProgress(string Message, double? PercentComplete);
