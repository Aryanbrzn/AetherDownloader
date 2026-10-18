namespace AetherDownloader.Core.Models;

public enum DownloadJobState
{
    Pending,
    Inspecting,
    Downloading,
    Completed,
    Failed,
    Cancelled,
}
