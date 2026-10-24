using System.ComponentModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageTopicViewer.Models;
using ImageTopicViewer.Services;

namespace ImageTopicViewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public TopicTreeViewModel TopicTree { get; }
    public ContinuousPageViewModel ContinuousPage { get; }

    [ObservableProperty]
    private bool _isSingleView;

    /// <summary>true면 홈 화면(대주제 카드 그리드)을 보여준다. 앱 실행 시 항상 true로 시작한다 (07-ui-layout.md "홈 화면").</summary>
    [ObservableProperty]
    private bool _isHomeScreen = true;

    /// <summary>홈 화면 카드 확대/축소 배율(%). 185x260이 100%다. `EdgeZoomLevels`(Microsoft Edge 단계)를 그대로
    /// 쓴다 (07-ui-layout.md "홈 화면"). 세션 간 저장하지 않고 앱을 실행할 때마다 100%로 시작한다.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HomeZoomScale))]
    private double _homeZoomPercent = 100;

    /// <summary>LayoutTransform에 바로 바인딩하는 배율(1.0 = 100%).</summary>
    public double HomeZoomScale => HomeZoomPercent / 100.0;

    /// <summary>Ctrl+스크롤로 홈 화면 배율을 바꾸는 동안/직후 잠깐 떠 있는 배율 팝업(Microsoft Edge 스타일)의
    /// 표시 여부 (07-ui-layout.md "홈 화면").</summary>
    [ObservableProperty]
    private bool _isHomeZoomIndicatorVisible;

    private readonly DispatcherTimer _homeZoomIndicatorTimer;

    /// <summary>상단 메뉴 "보기 > 이어보기"가 선택되어 있는지. "단일보기"는 IsSingleView 그대로 쓴다
    /// (07-ui-layout.md "상단 메뉴").</summary>
    public bool IsContinuousView => !IsSingleView;

    /// <summary>타이틀바에 현재 뷰 모드를 표시한다.</summary>
    public string WindowTitle => $"ImageTopicViewer ({(IsSingleView ? "단일보기" : "연속보기")})";

    public MainViewModel(
        ITopicRepository topicRepository,
        IImageStorageService imageStorageService,
        IImageSourceProvider imageSourceProvider)
    {
        TopicTree = new TopicTreeViewModel(topicRepository);
        ContinuousPage = new ContinuousPageViewModel(imageStorageService, imageSourceProvider);

        TopicTree.PropertyChanged += OnTopicTreePropertyChanged;
        ContinuousPage.PropertyChanged += OnContinuousPagePropertyChanged;

        _homeZoomIndicatorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _homeZoomIndicatorTimer.Tick += (_, _) =>
        {
            _homeZoomIndicatorTimer.Stop();
            IsHomeZoomIndicatorVisible = false;
        };
    }

    partial void OnIsSingleViewChanged(bool value)
    {
        OnPropertyChanged(nameof(IsContinuousView));
        OnPropertyChanged(nameof(WindowTitle));
    }

    /// <summary>앱 시작 시 마지막 세션 상태를 복원한다 (02-architecture.md "세션 상태 저장/복원").</summary>
    public void RestoreSession(AppSettings settings)
    {
        ApplyTopicProgress(settings.TopicProgress);
        ApplyMajorTopicRating(settings.MajorTopicRating);
        ApplyMajorTopicGenre(settings.MajorTopicGenre);
        ApplyMajorTopicThumbnailPath(settings.MajorTopicThumbnailPath);
        ApplyMajorTopicLastMinorTopic(settings.MajorTopicLastMinorTopic);

        TopicTree.SelectByName(settings.LastMajorTopicName, settings.LastMinorTopicName);
        IsSingleView = settings.LastIsSingleView;
        ContinuousPage.ZoomPercent = settings.LastZoomPercent;
        ContinuousPage.ScrollSpeedMultiplier = settings.LastScrollSpeedMultiplier;

        if (ContinuousPage.Images.Count > 0)
        {
            ContinuousPage.CurrentIndex = Math.Clamp(settings.LastImageIndex, 0, ContinuousPage.Images.Count - 1);
        }
    }

    /// <summary>앱 종료 시 현재 상태를 설정 객체에 기록한다 (호출자가 저장을 수행한다).</summary>
    public void CaptureSession(AppSettings settings)
    {
        var selected = TopicTree.SelectedNode;
        if (selected is { IsMajorTopic: false })
        {
            var major = TopicTree.Topics.FirstOrDefault(t => t.Children.Contains(selected));
            settings.LastMajorTopicName = major?.Name;
            settings.LastMinorTopicName = selected.Name;
        }
        else if (selected is { IsMajorTopic: true })
        {
            settings.LastMajorTopicName = selected.Name;
            settings.LastMinorTopicName = null;
        }
        else
        {
            settings.LastMajorTopicName = null;
            settings.LastMinorTopicName = null;
        }

        settings.LastIsSingleView = IsSingleView;
        settings.LastImageIndex = ContinuousPage.CurrentIndex;
        settings.LastZoomPercent = ContinuousPage.ZoomPercent;
        settings.LastScrollSpeedMultiplier = ContinuousPage.ScrollSpeedMultiplier;
        settings.TopicProgress = CollectTopicProgress();
        settings.MajorTopicRating = CollectMajorTopicRating();
        settings.MajorTopicGenre = CollectMajorTopicGenre();
        settings.MajorTopicThumbnailPath = CollectMajorTopicThumbnailPath();
        settings.MajorTopicLastMinorTopic = CollectMajorTopicLastMinorTopic();
    }

    [RelayCommand]
    private void SetSingleView() => IsSingleView = true;

    [RelayCommand]
    private void SetContinuousView() => IsSingleView = false;

    [RelayCommand]
    private void GoHome() => IsHomeScreen = true;

    /// <summary>홈 화면 카드를 클릭해 해당 대주제를 연다 — 최근에 보던 소주제가 있으면 그 소주제로 바로
    /// 이동하고, 없으면 대주제만 펼친다(홈 화면은 숨긴다). 07-ui-layout.md "홈 화면" 참조.</summary>
    [RelayCommand]
    private void OpenMajorTopic(TopicNode? node)
    {
        if (node is not { IsMajorTopic: true })
        {
            return;
        }

        var lastMinor = node.LastViewedMinorName is { } name
            ? node.Children.FirstOrDefault(c => c.Name == name)
            : null;

        node.IsExpanded = true;
        TopicTree.SelectedNode = lastMinor ?? node;
        IsHomeScreen = false;
    }

    /// <summary>홈 화면의 Ctrl+스크롤 확대/축소 — Edge 단계 목록에서 한 칸 위/아래로 이동한다.</summary>
    public void AdjustHomeZoom(int direction)
    {
        HomeZoomPercent = EdgeZoomLevels.Step(HomeZoomPercent, direction);

        IsHomeZoomIndicatorVisible = true;
        _homeZoomIndicatorTimer.Stop();
        _homeZoomIndicatorTimer.Start();
    }

    [RelayCommand]
    private void ZoomHomeIn() => AdjustHomeZoom(1);

    [RelayCommand]
    private void ZoomHomeOut() => AdjustHomeZoom(-1);

    private void OnTopicTreePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TopicTreeViewModel.SelectedNode))
        {
            return;
        }

        var node = TopicTree.SelectedNode;
        var minorTopic = node is { IsMajorTopic: false } ? node : null;
        ContinuousPage.LoadSubtopic(minorTopic);
        UpdateCurrentTopicProgress();
        RememberLastViewedMinorTopic(minorTopic);
    }

    /// <summary>소주제를 선택할 때마다 그 부모 대주제에 "최근에 본 소주제"로 기록한다 — 홈 화면 카드를
    /// 클릭했을 때 이 소주제로 바로 이동하는 데 쓴다 (07-ui-layout.md "홈 화면").</summary>
    private void RememberLastViewedMinorTopic(TopicNode? minorTopic)
    {
        if (minorTopic is null)
        {
            return;
        }

        var major = TopicTree.Topics.FirstOrDefault(t => t.Children.Contains(minorTopic));
        if (major is not null)
        {
            major.LastViewedMinorName = minorTopic.Name;
        }
    }

    private void OnContinuousPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ContinuousPageViewModel.CurrentIndex))
        {
            UpdateCurrentTopicProgress();
        }
    }

    /// <summary>현재 선택된 소주제 노드에 "본 위치"를 기록해 트리 라벨에 즉시 반영한다 (07-ui-layout.md).</summary>
    private void UpdateCurrentTopicProgress()
    {
        var node = TopicTree.SelectedNode;
        if (node is not { IsMajorTopic: false } || ContinuousPage.Images.Count == 0)
        {
            return;
        }

        node.ViewedIndex = ContinuousPage.CurrentIndex;
        node.ViewedTotalCount = ContinuousPage.Images.Count;
    }

    private static string GetProgressKey(TopicNode major, TopicNode minor) => $"{major.Name}/{minor.Name}";

    private void ApplyTopicProgress(Dictionary<string, TopicProgressEntry>? progress)
    {
        if (progress is null || progress.Count == 0)
        {
            return;
        }

        foreach (var major in TopicTree.Topics)
        {
            foreach (var minor in major.Children)
            {
                if (progress.TryGetValue(GetProgressKey(major, minor), out var entry))
                {
                    minor.ViewedIndex = entry.Index;
                    minor.ViewedTotalCount = entry.Count;
                }
            }
        }
    }

    private Dictionary<string, TopicProgressEntry> CollectTopicProgress()
    {
        var result = new Dictionary<string, TopicProgressEntry>();

        foreach (var major in TopicTree.Topics)
        {
            foreach (var minor in major.Children)
            {
                if (minor.ViewedIndex is { } index)
                {
                    result[GetProgressKey(major, minor)] = new TopicProgressEntry { Index = index, Count = minor.ViewedTotalCount };
                }
            }
        }

        return result;
    }

    private void ApplyMajorTopicRating(Dictionary<string, int>? rating)
    {
        if (rating is null || rating.Count == 0)
        {
            return;
        }

        foreach (var major in TopicTree.Topics)
        {
            if (rating.TryGetValue(major.Name, out var value))
            {
                major.Rating = Math.Clamp(value, 1, 10);
            }
        }
    }

    private Dictionary<string, int> CollectMajorTopicRating() =>
        TopicTree.Topics.ToDictionary(major => major.Name, major => major.Rating);

    private void ApplyMajorTopicGenre(Dictionary<string, string>? genres)
    {
        if (genres is null || genres.Count == 0)
        {
            return;
        }

        foreach (var major in TopicTree.Topics)
        {
            if (genres.TryGetValue(major.Name, out var value))
            {
                major.Genre = value;
            }
        }
    }

    private Dictionary<string, string> CollectMajorTopicGenre() =>
        TopicTree.Topics
            .Where(major => !string.IsNullOrEmpty(major.Genre))
            .ToDictionary(major => major.Name, major => major.Genre!);

    private void ApplyMajorTopicThumbnailPath(Dictionary<string, string>? thumbnailPaths)
    {
        if (thumbnailPaths is null || thumbnailPaths.Count == 0)
        {
            return;
        }

        foreach (var major in TopicTree.Topics)
        {
            if (thumbnailPaths.TryGetValue(major.Name, out var value))
            {
                major.ThumbnailPath = value;
            }
        }
    }

    private Dictionary<string, string> CollectMajorTopicThumbnailPath() =>
        TopicTree.Topics
            .Where(major => !string.IsNullOrEmpty(major.ThumbnailPath))
            .ToDictionary(major => major.Name, major => major.ThumbnailPath!);

    private void ApplyMajorTopicLastMinorTopic(Dictionary<string, string>? lastMinorByMajor)
    {
        if (lastMinorByMajor is null || lastMinorByMajor.Count == 0)
        {
            return;
        }

        foreach (var major in TopicTree.Topics)
        {
            if (lastMinorByMajor.TryGetValue(major.Name, out var value))
            {
                major.LastViewedMinorName = value;
            }
        }
    }

    private Dictionary<string, string> CollectMajorTopicLastMinorTopic() =>
        TopicTree.Topics
            .Where(major => !string.IsNullOrEmpty(major.LastViewedMinorName))
            .ToDictionary(major => major.Name, major => major.LastViewedMinorName!);
}
