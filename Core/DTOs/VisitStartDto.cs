namespace Core.DTOs
{
    public sealed class VisitStartResultDto
    {
        public int VisitId { get; init; }
        public int PatientId { get; init; }
        public bool IsResumed { get; init; }
        public DateTime StartedAt { get; init; }

        public bool HasPausedVisit { get; init; }
        public int? PausedVisitId { get; init; }

    }

    public sealed class VisitStartRequestDto
    {
        public int PatientId { get; init; }
        public string PresentingSymptom { get; init; } = string.Empty;
        public string? Duration { get; init; }
        public string? ShortNote { get; init; }
    }
}
