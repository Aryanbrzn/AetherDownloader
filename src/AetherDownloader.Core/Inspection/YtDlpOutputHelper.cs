using AetherDownloader.Core.Process;

namespace AetherDownloader.Core.Inspection;

internal static class YtDlpOutputHelper
{
    public static string GetFailureMessage(ProcessRunResult result, string fallback)
    {
        for (var i = result.StandardError.Count - 1; i >= 0; i--)
        {
            var trimmed = result.StandardError[i].Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed["ERROR:".Length..].Trim();
            }

            return trimmed;
        }

        return fallback;
    }

    public static string GetJsonPayload(ProcessRunResult result)
    {
        if (result.StandardOutput.Count == 0)
        {
            return string.Empty;
        }

        if (result.StandardOutput.Count == 1)
        {
            return result.StandardOutput[0];
        }

        for (var i = result.StandardOutput.Count - 1; i >= 0; i--)
        {
            var line = result.StandardOutput[i].Trim();
            if (line.StartsWith('{') || line.StartsWith('['))
            {
                return line;
            }
        }

        return string.Join('\n', result.StandardOutput);
    }

    public static bool IsUsableJsonPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        var trimmed = payload.Trim();
        if (string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    public static string RequireUsableJsonPayload(ProcessRunResult result, string fallbackMessage)
    {
        var payload = GetJsonPayload(result);
        if (IsUsableJsonPayload(payload))
        {
            return payload;
        }

        throw new InvalidOperationException(GetFailureMessage(result, fallbackMessage));
    }
}
