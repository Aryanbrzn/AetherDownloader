namespace AetherDownloader.Core.Models;

public sealed record DownloadResult
{
    public required bool Success { get; init; }
    public string? OutputFilePath { get; init; }
    public required int ExitCode { get; init; }
    public TimeSpan Duration { get; init; }
    public UserFacingError? Error { get; init; }
}
