using System.Text.RegularExpressions;

namespace AetherDownloader.Core.Constants;

public static partial class ProgressPatterns
{
    public static readonly Regex PercentComplete = PercentCompleteRegex();
    public static readonly Regex Speed = SpeedRegex();
    public static readonly Regex Eta = EtaRegex();
    public static readonly Regex TotalSize = TotalSizeRegex();
    public static readonly Regex Destination = DestinationRegex();
    public static readonly Regex Merger = MergerRegex();
    public static readonly Regex ExtractAudio = ExtractAudioRegex();

    [GeneratedRegex(@"\[download\]\s+([\d.]+)%", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex PercentCompleteRegex();

    [GeneratedRegex(@"at\s+([\d.]+\S+/s)", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex SpeedRegex();

    [GeneratedRegex(@"ETA\s+([\d:]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex EtaRegex();

    [GeneratedRegex(@"of\s+([\d.]+\ \w+)", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex TotalSizeRegex();

    [GeneratedRegex(@"\[download\] Destination:\s*(.+)", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex DestinationRegex();

    [GeneratedRegex(@"\[Merger\]\s+Merging formats", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex MergerRegex();

    [GeneratedRegex(@"\[ExtractAudio\]", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex ExtractAudioRegex();
}
