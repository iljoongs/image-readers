using System.IO;

namespace ImageTopicViewer.Services;

/// <summary>
/// 소주제 폴더(또는 압축 파일)가 담을 수 있는 이미지 파일 확장자 목록. 이미지 저장은 원본 형식을 따라가므로
/// (03-data-storage.md), 같은 폴더 안에도 여러 확장자가 섞일 수 있다.
/// </summary>
internal static class ImageFileExtensions
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff", ".tif",
        ".webp", ".jfif", ".ico", ".heic", ".heif", ".avif",
    };

    /// <summary>
    /// 폴더 내 이미지로 인식되는 모든 파일을 열거한다(06번 요청: "폴더 내 모든 이미지를 불러온다").
    /// 이 앱이 직접 저장하지 않는 형식(webp 등, 인코더 미지원)도 사용자가 수동으로 넣었을 수 있으므로
    /// 목록에는 포함하되, 디코딩 가능 여부는 실제 로드 시점에 판가름난다.
    /// </summary>
    public static IEnumerable<string> EnumerateImageFiles(string folderPath)
    {
        return Extensions.SelectMany(ext => Directory.GetFiles(folderPath, "*" + ext));
    }

    /// <summary>주어진 파일명(압축 파일 내부 엔트리 이름 등)이 인식하는 이미지 확장자인지 판별한다.</summary>
    public static bool IsImageFile(string fileName) => Extensions.Contains(Path.GetExtension(fileName));
}
