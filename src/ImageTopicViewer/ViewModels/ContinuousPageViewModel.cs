using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    /// <summary>연속보기/단일보기가 공유하는 "현재 이미지" 위치. 모드 전환 시 유지된다 (06-view-modes.md 공통 절).</summary>
    [ObservableProperty]
    private int _currentIndex;

    /// <summary>단일보기가 표시할 현재 이미지. Images나 CurrentIndex가 바뀌면 갱신된다.</summary>
    public ImageItem? CurrentItem =>
        CurrentIndex >= 0 && CurrentIndex < Images.Count ? Images[CurrentIndex] : null;

    public ContinuousPageViewModel(IImageStorageService imageStorageService, IImageSourceProvider imageSourceProvider)
    {
        _imageStorageService = imageStorageService;
        _imageSourceProvider = imageSourceProvider;
    }

    partial void OnCurrentIndexChanged(int value) => OnPropertyChanged(nameof(CurrentItem));

    /// <summary>연속보기에서 특정 이미지를 클릭했을 때 "현재 이미지"로 지정한다.</summary>
    public void SetCurrentIndex(ImageItem item)
    {
        var index = Images.IndexOf(item);
        if (index >= 0)
        {
            CurrentIndex = index;
        }
    }

    [RelayCommand]
    private void GoToPreviousImage()
    {
        if (CurrentIndex > 0)
        {
            CurrentIndex--;
        }
    }

    [RelayCommand]
    private void GoToNextImage()
    {
        if (CurrentIndex < Images.Count - 1)
        {
            CurrentIndex++;
        }
    }

    public void LoadSubtopic(TopicNode? minorTopic)
    {
        _currentMinorTopic = minorTopic;
        _loadCts?.Cancel();
        Images.Clear();
        CurrentIndex = 0;

        if (minorTopic is null)
        {
            ShowNoSelectionMessage = true;
            ShowEmptyMessage = false;
            ShowImages = false;
            OnPropertyChanged(nameof(CurrentItem));
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
        OnPropertyChanged(nameof(CurrentItem));

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

    /// <summary>드롭된 이미지 소스 목록을 추가한다 (로컬 파일/비트맵/가상 파일 스트림 등, 05-image-features.md 참조).</summary>
    public void AddDroppedImages(IReadOnlyList<ImageSourceInput> inputs)
    {
        if (_currentMinorTopic is null || inputs.Count == 0)
        {
            return;
        }

        ProcessAdd(inputs);
    }

    public void MoveImage(ImageItem draggedItem, ImageItem targetItem)
    {
        if (_currentMinorTopic is null)
        {
            return;
        }

        var oldIndex = Images.IndexOf(draggedItem);
        var newIndex = Images.IndexOf(targetItem);
        if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex)
        {
            return;
        }

        Images.Move(oldIndex, newIndex);
        _imageStorageService.Renumber(_currentMinorTopic, Images.ToList());
        OnPropertyChanged(nameof(CurrentItem));
    }

    public void RequestDeleteImage(ImageItem item)
    {
        if (_currentMinorTopic is null)
        {
            return;
        }

        var confirm = MessageBox.Show(
            $"'{item.FileName}' 이미지를 삭제하시겠습니까? (휴지통으로 이동됩니다)",
            "이미지 삭제",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _imageStorageService.DeleteToRecycleBin(item);
        }
        catch (IOException ex)
        {
            MessageBox.Show(ex.Message, "이미지 삭제 실패", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Images.Remove(item);

        if (Images.Count > 0)
        {
            _imageStorageService.Renumber(_currentMinorTopic, Images.ToList());
        }

        ShowEmptyMessage = Images.Count == 0;
        ShowImages = Images.Count > 0;

        if (CurrentIndex >= Images.Count)
        {
            CurrentIndex = Math.Max(0, Images.Count - 1);
        }
        else
        {
            OnPropertyChanged(nameof(CurrentItem));
        }
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
                $"{result.FailedCount}개 이미지를 추가하지 못했습니다. (지원하지 않는 형식이거나 파일을 읽을 수 없음)",
                "이미지 추가",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
