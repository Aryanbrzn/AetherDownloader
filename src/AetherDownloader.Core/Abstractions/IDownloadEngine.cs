using AetherDownloader.Core.Models;
using AetherDownloader.Core.Process;

namespace AetherDownloader.Core.Abstractions;

/// <summary>
/// Primary download engine contract wrapping yt-dlp operations.
/// </summary>
public interface IDownloadEngine
{
    Task<MediaMetadata> InspectAsync(string url, InspectOptions opts, CancellationToken cancellationToken);

    Task<IReadOnlyList<FormatOption>> ListFormatsAsync(string url, InspectOptions opts, CancellationToken cancellationToken);

    Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgressEvent>? progress,
        CancellationToken cancellationToken);
}
