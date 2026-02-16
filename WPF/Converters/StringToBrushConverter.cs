using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WPF.Converters;

public class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string colorName)
        {
            return colorName switch
            {
                "Green" => Brushes.Green,
                "Red" => Brushes.Red,
                "Yellow" => Brushes.Yellow,
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}