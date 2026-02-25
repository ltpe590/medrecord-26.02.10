namespace Core.DTOs
{
    public class DrugCatalogDto
    {
        public int     DrugId         { get; set; }
        public string  BrandName      { get; set; } = string.Empty;
        public string? Composition    { get; set; }
        public string? Form           { get; set; }
        public string? DosageStrength { get; set; }
        public string? Frequency      { get; set; }
        public string? Route          { get; set; }
        public string? Instructions   { get; set; }
    }
}