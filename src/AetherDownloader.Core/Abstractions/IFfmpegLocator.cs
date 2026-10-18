namespace AetherDownloader.Core.Abstractions;

/// <summary>
/// Resolves the FFmpeg executable path from settings or common install locations.
/// </summary>
public interface IFfmpegLocator
{
    bool TryResolve(out string executablePath);
}
