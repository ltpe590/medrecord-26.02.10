using System.ComponentModel.DataAnnotations;

public sealed class DrugCreateDto
{
    [Required] public string BrandName { get; init; } = string.Empty;
    public string? Composition { get; init; }
    public string? Form { get; init; }   // "Tablet"
    public string? DosageStrength { get; init; }   // "500 mg"
}