using System.Text;

namespace AetherDownloader.Core.Cookies;

public static class NetscapeCookieValidator
{
    private const string RequiredHeader = "# Netscape HTTP Cookie File";

    public static CookieValidationResult Validate(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            return Validate(stream);
        }
        catch (IOException ex)
        {
            return CookieValidationResult.Failure($"Unable to read cookie file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return CookieValidationResult.Failure($"Unable to read cookie file: {ex.Message}");
        }
    }

    public static CookieValidationResult Validate(Stream stream)
    {
        if (stream.Length == 0)
        {
            return CookieValidationResult.Failure("Cookie file is empty.");
        }

        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var firstLine = reader.ReadLine();
        if (firstLine is null || firstLine.Length == 0)
        {
            return CookieValidationResult.Failure("Cookie file is empty.");
        }

        if (firstLine.TrimStart().StartsWith('{'))
        {
            return CookieValidationResult.Failure("Unsupported format — export Netscape cookies.txt");
        }

        stream.Position = 0;
        reader.DiscardBufferedData();
        reader.BaseStream.Position = 0;

        var headerFound = false;
        var cookieCount = 0;
        var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith('#'))
            {
                if (line.Contains(RequiredHeader, StringComparison.Ordinal))
                {
                    headerFound = true;
                }

                continue;
            }

            var fields = line.Split('\t');
            if (fields.Length != 7)
            {
                return CookieValidationResult.Failure(
                    "Invalid cookie row — expected 7 tab-separated fields per Netscape format.");
            }

            var domain = fields[0];
            if (domain.StartsWith("#HttpOnly_", StringComparison.Ordinal))
            {
                domain = domain["#HttpOnly_".Length..];
            }

            if (domain.Length == 0)
            {
                return CookieValidationResult.Failure("Invalid cookie row — domain field is empty.");
            }

            domains.Add(domain);
            cookieCount++;
        }

        if (!headerFound)
        {
            return CookieValidationResult.Failure(
                "Missing Netscape header — file must contain '# Netscape HTTP Cookie File'.");
        }

        if (cookieCount == 0)
        {
            return CookieValidationResult.Failure("Cookie file contains no cookie entries.");
        }

        return CookieValidationResult.Success(cookieCount, domains.OrderBy(static d => d, StringComparer.OrdinalIgnoreCase).ToArray());
    }
}
