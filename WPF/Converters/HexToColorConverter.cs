using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WPF.Converters
{
    /// <summary>
    /// Converts a hex color string like "#2196F3" to a WPF Color struct.
    /// Used in the Doctor and Appearance tabs for the accent preview swatch.
    /// </summary>
    public sealed class HexToColorConverter : IValueConverter
    {
        public static readonly HexToColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                try
                {
                    var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
                    return brush.Color;
                }
                catch { /* fall through */ }
            }
            return Colors.DodgerBlue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
