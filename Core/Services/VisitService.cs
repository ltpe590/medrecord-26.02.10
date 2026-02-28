using Core.DTOs;
using Core.Entities;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;

namespace Core.Services
{
    public class VisitService : IVisitService
    {
        private readonly IVisitRepository    _repo;
        private readonly ISpecialtyProfile   _obGyneProfile;
        private readonly IProfileService     _profileService;
        private readonly IAppSettingsService _appSettings;

        public VisitService(
            IVisitRepository    repo,
            IProfileService     profileService,
            IAppSettingsService appSettings,
            ISpecialtyProfile   obGyneProfile)
        {
            _repo           = repo;
            _profileService = profileService;
            _appSettings    = appSettings;
            _obGyneProfile  = obGyneProfile;
        }

        // ── Start / resume ────────────────────────────────────────────────────

        public async Task<VisitStartResultDto> StartOrResumeVisitAsync(
            int patientId,
            string presentingSymptom,
            string? duration,
            string? shortNote)
        {
            ValidationHelpers.ValidatePatientId(patientId);
            ValidationHelpers.ValidateNotNullOrWhiteSpace(presentingSymptom, nameof(presentingSymptom));

            var activeVisit = await _repo.GetActiveForPatientAsync(patientId);
            if (activeVisit != null)
                return new VisitStartResultDto { VisitId = activeVisit.VisitId, PatientId = patientId, IsResumed = false, StartedAt = activeVisit.StartedAt };

            var pausedVisit = await _repo.GetPausedForPatientAsync(patientId);
            if (pausedVisit != null)
                return new VisitStartResultDto { VisitId = pausedVisit.VisitId, PatientId = patientId, HasPausedVisit = true, PausedVisitId = pausedVisit.VisitId, IsResumed = false, StartedAt = pausedVisit.StartedAt };

            var visit = new Visit(patientId, presentingSymptom, duration ?? "", shortNote ?? "");
            _profileService.InitializeClinicalSections(visit, new[] { _obGyneProfile });
            _repo.Add(visit);
            await _repo.SaveChangesAsync();

            return new VisitStartResultDto { VisitId = visit.VisitId, PatientId = patientId, IsResumed = false, StartedAt = visit.StartedAt };
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public async Task PauseVisitAsync(int visitId)
        {
            ValidationHelpers.ValidateVisitId(visitId);
            var visit = await _repo.GetByIdAsync(visitId) ?? throw new InvalidOperationException($"Visit {visitId} not found");
            visit.Pause();
            await _repo.SaveChangesAsync();
        }

        public async Task ResumeVisitAsync(int visitId)
        {
            ValidationHelpers.ValidateVisitId(visitId);
            var visit = await _repo.GetByIdAsync(visitId) ?? throw new InvalidOperationException($"Visit {visitId} not found");
            visit.Resume();
            await _repo.SaveChangesAsync();
        }

        public async Task EndVisitAsync(int visitId)
        {
            ValidationHelpers.ValidateVisitId(visitId);
            var visit = await _repo.GetByIdAsync(visitId) ?? throw new InvalidOperationException($"Visit {visitId} not found");
            if (visit.EndedAt != null) return;
            visit.EndVisit();
            await _repo.SaveChangesAsync();
        }

        // ── Save ──────────────────────────────────────────────────────────────

        public async Task<VisitSaveResult> SaveVisitAsync(VisitSaveRequest request)
        {
            try
            {
                if (request == null)                         return VisitSaveResult.CreateFailure("Visit request is required.");
                if (request.PatientId <= 0)                  return VisitSaveResult.CreateFailure("Patient ID is required.");
                if (!await _repo.PatientExistsAsync(request.PatientId)) return VisitSaveResult.CreateFailure("Patient not found.");
                if (request.SaveType == VisitSaveType.Edit && (!request.VisitId.HasValue || request.VisitId.Value <= 0))
                    return VisitSaveResult.CreateFailure("Visit ID is required when editing an existing visit.");

                var symptom   = string.IsNullOrWhiteSpace(request.Diagnosis) ? "General review" : request.Diagnosis.Trim();
                var shortNote = string.IsNullOrWhiteSpace(request.Notes)     ? "Visit note"     : request.Notes.Trim();

                Visit visit;
                if (request.VisitId.HasValue && request.VisitId.Value > 0)
                {
                    visit = await _repo.GetByIdAsync(request.VisitId.Value) ?? throw new InvalidOperationException("Visit not found");
                    visit.UpdatePresentingSymptom(symptom, "N/A", shortNote);
                    visit.UpdateVitals(request.Temperature, request.BloodPressureSystolic, request.BloodPressureDiastolic);
                }
                else
                {
                    visit = new Visit(request.PatientId, symptom, "N/A", shortNote);
                    visit.UpdateVitals(request.Temperature, request.BloodPressureSystolic, request.BloodPressureDiastolic);
                    _repo.Add(visit);
                }

                if (request.SaveType == VisitSaveType.New) visit.EndVisit();

                await _repo.SaveChangesAsync();
                return VisitSaveResult.CreateSuccess(visit.VisitId, visit.StartedAt);
            }
            catch (Exception ex)
            {
                return VisitSaveResult.CreateFailure("Failed to save visit", ex);
            }
        }

        // ── Lists ─────────────────────────────────────────────────────────────

        public Task<List<VisitDto>> GetVisitHistoryForPatientAsync(int patientId)
        {
            ValidationHelpers.ValidatePatientId(patientId);
            return _repo.GetVisitHistoryAsync(patientId);
        }

        public Task<List<PausedVisitDto>> GetPausedVisitsTodayAsync()
        {
            var (start, end) = GetClinicDayRangeUtc();
            return _repo.GetPausedVisitsTodayAsync(start, end);
        }

        public Task<List<PausedVisitDto>> GetStalePausedVisitsAsync()
        {
            var (start, _) = GetClinicDayRangeUtc();
            return _repo.GetStalePausedVisitsAsync(start);
        }

        // ── Specialty sections ────────────────────────────────────────────────

        public async Task SaveObGyneGpaAsync(int visitId, DTOs.ObGyne.GPADto gpa)
        {
            ValidationHelpers.ValidateVisitId(visitId);
            if (gpa == null) throw new ArgumentNullException(nameof(gpa));

            var visit = await _repo.GetWithDetailsAsync(visitId)
                ?? throw new InvalidOperationException($"Visit {visitId} not found");

            if (!gpa.Gravida.HasValue && !gpa.Para.HasValue && !gpa.Abortion.HasValue) return;

            visit.AddEntry(_obGyneProfile, "Obstetric History", $"G{gpa.Gravida} P{gpa.Para} A{gpa.Abortion}", ClinicalSystem.GyneOb);
            await _repo.SaveChangesAsync();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>Timezone comes from IAppSettingsService.ClinicTimeZoneId — no hardcoded value here.</summary>
        private (DateTime todayStartUtc, DateTime tomorrowStartUtc) GetClinicDayRangeUtc()
        {
            var tz            = TimeZoneInfo.FindSystemTimeZoneById(_appSettings.ClinicTimeZoneId);
            var nowLocal      = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var todayLocal    = nowLocal.Date;
            var tomorrowLocal = todayLocal.AddDays(1);
            return (
                TimeZoneInfo.ConvertTimeToUtc(todayLocal,    tz),
                TimeZoneInfo.ConvertTimeToUtc(tomorrowLocal, tz)
            );
        }
    }
}
