using AetherDownloader.Core.Constants;
using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Download;

public static class FormatArgumentBuilder
{
    public static IReadOnlyList<string> BuildArgs(FormatSelection selection)
    {
        return selection.Mode switch
        {
            FormatSelectionMode.BestMerge => SplitTemplate(YtDlpArgumentTemplates.BestMerge).ToList(),
            FormatSelectionMode.AudioMp3 => SplitTemplate(YtDlpArgumentTemplates.AudioMp3).ToList(),
            FormatSelectionMode.FixedQuality => BuildFixedQualityArgs(selection.TargetQualityHeight ?? 1080),
            FormatSelectionMode.CustomFormat => BuildCustomFormatArgs(selection.FormatId),
            _ => throw new ArgumentOutOfRangeException(nameof(selection), selection.Mode, "Unsupported format selection mode."),
        };
    }

    public static bool RequiresFfmpeg(FormatSelection selection) =>
        selection.Mode is FormatSelectionMode.BestMerge or FormatSelectionMode.FixedQuality;

    private static IReadOnlyList<string> BuildFixedQualityArgs(int height) =>
        ["-f", $"bv*[height<={height}]+ba/b[height<={height}]"];

    private static IReadOnlyList<string> BuildCustomFormatArgs(string? formatId)
    {
        if (string.IsNullOrWhiteSpace(formatId))
        {
            throw new ArgumentException("FormatId is required for CustomFormat mode.", nameof(formatId));
        }

        return SplitTemplate(YtDlpArgumentTemplates.CustomFormat(formatId)).ToList();
    }

    private static IEnumerable<string> SplitTemplate(string template) =>
        template.Split(' ', StringSplitOptions.RemoveEmptyEntries);
}
