using System.Collections.ObjectModel;
using System.Windows;
using AetherDownloader.App.Services;
using AetherDownloader.Core.Models;
using AetherDownloader.Core.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AetherDownloader.App.ViewModels;

public partial class DownloadQueueViewModel : ViewModelBase
{
    private readonly DownloadOrchestrator _orchestrator;
    private readonly IDialogService _dialogService;
    private readonly NavigationService _navigation;
    private readonly AppSettingsHolder _settings;

    public DownloadQueueViewModel(
        DownloadOrchestrator orchestrator,
        IDialogService dialogService,
        NavigationService navigation,
        AppSettingsHolder settings)
    {
        _orchestrator = orchestrator;
        _dialogService = dialogService;
        _navigation = navigation;
        _settings = settings;

        Downloads = [];
        _orchestrator.ItemRemoved += (_, item) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Downloads.Remove(item);
                OnPropertyChanged(nameof(HeaderText));
            });
        };
    }

    public ObservableCollection<DownloadItemViewModel> Downloads { get; }

    public string HeaderText
    {
        get
        {
            var active = Downloads.Count(d => d.Status == DownloadJobState.Downloading);
            var queued = Downloads.Count(d => d.Status == DownloadJobState.Pending);
            return $"Downloads ({active} active · {queued} queued)";
        }
    }

    public Task EnqueueFromMetadataAsync(MetadataCardViewModel metadata, bool startImmediately)
    {
        if (metadata.Metadata is null || metadata.SelectedFormat is null)
        {
            return Task.CompletedTask;
        }

        var downloadPlaylist = false;
        if (metadata.Metadata.IsPlaylist)
        {
            var result = _dialogService.ShowConfirm(
                "Playlist detected",
                "Download entire playlist?",
                MessageBoxButton.YesNoCancel);

            if (result == MessageBoxResult.Cancel)
            {
                return Task.CompletedTask;
            }

            downloadPlaylist = result == MessageBoxResult.Yes;
        }

        var formatSelection = metadata.BuildFormatSelection();
        var item = new DownloadItemViewModel(_orchestrator, _navigation, _settings)
        {
            JobId = Guid.NewGuid(),
            Url = metadata.Metadata.SourceUrl,
            Title = metadata.Metadata.Title,
            FormatSelection = formatSelection,
            DownloadPlaylist = downloadPlaylist,
            FormatLabel = metadata.SelectedFormat.GroupLabel ?? metadata.SelectedFormat.Extension,
            Status = startImmediately ? DownloadJobState.Downloading : DownloadJobState.Pending,
        };

        Downloads.Insert(0, item);
        OnPropertyChanged(nameof(HeaderText));

        _ = _orchestrator.StartDownloadAsync(item, formatSelection, downloadPlaylist);
        OnPropertyChanged(nameof(HeaderText));
        return Task.CompletedTask;
    }

    public void RefreshErrorMessages()
    {
        foreach (var item in Downloads)
        {
            item.RefreshLanguage(_settings.Current.Language);
        }
    }
}
