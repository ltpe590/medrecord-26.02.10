using Core.Data.Context;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories
{
    public sealed class VisitRepository : IVisitRepository
    {
        private readonly ApplicationDbContext _ctx;
        public VisitRepository(ApplicationDbContext ctx) => _ctx = ctx;

        // ── Single-entity loads ───────────────────────────────────────────────

        public Task<Visit?> GetByIdAsync(int visitId) =>
            _ctx.Visits.FirstOrDefaultAsync(v => v.VisitId == visitId);

        public Task<Visit?> GetWithDetailsAsync(int visitId) =>
            _ctx.Visits
                .Include(v => v.Entries)
                .SingleOrDefaultAsync(v => v.VisitId == visitId);

        public Task<Visit?> GetActiveForPatientAsync(int patientId) =>
            _ctx.Visits
                .Where(v => v.PatientId == patientId && v.EndedAt == null && v.PausedAt == null)
                .FirstOrDefaultAsync();

        public Task<Visit?> GetPausedForPatientAsync(int patientId) =>
            _ctx.Visits
                .Where(v => v.PatientId == patientId && v.EndedAt == null && v.PausedAt != null)
                .OrderByDescending(v => v.PausedAt)
                .FirstOrDefaultAsync();

        public Task<bool> PatientExistsAsync(int patientId) =>
            _ctx.Patients.AsNoTracking().AnyAsync(p => p.PatientId == patientId && !p.IsDeleted);

        public Task<bool> ExistsAsync(int visitId) =>
            _ctx.Visits.AnyAsync(v => v.VisitId == visitId);

        // ── List queries ──────────────────────────────────────────────────────

        public Task<List<Visit>> GetAllAsync() =>
            _ctx.Visits.AsNoTracking().ToListAsync();

        public Task<List<Visit>> GetByPatientIdAsync(int patientId) =>
            _ctx.Visits
                .Where(v => v.PatientId == patientId)
                .OrderByDescending(v => v.StartedAt)
                .AsNoTracking()
                .ToListAsync();

        public async Task<List<VisitDto>> GetVisitHistoryAsync(int patientId)
        {
            var visits = await _ctx.Visits
                .Where(v => v.PatientId == patientId && v.EndedAt != null)
                .OrderByDescending(v => v.EndedAt)
                .AsNoTracking()
                .ToListAsync();

            return visits.Select(v => new VisitDto
            {
                VisitId     = v.VisitId,
                PatientId   = v.PatientId,
                DateOfVisit = v.EndedAt ?? v.StartedAt,
                Diagnosis   = v.PresentingSymptomText,
                Notes       = v.ShortNote
            }).ToList();
        }

        public Task<List<PausedVisitDto>> GetPausedVisitsTodayAsync(
            DateTime todayUtcStart, DateTime tomorrowUtcStart) =>
            _ctx.Visits
                .Join(_ctx.Patients,
                    v => v.PatientId,
                    p => p.PatientId,
                    (v, p) => new { v, p })
                .Where(x => x.v.EndedAt == null && x.v.PausedAt != null
                            && x.v.PausedAt >= todayUtcStart
                            && x.v.PausedAt <  tomorrowUtcStart
                            && !x.p.IsDeleted)
                .OrderByDescending(x => x.v.PausedAt)
                .Select(x => new PausedVisitDto
                {
                    VisitId     = x.v.VisitId,
                    PatientId   = x.p.PatientId,
                    PatientName = x.p.Name,
                    PausedAt    = x.v.PausedAt!.Value,
                    IsStale     = false
                })
                .AsNoTracking()
                .ToListAsync();

        public Task<List<PausedVisitDto>> GetStalePausedVisitsAsync(DateTime todayUtcStart) =>
            _ctx.Visits
                .Join(_ctx.Patients,
                    v => v.PatientId,
                    p => p.PatientId,
                    (v, p) => new { v, p })
                .Where(x => x.v.EndedAt == null && x.v.PausedAt != null
                            && x.v.PausedAt < todayUtcStart
                            && !x.p.IsDeleted)
                .OrderBy(x => x.v.PausedAt)
                .Select(x => new PausedVisitDto
                {
                    VisitId     = x.v.VisitId,
                    PatientId   = x.p.PatientId,
                    PatientName = x.p.Name,
                    PausedAt    = x.v.PausedAt!.Value,
                    IsStale     = true
                })
                .AsNoTracking()
                .ToListAsync();

        // ── Persistence ───────────────────────────────────────────────────────

        public void Add(Visit visit) => _ctx.Visits.Add(visit);
        public Task SaveChangesAsync() => _ctx.SaveChangesAsync();
    }
}
