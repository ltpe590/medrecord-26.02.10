using System;
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs
{
    public sealed class PatientDto
    {
        public int PatientId { get; init; }

        [Required, StringLength(100)]
        public string Name { get; init; } = string.Empty;

        public string? Sex { get; init; }

        public DateTime DateOfBirth { get; init; }

        // Pure data — calculated during mapping, NOT inside DTO
        public int? Age { get; init; }

        public string? BloodGroup { get; init; }
        public string? Allergies { get; init; }
        public string? PhoneNumber { get; init; }
        public string? Address { get; init; }
        public string? ShortNote { get; init; }
    }

    public sealed class PatientCreateDto
    {
        [Required, StringLength(100)]
        public string Name { get; init; } = string.Empty;

        public string? Sex { get; init; }

        // EITHER age OR date of birth supplied (validate in service layer)
        public int? Age { get; init; }

        public DateOnly? DateOfBirth { get; init; }

        public string? BloodGroup { get; init; }
        public string? Allergies { get; init; }
        public string? PhoneNumber { get; init; }
        public string? Address { get; init; }
        public string? ShortNote { get; init; }
    }

    public sealed class PatientUpdateDto
    {
        [Range(1, int.MaxValue)]
        public int PatientId { get; init; }

        [StringLength(100)]
        public string? Name { get; init; }

        public string? Sex { get; init; }

        public DateOnly? DateOfBirth { get; init; }

        [StringLength(20)]
        public string? PhoneNumber { get; init; }

        [StringLength(250)]
        public string? Address { get; init; }

        [StringLength(10)]
        public string? BloodGroup { get; init; }

        [StringLength(500)]
        public string? Allergies { get; init; }

        [StringLength(500)]
        public string? ShortNote { get; init; }
    }

    public sealed class PatientDeleteDto
    {
        [Range(1, int.MaxValue)]
        public int PatientId { get; init; }
    }
}
