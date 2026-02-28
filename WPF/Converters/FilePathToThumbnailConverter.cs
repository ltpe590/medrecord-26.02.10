using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WPF.Converters
{
    /// <summary>
    /// Converts a file path (string) to a BitmapImage thumbnail for WPF Image.Source binding.
    /// Lives in the view layer so LabAttachment stays WPF-free.
    /// </summary>
    [ValueConversion(typeof(string), typeof(BitmapImage))]
    public sealed class FilePathToThumbnailConverter : IValueConverter
    {
        /// <summary>Decode width in pixels. Set via XAML ConverterParameter if needed.</summary>
        public int DecodePixelWidth { get; set; } = 120;

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string path || string.IsNullOrWhiteSpace(path)) return null;
            if (!System.IO.File.Exists(path)) return null;

            var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            bool isImage = ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff" or ".tif";
            if (!isImage) return null;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource        = new Uri(path, UriKind.Absolute);
                bmp.DecodePixelWidth = DecodePixelWidth;
                bmp.CacheOption      = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null; // thumbnail is optional
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}