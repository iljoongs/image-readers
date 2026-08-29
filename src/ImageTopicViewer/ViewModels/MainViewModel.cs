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

        TopicTree.SelectByName(settings.LastMajorTopicName, settings.LastMinorTopicName);
        IsSingleView = settings.LastIsSingleView;
        ContinuousPage.ZoomPercent = settings.LastZoomPercent;

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
        else
        {
            settings.LastMajorTopicName = null;
            settings.LastMinorTopicName = null;
        }

        settings.LastIsSingleView = IsSingleView;
        settings.LastImageIndex = ContinuousPage.CurrentIndex;
        settings.LastZoomPercent = ContinuousPage.ZoomPercent;
        settings.TopicProgress = CollectTopicProgress();
    }

    [RelayCommand]
    private void ToggleViewMode() => IsSingleView = !IsSingleView;

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
}
