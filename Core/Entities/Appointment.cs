namespace Core.Entities
{
    /// <summary>
    /// Represents a scheduled appointment between a patient and the clinic.
    /// Domain invariants are enforced through the factory constructor.
    /// EF Core materialises instances via the protected parameterless constructor.
    /// </summary>
    public class Appointment
    {
        protected Appointment() { }

        public Appointment(int patientId, DateTime scheduledAt, string? reason = null, string? notes = null)
        {
            if (patientId <= 0) throw new ArgumentOutOfRangeException(nameof(patientId));
            if (scheduledAt < DateTime.UtcNow.AddMinutes(-1))
                throw new ArgumentException("Scheduled time cannot be in the past.", nameof(scheduledAt));

            PatientId   = patientId;
            ScheduledAt = scheduledAt;
            Reason      = reason?.Trim();
            Notes       = notes?.Trim();
            Status      = AppointmentStatus.Scheduled;
            CreatedAt   = DateTime.UtcNow;
        }

        public int                AppointmentId { get; private set; }
        public int                PatientId     { get; private set; }
        public DateTime           ScheduledAt   { get; private set; }
        public string?            Reason        { get; private set; }
        public string?            Notes         { get; private set; }
        public AppointmentStatus  Status        { get; private set; }
        public DateTime           CreatedAt     { get; private set; }

        // Navigation
        public Patient? Patient { get; private set; }

        // ── Domain methods ────────────────────────────────────────────────────

        public void Reschedule(DateTime newTime)
        {
            if (newTime < DateTime.UtcNow.AddMinutes(-1))
                throw new ArgumentException("New time cannot be in the past.", nameof(newTime));
            ScheduledAt = newTime;
        }

        public void Cancel() => Status = AppointmentStatus.Cancelled;
        public void MarkArrived() => Status = AppointmentStatus.Arrived;
        public void Complete()    => Status = AppointmentStatus.Completed;

        public void UpdateNotes(string? notes) => Notes = notes?.Trim();
    }

    public enum AppointmentStatus
    {
        Scheduled = 0,
        Arrived   = 1,
        Completed = 2,
        Cancelled = 3
    }
}
