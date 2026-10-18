using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Settings;

public sealed class AppSettingsHolder
{
    public AppSettings Current { get; private set; } = new();

    public void Update(AppSettings settings) => Current = settings;
}
