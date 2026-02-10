namespace Core.DTOs
{
    public class PrescriptionCreateDto
    {
        public int VisitId { get; set; }
        public int DrugId { get; set; }
        public string Dosage { get; set; } = string.Empty;
        public string? DurationDays { get; set; }
    }
}
