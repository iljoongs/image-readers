using ImageTopicViewer.Models;

namespace ImageTopicViewer.Services;

public interface IImageStorageService
{
    /// <summary>소주제 폴더의 이미지를 파일명(번호) 순서대로 반환한다.</summary>
    IReadOnlyList<ImageItem> GetImages(TopicNode minorTopic);

    /// <summary>
    /// 이미지를 디코딩하여 PNG로 저장한다(03-data-storage.md "지원 이미지 포맷 및 포맷 통일" 참조).
    /// 로컬 파일이 원본이면 저장 성공 후 원본을 삭제한다. 디코딩 실패한 항목은 건너뛰고 결과에 집계한다.
    /// </summary>
    ImageAddResult AddImages(TopicNode minorTopic, IReadOnlyList<ImageSourceInput> inputs);
}
