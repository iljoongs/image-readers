using System.ComponentModel;
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

    public string ViewModeToggleLabel => IsSingleView ? "연속보기로 전환" : "단일보기로 전환";

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
    }

    partial void OnIsSingleViewChanged(bool value)
    {
        OnPropertyChanged(nameof(ViewModeToggleLabel));
        OnPropertyChanged(nameof(WindowTitle));
    }

    /// <summary>앱 시작 시 마지막 세션 상태를 복원한다 (02-architecture.md "세션 상태 저장/복원").</summary>
    public void RestoreSession(AppSettings settings)
    {
        ApplyTopicProgress(settings.TopicProgress);
        ApplyMajorTopicRating(settings.MajorTopicRating);
        ApplyMajorTopicGenre(settings.MajorTopicGenre);
        ApplyMajorTopicCoverImagePath(settings.MajorTopicCoverImagePath);

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
        settings.MajorTopicCoverImagePath = CollectMajorTopicCoverImagePath();
    }

    [RelayCommand]
    private void ToggleViewMode() => IsSingleView = !IsSingleView;

    [RelayCommand]
    private void GoHome() => IsHomeScreen = true;

    /// <summary>홈 화면 카드를 클릭해 해당 대주제를 연다(트리에서 선택+펼침, 홈 화면은 숨긴다).</summary>
    [RelayCommand]
    private void OpenMajorTopic(TopicNode? node)
    {
        if (node is not { IsMajorTopic: true })
        {
            return;
        }

        TopicTree.SelectedNode = node;
        IsHomeScreen = false;
    }

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

    private void ApplyMajorTopicCoverImagePath(Dictionary<string, string>? coverPaths)
    {
        if (coverPaths is null || coverPaths.Count == 0)
        {
            return;
        }

        foreach (var major in TopicTree.Topics)
        {
            if (coverPaths.TryGetValue(major.Name, out var value))
            {
                major.CoverImagePath = value;
            }
        }
    }

    private Dictionary<string, string> CollectMajorTopicCoverImagePath() =>
        TopicTree.Topics
            .Where(major => !string.IsNullOrEmpty(major.CoverImagePath))
            .ToDictionary(major => major.Name, major => major.CoverImagePath!);
}
