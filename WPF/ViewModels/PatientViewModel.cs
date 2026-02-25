using Core.Entities;
using Core.Helpers;

namespace WPF.ViewModels
{
    public sealed class PatientViewModel
    {
        public int PatientId { get; init; }
        public string Name { get; init; } = string.Empty;
        public Sex Sex { get; init; }
        public DateTime DateOfBirth { get; init; }
        public string? PhoneNumber { get; init; }
        public string? Address { get; init; }
        public string? BloodGroup { get; init; }
        public string? Allergies { get; init; }

        /// <summary>True when this patient has a paused visit today.</summary>
        public bool IsPaused { get; set; }

        public string DisplayName => Name;
        public int Age => AgeCalculator.FromDateOfBirth(DateOnly.FromDateTime(DateOfBirth));
        public string SexDisplay => Sex.ToString();
        public string Phone => PhoneNumber ?? "—";
        public string DateOfBirthDisplay => DateOfBirth.ToString("dd MMM yyyy");
    }
}