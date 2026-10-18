using AetherDownloader.Core.Abstractions;
using AetherDownloader.Core.Models;
using AetherDownloader.Core.Process;
using Microsoft.Extensions.Logging;

namespace AetherDownloader.Core.Inspection;

public sealed class FormatDiscoveryService
{
    private readonly IProcessRunner _processRunner;
    private readonly IYtDlpLocator _ytDlpLocator;
    private readonly ICookieStore _cookieStore;
    private readonly ILogger<FormatDiscoveryService> _logger;

    public FormatDiscoveryService(
        IProcessRunner processRunner,
        IYtDlpLocator ytDlpLocator,
        ICookieStore cookieStore,
        ILogger<FormatDiscoveryService> logger)
    {
        _processRunner = processRunner;
        _ytDlpLocator = ytDlpLocator;
        _cookieStore = cookieStore;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FormatOption>> ListFormatsAsync(
        string url,
        InspectOptions opts,
        CancellationToken cancellationToken)
    {
        var validated = UrlValidator.Validate(url);
        var cookiePath = await CookiePathResolver.ResolveAsync(_cookieStore, opts.CookieProfileId, cancellationToken)
            .ConfigureAwait(false);

        var jsonResult = await RunInspectJsonAsync(
                validated.Normalized,
                cookiePath,
                opts.TimeoutSeconds,
                cancellationToken)
            .ConfigureAwait(false);

        var formats = YtDlpJsonParser.ParseFormats(jsonResult);
        if (formats.Count > 0)
        {
            return FormatGrouper.Group(formats);
        }

        _logger.LogDebug("JSON formats empty; falling back to --list-formats");
        var listResult = await RunListFormatsAsync(
                validated.Normalized,
                cookiePath,
                opts.TimeoutSeconds,
                cancellationToken)
            .ConfigureAwait(false);

        if (listResult.ExitCode != 0 && YtDlpCookieRetryPolicy.ShouldRetryWithoutCookies(listResult, cookiePath))
        {
            _logger.LogDebug("Format list failed with cookies; retrying without cookies");
            listResult = await RunListFormatsAsync(
                    validated.Normalized,
                    cookiePath: null,
                    opts.TimeoutSeconds,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (listResult.ExitCode != 0)
        {
            throw new InvalidOperationException(
                YtDlpOutputHelper.GetFailureMessage(listResult, "Format discovery failed."));
        }

        var parsed = ListFormatsOutputParser.Parse(listResult.StandardOutput);
        return FormatGrouper.Group(parsed);
    }

    private async Task<string> RunInspectJsonAsync(
        string url,
        string? cookiePath,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var result = await RunInspectJsonProcessAsync(url, cookiePath, timeoutSeconds, cancellationToken)
            .ConfigureAwait(false);

        if (result.ExitCode != 0 && YtDlpCookieRetryPolicy.ShouldRetryWithoutCookies(result, cookiePath))
        {
            _logger.LogDebug("Format discovery JSON failed with cookies; retrying without cookies");
            result = await RunInspectJsonProcessAsync(url, cookiePath: null, timeoutSeconds, cancellationToken)
                .ConfigureAwait(false);
        }

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                YtDlpOutputHelper.GetFailureMessage(result, "Format discovery failed."));
        }

        return YtDlpOutputHelper.RequireUsableJsonPayload(result, "Format discovery failed.");
    }

    private async Task<ProcessRunResult> RunInspectJsonProcessAsync(
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
            _logger.LogDebug(ex, "Format discovery JSON process failed");
            throw;
        }
    }

    private async Task<ProcessRunResult> RunListFormatsAsync(
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
                    Arguments = YtDlpCommandBuilder.BuildListFormatsArgs(url, cookiePath),
                    TimeoutSeconds = timeoutSeconds,
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Format list process failed");
            throw;
        }
    }
}
