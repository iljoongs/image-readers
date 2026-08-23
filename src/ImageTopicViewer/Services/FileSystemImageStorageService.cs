using System.IO;
using System.Windows.Media.Imaging;
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
            .Select(path => new ImageItem(path, Path.GetFileName(path)))
            .ToList();
    }

    public ImageAddResult AddImages(TopicNode minorTopic, IReadOnlyList<ImageSourceInput> inputs)
    {
        Directory.CreateDirectory(minorTopic.FullPath);

        var majorTopicName = Directory.GetParent(minorTopic.FullPath)!.Name;
        var nextIndex = Directory.GetFiles(minorTopic.FullPath, "*.png").Length + 1;

        var succeeded = 0;
        var failed = 0;

        foreach (var input in inputs)
        {
            var bitmap = TryDecode(input);
            if (bitmap is null)
            {
                failed++;
                continue;
            }

            var fileName = $"{majorTopicName}_{minorTopic.Name}_{nextIndex:000}.png";
            var destinationPath = Path.Combine(minorTopic.FullPath, fileName);

            try
            {
                SaveAsPng(bitmap, destinationPath);
            }
            catch (IOException)
            {
                failed++;
                continue;
            }

            if (input is ImageSourceInput.FromFile fromFile)
            {
                // 원본을 옮긴 것과 같은 효과: 새 PNG가 이미 저장되었으므로 삭제 실패는 추가 실패로 취급하지 않는다.
                TryDeleteOriginal(fromFile.SourceFilePath);
            }

            nextIndex++;
            succeeded++;
        }

        return new ImageAddResult(succeeded, failed);
    }

    private static BitmapSource? TryDecode(ImageSourceInput input)
    {
        try
        {
            return input switch
            {
                ImageSourceInput.FromFile fromFile => DecodeFile(fromFile.SourceFilePath),
                ImageSourceInput.FromBitmap fromBitmap => fromBitmap.Bitmap,
                _ => null,
            };
        }
        catch (Exception ex) when (ex is NotSupportedException or FileFormatException or IOException or ArgumentException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static BitmapSource DecodeFile(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        frame.Freeze();
        return frame;
    }

    private static void SaveAsPng(BitmapSource bitmap, string destinationPath)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
        encoder.Save(stream);
    }

    private static void TryDeleteOriginal(string sourceFilePath)
    {
        try
        {
            File.Delete(sourceFilePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    public void Renumber(TopicNode minorTopic, IReadOnlyList<ImageItem> orderedItems)
    {
        var majorTopicName = Directory.GetParent(minorTopic.FullPath)!.Name;

        // 1단계: 번호 충돌 방지를 위해 전부 임시 파일명으로 변경 (03-data-storage.md).
        var tempPaths = new List<string>(orderedItems.Count);
        foreach (var item in orderedItems)
        {
            var tempPath = Path.Combine(minorTopic.FullPath, $".tmp_{Guid.NewGuid():N}.png");
            File.Move(item.FullPath, tempPath);
            tempPaths.Add(tempPath);
        }

        // 2단계: 새 순서대로 001, 002... 최종 파일명으로 변경.
        for (var i = 0; i < orderedItems.Count; i++)
        {
            var fileName = $"{majorTopicName}_{minorTopic.Name}_{(i + 1):000}.png";
            var finalPath = Path.Combine(minorTopic.FullPath, fileName);
            File.Move(tempPaths[i], finalPath);
            orderedItems[i].UpdatePath(finalPath, fileName);
        }
    }

    public void DeleteToRecycleBin(ImageItem item)
    {
        RecycleBin.Send(item.FullPath);
    }
}
