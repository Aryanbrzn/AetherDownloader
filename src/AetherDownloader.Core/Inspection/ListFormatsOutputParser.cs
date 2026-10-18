using System.Text.RegularExpressions;
using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Inspection;

internal static partial class ListFormatsOutputParser
{
    [GeneratedRegex(@"^\s*(?<id>\S+)\s+(?<ext>\S+)\s+(?<resolution>\S+(?:\s+\S+)?)\s", RegexOptions.Compiled)]
    private static partial Regex FormatLineRegex();

    public static IReadOnlyList<FormatOption> Parse(IReadOnlyList<string> lines)
    {
        var formats = new List<FormatOption>();
        var inTable = false;

        foreach (var line in lines)
        {
            if (line.Contains("ID  EXT", StringComparison.Ordinal) || line.Contains("ID EXT", StringComparison.Ordinal))
            {
                inTable = true;
                continue;
            }

            if (!inTable || string.IsNullOrWhiteSpace(line) || line.StartsWith('-'))
            {
                continue;
            }

            var match = FormatLineRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var formatId = match.Groups["id"].Value;
            if (formatId is "audio" or "video" or "storyboard")
            {
                continue;
            }

            var resolution = match.Groups["resolution"].Value.Trim();
            var ext = match.Groups["ext"].Value;

            formats.Add(new FormatOption
            {
                FormatId = formatId,
                Extension = ext,
                Resolution = resolution,
                Note = ExtractNote(line),
            });
        }

        return formats;
    }

    private static string? ExtractNote(string line)
    {
        var pipeIndex = line.LastIndexOf('|');
        if (pipeIndex < 0 || pipeIndex >= line.Length - 1)
        {
            return null;
        }

        var note = line[(pipeIndex + 1)..].Trim();
        return string.IsNullOrEmpty(note) ? null : note;
    }
}
