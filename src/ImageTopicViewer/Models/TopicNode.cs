using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageTopicViewer.Models;

public partial class TopicNode : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    public string FullPath { get; set; }
    public bool IsMajorTopic { get; init; }
    public ObservableCollection<TopicNode> Children { get; } = new();

    public TopicNode(string name, string fullPath, bool isMajorTopic)
    {
        _name = name;
        FullPath = fullPath;
        IsMajorTopic = isMajorTopic;
    }
}
