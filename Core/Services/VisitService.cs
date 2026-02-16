using Core.Data.Context;
using Core.DTOs;
using Core.Entities;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Core.Services
{
    public class VisitService : IVisitService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISpecialtyProfile _obGyneProfile;
        private readonly IProfileService _profileService;

        public VisitService(
            ApplicationDbContext db,
            IProfileService profileService,
            ISpecialtyProfile obGyneProfile)
        {
            _db = db;
            _profileService = profileService;
            _obGyneProfile = obGyneProfile;
        }

        public async Task<VisitStartResultDto> StartOrResumeVisitAsync(
            int patientId,
            string presentingSymptom,
            string? duration,
            string? shortNote)
        {
            ValidationHelpers.ValidatePatientId(patientId);
            ValidationHelpers.ValidateNotNullOrWhiteSpace(presentingSymptom, nameof(presentingSymptom));

            // 1. Active visit
            var activeVisit = await GetActiveVisitForPatientAsync(patientId);
            if (activeVisit != null)
            {
                return new VisitStartResultDto
                {
                    VisitId = activeVisit.VisitId,
                    PatientId = patientId,
                    IsResumed = false,
                    StartedAt = activeVisit.StartedAt
                };
            }

            // 2) If a paused visit exists for this patient, block starting a new one
            var pausedVisit = await _db.Visits
                .Where(v => v.PatientId == patientId && v.EndedAt == null && v.PausedAt != null)
                .OrderByDescending(v => v.PausedAt)
                .Select(v => new { v.VisitId, v.StartedAt })
                .FirstOrDefaultAsync();

            if (pausedVisit != null)
            {
                return new VisitStartResultDto
                {
                    VisitId = pausedVisit.VisitId,   // <-- paused visit id
                    PatientId = patientId,
                    HasPausedVisit = true,
                    PausedVisitId = pausedVisit.VisitId,
                    IsResumed = false,
                    StartedAt = pausedVisit.StartedAt
                };
            }

            // 3. Create new visit
            var visitNew = new Visit(patientId, presentingSymptom, duration ?? "", shortNote ?? "");

            var profiles = new[] { _obGyneProfile };
            _profileService.InitializeClinicalSections(visitNew, profiles);

            _db.Visits.Add(visitNew);
            await _db.SaveChangesAsync();

            return new VisitStartResultDto
            {
                VisitId = visitNew.VisitId,
                PatientId = patientId,
                IsResumed = false,
                StartedAt = visitNew.StartedAt
            };
        }

        public async Task PauseVisitAsync(int visitId)
        {
            ValidationHelpers.ValidateVisitId(visitId);

            var visit = await _db.Visits.FindAsync(visitId);
            if (visit == null)
                throw new InvalidOperationException($"Visit with ID {visitId} not found");

            visit.Pause();
            await _db.SaveChangesAsync();
        }

        public async Task ResumeVisitAsync(int visitId)
        {
            ValidationHelpers.ValidateVisitId(visitId);

            var visit = await _db.Visits.FindAsync(visitId);
            if (visit == null)
                throw new InvalidOperationException($"Visit with ID {visitId} not found");

            visit.Resume();
            await _db.SaveChangesAsync();
        }

        public async Task EndVisitAsync(int visitId)
        {
            ValidationHelpers.ValidateVisitId(visitId);

            var visit = await _db.Visits.FindAsync(visitId);
            if (visit == null)
                throw new InvalidOperationException($"Visit with ID {visitId} not found");

            if (visit.EndedAt != null)
                return;

            visit.EndVisit();
            await _db.SaveChangesAsync();
        }

        public async Task<VisitSaveResult> SaveVisitAsync(VisitSaveRequest request)
        {
            try
            {
                if (request == null)
                    return VisitSaveResult.CreateFailure("Visit request is required.");

                if (request.PatientId <= 0)
                    return VisitSaveResult.CreateFailure("Patient ID is required.");

                var patientExists = await _db.Patients
                    .AnyAsync(p => p.PatientId == request.PatientId && !p.IsDeleted);

                if (!patientExists)
                    return VisitSaveResult.CreateFailure("Patient not found.");

                var symptom = string.IsNullOrWhiteSpace(request.Diagnosis) ? "General review" : request.Diagnosis.Trim();
                var duration = "N/A";
                var shortNote = string.IsNullOrWhiteSpace(request.Notes) ? "Visit note" : request.Notes.Trim();

                Visit visit;

                if (request.SaveType == VisitSaveType.Edit && (!request.VisitId.HasValue || request.VisitId.Value <= 0))
                    return VisitSaveResult.CreateFailure("Visit ID is required when editing an existing visit.");

                if (request.VisitId.HasValue && request.VisitId.Value > 0)
                {
                    visit = await _db.Visits.FirstOrDefaultAsync(v => v.VisitId == request.VisitId.Value)
                        ?? throw new InvalidOperationException("Visit not found");

                    visit.UpdatePresentingSymptom(symptom, duration, shortNote);
                    visit.UpdateVitals(request.Temperature, request.BloodPressureSystolic, request.BloodPressureDiastolic);
                }
                else
                {
                    visit = new Visit(request.PatientId, symptom, duration, shortNote);
                    visit.UpdateVitals(request.Temperature, request.BloodPressureSystolic, request.BloodPressureDiastolic);
                    _db.Visits.Add(visit);
                }

                if (request.SaveType == VisitSaveType.New)
                {
                    visit.EndVisit();
                }

                await _db.SaveChangesAsync();
                return VisitSaveResult.CreateSuccess(visit.VisitId, visit.StartedAt);
            }
            catch (Exception ex)
            {
                return VisitSaveResult.CreateFailure("Failed to save visit", ex);
            }
        }

        #region Lists Management

        public async Task<Visit?> GetActiveVisitForPatientAsync(int patientId)
        {
            ValidationHelpers.ValidatePatientId(patientId);

            return await _db.Visits
                .Where(v => v.PatientId == patientId && v.EndedAt == null && v.PausedAt == null)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Visit>> GetVisitHistoryForPatientAsync(int patientId)
        {
            ValidationHelpers.ValidatePatientId(patientId);

            return await _db.Visits
                .Include(v => v.Entries)  // Prevent N+1 query
                .Where(v => v.PatientId == patientId && v.EndedAt != null)
                .OrderByDescending(v => v.EndedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<PausedVisitDto>> GetPausedVisitsTodayAsync()
        {
            var (clinicTodayUtcStart, clinicTomorrowUtcStart) = GetClinicDayRangeUtc();

            return await _db.Visits
                .Join(_db.Patients,
                    v => v.PatientId,
                    p => p.PatientId,
                    (v, p) => new { v, p })
                .Where(x => x.v.EndedAt == null && x.v.PausedAt != null
                            && x.v.PausedAt >= clinicTodayUtcStart
                            && x.v.PausedAt < clinicTomorrowUtcStart
                            && !x.p.IsDeleted)
                .OrderByDescending(x => x.v.PausedAt)
                .Select(x => new PausedVisitDto
                {
                    VisitId = x.v.VisitId,
                    PatientId = x.p.PatientId,
                    PatientName = x.p.Name,
                    PausedAt = x.v.PausedAt!.Value,
                    IsStale = false
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<PausedVisitDto>> GetStalePausedVisitsAsync()
        {
            var (clinicTodayUtcStart, _) = GetClinicDayRangeUtc();

            return await _db.Visits
                .Join(_db.Patients,
                    v => v.PatientId,
                    p => p.PatientId,
                    (v, p) => new { v, p })
                .Where(x => x.v.EndedAt == null && x.v.PausedAt != null
                            && x.v.PausedAt < clinicTodayUtcStart
                            && !x.p.IsDeleted)
                .OrderBy(x => x.v.PausedAt)
                .Select(x => new PausedVisitDto
                {
                    VisitId = x.v.VisitId,
                    PatientId = x.p.PatientId,
                    PatientName = x.p.Name,
                    PausedAt = x.v.PausedAt!.Value,
                    IsStale = true
                })
                .AsNoTracking()
                .ToListAsync();
        }

        #endregion Lists Management

        #region Specialty Sections

        #region Ob/Gyn

        public async Task SaveObGyneGpaAsync(int visitId, DTOs.ObGyne.GPADto gpa)
        {
            ValidationHelpers.ValidateVisitId(visitId);

            if (gpa == null)
                throw new ArgumentNullException(nameof(gpa));

            var visit = await _db.Visits
                .Include(v => v.Entries)
                .FirstOrDefaultAsync(v => v.VisitId == visitId);

            if (visit == null)
                throw new InvalidOperationException($"Visit with ID {visitId} not found");

            var g = gpa.Gravida;
            var p = gpa.Para;
            var a = gpa.Abortion;

            // Save only if something is filled (matches your “save filled sections only” rule)
            if (!g.HasValue && !p.HasValue && !a.HasValue)
                return;

            visit.AddEntry(
                _obGyneProfile,
                "Obstetric History",
                $"G{g} P{p} A{a}",
                ClinicalSystem.GyneOb);

            await _db.SaveChangesAsync();
        }

        #endregion Ob/Gyn

        #endregion Specialty Sections

        #region Helper Methods

        private static (DateTime todayStartUtc, DateTime tomorrowStartUtc) GetClinicDayRangeUtc()
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Baghdad");

            var nowUtc = DateTime.UtcNow;
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);

            var todayLocalStart = nowLocal.Date;           // 00:00 Baghdad
            var tomorrowLocalStart = todayLocalStart.AddDays(1);

            var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(todayLocalStart, tz);
            var tomorrowStartUtc = TimeZoneInfo.ConvertTimeToUtc(tomorrowLocalStart, tz);

            return (todayStartUtc, tomorrowStartUtc);
        }

        #endregion Helper Methods
    }
}