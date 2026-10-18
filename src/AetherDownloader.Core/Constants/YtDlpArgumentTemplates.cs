namespace AetherDownloader.Core.Constants;

public static class YtDlpArgumentTemplates
{
    public const string BestMerge = "-f bestvideo+bestaudio --merge-output-format mp4";
    public const string AudioMp3 = "-x --audio-format mp3 --audio-quality 0";
    public const string FixedQuality1080p = "-f bv*[height<=1080]+ba/b[height<=1080]";
    public static string CustomFormat(string formatId) => $"-f {formatId}";
    public const string DumpSingleJson = "--dump-single-json --no-download --no-warnings";
    public const string ListFormats = "--list-formats";
    public const string NewlineProgress = "--newline --progress";
}
