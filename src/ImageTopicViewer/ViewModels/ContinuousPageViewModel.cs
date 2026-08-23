using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using ImageTopicViewer.Models;
using ImageTopicViewer.Services;

namespace ImageTopicViewer.ViewModels;

public partial class ContinuousPageViewModel : ObservableObject
{
    private readonly IImageStorageService _imageStorageService;
    private readonly IImageSourceProvider _imageSourceProvider;
    private CancellationTokenSource? _loadCts;

    public ObservableCollection<ImageItem> Images { get; } = new();

    [ObservableProperty]
    private bool _showNoSelectionMessage = true;

    [ObservableProperty]
    private bool _showEmptyMessage;

    [ObservableProperty]
    private bool _showImages;

    public ContinuousPageViewModel(IImageStorageService imageStorageService, IImageSourceProvider imageSourceProvider)
    {
        _imageStorageService = imageStorageService;
        _imageSourceProvider = imageSourceProvider;
    }

    public void LoadSubtopic(TopicNode? minorTopic)
    {
        _loadCts?.Cancel();
        Images.Clear();

        if (minorTopic is null)
        {
            ShowNoSelectionMessage = true;
            ShowEmptyMessage = false;
            ShowImages = false;
            return;
        }

        var items = _imageStorageService.GetImages(minorTopic);
        foreach (var item in items)
        {
            Images.Add(item);
        }

        ShowNoSelectionMessage = false;
        ShowEmptyMessage = items.Count == 0;
        ShowImages = items.Count > 0;

        _loadCts = new CancellationTokenSource();
        _ = LoadImagesAsync(items, _loadCts.Token);
    }

    private async Task LoadImagesAsync(IReadOnlyList<ImageItem> items, CancellationToken token)
    {
        foreach (var item in items)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            try
            {
                var image = await _imageSourceProvider.LoadAsync(item.FullPath, token);
                if (!token.IsCancellationRequested)
                {
                    item.Image = image;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (IOException)
            {
                // 파일을 읽을 수 없으면 해당 이미지만 건너뛴다.
            }
        }
    }
}
