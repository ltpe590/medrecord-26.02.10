namespace Core.Helpers
{
    /// <summary>
    /// Pure static BMI helper — no side-effects, no dependencies.
    /// Weight in kilograms, height in centimetres.
    /// </summary>
    public static class BmiCalculator
    {
        /// <summary>
        /// Returns BMI rounded to 1 decimal place, or null when either input is zero/negative.
        /// Formula: weight (kg) / (height (m))^2
        /// </summary>
        public static decimal? Calculate(decimal weightKg, decimal heightCm)
        {
            if (weightKg <= 0 || heightCm <= 0) return null;
            var heightM = heightCm / 100m;
            return Math.Round(weightKg / (heightM * heightM), 1);
        }

        /// <summary>WHO classification.</summary>
        public static string Classify(decimal bmi) => bmi switch
        {
            < 18.5m => "Underweight",
            < 25.0m => "Normal",
            < 30.0m => "Overweight",
            _       => "Obese"
        };
    }
}
