// FIX #3: Add Input Validation to Service Methods
// File: Core/Services/VisitService.cs
// Issue: Missing validation for input parameters

// Add this helper class at the top of the file or in a separate ValidationHelpers.cs:
internal static class ValidationHelpers
{
    public static void ValidateVisitId(int visitId, string paramName = "visitId")
    {
        if (visitId <= 0)
            throw new ArgumentException("Visit ID must be positive", paramName);
    }

    public static void ValidatePatientId(int patientId, string paramName = "patientId")
    {
        if (patientId <= 0)
            throw new ArgumentException("Patient ID must be positive", paramName);
    }

    public static void ValidateNotNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} cannot be null or whitespace", paramName);
    }
}

// Update PauseVisitAsync:
public async Task PauseVisitAsync(int visitId)
{
    ValidationHelpers.ValidateVisitId(visitId);  // ✅ Add validation

    var visit = await _db.Visits.FindAsync(visitId);
    if (visit == null)
        throw new InvalidOperationException($"Visit with ID {visitId} not found");

    visit.Pause();
    await _db.SaveChangesAsync();
}

// Update ResumeVisitAsync:
public async Task ResumeVisitAsync(int visitId)
{
    ValidationHelpers.ValidateVisitId(visitId);  // ✅ Add validation

    var visit = await _db.Visits.FindAsync(visitId);
    if (visit == null)
        throw new InvalidOperationException($"Visit with ID {visitId} not found");

    visit.Resume();
    await _db.SaveChangesAsync();
}

// Update EndVisitAsync:
public async Task EndVisitAsync(int visitId)
{
    ValidationHelpers.ValidateVisitId(visitId);  // ✅ Add validation

    var visit = await _db.Visits.FindAsync(visitId);
    if (visit == null)
        throw new InvalidOperationException($"Visit with ID {visitId} not found");

    if (visit.EndedAt != null)
        return;

    visit.EndVisit();
    await _db.SaveChangesAsync();
}

// Update GetActiveVisitForPatientAsync:
public async Task<Visit?> GetActiveVisitForPatientAsync(int patientId)
{
    ValidationHelpers.ValidatePatientId(patientId);  // ✅ Add validation

    return await _db.Visits
        .Where(v => v.PatientId == patientId && v.EndedAt == null && v.PausedAt == null)
        .FirstOrDefaultAsync();
}

// Update GetVisitHistoryForPatientAsync:
public async Task<List<Visit>> GetVisitHistoryForPatientAsync(int patientId)
{
    ValidationHelpers.ValidatePatientId(patientId);  // ✅ Add validation

    return await _db.Visits
        .Where(v => v.PatientId == patientId && v.EndedAt != null)
        .OrderByDescending(v => v.EndedAt)
        .AsNoTracking()
        .ToListAsync();
}

// Update SaveObGyneGpaAsync:
public async Task SaveObGyneGpaAsync(int visitId, DTOs.ObGyne.GPADto gpa)
{
    ValidationHelpers.ValidateVisitId(visitId);  // ✅ Add validation
    
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

    // Save only if something is filled
    if (!g.HasValue && !p.HasValue && !a.HasValue)
        return;

    visit.AddEntry(
        _obGyneProfile,
        "Obstetric History",
        $"G{g} P{p} A{a}",
        ClinicalSystem.GyneOb);

    await _db.SaveChangesAsync();
}
