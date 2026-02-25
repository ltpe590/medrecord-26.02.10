namespace Core.DTOs
{
    public class PrescriptionCreateDto
    {
        public int VisitId { get; set; }
        public int DrugId { get; set; }
        public string Dosage { get; set; } = string.Empty;
        public string? DurationDays { get; set; }
        public string? Route { get; set; }
        public string? Frequency { get; set; }
        public string? Instructions { get; set; }
    }
}