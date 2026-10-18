using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Download;

internal sealed class DownloadQueueItem
{
    public required DownloadRequest Request { get; init; }
    public DownloadJobState State { get; set; } = DownloadJobState.Pending;
    public IProgress<DownloadProgressEvent>? Progress { get; init; }
    public TaskCompletionSource<DownloadResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
