using Core.Entities;
using Core.Helpers;

namespace WPF.ViewModels
{
    public sealed class PatientViewModel
    {
        public int PatientId { get; init; }
        public string Name { get; init; } = string.Empty;
        public Sex Sex { get; init; }  // Changed from string? to Sex enum
        public DateTime DateOfBirth { get; init; }
        public string? PhoneNumber { get; init; }
        public string? Address { get; init; }

        public string DisplayName => Name;
        public int Age => AgeCalculator.FromDateOfBirth(DateOnly.FromDateTime(DateOfBirth));
        public string SexDisplay => Sex.ToString();  // For UI display
    }
}