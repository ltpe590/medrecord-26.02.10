using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    /// <summary>
    /// EF-mapped catalog entry for a drug/medication.
    /// Scalar properties are init-only: set at construction, not mutated afterward.
    /// The public parameterless constructor is required by EF Core and System.Text.Json.
    /// </summary>
    public class DrugCatalog
    {
        [Key]
        public int DrugId { get; set; }          // set: EF assigns after INSERT

        public string  BrandName      { get; init; } = string.Empty;
        public string? Composition    { get; init; }
        public string? Form           { get; init; }
        public string? DosageStrength { get; init; }
        public string? Frequency      { get; init; }
        public string? Route          { get; init; }
        public string? Instructions   { get; init; }
    }
}
