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

    /// <summary>연속보기 마우스 휠 스크롤 배속(x1~x10). 기본 x1 (06-view-modes.md "연속보기").</summary>
    public int LastScrollSpeedMultiplier { get; set; } = 1;

    /// <summary>
    /// 소주제별로 마지막으로 보던 이미지 위치를 기억한다 ("{대주제}/{소주제}" 키, 07-ui-layout.md "주제 트리").
    /// 이름을 변경하면 키가 달라져 기록이 끊기는 건 알려진 제약이다(세션 복원의 SelectByName과 동일한 한계).
    /// </summary>
    public Dictionary<string, TopicProgressEntry> TopicProgress { get; set; } = new();

    /// <summary>
    /// 대주제별 평점(1~10, 팝업 메뉴 "평점 설정" — 04-topic-management.md, 07-ui-layout.md). 대주제 이름을 키로 한다.
    /// TopicProgress와 동일하게, 이름을 변경하면 키가 달라져 기록이 끊기는 건 알려진 제약이다.
    /// </summary>
    public Dictionary<string, int> MajorTopicRating { get; set; } = new();

    /// <summary>대주제별 장르("/"로 이어붙인 문자열, 팝업 메뉴 "장르 설정" — 04-topic-management.md, 07-ui-layout.md "홈 화면").
    /// 대주제 이름을 키로 한다. 위 평점과 동일한 이름 변경 제약이 있다.</summary>
    public Dictionary<string, string> MajorTopicGenre { get; set; } = new();

    /// <summary>대주제별 커버(타이틀) 이미지 경로(`CoverImageStore`가 covers/ 폴더에 복사한 파일, 07-ui-layout.md "홈 화면").
    /// 대주제 이름을 키로 한다. 위 평점과 동일한 이름 변경 제약이 있다.</summary>
    public Dictionary<string, string> MajorTopicCoverImagePath { get; set; } = new();

    /// <summary>대주제별로 가장 최근에 선택했던 소주제 이름(2026-10-19 추가). 대주제 이름을 키로 하며, 홈 화면
    /// 카드를 클릭했을 때 이 소주제로 바로 이동하는 데 쓴다(07-ui-layout.md "홈 화면"). 위 평점과 동일한 이름
    /// 변경 제약이 있다. 이 필드가 생기기 전에는 기록이 없었으므로, 그런 대주제는 처음 소주제를 열 때부터
    /// 새로 쌓인다.</summary>
    public Dictionary<string, string> MajorTopicLastMinorTopic { get; set; } = new();
}

public class TopicProgressEntry
{
    /// <summary>0부터 시작하는 마지막 조회 인덱스.</summary>
    public int Index { get; set; }

    /// <summary>기록 당시 그 소주제의 전체 이미지 수.</summary>
    public int Count { get; set; }
}
