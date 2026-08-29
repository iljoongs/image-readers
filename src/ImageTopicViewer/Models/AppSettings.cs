namespace ImageTopicViewer.Models;

public class AppSettings
{
    public string? DataFolderPath { get; set; }

    // 창 상태 (02-architecture.md "세션 상태 저장/복원")
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public bool WindowMaximized { get; set; }

    // 마지막 세션 상태
    public string? LastMajorTopicName { get; set; }
    public string? LastMinorTopicName { get; set; }
    public bool LastIsSingleView { get; set; }
    public int LastImageIndex { get; set; }

    /// <summary>확대/축소 배율(%). 100 = 원본 크기 (06-view-modes.md "확대/축소").</summary>
    public double LastZoomPercent { get; set; } = 100;

    /// <summary>
    /// 소주제별로 마지막으로 보던 이미지 위치를 기억한다 ("{대주제}/{소주제}" 키, 07-ui-layout.md "주제 트리").
    /// 이름을 변경하면 키가 달라져 기록이 끊기는 건 알려진 제약이다(세션 복원의 SelectByName과 동일한 한계).
    /// </summary>
    public Dictionary<string, TopicProgressEntry> TopicProgress { get; set; } = new();
}

public class TopicProgressEntry
{
    /// <summary>0부터 시작하는 마지막 조회 인덱스.</summary>
    public int Index { get; set; }

    /// <summary>기록 당시 그 소주제의 전체 이미지 수.</summary>
    public int Count { get; set; }
}
