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

        // 읽기 순서는 파일명에 포함된 숫자를 실제 값으로 비교하는 자연 정렬을 따른다(03-data-storage.md).
        // 예: "2.jpg"가 "10.jpg"보다 앞에 오도록 — 단순 문자열 정렬이면 반대로 정렬된다.
        return ImageFileExtensions.EnumerateImageFiles(minorTopic.FullPath)
            .OrderBy(Path.GetFileName, NaturalStringComparer.Instance)
            .Select(path => new ImageItem(path, Path.GetFileName(path)))
            .ToList();
    }

    public ImageAddResult AddImages(TopicNode minorTopic, IReadOnlyList<ImageSourceInput> inputs)
    {
        Directory.CreateDirectory(minorTopic.FullPath);

        var majorTopicName = Directory.GetParent(minorTopic.FullPath)!.Name;
        var nextIndex = GetNextIndex(minorTopic.FullPath);

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

            // 이미지 저장은 원본 형식을 따라간다 (03-data-storage.md). 인코더가 없는 형식/원본 정보 없음은 PNG로 대체.
            var (encoder, extension) = CreateEncoder(GetOriginalExtension(input));
            var fileName = $"{majorTopicName}_{minorTopic.Name}_{nextIndex:000}{extension}";
            var destinationPath = Path.Combine(minorTopic.FullPath, fileName);

            try
            {
                SaveWithEncoder(bitmap, encoder, destinationPath);
            }
            catch (IOException)
            {
                failed++;
                continue;
            }

            if (input is ImageSourceInput.FromFile fromFile)
            {
                // 원본을 옮긴 것과 같은 효과: 새 파일이 이미 저장되었으므로 삭제 실패는 추가 실패로 취급하지 않는다.
                TryDeleteOriginal(fromFile.SourceFilePath);
            }

            nextIndex++;
            succeeded++;
        }

        return new ImageAddResult(succeeded, failed);
    }

    /// <summary>
    /// 기존 파일명 끝의 "_번호"를 찾아 그중 최댓값+1을 다음 번호로 쓴다(파일 개수만으로 계산하면,
    /// 이미 저장된 이름을 그대로 두는 정책상 번호가 비어있거나 관례를 따르지 않는 파일이 섞여 있을 때
    /// 기존 파일과 같은 이름이 생성되어 덮어써버릴 수 있다).
    /// </summary>
    private static int GetNextIndex(string folderPath)
    {
        var maxExisting = ImageFileExtensions.EnumerateImageFiles(folderPath)
            .Select(path => GetTrailingNumber(Path.GetFileNameWithoutExtension(path)))
            .DefaultIfEmpty(0)
            .Max();

        return maxExisting + 1;
    }

    private static int GetTrailingNumber(string fileNameWithoutExtension)
    {
        var lastUnderscore = fileNameWithoutExtension.LastIndexOf('_');
        if (lastUnderscore < 0)
        {
            return 0;
        }

        var numberPart = fileNameWithoutExtension[(lastUnderscore + 1)..];
        return int.TryParse(numberPart, out var number) ? number : 0;
    }

    private static string GetOriginalExtension(ImageSourceInput input) => input switch
    {
        ImageSourceInput.FromFile f => Path.GetExtension(f.SourceFilePath),
        ImageSourceInput.FromStream { SuggestedFileName: not null } s => Path.GetExtension(s.SuggestedFileName),
        _ => string.Empty, // FromBitmap 등 원본 형식을 알 수 없는 경우 → PNG로 대체
    };

    /// <summary>WPF가 기본 제공하는 인코더로 저장 가능한 형식만 원본 확장자를 유지하고, 그 외(webp 등)는 PNG로 대체한다.</summary>
    private static (BitmapEncoder Encoder, string Extension) CreateEncoder(string originalExtension)
    {
        return originalExtension.ToLowerInvariant() switch
        {
            ".jpg" => (new JpegBitmapEncoder(), ".jpg"),
            ".jpeg" => (new JpegBitmapEncoder(), ".jpeg"),
            ".bmp" => (new BmpBitmapEncoder(), ".bmp"),
            ".gif" => (new GifBitmapEncoder(), ".gif"),
            ".tiff" => (new TiffBitmapEncoder(), ".tiff"),
            ".tif" => (new TiffBitmapEncoder(), ".tif"),
            _ => (new PngBitmapEncoder(), ".png"),
        };
    }

    private static BitmapSource? TryDecode(ImageSourceInput input)
    {
        try
        {
            return input switch
            {
                ImageSourceInput.FromFile fromFile => DecodeFile(fromFile.SourceFilePath),
                ImageSourceInput.FromBitmap fromBitmap => fromBitmap.Bitmap,
                ImageSourceInput.FromStream fromStream => DecodeStream(fromStream.Content),
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
        return DecodeStream(stream);
    }

    private static BitmapSource DecodeStream(Stream stream)
    {
        using (stream)
        {
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            frame.Freeze();
            return frame;
        }
    }

    private static void SaveWithEncoder(BitmapSource bitmap, BitmapEncoder encoder, string destinationPath)
    {
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

        // 이미지마다 원본 확장자가 다를 수 있으므로(03-data-storage.md), 임시 이름으로 바꾸기 전에 각자 확장자를 기억해둔다.
        var extensions = orderedItems.Select(item => Path.GetExtension(item.FileName)).ToList();

        // 1단계: 번호 충돌 방지를 위해 전부 임시 파일명으로 변경.
        var tempPaths = new List<string>(orderedItems.Count);
        for (var i = 0; i < orderedItems.Count; i++)
        {
            var tempPath = Path.Combine(minorTopic.FullPath, $".tmp_{Guid.NewGuid():N}{extensions[i]}");
            File.Move(orderedItems[i].FullPath, tempPath);
            tempPaths.Add(tempPath);
        }

        // 2단계: 새 순서대로 001, 002... 최종 파일명으로 변경 (확장자는 원래 것 유지).
        for (var i = 0; i < orderedItems.Count; i++)
        {
            var fileName = $"{majorTopicName}_{minorTopic.Name}_{(i + 1):000}{extensions[i]}";
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
