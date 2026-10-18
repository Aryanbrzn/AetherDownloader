using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Download;

public sealed class DownloadQueue
{
    private readonly SemaphoreSlim _semaphore;
    private readonly Func<DownloadRequest, IProgress<DownloadProgressEvent>?, CancellationToken, Task<DownloadResult>> _downloadFunc;

    public DownloadQueue(
        int maxParallel,
        Func<DownloadRequest, IProgress<DownloadProgressEvent>?, CancellationToken, Task<DownloadResult>> downloadFunc)
    {
        if (maxParallel is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(maxParallel), "Max parallel downloads must be between 1 and 4.");
        }

        _semaphore = new SemaphoreSlim(maxParallel, maxParallel);
        _downloadFunc = downloadFunc;
    }

    public async Task<DownloadResult> EnqueueAsync(
        DownloadRequest request,
        IProgress<DownloadProgressEvent>? progress,
        CancellationToken cancellationToken)
    {
        var item = new DownloadQueueItem
        {
            Request = request,
            Progress = progress,
        };

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            item.State = DownloadJobState.Downloading;
            var result = await _downloadFunc(request, progress, cancellationToken).ConfigureAwait(false);
            item.State = result.Success ? DownloadJobState.Completed : DownloadJobState.Failed;
            item.Completion.TrySetResult(result);
            return result;
        }
        catch (OperationCanceledException)
        {
            item.State = DownloadJobState.Cancelled;
            item.Completion.TrySetCanceled(cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            item.State = DownloadJobState.Failed;
            item.Completion.TrySetException(ex);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
