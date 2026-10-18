using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Inspection;

public static class FormatGrouper
{
    public const string GroupBest = "Best";
    public const string GroupAudio = "Audio Only";
    public const string GroupAdvanced = "Advanced";

    private static readonly (int Height, string Label)[] VideoTierHeights =
    [
        (2160, "4K"),
        (1440, "1440p"),
        (1080, "1080p"),
        (720, "720p"),
        (480, "480p"),
        (360, "360p"),
        (240, "240p"),
        (144, "144p"),
    ];

    public static IReadOnlyList<FormatOption> Group(IReadOnlyList<FormatOption> rawFormats)
    {
        var result = new List<FormatOption>
        {
            new()
            {
                FormatId = "best",
                Extension = "mp4",
                Note = "Auto merge video+audio via FFmpeg",
                GroupLabel = GroupBest,
            },
        };

        var assignedIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var format in rawFormats.Where(IsAudioOnly))
        {
            result.Add(format with { GroupLabel = GroupAudio });
            assignedIds.Add(format.FormatId);
        }

        foreach (var (height, label) in VideoTierHeights)
        {
            var tierFormats = rawFormats
                .Where(f => !assignedIds.Contains(f.FormatId))
                .Where(f => TryGetHeight(f) == height)
                .ToList();

            foreach (var format in tierFormats)
            {
                result.Add(format with { GroupLabel = label });
                assignedIds.Add(format.FormatId);
            }
        }

        foreach (var format in rawFormats.Where(f => !assignedIds.Contains(f.FormatId)))
        {
            result.Add(format with { GroupLabel = GroupAdvanced });
        }

        return result;
    }

    internal static bool IsAudioOnly(FormatOption format) =>
        !string.IsNullOrEmpty(format.AudioCodec)
        && format.AudioCodec != "none"
        && (string.IsNullOrEmpty(format.VideoCodec) || format.VideoCodec == "none");

    public static int? TryGetHeight(FormatOption format)
    {
        if (string.IsNullOrWhiteSpace(format.Resolution))
        {
            return null;
        }

        var resolution = format.Resolution;
        if (resolution.Contains("audio only", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parts = resolution.Split('x', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && int.TryParse(parts[1], out var height))
        {
            return height;
        }

        return null;
    }
}
