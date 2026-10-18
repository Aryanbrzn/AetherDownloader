using System.Text.Json;
using System.Text.Json.Serialization;

namespace AetherDownloader.Core.Cookies;

internal sealed record CookieProfileEntry
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTimeOffset ImportedAt { get; init; }
    public required IReadOnlyList<string> DomainSummary { get; init; }
}

internal sealed class CookieProfileManifest
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    private readonly CookieFileStorage _storage;

    public CookieProfileManifest(CookieFileStorage storage) => _storage = storage;

    public async Task<IReadOnlyList<CookieProfileEntry>> LoadAsync(CancellationToken cancellationToken)
    {
        var manifestPath = _storage.GetManifestPath();
        if (!File.Exists(manifestPath))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(manifestPath);
            var entries = await JsonSerializer.DeserializeAsync<List<CookieProfileEntry>>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            return entries ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    public async Task SaveAsync(IReadOnlyList<CookieProfileEntry> entries, CancellationToken cancellationToken)
    {
        var manifestPath = _storage.GetManifestPath();
        var tempPath = manifestPath + ".tmp";

        try
        {
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, entries, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }

            File.Move(tempPath, manifestPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public async Task AddAsync(CookieProfileEntry entry, CancellationToken cancellationToken)
    {
        var entries = (await LoadAsync(cancellationToken).ConfigureAwait(false)).ToList();
        entries.Add(entry);
        await SaveAsync(entries, cancellationToken).ConfigureAwait(false);
    }
}
