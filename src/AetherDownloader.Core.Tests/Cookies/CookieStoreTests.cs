using AetherDownloader.Core.Cookies;
using AetherDownloader.Core.Models;
using AetherDownloader.Core.Process;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherDownloader.Core.Tests.Cookies;

public sealed class CookieStoreTests : IDisposable
{
    private readonly string _testBaseDirectory;
    private readonly CookieStore _store;

    public CookieStoreTests()
    {
        _testBaseDirectory = Path.Combine(Path.GetTempPath(), $"aether-cookies-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testBaseDirectory);
        _store = new CookieStore(NullLogger<CookieStore>.Instance, _testBaseDirectory);
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_testBaseDirectory))
        {
            Directory.Delete(_testBaseDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ImportAsync_ValidStream_CreatesProfileAndFile()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "cookies-valid.txt");
        await using var stream = File.OpenRead(fixturePath);

        var profile = await _store.ImportAsync(stream, "Test Profile", CancellationToken.None);

        profile.Name.Should().Be("Test Profile");
        profile.DomainSummary.Should().Contain(".example.com");
        File.Exists(profile.FilePath).Should().BeTrue();

        var profiles = await _store.ListAsync(CancellationToken.None);
        profiles.Should().ContainSingle(p => p.Id == profile.Id);
    }

    [Fact]
    public async Task ImportAsync_InvalidStream_ThrowsCookieValidationException()
    {
        await using var stream = new MemoryStream([]);

        var act = async () => await _store.ImportAsync(stream, "Bad", CancellationToken.None);

        await act.Should().ThrowAsync<CookieValidationException>();
    }

    [Fact]
    public void OnProcessOutputLine_SignInRequired_EmitsHealthEvent()
    {
        CookieHealthEvent? captured = null;
        using var subscription = _store.HealthEvents.Subscribe(e => captured = e);

        _store.OnProcessOutputLine(new ProcessOutputLine(
            IsError: true,
            Text: "Sign in required to confirm your age",
            Timestamp: DateTimeOffset.UtcNow));

        captured.Should().NotBeNull();
        captured!.Severity.Should().Be(CookieHealthSeverity.High);
    }
}
