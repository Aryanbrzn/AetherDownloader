using System.Globalization;
using System.Windows;
using System.Windows.Data;
using AetherDownloader.App.ViewModels;
using AetherDownloader.Core.Models;

namespace AetherDownloader.App.Converters;

public sealed class MetadataStateToVisibilityConverter : IValueConverter
{
    public MetadataCardState TargetState { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is MetadataCardState state && state == TargetState
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class DownloadStateToVisibilityConverter : IValueConverter
{
    public DownloadJobState TargetState { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DownloadJobState state && state == TargetState
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visible = value is not null;
        if (Invert)
        {
            visible = !visible;
        }

        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
