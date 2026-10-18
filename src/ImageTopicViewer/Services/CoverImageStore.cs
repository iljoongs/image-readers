using System.IO;

namespace ImageTopicViewer.Services;

/// <summary>대주제 커버(타이틀) 이미지를 설정 폴더 하위 covers/에 원본 크기 그대로 복사해 보관한다
/// (02-architecture.md, 07-ui-layout.md "홈 화면"). 리사이즈본은 만들지 않고, 화면에는 항상 고정 Width/Height +
/// `ImageLoadHelper`의 DecodePixelWidth로 스케일해서 보여준다 — game-platform 프로젝트의 `ThumbnailHelper`가
/// 쓰는 방식(원본 그대로 복사 + "{baseName}.original{확장자}" 이름)을 그대로 가져왔다.
/// 사용자가 고른 원본 파일은 외부 어디에나 있을 수 있어, 삭제/이동되어도 끊기지 않도록 앱 소유 폴더로 복사해 둔다.</summary>
public static class CoverImageStore
{
    private static readonly string CoversDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ImageTopicViewer",
        "covers");

    /// <summary>원본 이미지 파일을 covers/ 폴더에 "{대주제 이름}.original{확장자}"로 복사하고 경로를 반환한다.
    /// 대주제 이름은 이미 Windows 폴더/파일명 규칙을 통과한 값이라(`TopicNameValidator`) 파일명으로 그대로
    /// 쓸 수 있다. 같은 대주제의 커버를 다른 확장자로 바꿔 끼우면 이전 확장자 파일은 지운다.</summary>
    public static string Save(string sourceFilePath, string majorTopicName)
    {
        Directory.CreateDirectory(CoversDirectory);

        var extension = Path.GetExtension(sourceFilePath);
        if (string.IsNullOrEmpty(extension))
        {
            extension = ".jpg";
        }

        var destinationPath = Path.Combine(CoversDirectory, $"{majorTopicName}.original{extension}");
        DeleteStaleCoverVariants(majorTopicName, keep: destinationPath);

        File.Copy(sourceFilePath, destinationPath, overwrite: true);
        return destinationPath;
    }

    /// <summary>대주제 이름이 바뀌었을 때, 이전 이름으로 저장된 커버 파일을 새 이름으로 옮긴다. 이전 커버가
    /// 없으면 아무 일도 하지 않는다. 성공하면 새 경로를, 없으면 null을 반환한다.</summary>
    public static string? Rename(string? currentCoverPath, string oldMajorTopicName, string newMajorTopicName)
    {
        if (string.IsNullOrEmpty(currentCoverPath) || !File.Exists(currentCoverPath))
        {
            return null;
        }

        var extension = Path.GetExtension(currentCoverPath);
        var newPath = Path.Combine(CoversDirectory, $"{newMajorTopicName}.original{extension}");

        try
        {
            File.Move(currentCoverPath, newPath, overwrite: true);
            return newPath;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static void DeleteStaleCoverVariants(string majorTopicName, string keep)
    {
        if (!Directory.Exists(CoversDirectory))
        {
            return;
        }

        foreach (var existing in Directory.GetFiles(CoversDirectory, $"{majorTopicName}.original.*"))
        {
            if (string.Equals(existing, keep, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                File.Delete(existing);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
