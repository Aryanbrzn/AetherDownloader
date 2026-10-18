using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AetherDownloader.App.Views;

namespace AetherDownloader.App.Services;

public sealed class ToastNotificationService
{
    private MainWindow? _mainWindow;
    private DispatcherTimer? _hideTimer;

    public void Attach(MainWindow mainWindow) => _mainWindow = mainWindow;

    public void ShowSuccess(string message) => Show(message, isError: false);

    public void ShowError(string title, string message) => Show($"{title}: {message}", isError: true);

    private void Show(string message, bool isError)
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Dispatcher.Invoke(() =>
        {
            var host = _mainWindow.ToastHost;
            host.Child = new TextBlock
            {
                Text = message,
                Foreground = isError
                    ? Application.Current.FindResource("Brush.Error") as Brush ?? Brushes.White
                    : Application.Current.FindResource("Brush.Success") as Brush ?? Brushes.White,
                TextWrapping = TextWrapping.Wrap,
            };
            host.Background = Application.Current.FindResource("Brush.SurfaceAlt") as Brush
                ?? new SolidColorBrush(Color.FromRgb(0x38, 0x38, 0x38));
            host.Visibility = Visibility.Visible;

            _hideTimer?.Stop();
            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _hideTimer.Tick += (_, _) =>
            {
                host.Visibility = Visibility.Collapsed;
                _hideTimer?.Stop();
            };
            _hideTimer.Start();
        });
    }
}
