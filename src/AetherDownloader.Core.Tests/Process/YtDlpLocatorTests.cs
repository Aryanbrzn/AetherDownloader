using AetherDownloader.Core.Process;
using FluentAssertions;

namespace AetherDownloader.Core.Tests.Process;

public sealed class YtDlpLocatorTests : IDisposable
{
    private readonly string _tempRoot;

    public YtDlpLocatorTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"aether-locator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [Fact]
    public void ResolveExecutable_SettingsOverrideExists_TakesPriority()
    {
        var overridePath = Path.Combine(_tempRoot, "custom-yt-dlp.exe");
        File.WriteAllText(overridePath, string.Empty);

        var locator = new YtDlpLocator(() => overridePath);
        var result = locator.ResolveExecutable();

        result.Should().Be(overridePath);
    }

    [Fact]
    public void ResolveExecutable_BundledOrManagedOrPath_DoesNotThrowWhenPresent()
    {
        var locator = new YtDlpLocator(() => null);

        if (File.Exists(ToolPaths.ManagedYtDlpPath)
            || File.Exists(ToolPaths.BundledYtDlpPath)
            || IsOnPath(ToolPaths.YtDlpFileName))
        {
            var act = () => locator.ResolveExecutable();
            act.Should().NotThrow();
            return;
        }

        var actMissing = () => locator.ResolveExecutable();
        actMissing.Should().Throw<FileNotFoundException>()
            .WithMessage("*yt-dlp executable not found*");
    }

    [Fact]
    public void ResolveExecutable_NoCandidates_ThrowsFileNotFoundException()
    {
        if (File.Exists(ToolPaths.ManagedYtDlpPath)
            || File.Exists(ToolPaths.BundledYtDlpPath)
            || IsOnPath(ToolPaths.YtDlpFileName))
        {
            return;
        }

        var locator = new YtDlpLocator(() => null);

        var act = () => locator.ResolveExecutable();

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*yt-dlp executable not found*");
    }

    private static bool IsOnPath(string executableName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return false;
        }

        return pathValue
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(directory => Path.Combine(directory, executableName))
            .Any(File.Exists);
    }
}
