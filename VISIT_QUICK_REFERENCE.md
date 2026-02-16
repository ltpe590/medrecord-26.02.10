# Visit State Machine - Quick Reference

## State Definitions

| State | EndedAt | PausedAt | Description |
|-------|---------|----------|-------------|
| **ACTIVE** | null | null | Visit in progress |
| **PAUSED** | null | NOT null | Visit temporarily suspended |
| **COMPLETED** | NOT null | null | Visit finished |

## Operations Matrix

| Operation | From State | To State | Allowed? | Notes |
|-----------|-----------|----------|----------|-------|
| **Start** | - | ACTIVE | ✅ | Only if no active/paused visit exists |
| **Pause** | ACTIVE | PAUSED | ✅ | Sets PausedAt timestamp |
| **Pause** | PAUSED | PAUSED | ✅ | Idempotent - no change |
| **Pause** | COMPLETED | - | ❌ | Throws exception |
| **Resume** | PAUSED | ACTIVE | ✅ | Clears PausedAt |
| **Resume** | ACTIVE | ACTIVE | ✅ | Idempotent - no change |
| **Resume** | COMPLETED | - | ❌ | Cannot resume finished visit |
| **End** | ACTIVE | COMPLETED | ✅ | Sets EndedAt, clears PausedAt |
| **End** | PAUSED | COMPLETED | ✅ | Sets EndedAt, clears PausedAt |
| **End** | COMPLETED | COMPLETED | ✅ | Idempotent - no change |

## Query Filters

### Active Visit Query
```sql
WHERE EndedAt IS NULL AND PausedAt IS NULL
```

### Paused Visit Query
```sql
WHERE EndedAt IS NULL AND PausedAt IS NOT NULL
```

### Completed Visit Query
```sql
WHERE EndedAt IS NOT NULL
```

### Today's Paused Visits
```sql
WHERE EndedAt IS NULL 
  AND PausedAt IS NOT NULL
  AND PausedAt >= @TodayStartUtc
  AND PausedAt < @TomorrowStartUtc
```

### Stale Paused Visits
```sql
WHERE EndedAt IS NULL 
  AND PausedAt IS NOT NULL
  AND PausedAt < @TodayStartUtc
```

## Business Rules

### Rule #1: One Active Visit Per Patient
- Patient can have max ONE active visit
- Patient can have max ONE paused visit
- Must resolve (resume or end) paused visit before starting new

### Rule #2: State Transitions Are Safe
- All operations are idempotent
- Invalid transitions throw exceptions
- State always remains consistent

### Rule #3: Timestamps Are Source of Truth
- States computed from timestamps, not flags
- All timestamps in UTC
- Business logic uses Baghdad timezone

### Rule #4: Efficient Data Storage
- Only filled clinical sections saved
- Upsert pattern for visit entries
- No empty database rows

## API Methods Quick Reference

```csharp
// Start new visit (checks for active/paused first)
Task<VisitStartResultDto> StartOrResumeVisitAsync(
    int patientId, string symptom, string? duration, string? note);

// Pause active visit
Task PauseVisitAsync(int visitId);

// Resume paused visit  
Task ResumeVisitAsync(int visitId);

// End visit (from any non-completed state)
Task EndVisitAsync(int visitId);

// Multi-purpose save (new/edit)
Task<VisitSaveResult> SaveVisitAsync(VisitSaveRequest request);

// Query operations
Task<Visit?> GetActiveVisitForPatientAsync(int patientId);
Task<List<Visit>> GetVisitHistoryForPatientAsync(int patientId);
Task<List<PausedVisitDto>> GetPausedVisitsTodayAsync(DateTime, DateTime);
Task<List<PausedVisitDto>> GetStalePausedVisitsAsync(DateTime);
```

## Common Workflows

### Workflow 1: Normal Visit
```
StartOrResumeVisitAsync() → ACTIVE
  ↓
AddEntry() / UpdateVitals()
  ↓
EndVisitAsync() → COMPLETED
```

### Workflow 2: Interrupted Visit
```
StartOrResumeVisitAsync() → ACTIVE
  ↓
PauseVisitAsync() → PAUSED
  ↓
ResumeVisitAsync() → ACTIVE
  ↓
EndVisitAsync() → COMPLETED
```

### Workflow 3: Quick Complete
```
SaveVisitAsync(SaveType.New) → COMPLETED
(Creates and ends in one operation)
```

### Workflow 4: Blocked Start
```
Patient has PAUSED visit
  ↓
StartOrResumeVisitAsync() → Returns HasPausedVisit=true
  ↓
Must ResumeVisitAsync() or EndVisitAsync() first
```

## Data Structure

```
Visit
├── VisitId (PK)
├── PatientId (FK)
├── StartedAt (DateTime UTC)
├── EndedAt? (DateTime? UTC)
├── PausedAt? (DateTime? UTC)
├── IsPaused (computed: PausedAt != null && EndedAt == null)
├── PresentingSymptomText
├── PresentingSymptomDurationText
├── ShortNote
├── Vitals? (value object)
│   ├── Temperature?
│   ├── Systolic?
│   └── Diastolic?
└── Entries (collection)
    └── VisitEntry
        ├── Section (e.g., "Obstetric History")
        ├── SystemCode (e.g., "OBGYN")
        └── Content (clinical text)
```

## Error Handling

| Scenario | Behavior |
|----------|----------|
| Visit not found | Throw `InvalidOperationException` |
| Pause completed visit | Throw `InvalidOperationException` |
| Start when paused exists | Return `HasPausedVisit=true` in DTO |
| Patient not found | Return failure result |
| Invalid save type | Return failure result |
| Resume non-paused | Idempotent - no error |
| End already ended | Idempotent - no error |

## Timezone Conversion

```csharp
// All timestamps stored as UTC
StartedAt = DateTime.UtcNow;
EndedAt = DateTime.UtcNow;
PausedAt = DateTime.UtcNow;

// Business logic uses Baghdad timezone
TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Baghdad");

// "Today" = Baghdad calendar day (00:00 - 23:59)
var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
var todayStart = nowLocal.Date; // 00:00 Baghdad
```

## Clinical Systems

Visit entries can be categorized by clinical system:

```
GEN     - General
CVS     - Cardiovascular
RESP    - Respiratory
OBGYN   - Gynecology / Obstetrics  ⭐ Currently implemented
NEURO   - Neurological
ENDO    - Endocrine
HEMA    - Hematology
GIT     - Gastrointestinal
MSK     - Musculoskeletal
RENAL   - Renal
DERM    - Dermatology
PSY     - Psychiatric
OPHT    - Ophthalmology
ORTHO   - Orthopedic
UNCAT   - Uncategorized
```

## Save Type Semantics

```csharp
VisitSaveType.New    → Create visit + EndVisit() immediately
VisitSaveType.Edit   → Update existing visit (requires VisitId)
VisitSaveType.Resume → Legacy (use ResumeVisitAsync instead)
```

## Key Design Patterns

1. **State Machine** - Clear states with validated transitions
2. **Domain-Driven Design** - Logic in entities, orchestration in services
3. **Value Objects** - Vitals immutable value object
4. **Repository Pattern** - Data access abstraction
5. **Upsert Pattern** - Efficient visit entry management
6. **Factory Methods** - Static factory for result DTOs
7. **Idempotency** - Safe to call operations multiple times

---

*Quick Reference Version: 1.0*
