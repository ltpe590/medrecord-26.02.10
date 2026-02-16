public sealed class PrescriptionDto
{
    public int PrescriptionId { get; init; }   // init-only keeps it read-only
    public int DrugId { get; init; }
    public string DrugName { get; init; } = string.Empty;
    public string Dosage { get; init; } = string.Empty;  // matches domain prop
    public string DurationDays { get; init; } = string.Empty;
    public string? Form { get; init; }   // nullable because DB allows it
}