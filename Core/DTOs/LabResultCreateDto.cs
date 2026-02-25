using System.ComponentModel.DataAnnotations;

public sealed class LabResultCreateDto
{
    [Required, Range(1, int.MaxValue)]
    public int TestId { get; init; }

    [Required, Range(1, int.MaxValue)]
    public int VisitId { get; init; }

    [Required, StringLength(500)]
    public string ResultValue { get; init; } = string.Empty;

    [StringLength(50)]
    public string Unit { get; init; } = string.Empty;

    /// <summary>Normal range as recorded at the time of this visit (lab/kit-specific).</summary>
    [StringLength(100)]
    public string NormalRange { get; init; } = string.Empty;

    [StringLength(500)]
    public string Notes { get; init; } = string.Empty;
}
