using System.Windows.Media;

namespace ImageTopicViewer.Services;

/// <summary>
/// 뷰에 표시할 이미지 소스를 비동기로 제공. v1은 원본 파일을 그대로 로드하지만,
/// 이 인터페이스 뒤에서 동작하므로 나중에 썸네일 캐싱으로 교체할 수 있다.
/// (doc/02-architecture.md, doc/08-open-decisions.md 참조)
/// </summary>
public interface IImageSourceProvider
{
    Task<ImageSource> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}
