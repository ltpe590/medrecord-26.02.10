// CRITICAL FIX #5: Timezone Parameter Bug
// File: Core/Services/VisitService.cs
// Issue: Method parameters are passed in but immediately overwritten

// CURRENT BROKEN CODE:
/*
public async Task<List<PausedVisitDto>> GetPausedVisitsTodayAsync(
    DateTime clinicTodayUtcStart, 
    DateTime clinicTomorrowUtcStart)
{
    // ⚠️ Parameters ignored! Recalculated inside method
    (clinicTodayUtcStart, clinicTomorrowUtcStart) = GetClinicDayRangeUtc();
    
    return await _db.Visits...
}
*/

// FIX: Remove unused parameters
public async Task<List<PausedVisitDto>> GetPausedVisitsTodayAsync()
{
    var (clinicTodayUtcStart, clinicTomorrowUtcStart) = GetClinicDayRangeUtc();

    return await _db.Visits
        .Join(_db.Patients,
            v => v.PatientId,
            p => p.PatientId,
            (v, p) => new { v, p })
        .Where(x => x.v.EndedAt == null 
                 && x.v.PausedAt != null
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

// SAME FIX for GetStalePausedVisitsAsync:
public async Task<List<PausedVisitDto>> GetStalePausedVisitsAsync()
{
    var (clinicTodayUtcStart, _) = GetClinicDayRangeUtc();

    return await _db.Visits
        .Join(_db.Patients,
            v => v.PatientId,
            p => p.PatientId,
            (v, p) => new { v, p })
        .Where(x => x.v.EndedAt == null 
                 && x.v.PausedAt != null
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

// Update IVisitService interface:
// File: Core/Interfaces/Services/IVisitService.cs
// FROM:
Task<List<PausedVisitDto>> GetPausedVisitsTodayAsync(DateTime clinicTodayUtcStart, DateTime clinicTomorrowUtcStart);
Task<List<PausedVisitDto>> GetStalePausedVisitsAsync(DateTime clinicTodayUtcStart);

// TO:
Task<List<PausedVisitDto>> GetPausedVisitsTodayAsync();
Task<List<PausedVisitDto>> GetStalePausedVisitsAsync();
