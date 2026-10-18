using AetherDownloader.Core.Constants;

namespace AetherDownloader.Core.Inspection;

internal static class YtDlpCommandBuilder
{
    public static IReadOnlyList<string> BuildInspectArgs(string url, string? cookieFilePath)
    {
        var args = new List<string>();
        args.AddRange(SplitTemplate(YtDlpArgumentTemplates.DumpSingleJson));
        if (!string.IsNullOrEmpty(cookieFilePath))
        {
            args.AddRange(["--cookies", cookieFilePath]);
        }

        args.Add(url);
        return args;
    }

    public static IReadOnlyList<string> BuildListFormatsArgs(string url, string? cookieFilePath)
    {
        var args = new List<string>();
        args.AddRange(SplitTemplate(YtDlpArgumentTemplates.ListFormats));
        args.Add("--no-download");
        if (!string.IsNullOrEmpty(cookieFilePath))
        {
            args.AddRange(["--cookies", cookieFilePath]);
        }

        args.Add(url);
        return args;
    }

    private static IEnumerable<string> SplitTemplate(string template) =>
        template.Split(' ', StringSplitOptions.RemoveEmptyEntries);
}
