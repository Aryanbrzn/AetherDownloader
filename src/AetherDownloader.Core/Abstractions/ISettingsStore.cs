using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Abstractions;

/// <summary>
/// Persists application settings to disk.
/// </summary>
public interface ISettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}
