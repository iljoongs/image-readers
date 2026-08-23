using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ImageTopicViewer.Services;

namespace ImageTopicViewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public TopicTreeViewModel TopicTree { get; }
    public ContinuousPageViewModel ContinuousPage { get; }

    public MainViewModel(
        ITopicRepository topicRepository,
        IImageStorageService imageStorageService,
        IImageSourceProvider imageSourceProvider)
    {
        TopicTree = new TopicTreeViewModel(topicRepository);
        ContinuousPage = new ContinuousPageViewModel(imageStorageService, imageSourceProvider);

        TopicTree.PropertyChanged += OnTopicTreePropertyChanged;
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
    }
}
