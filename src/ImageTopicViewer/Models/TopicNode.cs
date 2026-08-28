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

    /// <summary>true면 이 소주제는 폴더가 아니라 .zip 압축 파일이다 (03-data-storage.md "압축 파일 기반 소주제"). 대주제는 항상 false.</summary>
    public bool IsArchive { get; init; }

    public ObservableCollection<TopicNode> Children { get; } = new();

    public TopicNode(string name, string fullPath, bool isMajorTopic)
    {
        _name = name;
        FullPath = fullPath;
        IsMajorTopic = isMajorTopic;
    }
}
