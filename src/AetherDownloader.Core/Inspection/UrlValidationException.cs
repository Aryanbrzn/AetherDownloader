namespace AetherDownloader.Core.Inspection;

public sealed class UrlValidationException : Exception
{
    public UrlValidationException(string message) : base(message)
    {
    }
}
