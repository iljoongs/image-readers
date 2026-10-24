namespace ImageTopicViewer.Models;

/// <summary>`management.json`(settings.json과 별도 파일, `IManagementService`)에 저장되는 관리용 데이터
/// (04-topic-management.md "관리").</summary>
public class TopicManagementData
{
    /// <summary>소주제별 마지막 업데이트 날짜/시간. "{대주제}/{소주제}" 키(`AppSettings.TopicProgress`와 동일한
    /// 키 규칙). 이미지가 추가/삭제/순서변경될 때마다 갱신된다.</summary>
    public Dictionary<string, DateTime> MinorTopicUpdatedAt { get; set; } = new();
}
