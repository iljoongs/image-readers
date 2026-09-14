using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ImageTopicViewer.Converters;

/// <summary>대주제 평점(1~10)을 game-platform 프로젝트의 평점 슬라이더와 같은 5구간 색
/// (청록 → 하늘색 → 보라 → 분홍 → 주황, 2단계마다 전환)으로 바꾼다 (07-ui-layout.md "좌측 패널").</summary>
public class MajorTopicRatingColorConverter : IValueConverter
{
    private static readonly (int UpperBound, Color Start, Color End)[] Bands =
    {
        (2, Color.FromRgb(0x17, 0x8F, 0x82), Color.FromRgb(0x2E, 0x6F, 0x68)), // 청록
        (4, Color.FromRgb(0x22, 0xAE, 0xDB), Color.FromRgb(0x2E, 0x86, 0xAE)), // 하늘색
        (6, Color.FromRgb(0x5B, 0x3F, 0xA6), Color.FromRgb(0x6D, 0x5A, 0xA0)), // 보라
        (8, Color.FromRgb(0xD6, 0x29, 0x5F), Color.FromRgb(0xB8, 0x45, 0x70)), // 분홍
        (10, Color.FromRgb(0xF2, 0x87, 0x2E), Color.FromRgb(0xE0, 0x95, 0x4B)), // 주황
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var rating = value is int i ? i : 1;
        var band = Array.Find(Bands, b => rating <= b.UpperBound);
        if (band == default)
        {
            band = Bands[^1];
        }

        return new LinearGradientBrush(band.Start, band.End, new Point(0, 0), new Point(1, 0));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
