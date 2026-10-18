using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Media;
using AetherDownloader.App.Services;
using AetherDownloader.Core.Abstractions;
using AetherDownloader.Core.Inspection;
using AetherDownloader.Core.Models;
using AetherDownloader.Core.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AetherDownloader.App.ViewModels;

public partial class MetadataCardViewModel : ViewModelBase
{
    private readonly IDownloadEngine _downloadEngine;
    private readonly ThumbnailLoaderService _thumbnailLoader;
    private readonly AppSettingsHolder _settings;
    private readonly DownloadQueueViewModel _downloadQueue;
    private string _sourceUrl = string.Empty;

    public MetadataCardViewModel(
        IDownloadEngine downloadEngine,
        ThumbnailLoaderService thumbnailLoader,
        AppSettingsHolder settings,
        DownloadQueueViewModel downloadQueue)
    {
        _downloadEngine = downloadEngine;
        _thumbnailLoader = thumbnailLoader;
        _settings = settings;
        _downloadQueue = downloadQueue;

        Formats = [];
        FormatGroupsView = CollectionViewSource.GetDefaultView(Formats);
        FormatGroupsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(FormatOption.GroupLabel)));
        FormatGroupsView.SortDescriptions.Add(new SortDescription(nameof(FormatOption.GroupLabel), ListSortDirection.Ascending));
    }

    [ObservableProperty]
    private MetadataCardState _state = MetadataCardState.Empty;

    [ObservableProperty]
    private MediaMetadata? _metadata;

    [ObservableProperty]
    private ImageSource? _thumbnailImage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(QueueCommand))]
    private FormatOption? _selectedFormat;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private double _thumbnailWidth = 320;

    [ObservableProperty]
    private double _thumbnailHeight = 180;

    public ObservableCollection<FormatOption> Formats { get; }

    public ICollectionView FormatGroupsView { get; }

    public bool HasFormats => Formats.Count > 0;

    public string Title => FormatDisplayTitle(Metadata?.Title);

    public string ChannelName => Metadata?.Uploader ?? string.Empty;

    public string DurationFormatted
    {
        get
        {
            if (Metadata?.DurationSeconds is not int seconds)
            {
                return string.Empty;
            }

            var ts = TimeSpan.FromSeconds(seconds);
            return ts.TotalHours >= 1
                ? ts.ToString(@"h\:mm\:ss")
                : ts.ToString(@"m\:ss");
        }
    }

    public string Extractor => Metadata?.Extractor ?? string.Empty;

    /// <summary>
    /// Channel · duration · extractor — empty fields are omitted (avoids " · · generic").
    /// </summary>
    public string MetadataSummary
    {
        get
        {
            if (Metadata is null)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(ChannelName))
            {
                parts.Add(ChannelName);
            }

            if (!string.IsNullOrWhiteSpace(DurationFormatted))
            {
                parts.Add(DurationFormatted);
            }

            if (!string.IsNullOrWhiteSpace(Extractor))
            {
                parts.Add(
                    string.Equals(Extractor, "generic", StringComparison.OrdinalIgnoreCase)
                        ? "Direct file"
                        : Extractor);
            }

            return string.Join(" · ", parts);
        }
    }

    public bool CanDownload => SelectedFormat is not null && State == MetadataCardState.Loaded;

    partial void OnStateChanged(MetadataCardState value)
    {
        OnPropertyChanged(nameof(CanDownload));
        DownloadCommand.NotifyCanExecuteChanged();
        QueueCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedFormatChanged(FormatOption? value)
    {
        OnPropertyChanged(nameof(CanDownload));
        DownloadCommand.NotifyCanExecuteChanged();
        QueueCommand.NotifyCanExecuteChanged();
    }

    partial void OnMetadataChanged(MediaMetadata? value)
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(ChannelName));
        OnPropertyChanged(nameof(DurationFormatted));
        OnPropertyChanged(nameof(Extractor));
        OnPropertyChanged(nameof(MetadataSummary));
    }

    private static string FormatDisplayTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        return title.Replace('_', ' ').Trim();
    }

    public void BeginInspect()
    {
        State = MetadataCardState.Loading;
        ErrorMessage = null;
        Metadata = null;
        ThumbnailImage = null;
        SelectedFormat = null;
        Formats.Clear();
        OnPropertyChanged(nameof(HasFormats));
    }

    public void HandleInspectFailed(string message)
    {
        State = MetadataCardState.Error;
        ErrorMessage = message;
    }

    public void UpdateWindowWidth(double width)
    {
        if (width >= 1100)
        {
            ThumbnailWidth = 320;
            ThumbnailHeight = 180;
        }
        else
        {
            ThumbnailWidth = 240;
            ThumbnailHeight = 135;
        }
    }

    public async Task LoadFromMetadataAsync(MediaMetadata metadata, string url, CancellationToken cancellationToken)
    {
        _sourceUrl = url;
        Metadata = metadata;

        try
        {
            ThumbnailImage = await _thumbnailLoader
                .LoadAsync(metadata.ThumbnailUrl, cancellationToken)
                .ConfigureAwait(true);

            var inspectOptions = new InspectOptions
            {
                CookieProfileId = _settings.Current.ActiveCookieProfileId,
            };

            var rawFormats = await _downloadEngine
                .ListFormatsAsync(url, inspectOptions, cancellationToken)
                .ConfigureAwait(true);

            Formats.Clear();
            foreach (var format in FormatGrouper.Group(rawFormats))
            {
                Formats.Add(format);
            }

            OnPropertyChanged(nameof(HasFormats));
            SelectedFormat = Formats.FirstOrDefault(f => f.GroupLabel == FormatGrouper.GroupBest);
            State = MetadataCardState.Loaded;
        }
        catch (Exception ex)
        {
            HandleInspectFailed(ex.Message);
        }
    }

    public FormatSelection BuildFormatSelection()
    {
        if (SelectedFormat is null)
        {
            throw new InvalidOperationException("No format selected.");
        }

        if (SelectedFormat.FormatId == "best" || SelectedFormat.GroupLabel == FormatGrouper.GroupBest)
        {
            return new FormatSelection { Mode = FormatSelectionMode.BestMerge };
        }

        if (SelectedFormat.GroupLabel == FormatGrouper.GroupAudio)
        {
            return new FormatSelection { Mode = FormatSelectionMode.AudioMp3 };
        }

        var height = FormatGrouper.TryGetHeight(SelectedFormat);
        if (height.HasValue && SelectedFormat.GroupLabel != FormatGrouper.GroupAdvanced)
        {
            return new FormatSelection
            {
                Mode = FormatSelectionMode.FixedQuality,
                TargetQualityHeight = height.Value,
            };
        }

        return new FormatSelection
        {
            Mode = FormatSelectionMode.CustomFormat,
            FormatId = SelectedFormat.FormatId,
        };
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync() =>
        await _downloadQueue.EnqueueFromMetadataAsync(this, startImmediately: true).ConfigureAwait(true);

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task QueueAsync() =>
        await _downloadQueue.EnqueueFromMetadataAsync(this, startImmediately: false).ConfigureAwait(true);
}
