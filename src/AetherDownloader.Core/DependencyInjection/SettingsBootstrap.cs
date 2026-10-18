using AetherDownloader.Core.Abstractions;

namespace AetherDownloader.Core.DependencyInjection;

/// <summary>
/// Eagerly loads settings once during DI construction.
/// Uses ConfigureAwait(false) so a sync wait never deadlocks the WPF UI thread.
/// </summary>
public sealed class SettingsBootstrap
{
    public SettingsBootstrap(ISettingsStore settingsStore)
    {
        settingsStore.LoadAsync(CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }
}
