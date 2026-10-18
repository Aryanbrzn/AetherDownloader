using System.ComponentModel;
using AetherDownloader.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AetherDownloader.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel(
        UrlCookieBarViewModel urlCookieBar,
        MetadataCardViewModel metadataCard,
        DownloadQueueViewModel downloadQueue,
        AppStatusBarViewModel statusBar)
    {
        UrlCookieBar = urlCookieBar;
        MetadataCard = metadataCard;
        DownloadQueue = downloadQueue;
        StatusBar = statusBar;

        UrlCookieBar.InspectCompleted += metadata =>
        {
            _ = MetadataCard.LoadFromMetadataAsync(metadata, UrlCookieBar.UrlInput, CancellationToken.None);
        };

        UrlCookieBar.InspectFailed += MetadataCard.HandleInspectFailed;

        UrlCookieBar.PropertyChanged += OnUrlCookieBarPropertyChanged;
    }

    [ObservableProperty]
    private string _windowTitle = "AetherDownloader";

    public UrlCookieBarViewModel UrlCookieBar { get; }

    public MetadataCardViewModel MetadataCard { get; }

    public DownloadQueueViewModel DownloadQueue { get; }

    public AppStatusBarViewModel StatusBar { get; }

    public void UpdateLayoutWidth(double width) => MetadataCard.UpdateWindowWidth(width);

    private void OnUrlCookieBarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UrlCookieBarViewModel.IsInspecting)
            && UrlCookieBar.IsInspecting)
        {
            MetadataCard.BeginInspect();
        }
    }
}
