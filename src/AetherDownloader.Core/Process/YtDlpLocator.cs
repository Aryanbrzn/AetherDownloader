using System.ComponentModel;
using System.Diagnostics;
using AetherDownloader.Core.Abstractions;

namespace AetherDownloader.Core.Process;

public sealed class YtDlpLocator : IYtDlpLocator
{
    private readonly Func<string?> _settingsPathProvider;

    public YtDlpLocator(Func<string?> settingsPathProvider) =>
        _settingsPathProvider = settingsPathProvider;

    public string ResolveExecutable()
    {
        var settingsPath = _settingsPathProvider();
        if (!string.IsNullOrWhiteSpace(settingsPath) && File.Exists(settingsPath))
        {
            return settingsPath;
        }

        if (File.Exists(ToolPaths.ManagedYtDlpPath))
        {
            return ToolPaths.ManagedYtDlpPath;
        }

        if (File.Exists(ToolPaths.BundledYtDlpPath))
        {
            return ToolPaths.BundledYtDlpPath;
        }

        var pathResult = FindOnPath(ToolPaths.YtDlpFileName);
        if (pathResult is not null)
        {
            return pathResult;
        }

        throw new FileNotFoundException(
            "yt-dlp executable not found. Use Settings → Download tools, place yt-dlp.exe under tools/yt-dlp/ for local builds, set a path override, or add yt-dlp to PATH.",
            ToolPaths.YtDlpFileName);
    }

    private static string? FindOnPath(string executableName)
    {
        var whereResult = FindViaWhereExe(executableName);
        if (whereResult is not null)
        {
            return whereResult;
        }

        return ScanPathEnvironment(executableName);
    }

    private static string? FindViaWhereExe(string executableName)
    {
        try
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "where.exe",
                    Arguments = executableName,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return null;
            }

            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (File.Exists(line))
                {
                    return line;
                }
            }
        }
        catch (IOException)
        {
            // Fall through to PATH scan.
        }
        catch (Win32Exception)
        {
            // Fall through to PATH scan.
        }

        return null;
    }

    private static string? ScanPathEnvironment(string executableName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (var directory in pathValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(directory, executableName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
