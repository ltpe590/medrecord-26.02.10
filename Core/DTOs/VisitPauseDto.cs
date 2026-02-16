namespace Core.DTOs
{
    public sealed class PausedVisitDto
    {
        public int VisitId { get; init; }
        public int PatientId { get; init; }
        public string PatientName { get; init; } = "";
        public DateTime PausedAt { get; init; } // UTC
        public bool IsStale { get; init; }
    }
}