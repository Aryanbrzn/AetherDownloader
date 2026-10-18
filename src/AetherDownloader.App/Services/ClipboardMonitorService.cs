using System.Windows.Threading;
using AetherDownloader.Core.Inspection;
using AetherDownloader.Core.Settings;

namespace AetherDownloader.App.Services;

public sealed class ClipboardMonitorService : IDisposable
{
    private readonly DispatcherTimer _timer;
    private string _lastClipboard = string.Empty;

    public event EventHandler<string>? UrlDetected;

    public ClipboardMonitorService(AppSettingsHolder settings)
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += OnTick;

        if (settings.Current.ClipboardMonitoring)
        {
            _timer.Start();
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        try
        {
            if (!System.Windows.Clipboard.ContainsText())
            {
                return;
            }

            var text = System.Windows.Clipboard.GetText()?.Trim();
            if (string.IsNullOrWhiteSpace(text) || text == _lastClipboard)
            {
                return;
            }

            try
            {
                UrlValidator.Validate(text);
            }
            catch (UrlValidationException)
            {
                return;
            }

            _lastClipboard = text;
            UrlDetected?.Invoke(this, text);
        }
        catch
        {
            // Clipboard locked or unavailable — skip silently.
        }
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            _timer.Start();
        }
        else
        {
            _timer.Stop();
        }
    }

    public void Dispose() => _timer.Stop();
}
