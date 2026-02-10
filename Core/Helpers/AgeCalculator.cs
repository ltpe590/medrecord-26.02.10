namespace Core.Helpers
{
    public static class AgeCalculator
    {
        public static int FromDateOfBirth(
            DateOnly dateOfBirth,
            DateOnly? asOf = null)
        {
            var today = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);

            int age = today.Year - dateOfBirth.Year;

            if (dateOfBirth > today.AddYears(-age))
                age--;

            return age < 0 ? 0 : age;
        }

        public static DateOnly ToDateOfBirth(
            int age,
            DateOnly? asOf = null)
        {
            if (age < 0) age = 0;

            var today = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);

            return today.AddYears(-age);
        }
    }
}
