using ImageTopicViewer.Models;

namespace ImageTopicViewer.Services;

/// <summary>소주제 업데이트 날짜/시간 등 "관리" 데이터를 settings.json과 별도의 파일에서 읽고 쓴다
/// (04-topic-management.md "관리").</summary>
public interface IManagementService
{
    TopicManagementData Data { get; }

    void Save();
}
