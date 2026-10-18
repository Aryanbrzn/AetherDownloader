using System.Diagnostics;
using System.Text;
using AetherDownloader.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace AetherDownloader.Core.Process;

public sealed class YtDlpProcessRunner : IProcessRunner
{
    private readonly ILogger<YtDlpProcessRunner> _logger;

    public YtDlpProcessRunner(ILogger<YtDlpProcessRunner> logger) => _logger = logger;

    public async Task<ProcessRunResult> RunAsync(
        ProcessRunOptions options,
        CancellationToken cancellationToken,
        IProgress<ProcessOutputLine>? outputProgress = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var stdout = new List<string>();
        var stderrRing = new StderrRingBuffer();
        var wasCancelled = false;

        using var linkedCts = CreateLinkedCancellation(options, cancellationToken);
        var effectiveToken = linkedCts.Token;

        using var process = new System.Diagnostics.Process { StartInfo = BuildStartInfo(options) };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Failed to start process: {options.ExecutablePath}");
            }

            _logger.LogDebug("Started process {Executable}", Path.GetFileName(options.ExecutablePath));

            using var registration = effectiveToken.Register(() =>
            {
                wasCancelled = true;
                TryKillProcessTree(process);
            });

            var stdoutTask = ReadStreamAsync(
                process.StandardOutput,
                isError: false,
                stdout,
                stderrRing,
                outputProgress,
                effectiveToken);
            var stderrTask = ReadStreamAsync(
                process.StandardError,
                isError: true,
                stdout,
                stderrRing,
                outputProgress,
                effectiveToken);

            await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
            await process.WaitForExitAsync(effectiveToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
            TryKillProcessTree(process);
            await WaitForExitSafeAsync(process).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Process execution failed for {Executable}", Path.GetFileName(options.ExecutablePath));
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }

        return new ProcessRunResult
        {
            ExitCode = process.HasExited ? process.ExitCode : -1,
            StandardOutput = stdout,
            StandardError = stderrRing.Snapshot(),
            Duration = stopwatch.Elapsed,
            WasCancelled = wasCancelled,
        };
    }

    private static CancellationTokenSource CreateLinkedCancellation(
        ProcessRunOptions options,
        CancellationToken cancellationToken)
    {
        if (options.TimeoutSeconds is int timeoutSeconds && timeoutSeconds > 0)
        {
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        }

        return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    private static ProcessStartInfo BuildStartInfo(ProcessRunOptions options)
    {
        var arguments = string.Join(' ', options.Arguments.Select(QuoteArgument));

        return new ProcessStartInfo
        {
            FileName = options.ExecutablePath,
            Arguments = arguments,
            WorkingDirectory = options.WorkingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
    }

    private static string QuoteArgument(string argument)
    {
        if (argument.Length == 0)
        {
            return "\"\"";
        }

        if (!argument.Contains(' ') && !argument.Contains('"'))
        {
            return argument;
        }

        return $"\"{argument.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }

    private static async Task ReadStreamAsync(
        StreamReader reader,
        bool isError,
        List<string> stdout,
        StderrRingBuffer stderrRing,
        IProgress<ProcessOutputLine>? outputProgress,
        CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                if (isError)
                {
                    stderrRing.Add(line);
                }
                else
                {
                    stdout.Add(line);
                }

                outputProgress?.Report(new ProcessOutputLine(isError, line, DateTimeOffset.UtcNow));
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation kills the process and closes streams.
        }
    }

    private void TryKillProcessTree(System.Diagnostics.Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to kill process tree");
        }
    }

    private static async Task WaitForExitSafeAsync(System.Diagnostics.Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Process did not exit within grace period after kill.
        }
    }
}
