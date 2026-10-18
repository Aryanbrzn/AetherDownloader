using AetherDownloader.Core.Abstractions;

namespace AetherDownloader.Core.DependencyInjection;

public sealed class SettingsBootstrap
{
    public SettingsBootstrap(ISettingsStore settingsStore)
    {
        settingsStore.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
    }
}
