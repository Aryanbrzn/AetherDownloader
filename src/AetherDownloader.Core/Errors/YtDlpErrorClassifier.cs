using System.Text.RegularExpressions;
using AetherDownloader.Core.Constants;
using AetherDownloader.Core.Models;
using AetherDownloader.Core.Process;

namespace AetherDownloader.Core.Errors;

public sealed class YtDlpErrorClassifier
{
    public ErrorCode Classify(int exitCode, IReadOnlyList<string> stderrLines)
    {
        var combined = string.Join('\n', stderrLines);
        foreach (var (code, pattern, _) in DownloadErrorPatterns.Rules)
        {
            if (Regex.IsMatch(combined, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                return code;
            }
        }

        return ErrorCode.DlUnknown;
    }

    public ErrorCode Classify(ProcessRunResult result) =>
        Classify(result.ExitCode, result.StandardError);
}
