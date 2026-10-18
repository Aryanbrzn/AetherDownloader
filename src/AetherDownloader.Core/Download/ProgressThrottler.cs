using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Download;

public sealed class ProgressThrottler
{
    private readonly IProgress<DownloadProgressEvent>? _inner;
    private readonly TimeSpan _interval = TimeSpan.FromMilliseconds(200);
    private DateTimeOffset _lastReport = DateTimeOffset.MinValue;
    private DownloadProgressEvent? _pending;

    public ProgressThrottler(IProgress<DownloadProgressEvent>? inner) => _inner = inner;

    public void Report(DownloadProgressEvent evt)
    {
        _pending = evt;
        var now = DateTimeOffset.UtcNow;
        if (now - _lastReport >= _interval)
        {
            _inner?.Report(_pending);
            _lastReport = now;
            _pending = null;
        }
    }

    public void Flush()
    {
        if (_pending is not null)
        {
            _inner?.Report(_pending);
            _pending = null;
            _lastReport = DateTimeOffset.UtcNow;
        }
    }
}
