using System.IO;
using ImageTopicViewer.Models;

namespace ImageTopicViewer.Services;

public class FileSystemImageStorageService : IImageStorageService
{
    public IReadOnlyList<ImageItem> GetImages(TopicNode minorTopic)
    {
        if (!Directory.Exists(minorTopic.FullPath))
        {
            return Array.Empty<ImageItem>();
        }

        // 파일명 규칙(03-data-storage.md): {대주제}_{소주제}_{3자리 번호}.png → 문자열 정렬이 곧 번호 순서.
        return Directory.GetFiles(minorTopic.FullPath, "*.png")
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .Select(path => new ImageItem { FullPath = path, FileName = Path.GetFileName(path) })
            .ToList();
    }
}
