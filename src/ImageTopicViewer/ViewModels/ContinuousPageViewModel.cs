using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageTopicViewer.Models;
using ImageTopicViewer.Services;

namespace ImageTopicViewer.ViewModels;

/// <summary>확대/축소 배율(%) 선택지. 100%가 원본 크기다.</summary>
public sealed record ZoomOption(string Label, double Percent);

/// <summary>연속보기 마우스 휠 스크롤 배속 선택지.</summary>
public sealed record ScrollSpeedOption(string Label, int Multiplier);

public partial class ContinuousPageViewModel : ObservableObject
{
    private const double MinZoomPercent = 10;
    private const double MaxZoomPercent = 250;
    private const double ZoomStepPercent = 10;

    /// <summary>확대/축소 콤보박스 항목이자 Ctrl+스크롤이 오르내리는 단계 목록 (연속보기/단일보기 공유). 10%~250%, 10% 단위.</summary>
    public static readonly IReadOnlyList<ZoomOption> ZoomOptions = Enumerable.Range(1, 25)
        .Select(i => new ZoomOption($"{i * 10}%", i * 10))
        .ToList();

    /// <summary>연속보기 스크롤 배속 콤보박스 항목 (06-view-modes.md).</summary>
    public static readonly IReadOnlyList<ScrollSpeedOption> ScrollSpeedOptions = new List<ScrollSpeedOption>
    {
        new("x1", 1),
        new("x2", 2),
        new("x3", 3),
        new("x4", 4),
        new("x5", 5),
    };

    private readonly IImageStorageService _imageStorageService;
    private readonly IImageSourceProvider _imageSourceProvider;
    private CancellationTokenSource? _loadCts;
    private TopicNode? _currentMinorTopic;

    public ObservableCollection<ImageItem> Images { get; } = new();

    /// <summary>현재 확대/축소 배율(%). 100 = 원본 크기. 콤보박스와 Ctrl+스크롤이 값을 공유한다.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZoomScale))]
    private double _zoomPercent = 100;

    /// <summary>ZoomPercent를 LayoutTransform에 바로 쓸 수 있는 배율(1.0 = 100%)로 환산한 값.</summary>
    public double ZoomScale => ZoomPercent / 100.0;

    /// <summary>연속보기 마우스 휠 스크롤 배속. 기본 x1.</summary>
    [ObservableProperty]
    private int _scrollSpeedMultiplier = 1;

    [ObservableProperty]
    private bool _showNoSelectionMessage = true;

    [ObservableProperty]
    private bool _showEmptyMessage;

    [ObservableProperty]
    private bool _showImages;

    /// <summary>압축(zip) 소주제는 읽기 전용 — 추가/순서 변경/삭제가 비활성화된다 (03-data-storage.md).</summary>
    [ObservableProperty]
    private bool _isCurrentTopicReadOnly;

    /// <summary>연속보기/단일보기가 공유하는 "현재 이미지" 위치. 모드 전환 시 유지된다 (06-view-modes.md 공통 절).</summary>
    [ObservableProperty]
    private int _currentIndex;

    /// <summary>단일보기가 표시할 현재 이미지. Images나 CurrentIndex가 바뀌면 갱신된다.</summary>
    public ImageItem? CurrentItem =>
        CurrentIndex >= 0 && CurrentIndex < Images.Count ? Images[CurrentIndex] : null;

    /// <summary>
    /// 툴바에 표시/편집하는 1부터 시작하는 페이지 번호. 값을 입력하면 그 페이지로 이동한다 (07-ui-layout.md).
    /// </summary>
    public int CurrentPageNumber
    {
        get => Images.Count == 0 ? 0 : CurrentIndex + 1;
        set
        {
            if (Images.Count == 0)
            {
                return;
            }

            var clamped = Math.Clamp(value, 1, Images.Count);
            CurrentIndex = clamped - 1;
            // CurrentIndex가 실제로 안 바뀌었어도(예: 범위를 벗어난 값을 입력) 텍스트박스가
            // 유효한 값으로 되돌아오도록 항상 알린다.
            OnPropertyChanged(nameof(CurrentPageNumber));
            ScrollToIndexRequested?.Invoke(this, CurrentIndex);
        }
    }

    /// <summary>연속보기 화면(View)이 스크롤 위치를 맞추기 위해 구독하는 이벤트.</summary>
    public event EventHandler? ScrollToTopRequested;

    public event EventHandler? ScrollToBottomRequested;

    /// <summary>페이지 번호를 직접 입력해 이동했을 때, 연속보기가 그 위치로 스크롤하도록 알린다.</summary>
    public event EventHandler<int>? ScrollToIndexRequested;

    public ContinuousPageViewModel(IImageStorageService imageStorageService, IImageSourceProvider imageSourceProvider)
    {
        _imageStorageService = imageStorageService;
        _imageSourceProvider = imageSourceProvider;
    }

    [RelayCommand]
    private void ScrollToTop() => ScrollToTopRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ScrollToBottom() => ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);

    partial void OnCurrentIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CurrentItem));
        OnPropertyChanged(nameof(CurrentPageNumber));
    }

    /// <summary>연속보기에서 특정 이미지를 클릭했을 때 "현재 이미지"로 지정한다.</summary>
    public void SetCurrentIndex(ImageItem item)
    {
        var index = Images.IndexOf(item);
        if (index >= 0)
        {
            CurrentIndex = index;
        }
    }

    /// <summary>10%p 확대한다. 최대 250%.</summary>
    [RelayCommand]
    private void ZoomIn() => ZoomPercent = Math.Min(MaxZoomPercent, ZoomPercent + ZoomStepPercent);

    /// <summary>10%p 축소한다. 최소 10%.</summary>
    [RelayCommand]
    private void ZoomOut() => ZoomPercent = Math.Max(MinZoomPercent, ZoomPercent - ZoomStepPercent);

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
        IsCurrentTopicReadOnly = minorTopic?.IsArchive ?? false;
        _loadCts?.Cancel();
        Images.Clear();
        CurrentIndex = 0;
        ScrollToTopRequested?.Invoke(this, EventArgs.Empty);

        if (minorTopic is null)
        {
            ShowNoSelectionMessage = true;
            ShowEmptyMessage = false;
            ShowImages = false;
            OnPropertyChanged(nameof(CurrentItem));
            OnPropertyChanged(nameof(CurrentPageNumber));
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
        OnPropertyChanged(nameof(CurrentPageNumber));

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
                var image = await _imageSourceProvider.LoadAsync(item, token);
                if (!token.IsCancellationRequested)
                {
                    item.Image = image;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex) when (ex is IOException or NotSupportedException or FileFormatException or UnauthorizedAccessException or InvalidDataException)
            {
                // 디코딩할 수 없는 파일(폴더에 수동으로 넣어진 미지원 형식 등)은 해당 이미지만 건너뛴다.
            }
        }
    }

    /// <summary>드롭된 이미지 소스 목록을 추가한다 (로컬 파일/비트맵/가상 파일 스트림 등, 05-image-features.md 참조).</summary>
    public void AddDroppedImages(IReadOnlyList<ImageSourceInput> inputs)
    {
        // 1차 방어선은 View의 DragOver 차단(IsCurrentTopicReadOnly)이고, 여기는 안전망이다.
        if (_currentMinorTopic is null || _currentMinorTopic.IsArchive || inputs.Count == 0)
        {
            return;
        }

        ProcessAdd(inputs);
    }

    public void MoveImage(ImageItem draggedItem, ImageItem targetItem)
    {
        // 압축(zip) 소주제는 읽기 전용이라 드래그 재정렬은 조용히 무시한다.
        if (_currentMinorTopic is null || _currentMinorTopic.IsArchive)
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

        if (_currentMinorTopic.IsArchive)
        {
            MessageBox.Show(
                "압축 파일 안의 이미지는 삭제할 수 없습니다. 압축 파일은 읽기 전용으로 표시됩니다.",
                "삭제 불가",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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
            OnPropertyChanged(nameof(CurrentPageNumber));
        }
    }

    private void ProcessAdd(IReadOnlyList<ImageSourceInput> inputs)
    {
        var minorTopic = _currentMinorTopic!;
        var previousCount = Images.Count;
        var result = _imageStorageService.AddImages(minorTopic, inputs);

        // 목록 전체를 다시 그리면 스크롤 위치가 맨 위로 초기화되므로,
        // 새로 추가된 항목만 뒤에 이어붙인다 (기존 이미지 순서는 05-image-features.md대로 유지됨).
        var allItems = _imageStorageService.GetImages(minorTopic);
        var newItems = allItems.Skip(previousCount).ToList();

        foreach (var item in newItems)
        {
            Images.Add(item);
        }

        ShowNoSelectionMessage = false;
        ShowEmptyMessage = Images.Count == 0;
        ShowImages = Images.Count > 0;
        OnPropertyChanged(nameof(CurrentPageNumber));

        if (newItems.Count > 0)
        {
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            _ = LoadImagesAsync(newItems, _loadCts.Token);
        }

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
