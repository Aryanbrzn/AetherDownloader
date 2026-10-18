namespace AetherDownloader.Core.Abstractions;

/// <summary>
/// Resolves the yt-dlp executable path (settings override, managed LocalAppData install, optional local tools copy, or PATH).
/// </summary>
public interface IYtDlpLocator
{
    string ResolveExecutable();
}
