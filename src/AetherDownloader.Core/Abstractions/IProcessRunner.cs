using AetherDownloader.Core.Process;

namespace AetherDownloader.Core.Abstractions;

/// <summary>
/// Executes external processes with async stdout/stderr streaming.
/// </summary>
public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(
        ProcessRunOptions options,
        CancellationToken cancellationToken,
        IProgress<ProcessOutputLine>? outputProgress = null);
}
