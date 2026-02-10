namespace Core.DTOs.ObGyne
{
    public sealed class GPADto
    {
        public int? Gravida { get; init; }
        public int? Para { get; init; }
        public int? Abortion { get; init; }
    }

    public sealed class PregnancyDto
    {
        public DateOnly? LMP { get; init; }
        public DateOnly? EDD => LMP?.AddDays(280);
    }

    public sealed class MenstrualDto
    {
        public int? CycleDays { get; init; }
        public int? DurationDays { get; init; }
    }
}
