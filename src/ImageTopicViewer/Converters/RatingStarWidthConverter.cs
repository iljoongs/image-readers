using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ImageTopicViewer.Converters;

/// <summary>대주제 평점(1~10)을 progress bar 모양의 채움/빈 칸 폭 비율(GridLength, Star)로 바꾼다.
/// ConverterParameter "Fill"이면 채워진 칸 비율(평점), "Empty"면 남은 칸 비율(10-평점)을 반환한다
/// (07-ui-layout.md "좌측 패널").</summary>
public class RatingStarWidthConverter : IValueConverter
{
    private const int Max = 10;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var rating = Math.Clamp(value is int i ? i : 1, 1, Max);
        var weight = string.Equals(parameter as string, "Fill", StringComparison.OrdinalIgnoreCase)
            ? rating
            : Max - rating;

        return new GridLength(weight, GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
