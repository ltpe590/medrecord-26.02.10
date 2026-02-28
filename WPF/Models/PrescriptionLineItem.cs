namespace WPF.ViewModels
{
    /// <summary>One row in the visit prescription list.</summary>
    public sealed class PrescriptionLineItem
    {
        public int     DrugId       { get; set; }
        public string  DrugName     { get; set; } = string.Empty;
        public string? Form         { get; set; }
        public string  Dose         { get; set; } = string.Empty;
        public string  Route        { get; set; } = string.Empty;
        public string  Frequency    { get; set; } = string.Empty;
        public string  DurationDays { get; set; } = string.Empty;
        public string  Instructions { get; set; } = string.Empty;
    }
}
