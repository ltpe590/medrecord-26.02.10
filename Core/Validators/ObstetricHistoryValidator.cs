namespace Core.Validators
{
    public static class ObstetricHistoryValidator
    {
        /// <summary>
        /// Validates obstetric history metrics.
        /// Ensures non-negative values, logical consistency (Para ≤ Gravida), and valid dates.
        /// </summary>
        /// <param name="gravida">Total number of pregnancies (non-negative)</param>
        /// <param name="para">Number of live births (non-negative, ≤ Gravida)</param>
        /// <param name="abortion">Number of miscarriages/abortions (non-negative)</param>
        /// <param name="lmp">Last menstrual period (must not be in future)</param>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        public static void ValidateObstetricMetrics(int? gravida, int? para, int? abortion, DateOnly? lmp)
        {
            // Validate non-negative integers
            IntegerValidator.ValidateNonNegative(gravida, nameof(gravida));
            IntegerValidator.ValidateNonNegative(para, nameof(para));
            IntegerValidator.ValidateNonNegative(abortion, nameof(abortion));

            // Ensure Para <= Gravida (logical consistency)
            IntegerValidator.ValidateNotExceeds(para, gravida, nameof(para), nameof(gravida));

            // Validate LMP date (must not be in future)
            if (lmp.HasValue)
                DateValidator.ValidateDateOfBirth(lmp.Value, nameof(lmp));
        }
    }
}