using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
