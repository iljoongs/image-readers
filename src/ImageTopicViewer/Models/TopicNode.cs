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

    /// <summary>대주제 평점(1~10). 팝업 메뉴 "평점 설정"으로 바꾼다 — progress bar 모양의 채움 정도와 색을 결정한다
    /// (07-ui-layout.md "좌측 패널"). 소주제는 사용하지 않는다.</summary>
    [ObservableProperty]
    private int _rating = 1;

    /// <summary>대주제 커버(타이틀) 이미지 파일의 절대 경로. 팝업 메뉴 "커버 이미지 설정"으로 지정하며,
    /// 선택한 파일은 %APPDATA%\ImageTopicViewer\covers\에 복사되어 보관된다(`CoverImageStore`). null이면 미지정.
    /// 홈 화면 카드에 쓰인다 (07-ui-layout.md "홈 화면"). 소주제는 사용하지 않는다.</summary>
    [ObservableProperty]
    private string? _coverImagePath;

    /// <summary>대주제 장르. 팝업 메뉴 "장르 설정"의 미리 정의된 목록(복수 선택 가능)에서 고르며 "/"로 이어붙인다
    /// (예: "판타지/액션"). 홈 화면 카드에 표시된다. 소주제는 사용하지 않는다.</summary>
    [ObservableProperty]
    private string? _genre;

    /// <summary>이 대주제에서 가장 최근에 선택했던 소주제 이름. 홈 화면 카드를 클릭하면 이 소주제로 바로
    /// 이동한다(null이면 대주제만 펼친다) — `MainViewModel`이 소주제 선택 때마다 갱신한다 (07-ui-layout.md
    /// "홈 화면"). 소주제는 사용하지 않는다.</summary>
    [ObservableProperty]
    private string? _lastViewedMinorName;

    public string FullPath { get; set; }
    public bool IsMajorTopic { get; init; }

    /// <summary>true면 이 소주제는 폴더가 아니라 .zip 압축 파일이다 (03-data-storage.md "압축 파일 기반 소주제"). 대주제는 항상 false.</summary>
    public bool IsArchive { get; init; }

    public ObservableCollection<TopicNode> Children { get; } = new();

    /// <summary>하위 소주제 총 개수. 홈 화면 카드에 "N화"로 표시된다(07-ui-layout.md "홈 화면").</summary>
    public int EpisodeCount => Children.Count;

    public TopicNode(string name, string fullPath, bool isMajorTopic)
    {
        _name = name;
        FullPath = fullPath;
        IsMajorTopic = isMajorTopic;
        Children.CollectionChanged += (_, _) => OnPropertyChanged(nameof(EpisodeCount));
    }
}
