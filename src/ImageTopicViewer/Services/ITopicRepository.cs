using System.Collections.ObjectModel;
using ImageTopicViewer.Models;

namespace ImageTopicViewer.Services;

public interface ITopicRepository
{
    ObservableCollection<TopicNode> GetTopics();

    /// <exception cref="ArgumentException">이름이 유효하지 않을 때 (doc/04 검증 규칙)</exception>
    TopicNode CreateMajorTopic(string name);

    /// <exception cref="ArgumentException">이름이 유효하지 않을 때 (doc/04 검증 규칙)</exception>
    TopicNode CreateMinorTopic(TopicNode majorTopic, string name);

    /// <summary>대주제 또는 소주제 폴더를 통째로 휴지통으로 이동한다 (04-topic-management.md 삭제 정책).</summary>
    void DeleteTopic(TopicNode node);

    /// <summary>
    /// 이름을 변경한다: 폴더 rename + (대주제인 경우) 하위 모든 소주제 파일명 prefix 일괄 재작성 (04-topic-management.md cascading).
    /// </summary>
    /// <exception cref="ArgumentException">이름이 유효하지 않을 때 (doc/04 검증 규칙)</exception>
    void RenameTopic(TopicNode node, string newName);
}
