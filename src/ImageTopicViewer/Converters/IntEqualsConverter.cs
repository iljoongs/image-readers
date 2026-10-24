using System.Globalization;
using System.Windows.Data;

namespace ImageTopicViewer.Converters;

/// <summary>상단 메뉴 "스크롤 > x1~x10" 각 항목의 체크 표시용. value(int)가 ConverterParameter(문자열로 쓴 숫자)와
/// 같으면 true (07-ui-layout.md "상단 메뉴").</summary>
public class IntEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int intValue && parameter is string text && int.TryParse(text, out var compare) && intValue == compare;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
