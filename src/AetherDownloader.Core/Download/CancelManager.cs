namespace AetherDownloader.Core.Download;

public static class CancelManager
{
    private static readonly string[] PartialExtensions = [".part", ".ytdl", ".temp"];

    public static IReadOnlyList<string> CleanupPartialFiles(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var removed = new List<string>();
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
        {
            if (PartialExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    File.Delete(file);
                    removed.Add(file);
                }
                catch (IOException)
                {
                    // Best-effort cleanup after cancel.
                }
                catch (UnauthorizedAccessException)
                {
                    // Best-effort cleanup after cancel.
                }
            }
        }

        return removed;
    }
}
