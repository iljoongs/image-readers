using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageTopicViewer.Converters;

/// <summary>대주제 커버 이미지 경로(문자열)를 홈 화면 카드의 Border.Background로 바로 쓸 수 있는 Brush로 바꾼다
/// (07-ui-layout.md "홈 화면"). 경로가 없거나 디코딩에 실패하면 연회색 placeholder를 반환한다.</summary>
public class CoverImageBrushConverter : IValueConverter
{
    private static readonly Brush PlaceholderBrush = CreatePlaceholder();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                var brush = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
                brush.Freeze();
                return brush;
            }
            catch (Exception ex) when (ex is NotSupportedException or FileFormatException or IOException)
            {
                // 디코딩 불가 파일(지원하지 않는 형식 등) — placeholder로 대체.
            }
        }

        return PlaceholderBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Brush CreatePlaceholder()
    {
        var brush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
        brush.Freeze();
        return brush;
    }
}
