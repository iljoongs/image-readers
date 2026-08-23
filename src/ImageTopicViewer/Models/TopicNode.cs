using System.Collections.ObjectModel;

namespace ImageTopicViewer.Models;

public class TopicNode
{
    public required string Name { get; set; }
    public required string FullPath { get; set; }
    public bool IsMajorTopic { get; init; }
    public ObservableCollection<TopicNode> Children { get; } = new();
}
