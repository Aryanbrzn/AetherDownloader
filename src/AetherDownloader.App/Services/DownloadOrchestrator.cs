using System.Windows;
using AetherDownloader.App.ViewModels;
using AetherDownloader.Core.Abstractions;
using AetherDownloader.Core.Download;
using AetherDownloader.Core.Models;
using AetherDownloader.Core.Settings;

namespace AetherDownloader.App.Services;

public sealed class DownloadOrchestrator
{
    private readonly IDownloadEngine _engine;
    private readonly AppSettingsHolder _settings;
    private readonly List<DownloadItemViewModel> _trackedItems = [];
    private DownloadQueue _queue;
    private int _activeDownloadCount;

    public DownloadOrchestrator(IDownloadEngine engine, AppSettingsHolder settings)
    {
        _engine = engine;
        _settings = settings;
        _queue = new DownloadQueue(_settings.Current.MaxParallelDownloads, DownloadCoreAsync);
    }

    public event EventHandler? ActiveCountChanged;

    public event EventHandler<DownloadItemViewModel>? ItemRemoved;

    public int ActiveDownloadCount
    {
        get => _activeDownloadCount;
        private set
        {
            if (_activeDownloadCount == value)
            {
                return;
            }

            _activeDownloadCount = value;
            ActiveCountChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void RebuildQueue()
    {
        _queue = new DownloadQueue(_settings.Current.MaxParallelDownloads, DownloadCoreAsync);
    }

    public void RegisterItem(DownloadItemViewModel item)
    {
        if (!_trackedItems.Contains(item))
        {
            _trackedItems.Add(item);
        }
    }

    public void RemoveItem(DownloadItemViewModel item)
    {
        if (_trackedItems.Remove(item))
        {
            ItemRemoved?.Invoke(this, item);
        }
    }

    public async Task StartDownloadAsync(
        DownloadItemViewModel item,
        FormatSelection format,
        bool downloadPlaylist)
    {
        RegisterItem(item);

        var request = new DownloadRequest
        {
            JobId = item.JobId,
            Url = item.Url,
            FormatSelection = format,
            OutputDirectory = _settings.Current.DefaultOutputPath,
            FileNameTemplate = item.Title,
            CookieProfileId = _settings.Current.ActiveCookieProfileId,
            DownloadPlaylist = downloadPlaylist,
        };

        var downloadStarted = false;
        var progress = new Progress<DownloadProgressEvent>(e =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!downloadStarted)
                {
                    downloadStarted = true;
                    item.Status = DownloadJobState.Downloading;
                    item.Phase = DownloadPhase.Downloading;
                    ActiveDownloadCount++;
                }

                ApplyProgress(item, e);
            });
        });

        try
        {
            var result = await _queue
                .EnqueueAsync(request, progress, item.Cancellation.Token)
                .ConfigureAwait(false);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (!downloadStarted)
                {
                    downloadStarted = true;
                    ActiveDownloadCount++;
                }

                ApplyResult(item, result);
            });
        }
        catch (OperationCanceledException)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                item.Status = DownloadJobState.Cancelled;
            });
        }
        finally
        {
            if (downloadStarted)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ActiveDownloadCount = Math.Max(0, ActiveDownloadCount - 1);
                });
            }
        }
    }

    public async Task WaitForDrainAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (ActiveDownloadCount > 0 && DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(200, cancellationToken).ConfigureAwait(false);
        }
    }

    public void CancelAll()
    {
        foreach (var item in _trackedItems.Where(i => i.Status == DownloadJobState.Downloading))
        {
            item.Cancellation.Cancel();
        }
    }

    private Task<DownloadResult> DownloadCoreAsync(
        DownloadRequest request,
        IProgress<DownloadProgressEvent>? progress,
        CancellationToken cancellationToken) =>
        _engine.DownloadAsync(request, progress, cancellationToken);

    private static void ApplyProgress(DownloadItemViewModel item, DownloadProgressEvent e)
    {
        if (e.PercentComplete.HasValue)
        {
            item.Percent = e.PercentComplete.Value;
        }

        item.Speed = e.Speed;
        item.Eta = e.Eta;
        item.Phase = e.Phase;

        if (e.BytesDownloaded.HasValue && e.TotalBytes.HasValue)
        {
            item.SizeProgress = $"{FormatBytes(e.BytesDownloaded.Value)} / {FormatBytes(e.TotalBytes.Value)}";
        }
        else if (e.BytesDownloaded.HasValue)
        {
            item.SizeProgress = FormatBytes(e.BytesDownloaded.Value);
        }
    }

    private static void ApplyResult(DownloadItemViewModel item, DownloadResult result)
    {
        item.OutputFilePath = result.OutputFilePath;

        if (result.Success)
        {
            item.Status = DownloadJobState.Completed;
            item.Percent = 100;
            item.Phase = DownloadPhase.Unknown;
            return;
        }

        if (item.Cancellation.IsCancellationRequested)
        {
            item.Status = DownloadJobState.Cancelled;
            return;
        }

        item.Status = DownloadJobState.Failed;
        item.UserFacingError = result.Error;
    }

    private static string FormatBytes(long bytes)
    {
        const double mb = 1024 * 1024;
        const double gb = mb * 1024;

        if (bytes >= gb)
        {
            return $"{bytes / gb:0.#} GB";
        }

        return $"{bytes / mb:0.#} MB";
    }
}
