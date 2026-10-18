using AetherDownloader.Core.Abstractions;
using AetherDownloader.Core.Models;
using AetherDownloader.Core.Process;
using Microsoft.Extensions.Logging;

namespace AetherDownloader.Core.Inspection;

public sealed class UrlInspector
{
    private readonly IProcessRunner _processRunner;
    private readonly IYtDlpLocator _ytDlpLocator;
    private readonly ICookieStore _cookieStore;
    private readonly ILogger<UrlInspector> _logger;

    public UrlInspector(
        IProcessRunner processRunner,
        IYtDlpLocator ytDlpLocator,
        ICookieStore cookieStore,
        ILogger<UrlInspector> logger)
    {
        _processRunner = processRunner;
        _ytDlpLocator = ytDlpLocator;
        _cookieStore = cookieStore;
        _logger = logger;
    }

    public async Task<MediaMetadata> InspectAsync(
        string url,
        InspectOptions opts,
        CancellationToken cancellationToken)
    {
        var validated = UrlValidator.Validate(url);
        var cookiePath = await CookiePathResolver.ResolveAsync(_cookieStore, opts.CookieProfileId, cancellationToken)
            .ConfigureAwait(false);

        var result = await RunInspectProcessAsync(validated.Normalized, cookiePath, opts.TimeoutSeconds, cancellationToken)
            .ConfigureAwait(false);

        if (result.ExitCode != 0 && YtDlpCookieRetryPolicy.ShouldRetryWithoutCookies(result, cookiePath))
        {
            _logger.LogDebug("Metadata extraction failed with cookies; retrying without cookies");
            result = await RunInspectProcessAsync(validated.Normalized, cookiePath: null, opts.TimeoutSeconds, cancellationToken)
                .ConfigureAwait(false);
        }

        if (result.ExitCode != 0)
        {
            _logger.LogDebug(
                "Metadata extraction exited with code {ExitCode}",
                result.ExitCode);
            throw new InvalidOperationException(
                YtDlpOutputHelper.GetFailureMessage(result, "Metadata extraction failed."));
        }

        try
        {
            return ParseMetadataFromResult(result, validated.Normalized);
        }
        catch (InvalidOperationException) when (YtDlpCookieRetryPolicy.ShouldRetryWithoutCookies(result, cookiePath))
        {
            _logger.LogDebug("Metadata JSON parse failed with cookies; retrying without cookies");
            result = await RunInspectProcessAsync(validated.Normalized, cookiePath: null, opts.TimeoutSeconds, cancellationToken)
                .ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    YtDlpOutputHelper.GetFailureMessage(result, "Metadata extraction failed."));
            }

            return ParseMetadataFromResult(result, validated.Normalized);
        }
    }

    private static MediaMetadata ParseMetadataFromResult(ProcessRunResult result, string sourceUrl)
    {
        var json = YtDlpOutputHelper.RequireUsableJsonPayload(result, "Metadata extraction failed.");
        return YtDlpJsonParser.ParseMetadata(json, sourceUrl);
    }

    private async Task<ProcessRunResult> RunInspectProcessAsync(
        string url,
        string? cookiePath,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _processRunner.RunAsync(
                new ProcessRunOptions
                {
                    ExecutablePath = _ytDlpLocator.ResolveExecutable(),
                    Arguments = YtDlpCommandBuilder.BuildInspectArgs(url, cookiePath),
                    TimeoutSeconds = timeoutSeconds,
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Metadata extraction process failed for URL");
            throw;
        }
    }
}
