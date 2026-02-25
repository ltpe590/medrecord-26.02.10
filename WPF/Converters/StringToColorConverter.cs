using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WPF.Converters;

/// <summary>
/// Converts a hex color string like "#2196F3" to a WPF Color struct.
/// Useful for binding SolidColorBrush.Color to a string property.
/// </summary>
public class StringToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
                return brush.Color;
            }
            catch { }
        }
        return Colors.DodgerBlue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
