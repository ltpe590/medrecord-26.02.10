namespace Core.DTOs
{
    /// <summary>Result of AI-powered OCR extraction from a lab result image.</summary>
    public sealed class LabOcrResult
    {
        public string TestName    { get; set; } = string.Empty;
        public string ResultValue { get; set; } = string.Empty;
        public string Unit        { get; set; } = string.Empty;
        public string NormalRange { get; set; } = string.Empty;
    }
}