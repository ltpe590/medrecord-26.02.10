namespace Core.Helpers
{
    public static class LMPCalculator
    {
        public static int? GetGestationalAgeInWeeks(DateOnly? lmp, DateOnly? onDate = null)
        {
            if (lmp == null) return null;
            var reference = onDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            return (int)((reference.ToDateTime(TimeOnly.MinValue) - lmp.Value.ToDateTime(TimeOnly.MinValue)).TotalDays / 7);
        }

        public static DateOnly? GetEDD(DateOnly? lmp)
        {
            if (lmp == null) return null;
            return lmp.Value.AddDays(280); // 40 weeks
        }
    }
}
