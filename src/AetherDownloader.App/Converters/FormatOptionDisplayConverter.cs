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

        var resolution = string.IsNullOrWhiteSpace(format.Resolution) ? format.Note ?? format.GroupLabel ?? "—" : format.Resolution;
        var size = format.FileSizeApproxBytes.HasValue
            ? FormatBytes(format.FileSizeApproxBytes.Value)
            : "—";

        return $"{resolution} · {format.Extension} · ~{size}";
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
