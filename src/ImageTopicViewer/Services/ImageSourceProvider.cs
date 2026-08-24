using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageTopicViewer.Services;

public class ImageSourceProvider : IImageSourceProvider
{
    public Task<ImageSource> LoadAsync(string filePath, CancellationToken cancellationToken = default)
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
}
