namespace WPF.ViewModels
{
    /// <summary>
    /// Pure model — no WPF dependencies.
    /// Thumbnail loading is handled by FilePathToThumbnailConverter in the view layer.
    /// </summary>
    public sealed class LabAttachment
    {
        public string FilePath { get; init; } = string.Empty;
        public string FileName => System.IO.Path.GetFileName(FilePath);
        public bool   IsImage  => IsImageExtension(FilePath);

        private static bool IsImageExtension(string path)
        {
            var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            return ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff" or ".tif";
        }

        /// <summary>Creates a LabAttachment from a file path.</summary>
        public static LabAttachment FromFile(string filePath) => new() { FilePath = filePath };
    }
}