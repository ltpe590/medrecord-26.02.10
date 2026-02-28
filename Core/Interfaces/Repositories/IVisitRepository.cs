using Core.DTOs;
using Core.Entities;

namespace Core.Interfaces.Repositories
{
    public interface IVisitRepository
    {
        // ── Single-entity loads ───────────────────────────────────────────────

        /// <summary>Load a visit by primary key. Returns null if not found.</summary>
        Task<Visit?> GetByIdAsync(int visitId);

        /// <summary>Load a visit with its Entries collection included.</summary>
        Task<Visit?> GetWithDetailsAsync(int visitId);

        /// <summary>Return the active (not ended, not paused) visit for a patient, or null.</summary>
        Task<Visit?> GetActiveForPatientAsync(int patientId);

        /// <summary>Return the most-recently-paused, not-ended visit for a patient, or null.</summary>
        Task<Visit?> GetPausedForPatientAsync(int patientId);

        /// <summary>True if a non-deleted patient with this ID exists.</summary>
        Task<bool> PatientExistsAsync(int patientId);

        /// <summary>True if a visit with this ID exists.</summary>
        Task<bool> ExistsAsync(int visitId);

        // ── List queries ──────────────────────────────────────────────────────

        /// <summary>All visits, unfiltered. No-tracking.</summary>
        Task<List<Visit>> GetAllAsync();

        /// <summary>All visits for a patient, newest first. No-tracking.</summary>
        Task<List<Visit>> GetByPatientIdAsync(int patientId);

        /// <summary>Completed visit history for a patient, newest first.</summary>
        Task<List<VisitDto>> GetVisitHistoryAsync(int patientId);

        /// <summary>Paused visits whose PausedAt falls within [todayUtcStart, tomorrowUtcStart).</summary>
        Task<List<PausedVisitDto>> GetPausedVisitsTodayAsync(DateTime todayUtcStart, DateTime tomorrowUtcStart);

        /// <summary>Paused visits whose PausedAt is before todayUtcStart (stale / carry-over).</summary>
        Task<List<PausedVisitDto>> GetStalePausedVisitsAsync(DateTime todayUtcStart);

        // ── Persistence ───────────────────────────────────────────────────────

        void Add(Visit visit);
        Task SaveChangesAsync();
    }
}
