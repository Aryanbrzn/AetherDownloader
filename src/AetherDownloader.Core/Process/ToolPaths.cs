namespace AetherDownloader.Core.Process;

/// <summary>
/// Well-known locations for optional local tool installs managed by the bootstrapper.
/// Binaries are never committed to source control.
/// </summary>
public static class ToolPaths
{
    public const string YtDlpFileName = "yt-dlp.exe";
    public const string FfmpegFileName = "ffmpeg.exe";
    public const string FfprobeFileName = "ffprobe.exe";

    public static string ManagedToolsRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AetherDownloader",
        "tools");

    public static string ManagedYtDlpPath { get; } =
        Path.Combine(ManagedToolsRoot, "yt-dlp", YtDlpFileName);

    public static string ManagedFfmpegPath { get; } =
        Path.Combine(ManagedToolsRoot, "ffmpeg", FfmpegFileName);

    public static string ManagedFfprobePath { get; } =
        Path.Combine(ManagedToolsRoot, "ffmpeg", FfprobeFileName);

    public static string BundledYtDlpPath { get; } =
        Path.Combine(AppContext.BaseDirectory, "tools", "yt-dlp", YtDlpFileName);
}
