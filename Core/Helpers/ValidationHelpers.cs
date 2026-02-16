namespace Core.Helpers;

/// <summary>
/// Helper class for common input validation patterns
/// </summary>
internal static class ValidationHelpers
{
    /// <summary>
    /// Validates that a visit ID is positive
    /// </summary>
    public static void ValidateVisitId(int visitId, string paramName = "visitId")
    {
        if (visitId <= 0)
            throw new ArgumentException("Visit ID must be positive", paramName);
    }

    /// <summary>
    /// Validates that a patient ID is positive
    /// </summary>
    public static void ValidatePatientId(int patientId, string paramName = "patientId")
    {
        if (patientId <= 0)
            throw new ArgumentException("Patient ID must be positive", paramName);
    }

    /// <summary>
    /// Validates that a string is not null or whitespace
    /// </summary>
    public static void ValidateNotNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} cannot be null or whitespace", paramName);
    }
}