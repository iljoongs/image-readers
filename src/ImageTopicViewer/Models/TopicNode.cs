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

    /// <summary>이 소주제에서 마지막으로 보던 이미지의 0부터 시작하는 인덱스. null이면 아직 본 적 없음 (07-ui-layout.md "주제 트리").</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressLabel))]
    private int? _viewedIndex;

    /// <summary>ViewedIndex 기록 당시 이 소주제의 전체 이미지 수.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressLabel))]
    private int _viewedTotalCount;

    /// <summary>트리에 이름 옆에 붙는 "(N/전체)" 표시. 본 적 없으면 빈 문자열.</summary>
    public string ProgressLabel => ViewedIndex.HasValue ? $" ({ViewedIndex.Value + 1}/{ViewedTotalCount})" : string.Empty;

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
