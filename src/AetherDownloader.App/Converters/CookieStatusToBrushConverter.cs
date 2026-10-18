using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AetherDownloader.Core.Models;

namespace AetherDownloader.App.Converters;

public sealed class CookieStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CookieStatus status)
        {
            return Brushes.Gray;
        }

        return status switch
        {
            CookieStatus.Valid => Application.Current.FindResource("Brush.Success") as Brush ?? Brushes.LimeGreen,
            CookieStatus.Missing or CookieStatus.Warning => Application.Current.FindResource("Brush.Warning") as Brush ?? Brushes.Gold,
            CookieStatus.Expired => Application.Current.FindResource("Brush.Error") as Brush ?? Brushes.Red,
            _ => Brushes.Gray,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
