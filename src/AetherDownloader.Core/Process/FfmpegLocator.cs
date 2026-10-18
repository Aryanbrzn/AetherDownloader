using System.ComponentModel;
using System.Diagnostics;
using AetherDownloader.Core.Abstractions;

namespace AetherDownloader.Core.Process;

public sealed class FfmpegLocator : IFfmpegLocator
{
    private readonly Func<string?> _settingsPathProvider;

    public FfmpegLocator(Func<string?> settingsPathProvider) =>
        _settingsPathProvider = settingsPathProvider;

    public bool TryResolve(out string executablePath)
    {
        var settingsPath = _settingsPathProvider();
        if (!string.IsNullOrWhiteSpace(settingsPath) && File.Exists(settingsPath))
        {
            executablePath = settingsPath;
            return true;
        }

        if (File.Exists(ToolPaths.ManagedFfmpegPath))
        {
            executablePath = ToolPaths.ManagedFfmpegPath;
            return true;
        }

        var pathResult = FindOnPath(ToolPaths.FfmpegFileName);
        if (pathResult is not null)
        {
            executablePath = pathResult;
            return true;
        }

        foreach (var commonPath in GetCommonInstallPaths())
        {
            if (File.Exists(commonPath))
            {
                executablePath = commonPath;
                return true;
            }
        }

        executablePath = string.Empty;
        return false;
    }

    private static IEnumerable<string> GetCommonInstallPaths()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        yield return Path.Combine(programFiles, "ffmpeg", "bin", ToolPaths.FfmpegFileName);

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Path.Combine(localAppData, "Microsoft", "WinGet", "Links", ToolPaths.FfmpegFileName);
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
