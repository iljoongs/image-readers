using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ImageTopicViewer.Services;

namespace ImageTopicViewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string NoSelectionMessage = "좌측에서 주제를 선택하세요";

    [ObservableProperty]
    private string _mainAreaMessage = NoSelectionMessage;

    public TopicTreeViewModel TopicTree { get; }

    public MainViewModel(ITopicRepository topicRepository)
    {
        TopicTree = new TopicTreeViewModel(topicRepository);
        TopicTree.PropertyChanged += OnTopicTreePropertyChanged;
    }

    private void OnTopicTreePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TopicTreeViewModel.SelectedNode))
        {
            return;
        }

        var node = TopicTree.SelectedNode;
        MainAreaMessage = node is { IsMajorTopic: false }
            ? $"선택된 소주제: {node.Name}\n경로: {node.FullPath}\n(이미지 표시는 M3에서 구현)"
            : NoSelectionMessage;
    }
}
