using System.IO;

namespace ImageTopicViewer.Services;

/// <summary>대주제 썸네일 이미지를 설정 폴더 하위 thumbnails/에 원본 크기 그대로 복사해 보관한다
/// (02-architecture.md, 07-ui-layout.md "홈 화면"). 리사이즈본은 만들지 않고, 화면에는 항상 고정 Width/Height +
/// `ImageLoadHelper`의 DecodePixelWidth로 스케일해서 보여준다 — game-platform 프로젝트의 `ThumbnailHelper`가
/// 쓰는 방식(원본 그대로 복사 + "{baseName}.original{확장자}" 이름)을 그대로 가져왔다.
/// 사용자가 고른 원본 파일은 외부 어디에나 있을 수 있어, 삭제/이동되어도 끊기지 않도록 앱 소유 폴더로 복사해 둔다.</summary>
public static class ThumbnailStore
{
    private static readonly string ThumbnailsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ImageTopicViewer",
        "thumbnails");

    /// <summary>원본 이미지 파일을 thumbnails/ 폴더에 "{대주제 이름}.original{확장자}"로 복사하고 경로를 반환한다.
    /// 대주제 이름은 이미 Windows 폴더/파일명 규칙을 통과한 값이라(`TopicNameValidator`) 파일명으로 그대로
    /// 쓸 수 있다. 같은 대주제의 썸네일을 다른 확장자로 바꿔 끼우면 이전 확장자 파일은 지운다.</summary>
    /// <param name="deleteSource">true면 복사가 끝난 <paramref name="sourceFilePath"/>를 삭제한다 — 드래그 앤
    /// 드롭(웹 이미지 다운로드/data: URI/렌더링된 비트맵)으로 만들어진 임시 파일 정리용으로만 true를 넘겨야
    /// 한다. 사용자가 직접 고른 로컬 파일(`DragDropImageHelper.TryGetImagePath`의 isTemporary가 false, 또는
    /// 파일 선택 대화상자)은 반드시 false로 호출해 원본을 건드리지 않는다.</param>
    public static string Save(string sourceFilePath, string majorTopicName, bool deleteSource = false)
    {
        Directory.CreateDirectory(ThumbnailsDirectory);

        var extension = Path.GetExtension(sourceFilePath);
        if (string.IsNullOrEmpty(extension))
        {
            extension = ".jpg";
        }

        var destinationPath = Path.Combine(ThumbnailsDirectory, $"{majorTopicName}.original{extension}");
        DeleteStaleThumbnailVariants(majorTopicName, keep: destinationPath);

        var isSourceSameAsDest = string.Equals(
            Path.GetFullPath(sourceFilePath), Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase);

        if (!isSourceSameAsDest)
        {
            File.Copy(sourceFilePath, destinationPath, overwrite: true);
            if (deleteSource)
            {
                TryDeleteSource(sourceFilePath);
            }
        }

        return destinationPath;
    }

    /// <summary>대주제 이름이 바뀌었을 때, 이전 이름으로 저장된 썸네일 파일을 새 이름으로 옮긴다. 이전
    /// 썸네일이 없으면 아무 일도 하지 않는다. 성공하면 새 경로를, 없으면 null을 반환한다.</summary>
    public static string? Rename(string? currentThumbnailPath, string oldMajorTopicName, string newMajorTopicName)
    {
        if (string.IsNullOrEmpty(currentThumbnailPath) || !File.Exists(currentThumbnailPath))
        {
            return null;
        }

        var extension = Path.GetExtension(currentThumbnailPath);
        var newPath = Path.Combine(ThumbnailsDirectory, $"{newMajorTopicName}.original{extension}");

        try
        {
            File.Move(currentThumbnailPath, newPath, overwrite: true);
            return newPath;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static void DeleteStaleThumbnailVariants(string majorTopicName, string keep)
    {
        if (!Directory.Exists(ThumbnailsDirectory))
        {
            return;
        }

        foreach (var existing in Directory.GetFiles(ThumbnailsDirectory, $"{majorTopicName}.original.*"))
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

    private static void TryDeleteSource(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
