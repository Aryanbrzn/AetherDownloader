using AetherDownloader.Core.Abstractions;
using AetherDownloader.Core.Constants;
using AetherDownloader.Core.Cookies;
using AetherDownloader.Core.Errors;
using AetherDownloader.Core.Inspection;
using AetherDownloader.Core.Models;
using AetherDownloader.Core.Process;
using Microsoft.Extensions.Logging;

namespace AetherDownloader.Core.Download;

public sealed class DownloadEngine : IDownloadEngine
{
    private const int MaxRetries = 2;

    private readonly UrlInspector _urlInspector;
    private readonly FormatDiscoveryService _formatDiscovery;
    private readonly IProcessRunner _processRunner;
    private readonly IYtDlpLocator _ytDlpLocator;
    private readonly IFfmpegLocator _ffmpegLocator;
    private readonly CookieStore _cookieStore;
    private readonly YtDlpErrorClassifier _errorClassifier;
    private readonly UserMessageTranslator _messageTranslator;
    private readonly ILogger<DownloadEngine> _logger;

    public DownloadEngine(
        UrlInspector urlInspector,
        FormatDiscoveryService formatDiscovery,
        IProcessRunner processRunner,
        IYtDlpLocator ytDlpLocator,
        IFfmpegLocator ffmpegLocator,
        CookieStore cookieStore,
        YtDlpErrorClassifier errorClassifier,
        UserMessageTranslator messageTranslator,
        ILogger<DownloadEngine> logger)
    {
        _urlInspector = urlInspector;
        _formatDiscovery = formatDiscovery;
        _processRunner = processRunner;
        _ytDlpLocator = ytDlpLocator;
        _ffmpegLocator = ffmpegLocator;
        _cookieStore = cookieStore;
        _errorClassifier = errorClassifier;
        _messageTranslator = messageTranslator;
        _logger = logger;
    }

    public Task<MediaMetadata> InspectAsync(string url, InspectOptions opts, CancellationToken cancellationToken) =>
        _urlInspector.InspectAsync(url, opts, cancellationToken);

    public Task<IReadOnlyList<FormatOption>> ListFormatsAsync(string url, InspectOptions opts, CancellationToken cancellationToken) =>
        _formatDiscovery.ListFormatsAsync(url, opts, cancellationToken);

    public async Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgressEvent>? progress,
        CancellationToken cancellationToken)
    {
        var validated = UrlValidator.Validate(request.Url);

        if (FormatArgumentBuilder.RequiresFfmpeg(request.FormatSelection)
            && !_ffmpegLocator.TryResolve(out _))
        {
            return new DownloadResult
            {
                Success = false,
                ExitCode = -1,
                Error = _messageTranslator.Translate(ErrorCode.DlFfmpeg, [RecoveryAction.Settings]),
            };
        }

        var cookiePath = await CookiePathResolver.ResolveAsync(
            _cookieStore,
            request.CookieProfileId,
            cancellationToken).ConfigureAwait(false);

        ProcessRunResult? lastResult = null;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            if (attempt > 0)
            {
                var delaySeconds = attempt == 1 ? 2 : 8;
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);
            }

            var parser = new ProgressParser();
            var throttler = new ProgressThrottler(progress);
            var cumulative = new DownloadProgressEvent { JobId = request.JobId };

            try
            {
                lastResult = await _processRunner.RunAsync(
                    new ProcessRunOptions
                    {
                        ExecutablePath = _ytDlpLocator.ResolveExecutable(),
                        Arguments = BuildDownloadArgs(request, validated.Normalized, cookiePath),
                        TimeoutSeconds = null,
                    },
                    cancellationToken,
                    new Progress<ProcessOutputLine>(line =>
                    {
                        _cookieStore.OnProcessOutputLine(line);
                        var parsed = parser.ParseLine(request.JobId, line.Text);
                        if (parsed is null)
                        {
                            return;
                        }

                        cumulative = MergeProgress(cumulative, parsed);
                        throttler.Report(cumulative);
                    })).ConfigureAwait(false);

                throttler.Flush();

                if (lastResult.WasCancelled)
                {
                    CancelManager.CleanupPartialFiles(request.OutputDirectory);
                    return new DownloadResult
                    {
                        Success = false,
                        ExitCode = lastResult.ExitCode,
                        Duration = lastResult.Duration,
                        Error = CreateCancelledError(),
                    };
                }

                if (lastResult.ExitCode == 0)
                {
                    return new DownloadResult
                    {
                        Success = true,
                        OutputFilePath = parser.LastOutputPath,
                        ExitCode = 0,
                        Duration = lastResult.Duration,
                    };
                }

                if (attempt < MaxRetries && IsTransientError(lastResult))
                {
                    _logger.LogDebug(
                        "Transient download error on attempt {Attempt}; retrying",
                        attempt + 1);
                    continue;
                }

                return new DownloadResult
                {
                    Success = false,
                    OutputFilePath = parser.LastOutputPath,
                    ExitCode = lastResult.ExitCode,
                    Duration = lastResult.Duration,
                    Error = _messageTranslator.FromProcessResult(lastResult, _errorClassifier),
                };
            }
            catch (OperationCanceledException)
            {
                CancelManager.CleanupPartialFiles(request.OutputDirectory);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Download process failed for job {JobId}", request.JobId);
                CancelManager.CleanupPartialFiles(request.OutputDirectory);
                return new DownloadResult
                {
                    Success = false,
                    ExitCode = -1,
                    Error = _messageTranslator.Translate(
                        ErrorCode.DlUnknown,
                        [RecoveryAction.Retry, RecoveryAction.CopyLog],
                        ex.Message),
                };
            }
        }

        return new DownloadResult
        {
            Success = false,
            ExitCode = lastResult?.ExitCode ?? -1,
            Duration = lastResult?.Duration ?? TimeSpan.Zero,
            Error = lastResult is null
                ? _messageTranslator.Translate(ErrorCode.DlUnknown, [RecoveryAction.Retry, RecoveryAction.CopyLog])
                : _messageTranslator.FromProcessResult(lastResult, _errorClassifier),
        };
    }

    private static IReadOnlyList<string> BuildDownloadArgs(
        DownloadRequest request,
        string url,
        string? cookiePath)
    {
        var args = new List<string>();
        args.AddRange(SplitTemplate(YtDlpArgumentTemplates.NewlineProgress));
        args.Add(request.DownloadPlaylist ? "--yes-playlist" : "--no-playlist");

        if (!string.IsNullOrEmpty(cookiePath))
        {
            args.AddRange(["--cookies", cookiePath]);
        }

        var sanitizedName = FilenameSanitizer.Sanitize(request.FileNameTemplate);
        var outputTemplate = Path.Combine(request.OutputDirectory, $"{sanitizedName}.%(ext)s");
        args.AddRange(["-o", outputTemplate]);
        args.AddRange(FormatArgumentBuilder.BuildArgs(request.FormatSelection));
        args.Add(url);
        return args;
    }

    private static IEnumerable<string> SplitTemplate(string template) =>
        template.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private static DownloadProgressEvent MergeProgress(DownloadProgressEvent current, DownloadProgressEvent update) =>
        current with
        {
            PercentComplete = update.PercentComplete ?? current.PercentComplete,
            Speed = update.Speed ?? current.Speed,
            Eta = update.Eta ?? current.Eta,
            Phase = update.Phase != DownloadPhase.Unknown ? update.Phase : current.Phase,
            BytesDownloaded = update.BytesDownloaded ?? current.BytesDownloaded,
            TotalBytes = update.TotalBytes ?? current.TotalBytes,
            Message = update.Message ?? current.Message,
        };

    private static bool IsTransientError(ProcessRunResult result)
    {
        var combined = string.Join('\n', result.StandardError);
        return combined.Contains("429", StringComparison.Ordinal)
            || combined.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("network", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Connection reset", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Unable to download", StringComparison.OrdinalIgnoreCase);
    }

    private static UserFacingError CreateCancelledError() =>
        new()
        {
            Code = ErrorCode.DlUnknown,
            TitleFa = "دانلود لغو شد",
            MessageFa = "دانلود توسط کاربر متوقف شد.",
            TitleEn = "Download cancelled",
            MessageEn = "Download was cancelled.",
            RecoveryActions = [RecoveryAction.Retry],
        };

}
