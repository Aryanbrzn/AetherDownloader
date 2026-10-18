namespace AetherDownloader.Core.Process;

public sealed record ProcessRunResult
{
    public required int ExitCode { get; init; }
    public required IReadOnlyList<string> StandardOutput { get; init; }
    public required IReadOnlyList<string> StandardError { get; init; }
    public TimeSpan Duration { get; init; }
    public bool WasCancelled { get; init; }
}
