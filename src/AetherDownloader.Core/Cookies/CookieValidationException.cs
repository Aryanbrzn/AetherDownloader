namespace AetherDownloader.Core.Cookies;

public sealed class CookieValidationException : Exception
{
    public CookieValidationException(string message)
        : base(message)
    {
    }
}
