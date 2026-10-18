using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ImageTopicViewer.Services;

namespace ImageTopicViewer.Converters;

/// <summary>대주제 커버 이미지 경로(문자열)를 홈 화면 카드의 Border.Background로 바로 쓸 수 있는 Brush로 바꾼다
/// (07-ui-layout.md "홈 화면"). `ImageLoadHelper`(game-platform 프로젝트에서 가져온 로딩 방식)로 디코딩하므로,
/// 커버를 교체해 같은 경로를 덮어써도(`CoverImageStore`) 캐시된 예전 이미지가 아니라 항상 새로 읽는다.
/// ConverterParameter로 DecodePixelWidth(디코딩 너비, 성능/메모리 절약용)를 지정할 수 있다.
/// 경로가 없거나 디코딩에 실패하면 연회색 placeholder를 반환한다.</summary>
public class CoverImageBrushConverter : IValueConverter
{
    private static readonly Brush PlaceholderBrush = CreatePlaceholder();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var decodePixelWidth = parameter is string s && int.TryParse(s, out var width) ? width : (int?)null;
        var bitmap = ImageLoadHelper.Load(value as string, decodePixelWidth);
        if (bitmap is null)
        {
            return PlaceholderBrush;
        }

        var brush = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
        brush.Freeze();
        return brush;
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
