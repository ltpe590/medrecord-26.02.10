# Code Fixes - Implementation Guide

This directory contains fixes for issues identified in the MedRecord codebase.

## 📋 Quick Start

### Priority Order (Apply in this sequence):

1. ✅ **FIX_05** - Timezone Parameter Bug (Quick, low risk)
2. ✅ **FIX_03** - Input Validation (Medium effort, important)
3. ✅ **FIX_01** - StartOrResumeVisit Logic (Critical, requires decision)
4. ✅ **FIX_02** - Cancellation Tokens (Large effort, can be gradual)
5. ✅ **FIX_10** - N+1 Query Problem (Quick, performance gain)
6. ✅ **FIX_11** - Database Indexes (Quick, performance gain)

---

## 🔧 Fix Details

### FIX_01: StartOrResumeVisit Logic ⚠️ CRITICAL
**File**: `FIX_01_StartOrResumeVisit.cs`
**Priority**: HIGH
**Effort**: Medium
**Risk**: Medium

**Issue**: Method name promises to resume but doesn't actually resume paused visits.

**Two Options**:
1. **RECOMMENDED**: Rename to `StartVisitAsync` (honest naming)
2. **ALTERNATIVE**: Implement auto-resume functionality

**Steps**:
1. Choose Option 1 or Option 2 from the fix file
2. Replace method in `Core/Services/VisitService.cs`
3. Update interface in `Core/Interfaces/Services/IVisitService.cs`
4. Update all callers (WPF, WebApi controllers)
5. Test thoroughly

**Testing Checklist**:
- [ ] Can start new visit for patient with no visits
- [ ] Cannot start new visit if active visit exists
- [ ] Paused visit blocks/resumes correctly
- [ ] All UI flows work after rename

---

### FIX_03: Input Validation
**File**: `FIX_03_AddInputValidation.cs`
**Priority**: HIGH
**Effort**: Low
**Risk**: Low

**Issue**: Service methods don't validate inputs, allowing invalid data.

**Steps**:
1. Add `ValidationHelpers` class to Core project
2. Update all methods in `VisitService.cs` with validation calls
3. Consider extending to other services

**Benefits**:
- Fail fast with clear error messages
- Prevent invalid database operations
- Better debugging experience

---

### FIX_05: Timezone Parameter Bug
**File**: `FIX_05_TimezoneParameterBug.cs`
**Priority**: HIGH
**Effort**: Low
**Risk**: Very Low

**Issue**: Method accepts parameters but ignores them and recalculates.

**Steps**:
1. Remove parameters from `GetPausedVisitsTodayAsync()`
2. Remove parameters from `GetStalePausedVisitsAsync()`
3. Update interface
4. Update all callers

**Impact**: 
- Cleaner API
- No behavior change
- Removes confusing parameters

---

### FIX_02: Cancellation Token Support
**File**: `FIX_02_CancellationTokens.cs` (to be created)
**Priority**: MEDIUM
**Effort**: High
**Risk**: Low

**Issue**: No async operations can be cancelled.

**Approach**: Add gradually, method by method

**Template for each method**:
```csharp
// Before:
public async Task<Visit?> GetActiveVisitForPatientAsync(int patientId)

// After:
public async Task<Visit?> GetActiveVisitForPatientAsync(
    int patientId, 
    CancellationToken cancellationToken = default)
{
    // Pass to all async calls
    return await _db.Visits
        .FirstOrDefaultAsync(cancellationToken);  // ✅
}
```

**Priority Methods** (add tokens to these first):
1. Database query methods
2. HTTP client calls
3. Long-running operations

---

### FIX_10: N+1 Query Problem
**Priority**: MEDIUM
**Effort**: Low
**Risk**: Low

**Quick Fix** - Add to `VisitService.cs`:
```csharp
public async Task<List<Visit>> GetVisitHistoryForPatientAsync(int patientId)
{
    return await _db.Visits
        .Include(v => v.Entries)  // ✅ Add this line
        .Where(v => v.PatientId == patientId && v.EndedAt != null)
        .OrderByDescending(v => v.EndedAt)
        .AsNoTracking()
        .ToListAsync();
}
```

**Benefit**: 
- 1 query instead of N+1 queries
- Significant performance improvement when accessing visit entries

---

### FIX_11: Database Indexes
**Priority**: MEDIUM
**Effort**: Low
**Risk**: Very Low

**Create Migration** - Add this to a new migration file:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Index for active visits query
    migrationBuilder.Sql(@"
        CREATE INDEX IX_Visits_PatientId_EndedAt_PausedAt 
        ON Visits(PatientId, EndedAt, PausedAt)
    ");

    // Index for visit history query
    migrationBuilder.Sql(@"
        CREATE INDEX IX_Visits_PatientId_EndedAt_Desc 
        ON Visits(PatientId, EndedAt DESC)
    ");

    // Index for paused visits by date
    migrationBuilder.Sql(@"
        CREATE INDEX IX_Visits_PausedAt 
        ON Visits(PausedAt) 
        WHERE EndedAt IS NULL
    ");

    // Unique constraint to prevent duplicate active visits
    migrationBuilder.Sql(@"
        CREATE UNIQUE INDEX IX_Visits_PatientId_Active 
        ON Visits(PatientId) 
        WHERE EndedAt IS NULL AND PausedAt IS NULL
    ");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DROP INDEX IX_Visits_PatientId_EndedAt_PausedAt ON Visits");
    migrationBuilder.Sql("DROP INDEX IX_Visits_PatientId_EndedAt_Desc ON Visits");
    migrationBuilder.Sql("DROP INDEX IX_Visits_PausedAt ON Visits");
    migrationBuilder.Sql("DROP INDEX IX_Visits_PatientId_Active ON Visits");
}
```

**Command to create migration**:
```bash
dotnet ef migrations add AddVisitIndexes --project Core
dotnet ef database update --project Core
```

---

## 🧪 Testing Strategy

### Unit Tests to Add

**File**: `Core.Tests/Services/VisitServiceTests.cs` (new file)

```csharp
public class VisitServiceTests
{
    [Fact]
    public async Task StartVisit_WithNegativePatientId_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateVisitService();
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.StartVisitAsync(-1, "Symptom", null, null));
    }

    [Fact]
    public async Task PauseVisit_WithNegativeVisitId_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateVisitService();
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.PauseVisitAsync(-1));
    }

    [Fact]
    public async Task GetActiveVisit_WhenNoActiveVisit_ReturnsNull()
    {
        // Arrange
        var service = CreateVisitService();
        
        // Act
        var result = await service.GetActiveVisitForPatientAsync(1);
        
        // Assert
        Assert.Null(result);
    }

    // Add more tests...
}
```

### Integration Tests

Test database operations with real DbContext:
- Visit lifecycle (Start → Pause → Resume → End)
- Paused visit blocking logic
- Timezone boundary conditions
- Concurrent visit creation

---

## 📊 Impact Analysis

### Before Fixes:
- ❌ Method name misleading
- ❌ Undefined parameters used
- ❌ No input validation
- ❌ No cancellation support
- ❌ N+1 query problems
- ❌ Slow queries (no indexes)

### After Fixes:
- ✅ Clear, honest method names
- ✅ Clean API signatures
- ✅ Fail-fast validation
- ✅ Cancellable operations
- ✅ Optimized queries
- ✅ Fast database access

---

## 🚀 Deployment Checklist

### Before Deploying:

- [ ] All fixes applied and tested locally
- [ ] Unit tests added and passing
- [ ] Integration tests passing
- [ ] Database migrations created
- [ ] Code reviewed by team
- [ ] Updated documentation
- [ ] API contracts unchanged (or versioned)

### Deploy Steps:

1. **Database**: Run migrations first
2. **API**: Deploy WebApi with new changes
3. **Client**: Deploy WPF with updated service calls
4. **Monitor**: Watch for errors in production logs

### Rollback Plan:

If issues occur:
1. Revert code deployment
2. Rollback database migration if needed
3. Investigate and fix
4. Redeploy with fix

---

## 💡 Best Practices Going Forward

### 1. Always Validate Inputs
```csharp
public async Task MyMethodAsync(int id, string name)
{
    if (id <= 0) throw new ArgumentException(...);
    if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(...);
    // ... rest of method
}
```

### 2. Always Support Cancellation
```csharp
public async Task MyMethodAsync(..., CancellationToken cancellationToken = default)
{
    return await _db.Set<T>().ToListAsync(cancellationToken);
}
```

### 3. Always Include Navigation Properties
```csharp
return await _db.Visits
    .Include(v => v.Entries)  // Prevent N+1
    .Include(v => v.Patient)  // If needed
    .ToListAsync();
```

### 4. Always Add Indexes for Queries
```csharp
// If you query on it frequently, index it
// Especially: foreign keys, status fields, dates
```

### 5. Always Write Tests
```csharp
// Test happy path
// Test error cases
// Test edge cases
// Test concurrent scenarios
```

---

## 📞 Questions?

If you need clarification on any fix:
1. Check the `CODE_ANALYSIS_REPORT.md` for detailed issue explanation
2. Review the specific fix file for implementation details
3. Run tests to verify behavior

---

*Last Updated: February 13, 2026*
