using System.IO;
using System.IO.Compression;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageTopicViewer.Models;

namespace ImageTopicViewer.Services;

public class ImageSourceProvider : IImageSourceProvider
{
    public Task<ImageSource> LoadAsync(ImageItem item, CancellationToken cancellationToken = default)
    {
        return item.IsFromArchive
            ? LoadFromArchiveAsync(item.ArchiveFilePath!, item.ArchiveEntryName!, cancellationToken)
            : LoadFromFileAsync(item.FullPath, cancellationToken);
    }

    private static Task<ImageSource> LoadFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            // 재넘버링으로 파일명이 재사용될 수 있으므로, WPF의 URI 기준 비트맵 캐시를 무시하고
            // 항상 디스크에서 다시 디코딩한다 (그렇지 않으면 삭제 후 같은 이름으로 저장된
            // 새 이미지 대신 예전에 캐시된 이미지가 표시된다).
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            return (ImageSource)bitmap;
        }, cancellationToken);
    }

    private static Task<ImageSource> LoadFromArchiveAsync(string archiveFilePath, string entryName, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var archive = ZipFile.OpenRead(archiveFilePath);
            var entry = archive.GetEntry(entryName)
                ?? throw new FileNotFoundException($"압축 파일 안에서 항목을 찾을 수 없습니다: {entryName}", entryName);

            // 엔트리 스트림은 seek을 지원하지 않을 수 있어, 메모리로 복사한 뒤 디코딩한다.
            using var entryStream = entry.Open();
            using var memoryStream = new MemoryStream();
            entryStream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            var decoder = BitmapDecoder.Create(memoryStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            frame.Freeze();

            return (ImageSource)frame;
        }, cancellationToken);
    }
}
