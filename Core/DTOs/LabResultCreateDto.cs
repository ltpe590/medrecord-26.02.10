using System.ComponentModel.DataAnnotations;

public sealed class LabResultCreateDto
{
    [Required, Range(1, int.MaxValue)] public int TestId { get; init; }
    [Required, Range(1, int.MaxValue)] public int VisitId { get; init; }
    [Required, StringLength(500)] public string ResultValue { get; init; } = string.Empty;
}