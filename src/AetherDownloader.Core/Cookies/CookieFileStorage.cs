namespace AetherDownloader.Core.Cookies;

internal sealed class CookieFileStorage
{
    private readonly string _cookiesDirectory;

    public CookieFileStorage(string? baseDirectoryOverride = null)
    {
        var baseDirectory = ResolveBaseDirectory(baseDirectoryOverride);
        _cookiesDirectory = Path.Combine(baseDirectory, "Cookies");
        Directory.CreateDirectory(_cookiesDirectory);
    }

    public string GetCookieFilePath(Guid profileId) =>
        Path.Combine(_cookiesDirectory, $"{profileId}.txt");

    public string GetManifestPath() =>
        Path.Combine(_cookiesDirectory, "profiles.json");

    public async Task CopyToStoreAsync(Stream source, Guid profileId, CancellationToken cancellationToken)
    {
        var destination = GetCookieFilePath(profileId);
        var tempPath = destination + ".tmp";

        try
        {
            await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await source.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempPath, destination, overwrite: true);
        }
        catch (IOException)
        {
            throw new CookieValidationException("Close your browser and try again.");
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public bool CookieFileExists(Guid profileId) =>
        File.Exists(GetCookieFilePath(profileId));

    private static string ResolveBaseDirectory(string? baseDirectoryOverride)
    {
        if (!string.IsNullOrWhiteSpace(baseDirectoryOverride))
        {
            return baseDirectoryOverride;
        }

        var testOverride = Environment.GetEnvironmentVariable("AETHER_TEST_APPDATA");
        if (!string.IsNullOrWhiteSpace(testOverride))
        {
            return testOverride;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AetherDownloader");
    }
}
