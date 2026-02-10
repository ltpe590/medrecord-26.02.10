using System.ComponentModel.DataAnnotations;

namespace Core.DTOs
{
    public class VisitDto
    {
        public int VisitId { get; init; }
        public int PatientId { get; init; }
        public DateTime DateOfVisit { get; init; }

        [StringLength(500)]
        public required string Diagnosis { get; init; } = string.Empty;

        [StringLength(2000)]
        public string? Notes { get; init; }

        // --- Vital signs ---
        public decimal Temperature { get; init; }
        public int BloodPressureSystolic { get; init; }
        public int BloodPressureDiastolic { get; init; }        

        /// <summary>Optional list of related prescription IDs.</summary>
        public IReadOnlyList<int> PrescriptionIds { get; init; } = Array.Empty<int>();

        /// <summary>Optional list of related lab-result IDs.</summary>
        public IReadOnlyList<int> LabResultIds { get; init; } = Array.Empty<int>();

        public List<PrescriptionDto> Prescriptions { get; init; } = new();
    }

    public sealed class VisitCreateDto
    {
        [Required]
        public int PatientId { get; init; }

        public DateTime DateOfVisit { get; init; } = DateTime.UtcNow;

        [StringLength(500)]
        public required string Diagnosis { get; init; } = string.Empty;

        [StringLength(2000)]
        public string? Notes { get; init; }

        public decimal Temperature { get; init; }
        public int BloodPressureSystolic { get; init; }
        public int BloodPressureDiastolic { get; init; }
    }
}
