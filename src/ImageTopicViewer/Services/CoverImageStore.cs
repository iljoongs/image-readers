using System.IO;

namespace ImageTopicViewer.Services;

/// <summary>대주제 커버(타이틀) 이미지를 설정 폴더 하위 covers/에 복사해 보관한다 (02-architecture.md).
/// 사용자가 고른 원본 파일은 외부 어디에나 있을 수 있어, 삭제/이동되어도 끊기지 않도록 앱 소유 폴더로 복사해 둔다.</summary>
public static class CoverImageStore
{
    private static readonly string CoversDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ImageTopicViewer",
        "covers");

    /// <summary>원본 이미지 파일을 covers/ 폴더로 복사하고 새 경로를 반환한다. 파일명 충돌을 피하려 GUID를 쓴다.</summary>
    public static string Save(string sourceFilePath)
    {
        Directory.CreateDirectory(CoversDirectory);

        var extension = Path.GetExtension(sourceFilePath);
        var destinationPath = Path.Combine(CoversDirectory, $"{Guid.NewGuid():N}{extension}");
        File.Copy(sourceFilePath, destinationPath, overwrite: true);

        return destinationPath;
    }

    /// <summary>더 이상 참조되지 않는 이전 커버 파일을 조용히 정리한다(실패해도 앱 동작에 영향 없음).</summary>
    public static void TryDelete(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
