# Visit Logic Consolidation - MedRecord Core Project

## 📌 Overview

This document consolidates the complete Visit lifecycle management logic in the MedRecord system, covering all operations: **Start**, **Pause**, **Resume**, **Save**, and **End**.

---

## 🎯 Visit State Machine

### Visit States

A Visit can be in one of **three states**:

```
┌─────────────────────────────────────────────────────────────┐
│                     VISIT STATE MACHINE                     │
└─────────────────────────────────────────────────────────────┘

1. ACTIVE      → EndedAt = null, PausedAt = null
2. PAUSED      → EndedAt = null, PausedAt != null  
3. COMPLETED   → EndedAt != null (PausedAt cleared)
```

### State Properties (in Visit.cs)

```csharp
public DateTime StartedAt { get; private set; }      // Always set on creation
public DateTime? EndedAt { get; private set; }       // null until visit ends
public DateTime? PausedAt { get; private set; }      // null unless paused
public bool IsPaused => PausedAt != null && EndedAt == null;  // Computed property
```

---

## 🔄 State Transitions

### 1. START VISIT

**Method**: `VisitService.StartOrResumeVisitAsync()`

**Logic Flow**:
```csharp
1. Check if patient has ACTIVE visit
   → If YES: Return existing active visit (no new visit created)
   
2. Check if patient has PAUSED visit
   → If YES: Block creation, return PausedVisitId (user must resume or end)
   
3. Create NEW visit
   → New Visit(patientId, symptom, duration, note)
   → StartedAt = DateTime.UtcNow
   → EndedAt = null
   → PausedAt = null
   → Initialize clinical sections (specialty profiles)
   → Save to database
```

**Business Rules**:
- ✅ **One active visit per patient** at any time
- ✅ **Cannot start new** if paused visit exists
- ✅ **Must resume or end** paused visit first
- ✅ Automatic specialty section initialization

**Code**:
```csharp
public async Task<VisitStartResultDto> StartOrResumeVisitAsync(
    int patientId,
    string presentingSymptom,
    string? duration,
    string? shortNote)
{
    // 1. Check for active visit
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

    // 2. Check for paused visit (blocks new visit creation)
    var pausedVisit = await _db.Visits
        .Where(v => v.PatientId == patientId 
                 && v.EndedAt == null 
                 && v.PausedAt != null)
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
        StartedAt = visitNew.StartedAt
    };
}
```

---

### 2. PAUSE VISIT

**Method**: `VisitService.PauseVisitAsync()` → `Visit.Pause()`

**Entity Logic** (Visit.cs):
```csharp
public void Pause()
{
    if (EndedAt != null)
        throw new InvalidOperationException("Cannot pause a finished visit.");
    
    if (IsPaused)
        return; // Already paused - idempotent
    
    PausedAt = DateTime.UtcNow;
}
```

**Service Logic** (VisitService.cs):
```csharp
public async Task PauseVisitAsync(int visitId)
{
    var visit = await _db.Visits.FindAsync(visitId);
    if (visit == null)
        throw new InvalidOperationException("Visit not found");

    visit.Pause();  // Domain logic in entity
    await _db.SaveChangesAsync();
}
```

**Business Rules**:
- ✅ Can only pause ACTIVE visits
- ✅ Cannot pause COMPLETED visits
- ✅ Idempotent - no error if already paused
- ✅ Sets PausedAt timestamp

**State Change**:
```
ACTIVE → PAUSED
  (EndedAt=null, PausedAt=null) → (EndedAt=null, PausedAt=Now)
```

---

### 3. RESUME VISIT

**Method**: `VisitService.ResumeVisitAsync()` → `Visit.Resume()`

**Entity Logic** (Visit.cs):
```csharp
public void Resume()
{
    if (!IsPaused)
        return; // Not paused, nothing to resume - idempotent
    
    PausedAt = null;  // Clear pause timestamp
}
```

**Service Logic** (VisitService.cs):
```csharp
public async Task ResumeVisitAsync(int visitId)
{
    var visit = await _db.Visits.FindAsync(visitId);
    if (visit == null)
        throw new InvalidOperationException("Visit not found");

    visit.Resume();  // Domain logic in entity
    await _db.SaveChangesAsync();
}
```

**Business Rules**:
- ✅ Can only resume PAUSED visits
- ✅ Idempotent - no error if not paused
- ✅ Clears PausedAt timestamp
- ✅ Visit returns to ACTIVE state

**State Change**:
```
PAUSED → ACTIVE
  (EndedAt=null, PausedAt=X) → (EndedAt=null, PausedAt=null)
```

---

### 4. END VISIT

**Method**: `VisitService.EndVisitAsync()` → `Visit.EndVisit()`

**Entity Logic** (Visit.cs):
```csharp
public void EndVisit()
{
    if (EndedAt != null)
        return; // Already ended - idempotent
    
    PausedAt = null;        // Clear pause state
    EndedAt = DateTime.UtcNow;  // Mark as completed
}
```

**Service Logic** (VisitService.cs):
```csharp
public async Task EndVisitAsync(int visitId)
{
    var visit = await _db.Visits.FindAsync(visitId);
    if (visit == null)
        throw new InvalidOperationException("Visit not found");

    if (visit.EndedAt != null)
        return;  // Already ended

    visit.EndVisit();  // Domain logic in entity
    await _db.SaveChangesAsync();
}
```

**Business Rules**:
- ✅ Can end from ACTIVE or PAUSED state
- ✅ Automatically clears pause state
- ✅ Idempotent - no error if already ended
- ✅ Sets EndedAt timestamp

**State Change**:
```
ACTIVE → COMPLETED
  (EndedAt=null, PausedAt=null) → (EndedAt=Now, PausedAt=null)

PAUSED → COMPLETED  
  (EndedAt=null, PausedAt=X) → (EndedAt=Now, PausedAt=null)
```

---

### 5. SAVE VISIT

**Method**: `VisitService.SaveVisitAsync(VisitSaveRequest)`

**Purpose**: Multi-purpose save operation supporting:
1. **New Visit** - Create and immediately complete
2. **Edit Visit** - Update existing visit
3. **Resume Visit** - Continue paused visit (though Resume logic is separate)

**Logic Flow**:
```csharp
public async Task<VisitSaveResult> SaveVisitAsync(VisitSaveRequest request)
{
    // 1. Validation
    if (request.PatientId <= 0)
        return VisitSaveResult.CreateFailure("Patient ID is required.");
    
    var patientExists = await _db.Patients
        .AnyAsync(p => p.PatientId == request.PatientId && !p.IsDeleted);
    
    if (!patientExists)
        return VisitSaveResult.CreateFailure("Patient not found.");

    // 2. Data preparation
    var symptom = string.IsNullOrWhiteSpace(request.Diagnosis) 
        ? "General review" 
        : request.Diagnosis.Trim();
    var duration = "N/A";
    var shortNote = string.IsNullOrWhiteSpace(request.Notes) 
        ? "Visit note" 
        : request.Notes.Trim();

    Visit visit;

    // 3. Edit validation
    if (request.SaveType == VisitSaveType.Edit 
        && (!request.VisitId.HasValue || request.VisitId.Value <= 0))
    {
        return VisitSaveResult.CreateFailure(
            "Visit ID is required when editing an existing visit.");
    }

    // 4. Update existing OR create new
    if (request.VisitId.HasValue && request.VisitId.Value > 0)
    {
        // UPDATE existing visit
        visit = await _db.Visits.FirstOrDefaultAsync(v => v.VisitId == request.VisitId.Value)
            ?? throw new InvalidOperationException("Visit not found");

        visit.UpdatePresentingSymptom(symptom, duration, shortNote);
        visit.UpdateVitals(request.Temperature, 
                          request.BloodPressureSystolic, 
                          request.BloodPressureDiastolic);
    }
    else
    {
        // CREATE new visit
        visit = new Visit(request.PatientId, symptom, duration, shortNote);
        visit.UpdateVitals(request.Temperature, 
                          request.BloodPressureSystolic, 
                          request.BloodPressureDiastolic);
        _db.Visits.Add(visit);
    }

    // 5. Auto-complete for new visits
    if (request.SaveType == VisitSaveType.New)
    {
        visit.EndVisit();  // Immediately mark as completed
    }

    // 6. Persist
    await _db.SaveChangesAsync();
    return VisitSaveResult.CreateSuccess(visit.VisitId, visit.StartedAt);
}
```

**SaveType Enum**:
```csharp
public enum VisitSaveType
{
    New,      // Create new visit and complete it immediately
    Resume,   // Continue paused visit (deprecated - use ResumeVisitAsync)
    Edit      // Update existing visit
}
```

**Business Rules**:
- ✅ `SaveType.New` → Creates AND completes visit in one operation
- ✅ `SaveType.Edit` → Requires VisitId, updates existing
- ✅ Auto-fills defaults: "General review", "Visit note", "N/A"
- ✅ Updates vitals (Temperature, BP)
- ✅ Validates patient exists and not deleted

---

## 🗂️ Visit Data Structure

### Core Visit Entity

```csharp
public class Visit
{
    // Identity
    public int VisitId { get; private set; }
    public int PatientId { get; private set; }

    // State timestamps
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public DateTime? PausedAt { get; private set; }
    
    // Computed state
    public bool IsPaused => PausedAt != null && EndedAt == null;

    // Clinical data
    public string PresentingSymptomText { get; private set; }
    public string PresentingSymptomDurationText { get; private set; }
    public string ShortNote { get; private set; }

    // Related entities
    public ICollection<VisitEntry> Entries { get; private set; }
    public Vitals? Vitals { get; private set; }
}
```

---

### Vitals Value Object

```csharp
public class Vitals
{
    public decimal? Temperature { get; private set; }
    public int? Systolic { get; private set; }
    public int? Diastolic { get; private set; }

    public Vitals(decimal? temperature, int? systolic, int? diastolic)
    {
        Temperature = temperature;
        Systolic = systolic;
        Diastolic = diastolic;
    }
}
```

**Usage**:
```csharp
visit.UpdateVitals(37.2m, 120, 80);  // Creates new Vitals value object
```

---

### Visit Entry (Clinical Documentation)

```csharp
public class VisitEntry
{
    public int VisitEntryId { get; private set; }
    public int VisitId { get; private set; }

    public string Section { get; private set; }      // e.g., "Obstetric History"
    public string? SystemCode { get; private set; }  // e.g., "OBGYN" (stored in DB)
    public string Content { get; private set; }      // Actual clinical text

    [NotMapped]
    public ClinicalSystem? System => SystemCode is null 
        ? null 
        : ClinicalSystem.FromCode(SystemCode);  // Computed from code
}
```

**Adding Entries** (Upsert pattern):
```csharp
public VisitEntry? AddEntry(
    ISpecialtyProfile profile, 
    string section, 
    string content, 
    ClinicalSystem? system = null)
{
    if (string.IsNullOrWhiteSpace(content))
        return null;  // Don't save empty entries

    var existing = Entries.FirstOrDefault(e =>
        e.Section.Equals(section, StringComparison.OrdinalIgnoreCase));

    if (existing != null)
    {
        existing.Update(section, content, system);  // UPDATE existing
        return existing;
    }

    var entry = new VisitEntry(VisitId, section, content, system);
    Entries.Add(entry);  // CREATE new
    return entry;
}
```

**Key Principle**: **Only filled sections are saved to database**
- UI can show all sections
- Database stores only non-empty entries
- Upsert pattern: update if exists, insert if new

---

## 🏥 Clinical System Categorization

### Clinical Systems (Predefined)

```csharp
public sealed class ClinicalSystem
{
    public string Code { get; }  // e.g., "OBGYN"
    public string Name { get; }  // e.g., "Gynecology / Obstetrics"

    // Predefined systems
    public static readonly ClinicalSystem General = new("GEN", "General");
    public static readonly ClinicalSystem Cardiovascular = new("CVS", "Cardiovascular");
    public static readonly ClinicalSystem Respiratory = new("RESP", "Respiratory");
    public static readonly ClinicalSystem GyneOb = new("OBGYN", "Gynecology / Obstetrics");
    public static readonly ClinicalSystem Neurological = new("NEURO", "Neurological");
    public static readonly ClinicalSystem Endocrine = new("ENDO", "Endocrine");
    public static readonly ClinicalSystem Hematology = new("HEMA", "Hematology");
    public static readonly ClinicalSystem Gastrointestinal = new("GIT", "Gastrointestinal");
    public static readonly ClinicalSystem Musculoskeletal = new("MSK", "Musculoskeletal");
    public static readonly ClinicalSystem Renal = new("RENAL", "Renal");
    public static readonly ClinicalSystem Dermatology = new("DERM", "Dermatology");
    public static readonly ClinicalSystem Psychiatric = new("PSY", "Psychiatric");
    public static readonly ClinicalSystem Ophthalmology = new("OPHT", "Ophthalmology");
    public static readonly ClinicalSystem Orthopedic = new("ORTHO", "Orthopedic");
    public static readonly ClinicalSystem Uncategorized = new("UNCAT", "Uncategorized");

    // Lookup methods
    public static ClinicalSystem? FromCode(string code) { ... }
    public static ClinicalSystem? FromName(string name) { ... }
}
```

**Usage in Visit Entries**:
```csharp
visit.AddEntry(
    _obGyneProfile,
    "Obstetric History",
    "G3 P2 A1",
    ClinicalSystem.GyneOb  // Categorizes entry
);
```

---

## 📋 Query Operations

### Get Active Visit for Patient

```csharp
public async Task<Visit?> GetActiveVisitForPatientAsync(int patientId)
{
    return await _db.Visits
        .Where(v => v.PatientId == patientId 
                 && v.EndedAt == null 
                 && v.PausedAt == null)
        .FirstOrDefaultAsync();
}
```

**Returns**: The ONE active visit, or null

---

### Get Visit History

```csharp
public async Task<List<Visit>> GetVisitHistoryForPatientAsync(int patientId)
{
    return await _db.Visits
        .Where(v => v.PatientId == patientId && v.EndedAt != null)
        .OrderByDescending(v => v.EndedAt)
        .AsNoTracking()
        .ToListAsync();
}
```

**Returns**: All completed visits for patient, newest first

---

### Get Paused Visits (Today)

```csharp
public async Task<List<PausedVisitDto>> GetPausedVisitsTodayAsync(
    DateTime clinicTodayUtcStart, 
    DateTime clinicTomorrowUtcStart)
{
    (clinicTodayUtcStart, clinicTomorrowUtcStart) = GetClinicDayRangeUtc();

    return await _db.Visits
        .Join(_db.Patients, v => v.PatientId, p => p.PatientId, (v, p) => new { v, p })
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
```

**Returns**: All visits paused today (00:00 Baghdad → 23:59 Baghdad)

---

### Get Stale Paused Visits

```csharp
public async Task<List<PausedVisitDto>> GetStalePausedVisitsAsync(
    DateTime clinicTodayUtcStart)
{
    (clinicTodayUtcStart, _) = GetClinicDayRangeUtc();

    return await _db.Visits
        .Join(_db.Patients, v => v.PatientId, p => p.PatientId, (v, p) => new { v, p })
        .Where(x => x.v.EndedAt == null 
                 && x.v.PausedAt != null
                 && x.v.PausedAt < clinicTodayUtcStart  // Before today
                 && !x.p.IsDeleted)
        .OrderBy(x => x.v.PausedAt)  // Oldest first
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
```

**Returns**: All visits paused before today (oldest first)

---

## 🌍 Timezone Management

### Clinic Day Calculation

```csharp
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
```

**Purpose**: 
- All timestamps stored in UTC
- Clinic operations in Baghdad timezone
- "Today" means Baghdad calendar day (00:00-23:59)
- Returns UTC boundaries for today's clinic hours

---

## 🎯 Specialty-Specific Logic

### Example: ObGyne GPA (Gravida, Para, Abortion)

```csharp
public async Task SaveObGyneGpaAsync(int visitId, DTOs.ObGyne.GPADto gpa)
{
    var visit = await _db.Visits
        .Include(v => v.Entries)
        .FirstOrDefaultAsync(v => v.VisitId == visitId);

    if (visit == null)
        throw new InvalidOperationException("Visit not found");

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
        ClinicalSystem.GyneOb
    );

    await _db.SaveChangesAsync();
}
```

**Pattern**: Specialty-specific data → Visit Entries with system categorization

---

## 📊 Complete Visit Lifecycle Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                     COMPLETE VISIT LIFECYCLE                        │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  START VISIT                                                        │
│  ↓                                                                  │
│  Check: Active visit exists? → YES → Return existing               │
│  Check: Paused visit exists? → YES → Block (must resume/end)       │
│  Create new visit → StartedAt=Now, EndedAt=null, PausedAt=null    │
│  Initialize clinical sections                                       │
│  Save to DB                                                         │
│  → STATE: ACTIVE                                                    │
└─────────────────────────────────────────────────────────────────────┘
                                ↓
┌─────────────────────────────────────────────────────────────────────┐
│  ACTIVE VISIT                                                       │
│  - Document clinical findings (AddEntry)                            │
│  - Update vitals (UpdateVitals)                                     │
│  - Update symptoms (UpdatePresentingSymptom)                        │
└─────────────────────────────────────────────────────────────────────┘
                      ↓                           ↓
         ┌────────────────────┐      ┌────────────────────┐
         │  PAUSE VISIT       │      │  END VISIT         │
         │  PausedAt=Now      │      │  EndedAt=Now       │
         │  → STATE: PAUSED   │      │  PausedAt=null     │
         └────────────────────┘      │  → STATE:COMPLETED │
                      ↓               └────────────────────┘
         ┌────────────────────┐                 ↓
         │  PAUSED VISIT      │           ┌─────────────┐
         │  - Waiting         │           │  ARCHIVED   │
         └────────────────────┘           │  Historical │
                      ↓                   │  Record     │
         ┌────────────────────┐           └─────────────┘
         │  RESUME VISIT      │
         │  PausedAt=null     │
         │  → STATE: ACTIVE   │
         └────────────────────┘
                      ↓
         ┌────────────────────┐
         │  END VISIT         │
         │  EndedAt=Now       │
         │  PausedAt=null     │
         │  → STATE:COMPLETED │
         └────────────────────┘
```

---

## 🔑 Key Principles

### 1. **State Immutability**
- States managed through timestamps, not boolean flags
- Computed properties (IsPaused) derived from timestamps
- Clean state transitions with validation

### 2. **Business Rule Enforcement**
- One active visit per patient maximum
- Cannot start new if paused exists
- Cannot pause completed visits
- All operations idempotent

### 3. **Data Efficiency**
- Only save filled clinical sections
- Upsert pattern for entries (update existing, insert new)
- No empty database rows

### 4. **Timezone Awareness**
- All DB timestamps in UTC
- Baghdad timezone for business logic
- Proper day boundaries for queries

### 5. **Domain-Driven Design**
- Entity encapsulates business logic
- Private setters prevent invalid states
- Services orchestrate, entities validate

---

## 🧪 Testing Scenarios

### Scenario 1: Normal Visit Flow
```
1. Start visit → ACTIVE
2. Document findings → ACTIVE (add entries)
3. End visit → COMPLETED
```

### Scenario 2: Interrupted Visit
```
1. Start visit → ACTIVE
2. Pause visit → PAUSED
3. Resume visit → ACTIVE
4. End visit → COMPLETED
```

### Scenario 3: Blocking New Visit
```
1. Patient has PAUSED visit
2. Try to start new visit → BLOCKED
3. Must resume or end paused visit first
```

### Scenario 4: Quick Visit (SaveVisitAsync with New)
```
1. Call SaveVisitAsync(SaveType.New)
2. Creates visit AND ends it immediately
3. → COMPLETED in one operation
```

---

## 📝 API Contract Summary

### IVisitService Interface

```csharp
public interface IVisitService
{
    // Lifecycle operations
    Task<VisitStartResultDto> StartOrResumeVisitAsync(int patientId, string symptom, string? duration, string? note);
    Task PauseVisitAsync(int visitId);
    Task ResumeVisitAsync(int visitId);
    Task EndVisitAsync(int visitId);
    Task<VisitSaveResult> SaveVisitAsync(VisitSaveRequest request);

    // Queries
    Task<Visit?> GetActiveVisitForPatientAsync(int patientId);
    Task<List<Visit>> GetVisitHistoryForPatientAsync(int patientId);
    Task<List<PausedVisitDto>> GetPausedVisitsTodayAsync(DateTime start, DateTime end);
    Task<List<PausedVisitDto>> GetStalePausedVisitsAsync(DateTime today);

    // Specialty-specific
    Task SaveObGyneGpaAsync(int visitId, DTOs.ObGyne.GPADto gpa);
}
```

---

## 🚀 Usage Examples

### Starting a Visit
```csharp
var result = await _visitService.StartOrResumeVisitAsync(
    patientId: 123,
    presentingSymptom: "Headache",
    duration: "2 days",
    shortNote: "Severe migraine"
);

if (result.HasPausedVisit)
{
    // Inform user: "Patient has paused visit #X. Resume or end it first."
}
else
{
    // Visit started successfully, VisitId = result.VisitId
}
```

### Pausing a Visit
```csharp
await _visitService.PauseVisitAsync(visitId: 456);
// Visit is now PAUSED
```

### Resuming a Visit
```csharp
await _visitService.ResumeVisitAsync(visitId: 456);
// Visit is now ACTIVE again
```

### Ending a Visit
```csharp
await _visitService.EndVisitAsync(visitId: 456);
// Visit is now COMPLETED
```

### Quick Save (Create & Complete)
```csharp
var saveResult = await _visitService.SaveVisitAsync(new VisitSaveRequest
{
    SaveType = VisitSaveType.New,
    PatientId = 123,
    Diagnosis = "Common cold",
    Notes = "Rest and fluids",
    Temperature = 37.5m,
    BloodPressureSystolic = 120,
    BloodPressureDiastolic = 80
});

if (saveResult.Success)
{
    // Visit created and completed, ID = saveResult.VisitId
}
```

---

## 🎓 Summary

The Visit logic in MedRecord follows a **state machine pattern** with three clear states (ACTIVE, PAUSED, COMPLETED) managed through timestamps. The system enforces strict business rules:

✅ One active visit per patient
✅ Must resolve paused visits before starting new ones  
✅ All operations are idempotent and safe
✅ Clinical data saved efficiently (no empty entries)
✅ Timezone-aware for international deployment
✅ Domain logic in entities, orchestration in services

This design ensures **data integrity**, **clear state management**, and **flexibility** for clinical workflows while maintaining code quality and testability.

---

*Document Version: 1.0*  
*Last Updated: February 2025*
