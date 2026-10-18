using AetherDownloader.Core.Abstractions;
using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Inspection;

internal static class CookiePathResolver
{
    public static async Task<string?> ResolveAsync(
        ICookieStore cookieStore,
        Guid? cookieProfileId,
        CancellationToken cancellationToken)
    {
        if (cookieProfileId is Guid profileId)
        {
            var profiles = await cookieStore.ListAsync(cancellationToken).ConfigureAwait(false);
            var match = profiles.FirstOrDefault(p => p.Id == profileId);
            return match?.FilePath;
        }

        var active = await cookieStore.GetActiveProfileAsync(cancellationToken).ConfigureAwait(false);
        return active?.FilePath;
    }
}
