// CRITICAL FIX #1: StartOrResumeVisitAsync Method
// File: Core/Services/VisitService.cs
// Issue: Method name suggests it resumes, but it doesn't

// OPTION 1: Rename to reflect actual behavior (RECOMMENDED)
// Replace the existing method with this:

public async Task<VisitStartResultDto> StartVisitAsync(
    int patientId,
    string presentingSymptom,
    string? duration,
    string? shortNote)
{
    if (patientId <= 0)
        throw new ArgumentException("Patient ID must be positive", nameof(patientId));
    
    ArgumentException.ThrowIfNullOrWhiteSpace(presentingSymptom);

    // 1. Check for active visit
    var activeVisit = await GetActiveVisitForPatientAsync(patientId);
    if (activeVisit != null)
    {
        return new VisitStartResultDto
        {
            VisitId = activeVisit.VisitId,
            PatientId = patientId,
            IsResumed = false,
            StartedAt = activeVisit.StartedAt,
            HasPausedVisit = false
        };
    }

    // 2. Check for paused visit - BLOCK new visit creation
    var pausedVisit = await _db.Visits
        .Where(v => v.PatientId == patientId && v.EndedAt == null && v.PausedAt != null)
        .OrderByDescending(v => v.PausedAt)
        .Select(v => new { v.VisitId, v.StartedAt })
        .FirstOrDefaultAsync();

    if (pausedVisit != null)
    {
        return new VisitStartResultDto
        {
            VisitId = pausedVisit.VisitId,
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
        StartedAt = visitNew.StartedAt,
        HasPausedVisit = false
    };
}

// OPTION 2: Actually implement auto-resume functionality
// Replace the existing method with this:

public async Task<VisitStartResultDto> StartOrResumeVisitAsync(
    int patientId,
    string presentingSymptom,
    string? duration,
    string? shortNote)
{
    if (patientId <= 0)
        throw new ArgumentException("Patient ID must be positive", nameof(patientId));
    
    ArgumentException.ThrowIfNullOrWhiteSpace(presentingSymptom);

    // 1. Check for active visit
    var activeVisit = await GetActiveVisitForPatientAsync(patientId);
    if (activeVisit != null)
    {
        return new VisitStartResultDto
        {
            VisitId = activeVisit.VisitId,
            PatientId = patientId,
            IsResumed = false,
            StartedAt = activeVisit.StartedAt,
            HasPausedVisit = false
        };
    }

    // 2. Check for paused visit - AUTO-RESUME it
    var pausedVisit = await _db.Visits
        .Where(v => v.PatientId == patientId && v.EndedAt == null && v.PausedAt != null)
        .OrderByDescending(v => v.PausedAt)
        .FirstOrDefaultAsync();

    if (pausedVisit != null)
    {
        // Actually resume the visit
        pausedVisit.Resume();
        
        // Update presenting symptom with new info
        pausedVisit.UpdatePresentingSymptom(presentingSymptom, duration ?? "", shortNote ?? "");
        
        await _db.SaveChangesAsync();

        return new VisitStartResultDto
        {
            VisitId = pausedVisit.VisitId,
            PatientId = patientId,
            HasPausedVisit = false,
            PausedVisitId = null,
            IsResumed = true,  // ✅ Actually resumed!
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
        StartedAt = visitNew.StartedAt,
        HasPausedVisit = false
    };
}

// Update IVisitService interface:
// File: Core/Interfaces/Services/IVisitService.cs
// Change this line:
// FROM:
Task<VisitStartResultDto> StartOrResumeVisitAsync(int patientId, string presentingSymptom, string? duration, string? shortNote);

// TO (if using Option 1):
Task<VisitStartResultDto> StartVisitAsync(int patientId, string presentingSymptom, string? duration, string? shortNote);

// OR keep as-is if using Option 2
