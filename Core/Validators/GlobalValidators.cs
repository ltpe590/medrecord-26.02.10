using System.Text.RegularExpressions;

namespace Core.Validators
{
    // Common string validations
    public static class StringValidator
    {
        public static void ValidateNotEmpty(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{parameterName} cannot be empty or whitespace.", parameterName);
        }
    }

    // String length and pattern validations
    public static class StringLengthValidator
    {
        public static void ValidateMaxLength(string? value, int maxLength, string parameterName)
        {
            if (!string.IsNullOrEmpty(value) && value.Length > maxLength)
                throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters. Length: {value.Length}", parameterName);
        }

        public static void ValidateLengthRange(string value, int minLength, int maxLength, string parameterName)
        {
            if (value.Length < minLength || value.Length > maxLength)
                throw new ArgumentException($"{parameterName} length must be between {minLength} and {maxLength}. Length: {value.Length}", parameterName);
        }

        public static void ValidatePattern(string value, string pattern, string parameterName, string description)
        {
            if (!Regex.IsMatch(value, pattern))
                throw new ArgumentException($"{parameterName} format is invalid. Expected: {description}", parameterName);
        }
    }

    // Date-related validations
    public static class DateValidator
    {
        public static void ValidateDateOfBirth(DateOnly dateOfBirth, string parameterName = "dateOfBirth")
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (dateOfBirth > today)
                throw new ArgumentException("Date of birth cannot be in the future.", parameterName);

            var age = today.Year - dateOfBirth.Year;
            if (age > 150)
                throw new ArgumentException("Date of birth appears invalid (age > 150 years).", parameterName);
        }
    }

    // Integer validations
    public static class IntegerValidator
    {
        public static void ValidatePositiveId(int id, string parameterName)
        {
            if (id <= 0)
                throw new ArgumentException($"{parameterName} must be positive (> 0).", parameterName);
        }

        public static void ValidatePositiveId(int? id, string parameterName)
        {
            if (id.HasValue && id.Value <= 0)
                throw new ArgumentException($"{parameterName} must be positive (> 0).", parameterName);
        }

        public static void ValidateNonNegative(int value, string parameterName)
        {
            if (value < 0)
                throw new ArgumentException($"{parameterName} must be non-negative (≥ 0).", parameterName);
        }

        public static void ValidateNonNegative(int? value, string parameterName)
        {
            if (value.HasValue && value.Value < 0)
                throw new ArgumentException($"{parameterName} must be non-negative (≥ 0).", parameterName);
        }

        public static void ValidateRange(int value, int min, int max, string parameterName)
        {
            if (value < min || value > max)
                throw new ArgumentException($"{parameterName} must be between {min} and {max}.", parameterName);
        }

        public static void ValidateRange(int? value, int min, int max, string parameterName)
        {
            if (value.HasValue && (value.Value < min || value.Value > max))
                throw new ArgumentException($"{parameterName} must be between {min} and {max}.", parameterName);
        }

        public static void ValidateNotExceeds(int value1, int value2, string value1Name, string value2Name)
        {
            if (value1 > value2)
                throw new ArgumentException($"{value1Name} cannot exceed {value2Name}.", value1Name);
        }

        public static void ValidateNotExceeds(int? value1, int? value2, string value1Name, string value2Name)
        {
            if (value1.HasValue && value2.HasValue && value1.Value > value2.Value)
                throw new ArgumentException($"{value1Name} cannot exceed {value2Name}.", value1Name);
        }
    }

    // Null and collection validations
    public static class NullValidator
    {
        public static void ValidateNotNull(object? obj, string parameterName)
        {
            if (obj is null)
                throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null.");
        }

        public static void ValidateNotNullOrEmpty<T>(IEnumerable<T>? collection, string parameterName)
        {
            if (collection is null)
                throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null.");

            if (!collection.Any())
                throw new ArgumentException($"{parameterName} cannot be empty.", parameterName);
        }

        public static void ValidateAllNotNull(params (object? obj, string name)[] objects)
        {
            foreach (var (obj, name) in objects)
            {
                if (obj is null)
                    throw new ArgumentNullException(name, $"{name} cannot be null.");
            }
        }
    }
}