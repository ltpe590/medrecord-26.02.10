using System.ComponentModel.DataAnnotations;

namespace Core.DTOs
{
    public sealed class AppointmentCreateDto
    {
        [Required, Range(1, int.MaxValue)]
        public int      PatientId   { get; init; }

        [Required]
        public DateTime ScheduledAt { get; init; }

        [StringLength(200)]
        public string?  Reason      { get; init; }

        [StringLength(500)]
        public string?  Notes       { get; init; }
    }
}
