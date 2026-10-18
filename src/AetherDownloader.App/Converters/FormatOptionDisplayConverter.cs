using System.Globalization;
using System.Windows.Data;
using AetherDownloader.Core.Models;

namespace AetherDownloader.App.Converters;

public sealed class FormatOptionDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not FormatOption format)
        {
            return string.Empty;
        }

        var label = !string.IsNullOrWhiteSpace(format.Resolution)
            ? format.Resolution
            : !string.IsNullOrWhiteSpace(format.Note)
                ? format.Note
                : format.GroupLabel ?? "—";

        var parts = new List<string> { label };

        if (!string.IsNullOrWhiteSpace(format.Extension) && format.Extension is not "unknown")
        {
            parts.Add(format.Extension);
        }

        if (format.FileSizeApproxBytes is long bytes and > 0)
        {
            parts.Add($"~{FormatBytes(bytes)}");
        }

        return string.Join(" · ", parts);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static string FormatBytes(long bytes)
    {
        const double kb = 1024;
        const double mb = kb * 1024;
        const double gb = mb * 1024;

        if (bytes >= gb)
        {
            return $"{bytes / gb:0.#} GB";
        }

        if (bytes >= mb)
        {
            return $"{bytes / mb:0.#} MB";
        }

        if (bytes >= kb)
        {
            return $"{bytes / kb:0.#} KB";
        }

        return $"{bytes} B";
    }
}
