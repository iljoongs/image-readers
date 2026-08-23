using ImageTopicViewer.Models;

namespace ImageTopicViewer.Services;

public interface IImageStorageService
{
    /// <summary>소주제 폴더의 이미지를 파일명(번호) 순서대로 반환한다.</summary>
    IReadOnlyList<ImageItem> GetImages(TopicNode minorTopic);
}
