namespace AetherDownloader.Core.Models;

public sealed record AppSettings
{
    public string DefaultOutputPath { get; init; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "AetherDownloader");

    public string? YtDlpPath { get; init; }
    public string? FfmpegPath { get; init; }
    public int MaxParallelDownloads { get; init; } = 2;
    public bool ClipboardMonitoring { get; init; } = true;
    public AppLanguage Language { get; init; } = AppLanguage.En;
    public AppTheme Theme { get; init; } = AppTheme.Dark;
    public Guid? ActiveCookieProfileId { get; init; }
}
