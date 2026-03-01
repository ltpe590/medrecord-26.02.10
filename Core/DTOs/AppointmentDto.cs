using Core.Entities;

namespace Core.DTOs
{
    public class AppointmentDto
    {
        public int               AppointmentId { get; set; }
        public int               PatientId     { get; set; }
        public string            PatientName   { get; set; } = string.Empty;
        public DateTime          ScheduledAt   { get; set; }
        public string?           Reason        { get; set; }
        public string?           Notes         { get; set; }
        public AppointmentStatus Status        { get; set; }
        public string            StatusDisplay => Status.ToString();
    }
}
