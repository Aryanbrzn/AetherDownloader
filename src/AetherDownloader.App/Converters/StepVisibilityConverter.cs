using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AetherDownloader.App.Converters;

public sealed class StepVisibilityConverter : IValueConverter
{
    public int TargetStep { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int currentStep)
        {
            return currentStep == TargetStep ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
