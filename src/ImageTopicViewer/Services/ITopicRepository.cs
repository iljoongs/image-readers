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
}
