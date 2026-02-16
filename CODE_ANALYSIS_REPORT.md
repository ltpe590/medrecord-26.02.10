# MedRecord Code Analysis Report
*Generated: February 13, 2026*

## 🔍 COMPREHENSIVE CODE ANALYSIS

---

## ⚠️ CRITICAL ISSUES

### 1. **Missing ResumeVisitAsync Call in StartOrResumeVisitAsync**
**File**: `Core/Services/VisitService.cs`
**Line**: ~50
**Severity**: HIGH

**Issue**:
The method is named `StartOrResumeVisitAsync` but it NEVER actually resumes a paused visit. When a paused visit is found, it returns the paused visit info but doesn't call `ResumeVisitAsync()`.

**Current Code**:
```csharp
if (pausedVisit != null)
{
    return new VisitStartResultDto
    {
        VisitId = pausedVisit.VisitId,
        PatientId = patientId,
        HasPausedVisit = true,
        PausedVisitId = pausedVisit.VisitId,
        IsResumed = false,  // ⚠️ Always false!
        StartedAt = pausedVisit.StartedAt
    };
}
```

**Fix**:
Option 1 - Rename method to reflect actual behavior:
```csharp
public async Task<VisitStartResultDto> StartVisitAsync(...)
```

Option 2 - Actually implement resume logic:
```csharp
if (pausedVisit != null)
{
    // Resume the paused visit
    await ResumeVisitAsync(pausedVisit.VisitId);
    
    return new VisitStartResultDto
    {
        VisitId = pausedVisit.VisitId,
        PatientId = patientId,
        HasPausedVisit = false,
        IsResumed = true,  // ✅ Actually resumed
        StartedAt = pausedVisit.StartedAt
    };
}
```

---

### 2. **Missing Cancellation Token Support**
**Files**: All async methods
**Severity**: MEDIUM

**Issue**:
No async methods accept `CancellationToken`, making it impossible to cancel long-running operations.

**Impact**:
- Cannot cancel database operations
- Cannot timeout API calls
- Cannot cancel on application shutdown
- UI hangs with no way to cancel

**Fix Example**:
```csharp
// Current
public async Task<Visit?> GetActiveVisitForPatientAsync(int patientId)

// Fixed
public async Task<Visit?> GetActiveVisitForPatientAsync(
    int patientId, 
    CancellationToken cancellationToken = default)
{
    return await _db.Visits
        .Where(v => v.PatientId == patientId && v.EndedAt == null && v.PausedAt == null)
        .FirstOrDefaultAsync(cancellationToken);  // ✅ Pass token
}
```

---

### 3. **No Input Validation in VisitService**
**File**: `Core/Services/VisitService.cs`
**Severity**: MEDIUM

**Issue**:
Most service methods don't validate input parameters before processing.

**Examples**:
```csharp
// ❌ No validation
public async Task PauseVisitAsync(int visitId)
{
    var visit = await _db.Visits.FindAsync(visitId);
    if (visit == null)
        throw new InvalidOperationException("Visit not found");
    // ...
}

// ✅ With validation
public async Task PauseVisitAsync(int visitId)
{
    if (visitId <= 0)
        throw new ArgumentException("Visit ID must be positive", nameof(visitId));
        
    var visit = await _db.Visits.FindAsync(visitId);
    if (visit == null)
        throw new InvalidOperationException($"Visit with ID {visitId} not found");
    // ...
}
```

---

### 4. **Inconsistent Error Handling**
**Files**: Services layer
**Severity**: MEDIUM

**Issue**:
Some methods throw exceptions, others return null, others return failure results. No consistent pattern.

**Examples**:
```csharp
// PatientService - throws exceptions
public async Task<PatientDto?> GetPatientByIdAsync(int id)
{
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting patient by ID {PatientId}", id);
        throw;  // ⚠️ Re-throws
    }
}

// VisitService.SaveVisitAsync - returns failure result
public async Task<VisitSaveResult> SaveVisitAsync(VisitSaveRequest request)
{
    catch (Exception ex)
    {
        return VisitSaveResult.CreateFailure("Failed to save visit", ex);  // ⚠️ Returns result
    }
}

// VisitService.GetActiveVisitForPatientAsync - returns null
public async Task<Visit?> GetActiveVisitForPatientAsync(int patientId)
{
    return await _db.Visits...FirstOrDefaultAsync();  // ⚠️ Returns null
}
```

**Recommendation**: 
Use Result pattern consistently across all services.

---

## 🐛 BUGS & LOGIC ERRORS

### 5. **Timezone Calculation Executed Twice**
**File**: `Core/Services/VisitService.cs`
**Line**: ~230

**Issue**:
```csharp
public async Task<List<PausedVisitDto>> GetPausedVisitsTodayAsync(
    DateTime clinicTodayUtcStart, 
    DateTime clinicTomorrowUtcStart)
{
    // ⚠️ Parameters passed in are IGNORED!
    (clinicTodayUtcStart, clinicTomorrowUtcStart) = GetClinicDayRangeUtc();
    // ...
}
```

The method accepts timezone parameters but immediately overwrites them!

**Fix**:
```csharp
// Option 1 - Remove parameters
public async Task<List<PausedVisitDto>> GetPausedVisitsTodayAsync()
{
    var (clinicTodayUtcStart, clinicTomorrowUtcStart) = GetClinicDayRangeUtc();
    // ...
}

// Option 2 - Use parameters
public async Task<List<PausedVisitDto>> GetPausedVisitsTodayAsync(
    DateTime clinicTodayUtcStart, 
    DateTime clinicTomorrowUtcStart)
{
    // Don't recalculate - use passed parameters
    return await _db.Visits...
}
```

---

### 6. **Missing Resume Logic Creates Orphaned Paused Visits**
**Severity**: HIGH

**Issue**:
If UI doesn't explicitly call `ResumeVisitAsync()`, paused visits remain paused forever. The `StartOrResumeVisitAsync` method doesn't auto-resume.

**Scenario**:
1. User pauses Visit A
2. User tries to start new visit for same patient
3. System blocks with "HasPausedVisit=true"
4. User clicks "OK" but doesn't explicitly resume
5. Visit A remains paused indefinitely

**Fix**: 
Either auto-resume in `StartOrResumeVisitAsync` or ensure UI always calls resume.

---

### 7. **Race Condition in StartOrResumeVisitAsync**
**Severity**: LOW

**Issue**:
Multiple concurrent calls could create duplicate active visits.

**Scenario**:
```
Thread 1: Check active visit → None found
Thread 2: Check active visit → None found
Thread 1: Create new visit → ACTIVE
Thread 2: Create new visit → ACTIVE (duplicate!)
```

**Fix**:
Add database unique constraint + handle exception:
```csharp
// Migration
CREATE UNIQUE INDEX IX_Visits_PatientId_Active 
ON Visits(PatientId) 
WHERE EndedAt IS NULL AND PausedAt IS NULL;

// Service
try
{
    _db.Visits.Add(visitNew);
    await _db.SaveChangesAsync();
}
catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
{
    // Concurrent creation detected - retry
    return await StartOrResumeVisitAsync(patientId, presentingSymptom, duration, shortNote);
}
```

---

## 🔒 SECURITY ISSUES

### 8. **No Authorization Checks in Services**
**Severity**: HIGH

**Issue**:
Service layer has no authorization checks. Any authenticated user can access/modify any patient.

**Example**:
```csharp
public async Task EndVisitAsync(int visitId)
{
    // ⚠️ No check if user has permission to end this visit
    var visit = await _db.Visits.FindAsync(visitId);
    visit.EndVisit();
    await _db.SaveChangesAsync();
}
```

**Fix**:
```csharp
public async Task EndVisitAsync(int visitId, string userId)
{
    var visit = await _db.Visits.FindAsync(visitId);
    
    // Check authorization
    if (!await _authorizationService.CanAccessVisit(userId, visitId))
        throw new UnauthorizedAccessException("User cannot access this visit");
        
    visit.EndVisit();
    await _db.SaveChangesAsync();
}
```

---

### 9. **SQL Injection Vulnerability (Low Risk)**
**File**: Query operations
**Severity**: LOW (EF Core protects, but anti-pattern exists)

**Issue**:
Using string comparisons without proper sanitization:
```csharp
var existing = Entries.FirstOrDefault(e =>
    e.Section.Equals(section, StringComparison.OrdinalIgnoreCase));
```

While EF Core parameterizes queries, this pattern is risky if ever moved to raw SQL.

**Recommendation**: 
Continue using EF Core LINQ. Avoid raw SQL without parameterization.

---

## ⚡ PERFORMANCE ISSUES

### 10. **N+1 Query Problem in Visit History**
**File**: `Core/Services/VisitService.cs`
**Severity**: MEDIUM

**Issue**:
```csharp
public async Task<List<Visit>> GetVisitHistoryForPatientAsync(int patientId)
{
    return await _db.Visits
        .Where(v => v.PatientId == patientId && v.EndedAt != null)
        .OrderByDescending(v => v.EndedAt)
        .AsNoTracking()
        .ToListAsync();
    // ⚠️ No .Include(v => v.Entries) - lazy loading will trigger N+1
}
```

If caller accesses `visit.Entries`, it triggers additional queries.

**Fix**:
```csharp
return await _db.Visits
    .Include(v => v.Entries)  // ✅ Eager load
    .Where(v => v.PatientId == patientId && v.EndedAt != null)
    .OrderByDescending(v => v.EndedAt)
    .AsNoTracking()
    .ToListAsync();
```

---

### 11. **Missing Database Indexes**
**Severity**: MEDIUM

**Issue**:
Queries on `PatientId`, `EndedAt`, `PausedAt` will perform table scans.

**Recommended Indexes**:
```sql
-- Active visits query
CREATE INDEX IX_Visits_PatientId_EndedAt_PausedAt 
ON Visits(PatientId, EndedAt, PausedAt);

-- Visit history query
CREATE INDEX IX_Visits_PatientId_EndedAt 
ON Visits(PatientId, EndedAt DESC);

-- Paused visits by date
CREATE INDEX IX_Visits_PausedAt 
ON Visits(PausedAt) 
WHERE EndedAt IS NULL;
```

---

### 12. **Inefficient JOIN in GetPausedVisitsTodayAsync**
**Severity**: LOW

**Issue**:
```csharp
.Join(_db.Patients, v => v.PatientId, p => p.PatientId, (v, p) => new { v, p })
```

Could use navigation property instead:

**Fix**:
```csharp
return await _db.Visits
    .Include(v => v.Patient)  // ✅ Use navigation property
    .Where(v => v.EndedAt == null 
             && v.PausedAt != null
             && v.PausedAt >= clinicTodayUtcStart
             && v.PausedAt < clinicTomorrowUtcStart
             && !v.Patient.IsDeleted)
    .OrderByDescending(v => v.PausedAt)
    .Select(v => new PausedVisitDto
    {
        VisitId = v.VisitId,
        PatientId = v.Patient.PatientId,
        PatientName = v.Patient.Name,
        PausedAt = v.PausedAt!.Value,
        IsStale = false
    })
    .AsNoTracking()
    .ToListAsync();
```

But requires navigation property in Visit entity:
```csharp
public class Visit
{
    // Add this
    public Patient Patient { get; private set; } = null!;
}
```

---

## 🧪 TESTABILITY ISSUES

### 13. **Tight Coupling to DateTime.UtcNow**
**Severity**: MEDIUM

**Issue**:
```csharp
public Visit(int patientId, string presentingSymptom, string duration, string shortNote)
{
    StartedAt = DateTime.UtcNow;  // ⚠️ Cannot mock for testing
}

public void Pause()
{
    PausedAt = DateTime.UtcNow;  // ⚠️ Cannot mock for testing
}
```

**Fix**:
Inject `IDateTimeProvider`:
```csharp
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

// In entity
private readonly IDateTimeProvider _dateTimeProvider;

public Visit(int patientId, ..., IDateTimeProvider dateTimeProvider)
{
    _dateTimeProvider = dateTimeProvider;
    StartedAt = _dateTimeProvider.UtcNow;  // ✅ Testable
}
```

---

### 14. **No Unit Tests**
**Severity**: HIGH

**Issue**:
No test projects found in solution.

**Recommendation**:
Add test projects:
- `Core.UnitTests` - Domain logic tests
- `Core.IntegrationTests` - Database tests
- `WebApi.Tests` - API controller tests

---

## 📐 DESIGN ISSUES

### 15. **God Service Anti-Pattern**
**File**: `Core/Services/VisitService.cs`
**Severity**: MEDIUM

**Issue**:
VisitService has too many responsibilities:
- Visit lifecycle management
- Querying (active, paused, history)
- Specialty-specific logic (ObGyne)
- Timezone calculations

**Fix**:
Split into focused services:
```csharp
IVisitLifecycleService  // Start, Pause, Resume, End
IVisitQueryService      // GetActive, GetHistory, GetPaused
IObGyneVisitService     // SaveObGyneGpaAsync
```

---

### 16. **Anemic Domain Model**
**Severity**: LOW

**Issue**:
Visit entity has some business logic but services still do a lot:
```csharp
// Service does too much
public async Task SaveObGyneGpaAsync(int visitId, DTOs.ObGyne.GPADto gpa)
{
    var visit = await _db.Visits.Include(v => v.Entries).FirstOrDefaultAsync(...);
    
    if (!g.HasValue && !p.HasValue && !a.HasValue)
        return;
        
    visit.AddEntry(_obGyneProfile, "Obstetric History", $"G{g} P{g} A{a}", ...);
}

// Could be in entity
public void AddObGyneGpa(int? gravida, int? para, int? abortion, ISpecialtyProfile profile)
{
    if (!gravida.HasValue && !para.HasValue && !abortion.HasValue)
        return;
        
    AddEntry(profile, "Obstetric History", $"G{gravida} P{para} A{abortion}", 
             ClinicalSystem.GyneOb);
}
```

---

### 17. **Missing Repository Methods**
**Severity**: LOW

**Issue**:
VisitService directly uses EF queries instead of repository:
```csharp
var pausedVisit = await _db.Visits
    .Where(v => v.PatientId == patientId && v.EndedAt == null && v.PausedAt != null)
    .OrderByDescending(v => v.PausedAt)
    .FirstOrDefaultAsync();
```

**Fix**:
Add to `IVisitRepository`:
```csharp
Task<Visit?> GetPausedVisitForPatientAsync(int patientId);
Task<List<Visit>> GetPausedVisitsTodayAsync(DateTime start, DateTime end);
Task<List<Visit>> GetStaleVisitsAsync(DateTime beforeDate);
```

---

## 🎨 CODE QUALITY ISSUES

### 18. **Magic Strings**
**Severity**: LOW

**Issue**:
```csharp
var symptom = string.IsNullOrWhiteSpace(request.Diagnosis) 
    ? "General review"  // ⚠️ Magic string
    : request.Diagnosis.Trim();
var duration = "N/A";  // ⚠️ Magic string
var shortNote = string.IsNullOrWhiteSpace(request.Notes) 
    ? "Visit note"  // ⚠️ Magic string
    : request.Notes.Trim();
```

**Fix**:
```csharp
public static class VisitDefaults
{
    public const string DefaultSymptom = "General review";
    public const string DefaultDuration = "N/A";
    public const string DefaultNote = "Visit note";
}
```

---

### 19. **Inconsistent Null Handling**
**Severity**: LOW

**Issue**:
```csharp
// Sometimes null-coalescing
var visitNew = new Visit(patientId, presentingSymptom, duration ?? "", shortNote ?? "");

// Sometimes check and default
var symptom = string.IsNullOrWhiteSpace(request.Diagnosis) 
    ? "General review" 
    : request.Diagnosis.Trim();
```

**Fix**: Pick one pattern and use consistently.

---

### 20. **No Logging in Domain Entities**
**Severity**: LOW (by design)

**Issue**:
Entities don't log state changes, making debugging difficult.

**Recommendation**:
Consider domain events for important state changes:
```csharp
public void Pause()
{
    if (EndedAt != null)
        throw new InvalidOperationException("Cannot pause a finished visit.");
    
    PausedAt = DateTime.UtcNow;
    
    // Raise event for logging/auditing
    AddDomainEvent(new VisitPausedEvent(VisitId, PausedAt.Value));
}
```

---

## 📊 SUMMARY

### Issue Count by Severity
- **CRITICAL**: 1 (Missing resume logic)
- **HIGH**: 4 (Cancellation tokens, authorization, tests, orphaned visits)
- **MEDIUM**: 8 (Validation, error handling, performance, design)
- **LOW**: 7 (Code quality, testability, patterns)

### Top 5 Priority Fixes
1. ✅ Fix `StartOrResumeVisitAsync` - either rename or implement resume
2. ✅ Add authorization checks to all service methods
3. ✅ Add `CancellationToken` support throughout
4. ✅ Add input validation to service methods
5. ✅ Fix timezone parameter bug in `GetPausedVisitsTodayAsync`

---

## 🛠️ RECOMMENDED NEXT STEPS

### Immediate (This Week)
1. Fix critical bugs (#1, #5, #6)
2. Add input validation (#3)
3. Add authorization (#8)

### Short Term (This Month)
4. Add cancellation token support (#2)
5. Standardize error handling (#4)
6. Add database indexes (#11)
7. Add unit tests (#14)

### Long Term (This Quarter)
8. Refactor services (#15, #16, #17)
9. Add domain events (#20)
10. Implement Result pattern consistently
11. Add integration tests
12. Performance optimization

---

*End of Analysis Report*
