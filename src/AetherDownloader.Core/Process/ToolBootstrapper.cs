using System.IO.Compression;
using System.Net.Http;
using AetherDownloader.Core.Abstractions;

namespace AetherDownloader.Core.Process;

public sealed class ToolBootstrapper : IToolBootstrapper
{
    private const string YtDlpDownloadUrl =
        "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";

    private const string FfmpegEssentialsZipUrl =
        "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly IYtDlpLocator _ytDlpLocator;
    private readonly IFfmpegLocator _ffmpegLocator;

    public ToolBootstrapper(IYtDlpLocator ytDlpLocator, IFfmpegLocator ffmpegLocator)
    {
        _ytDlpLocator = ytDlpLocator;
        _ffmpegLocator = ffmpegLocator;
    }

    public ToolAvailability CheckAvailability()
    {
        string? ytDlpPath = null;
        var ytDlpAvailable = false;
        try
        {
            ytDlpPath = _ytDlpLocator.ResolveExecutable();
            ytDlpAvailable = true;
        }
        catch (FileNotFoundException)
        {
            // Missing — caller may offer download.
        }

        var ffmpegAvailable = _ffmpegLocator.TryResolve(out var ffmpegPath);
        return new ToolAvailability(
            ytDlpAvailable,
            ffmpegAvailable,
            ytDlpAvailable ? ytDlpPath : null,
            ffmpegAvailable ? ffmpegPath : null);
    }

    public async Task<string> EnsureYtDlpAsync(
        IProgress<ToolDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return _ytDlpLocator.ResolveExecutable();
        }
        catch (FileNotFoundException)
        {
            // Download into managed LocalAppData tools folder.
        }

        progress?.Report(new ToolDownloadProgress("Downloading yt-dlp (official GitHub release)…", 0));
        var destination = ToolPaths.ManagedYtDlpPath;
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        await DownloadFileAsync(YtDlpDownloadUrl, destination, "yt-dlp", progress, cancellationToken)
            .ConfigureAwait(false);

        if (!File.Exists(destination))
        {
            throw new InvalidOperationException(
                "Failed to download yt-dlp. Install it manually from https://github.com/yt-dlp/yt-dlp/releases or set a path in Settings.");
        }

        progress?.Report(new ToolDownloadProgress("yt-dlp installed.", 100));
        return destination;
    }

    public async Task<string> EnsureFfmpegAsync(
        IProgress<ToolDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (_ffmpegLocator.TryResolve(out var existing))
        {
            return existing;
        }

        progress?.Report(new ToolDownloadProgress("Downloading FFmpeg essentials (official Windows build)…", 0));
        var toolsDir = Path.Combine(ToolPaths.ManagedToolsRoot, "ffmpeg");
        Directory.CreateDirectory(toolsDir);

        var zipPath = Path.Combine(Path.GetTempPath(), $"aether-ffmpeg-{Guid.NewGuid():N}.zip");
        try
        {
            await DownloadFileAsync(FfmpegEssentialsZipUrl, zipPath, "FFmpeg", progress, cancellationToken)
                .ConfigureAwait(false);
            progress?.Report(new ToolDownloadProgress("Extracting ffmpeg.exe / ffprobe.exe…", null));
            ExtractNamedEntries(
                zipPath,
                toolsDir,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [ToolPaths.FfmpegFileName] = ToolPaths.ManagedFfmpegPath,
                    [ToolPaths.FfprobeFileName] = ToolPaths.ManagedFfprobePath,
                });
        }
        finally
        {
            TryDelete(zipPath);
        }

        if (!File.Exists(ToolPaths.ManagedFfmpegPath))
        {
            throw new InvalidOperationException(
                "Failed to install FFmpeg. Download a Windows build from https://ffmpeg.org/download.html or https://www.gyan.dev/ffmpeg/builds/ and set the path in Settings.");
        }

        progress?.Report(new ToolDownloadProgress("FFmpeg installed.", 100));
        return ToolPaths.ManagedFfmpegPath;
    }

    private static async Task DownloadFileAsync(
        string url,
        string destinationPath,
        string label,
        IProgress<ToolDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(
                new Uri(url),
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var fileStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        var buffer = new byte[81920];
        long downloaded = 0;
        int read;
        var lastReportedPercent = -1;

        while ((read = await responseStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                   .ConfigureAwait(false)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            downloaded += read;

            if (totalBytes is null or <= 0)
            {
                progress?.Report(new ToolDownloadProgress(
                    $"Downloading {label}… {FormatBytes(downloaded)}",
                    PercentComplete: null));
                continue;
            }

            var percent = Math.Clamp(downloaded * 100.0 / totalBytes.Value, 0, 100);
            var rounded = (int)Math.Floor(percent);
            if (rounded == lastReportedPercent && rounded is not (0 or 100))
            {
                continue;
            }

            lastReportedPercent = rounded;
            progress?.Report(new ToolDownloadProgress(
                $"Downloading {label}… {rounded}% ({FormatBytes(downloaded)} / {FormatBytes(totalBytes.Value)})",
                percent));
        }
    }

    private static string FormatBytes(long bytes)
    {
        const double kilo = 1024;
        const double mega = kilo * 1024;
        if (bytes >= mega)
        {
            return $"{bytes / mega:0.0} MB";
        }

        if (bytes >= kilo)
        {
            return $"{bytes / kilo:0.0} KB";
        }

        return $"{bytes} B";
    }

    private static void ExtractNamedEntries(
        string zipPath,
        string destinationDirectory,
        IReadOnlyDictionary<string, string> fileNameToDestination)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            if (!fileNameToDestination.TryGetValue(entry.Name, out var destination))
            {
                continue;
            }

            Directory.CreateDirectory(destinationDirectory);
            entry.ExtractToFile(destination, overwrite: true);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup.
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10),
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AetherDownloader/1.0 (+https://github.com/Aryanbrzn/AetherDownloader)");
        return client;
    }
}
