using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ImageTopicViewer.Models;
using ImageTopicViewer.Services;

namespace ImageTopicViewer.ViewModels;

public partial class ContinuousPageViewModel : ObservableObject
{
    private readonly IImageStorageService _imageStorageService;
    private readonly IImageSourceProvider _imageSourceProvider;
    private CancellationTokenSource? _loadCts;
    private TopicNode? _currentMinorTopic;

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
        _currentMinorTopic = minorTopic;
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

    public void AddDroppedFiles(IReadOnlyList<string> filePaths)
    {
        if (_currentMinorTopic is null || filePaths.Count == 0)
        {
            return;
        }

        IReadOnlyList<ImageSourceInput> inputs = filePaths
            .Select(path => (ImageSourceInput)new ImageSourceInput.FromFile(path))
            .ToList();

        ProcessAdd(inputs);
    }

    public void AddDroppedBitmap(BitmapSource bitmap)
    {
        if (_currentMinorTopic is null)
        {
            return;
        }

        ProcessAdd(new List<ImageSourceInput> { new ImageSourceInput.FromBitmap(bitmap) });
    }

    private void ProcessAdd(IReadOnlyList<ImageSourceInput> inputs)
    {
        var minorTopic = _currentMinorTopic!;
        var result = _imageStorageService.AddImages(minorTopic, inputs);

        // 새로 추가된 파일을 반영하고 비동기 로딩을 다시 트리거한다 (05-image-features.md).
        LoadSubtopic(minorTopic);

        if (result.FailedCount > 0)
        {
            MessageBox.Show(
                $"{result.FailedCount}개 이미지는 지원하지 않는 형식이라 추가되지 않았습니다.",
                "이미지 추가",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
