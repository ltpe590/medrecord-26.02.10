public sealed class PrescriptionDto
{
    public int PrescriptionId { get; init; }
    public int DrugId { get; init; }
    public string DrugName { get; init; } = string.Empty;
    public string Dosage { get; init; } = string.Empty;
    public string DurationDays { get; init; } = string.Empty;
    public string? Form { get; init; }
    public string? Route { get; init; }
    public string? Frequency { get; init; }
    public string? Instructions { get; init; }
}