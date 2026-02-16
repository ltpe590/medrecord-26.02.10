using Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class Doctor
    {
        [Key]
        public int DoctorId { get; set; }

        [Required]
        public string DoctorName { get; set; } = string.Empty;

        public string Degree { get; set; } = string.Empty; // M.b.Ch.B, Ph.D.

        public string Specialty { get; set; } = string.Empty; // Cardiology, Dermatology

        public string ClinicName { get; set; } = string.Empty;

        public string ClinicAddress { get; set; } = string.Empty;

        public string ClinicPhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // Navigation property
        public ICollection<Visit> MedicalRecords { get; set; } = new List<Visit>();
    }
}