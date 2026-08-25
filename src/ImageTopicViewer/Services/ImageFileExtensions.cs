using System.IO;

namespace ImageTopicViewer.Services;

/// <summary>
/// 소주제 폴더가 담을 수 있는 이미지 파일 확장자 목록. 이미지 저장은 원본 형식을 따라가므로
/// (03-data-storage.md), 같은 폴더 안에도 여러 확장자가 섞일 수 있다.
/// </summary>
internal static class ImageFileExtensions
{
    private static readonly string[] SearchPatterns =
    {
        "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.tiff", "*.tif",
    };

    public static IEnumerable<string> EnumerateImageFiles(string folderPath)
    {
        return SearchPatterns.SelectMany(pattern => Directory.GetFiles(folderPath, pattern));
    }
}
