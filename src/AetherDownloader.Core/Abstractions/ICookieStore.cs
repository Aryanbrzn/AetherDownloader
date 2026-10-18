using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Abstractions;

/// <summary>
/// Cookie profile storage and health monitoring.
/// </summary>
public interface ICookieStore
{
    IObservable<CookieHealthEvent> HealthEvents { get; }

    Task<CookieProfile> ImportAsync(Stream netscapeFile, string name, CancellationToken cancellationToken);

    Task<IReadOnlyList<CookieProfile>> ListAsync(CancellationToken cancellationToken);

    Task SetActiveProfileAsync(Guid? profileId, CancellationToken cancellationToken);

    Task<CookieProfile?> GetActiveProfileAsync(CancellationToken cancellationToken);
}
