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
        IProgress<string>? progress = null,
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

        progress?.Report("Downloading yt-dlp (official GitHub release)…");
        var destination = ToolPaths.ManagedYtDlpPath;
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        await DownloadFileAsync(YtDlpDownloadUrl, destination, cancellationToken).ConfigureAwait(false);

        if (!File.Exists(destination))
        {
            throw new InvalidOperationException(
                "Failed to download yt-dlp. Install it manually from https://github.com/yt-dlp/yt-dlp/releases or set a path in Settings.");
        }

        progress?.Report("yt-dlp installed.");
        return destination;
    }

    public async Task<string> EnsureFfmpegAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (_ffmpegLocator.TryResolve(out var existing))
        {
            return existing;
        }

        progress?.Report("Downloading FFmpeg essentials (official Windows build)…");
        var toolsDir = Path.Combine(ToolPaths.ManagedToolsRoot, "ffmpeg");
        Directory.CreateDirectory(toolsDir);

        var zipPath = Path.Combine(Path.GetTempPath(), $"aether-ffmpeg-{Guid.NewGuid():N}.zip");
        try
        {
            await DownloadFileAsync(FfmpegEssentialsZipUrl, zipPath, cancellationToken).ConfigureAwait(false);
            progress?.Report("Extracting ffmpeg.exe / ffprobe.exe…");
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

        progress?.Report("FFmpeg installed.");
        return ToolPaths.ManagedFfmpegPath;
    }

    private static async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        await using var responseStream = await HttpClient.GetStreamAsync(new Uri(url), cancellationToken)
            .ConfigureAwait(false);
        await using var fileStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);
        await responseStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
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
