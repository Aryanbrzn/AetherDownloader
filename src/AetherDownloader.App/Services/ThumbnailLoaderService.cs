using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;

namespace AetherDownloader.App.Services;

public sealed class ThumbnailLoaderService
{
    private static readonly HttpClient HttpClient = new();

    public async Task<BitmapImage?> LoadAsync(string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        try
        {
            var bytes = await HttpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
            return await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    using var stream = new MemoryStream(bytes);
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.DecodePixelWidth = 640;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
                catch
                {
                    return null;
                }
            });
        }
        catch
        {
            return null;
        }
    }
}
