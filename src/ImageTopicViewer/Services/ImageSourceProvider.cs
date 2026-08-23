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
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            return (ImageSource)bitmap;
        }, cancellationToken);
    }
}
