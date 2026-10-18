using System.Security.Cryptography;
using System.Text;

namespace AetherDownloader.Core.Download;

public static class FilenameSanitizer
{
    private const int MaxLength = 180;
    private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();

    public static string Sanitize(string title)
    {
        var cleaned = new string(title.Where(c => !InvalidChars.Contains(c)).ToArray()).Trim();
        cleaned = cleaned.Replace("..", string.Empty, StringComparison.Ordinal);

        if (cleaned.Length <= MaxLength)
        {
            return cleaned;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cleaned)))[..8];
        return cleaned[..(MaxLength - 9)] + "_" + hash;
    }
}
