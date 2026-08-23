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

    public MainViewModel(
        ITopicRepository topicRepository,
        IImageStorageService imageStorageService,
        IImageSourceProvider imageSourceProvider)
    {
        TopicTree = new TopicTreeViewModel(topicRepository);
        ContinuousPage = new ContinuousPageViewModel(imageStorageService, imageSourceProvider);

        TopicTree.PropertyChanged += OnTopicTreePropertyChanged;
    }

    partial void OnIsSingleViewChanged(bool value) => OnPropertyChanged(nameof(ViewModeToggleLabel));

    /// <summary>앱 시작 시 마지막 세션 상태를 복원한다 (02-architecture.md "세션 상태 저장/복원").</summary>
    public void RestoreSession(AppSettings settings)
    {
        TopicTree.SelectByName(settings.LastMajorTopicName, settings.LastMinorTopicName);
        IsSingleView = settings.LastIsSingleView;

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
    }
}
