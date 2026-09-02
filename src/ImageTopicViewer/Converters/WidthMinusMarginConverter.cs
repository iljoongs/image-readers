using System;
using System.Globalization;
using System.Windows.Data;

namespace ImageTopicViewer.Converters;

public class WidthMinusMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double width && parameter is string marginText && double.TryParse(marginText, NumberStyles.Float, CultureInfo.InvariantCulture, out var margin))
        {
            return Math.Max(0, width - margin);
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
