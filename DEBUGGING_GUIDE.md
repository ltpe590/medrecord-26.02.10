# 🐛 Debugging Guide for Applied Fixes

## 📋 **How I Can Help with Debugging**

While I cannot directly "see" Visual Studio's debugger in real-time, I can help you debug in several powerful ways:

### ✅ **What I CAN Do**
1. ✅ Analyze error messages you share
2. ✅ Read log files and stack traces
3. ✅ Explain variable values and states
4. ✅ Suggest what to check at breakpoints
5. ✅ Help interpret debug output
6. ✅ Review code logic for issues
7. ✅ Suggest test scenarios

### ❌ **What I CANNOT Do**
- ❌ See your Visual Studio window directly
- ❌ Set breakpoints for you
- ❌ Step through code in your debugger
- ❌ See real-time variable values

---

## 🧪 **Testing the Applied Fixes**

### Test #1: Input Validation ✅

**What Changed**: Added validation to all service methods

**How to Test**:
1. Start debugging (F5)
2. Open Immediate Window (`Debug > Windows > Immediate`)
3. Try these commands:

```csharp
// Should throw ArgumentException
await _visitService.PauseVisitAsync(-1);
await _visitService.PauseVisitAsync(0);
await _visitService.GetActiveVisitForPatientAsync(0);
await _visitService.GetActiveVisitForPatientAsync(-5);
```

**Expected Results**:
- ✅ ArgumentException: "Visit ID must be positive"
- ✅ ArgumentException: "Patient ID must be positive"

**If Not Working**:
- Check ValidationHelpers.cs exists and compiles
- Check VisitService.cs has validation calls
- Check exceptions aren't being swallowed

---

### Test #2: API Signature Changes ✅

**What Changed**: Removed timezone parameters from methods

**How to Test**:
1. Rebuild solution (`Ctrl+Shift+B`)
2. Should compile with 0 errors
3. Try calling old signature (should fail):

```csharp
// This should NOT compile:
var paused = await _visitService.GetPausedVisitsTodayAsync(
    DateTime.UtcNow, 
    DateTime.UtcNow.AddDays(1));

// This SHOULD compile:
var paused = await _visitService.GetPausedVisitsTodayAsync();
```

**Expected Results**:
- ✅ Old signature causes compile error
- ✅ New signature compiles and runs

**If Not Working**:
- Make sure you saved all files
- Clean and rebuild solution
- Check IVisitService.cs has new signature

---

### Test #3: N+1 Query Fix ✅

**What Changed**: Added `.Include(v => v.Entries)` to visit history query

**How to Test (Option 1 - Enable EF Logging)**:
Add to `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**How to Test (Option 2 - SQL Profiler)**:
1. Run SQL Server Profiler
2. Filter to your database
3. Call `GetVisitHistoryForPatientAsync()`
4. Count queries executed

**Expected Results**:
- ✅ Before: N+1 queries (1 for visits + N for entries)
- ✅ After: 1 query total (joins visits and entries)

**If Not Working**:
- Check .Include(v => v.Entries) is in code
- Rebuild and redeploy
- Clear EF cache

---

## 🔍 **Common Debug Scenarios**

### Scenario 1: Validation Not Throwing Exceptions

**Symptoms**:
- Invalid IDs accepted
- No ArgumentException thrown
- App doesn't crash on bad input

**Debug Steps**:
1. Set breakpoint at start of service method
2. Check if ValidationHelpers is called
3. Check if exception is caught elsewhere

```csharp
// In VisitService.PauseVisitAsync:
public async Task PauseVisitAsync(int visitId)
{
    // BREAKPOINT HERE - Does it exist?
    ValidationHelpers.ValidateVisitId(visitId);
    
    // BREAKPOINT HERE - Did we get past validation?
    var visit = await _db.Visits.FindAsync(visitId);
    // ...
}
```

**What to Check**:
- Is ValidationHelpers.cs in the project?
- Is `using Core.Helpers;` at top of file?
- Is exception being caught in UI layer?

---

### Scenario 2: "Method Not Found" Error

**Symptoms**:
- `No overload for method 'GetPausedVisitsTodayAsync' takes 2 arguments`
- Compile errors after applying fixes

**Debug Steps**:
1. Press `Ctrl+Shift+F` (Find in Files)
2. Search: `GetPausedVisitsTodayAsync(`
3. Find all callers
4. Update to new signature (no parameters)

**Fix Example**:
```csharp
// OLD (2 parameters) - REMOVE THIS
var paused = await _visitService.GetPausedVisitsTodayAsync(
    todayStart, tomorrowStart);

// NEW (no parameters) - USE THIS
var paused = await _visitService.GetPausedVisitsTodayAsync();
```

---

### Scenario 3: Database Connection Issues

**Symptoms**:
- Cannot connect to database
- Timeout errors
- "Login failed for user"

**Debug Steps**:
1. Check connection string in `appsettings.json`
2. Verify SQL Server is running
3. Test connection in SSMS

```csharp
// In Immediate Window:
var connString = _db.Database.GetConnectionString();
// Check if this looks correct
```

---

## 🛠️ **Debug Tools & Techniques**

### Visual Studio Immediate Window

**How to Use**:
1. Start debugging (F5)
2. Open Immediate Window (`Debug > Windows > Immediate`)
3. Type C# expressions to evaluate

**Useful Commands**:
```csharp
// Check type exists
typeof(ValidationHelpers)

// Check current values
visitId
patientId
visit.IsPaused
visit.EndedAt

// Call methods
await _visitService.GetPausedVisitsTodayAsync()

// Check exception details
$exception.Message
$exception.StackTrace
```

---

### SQL Server Profiler

**How to Monitor Queries**:
1. Start SQL Server Profiler
2. New Trace → Connect to your database
3. Filter by database name
4. Run your application
5. Watch queries execute

**What to Look For**:
- N+1 pattern: Many similar SELECT queries
- Unindexed queries: Long execution times
- Parameter sniffing: Same query, different times

---

### Output Window

**How to View**:
1. `Debug > Windows > Output`
2. Select "Debug" from dropdown
3. Watch for:
   - Exception messages
   - Log statements
   - EF SQL queries (if enabled)

---

## 📊 **Debugging Checklist**

### Before Running Debug:
- [ ] Solution builds successfully (0 errors)
- [ ] All files saved
- [ ] Database connection string correct
- [ ] SQL Server running
- [ ] Correct startup project selected

### During Debug Session:
- [ ] Set breakpoints in key methods
- [ ] Watch window shows correct variables
- [ ] Immediate window available for testing
- [ ] Output window shows expected logs

### Testing Applied Fixes:
- [ ] Validation throws exceptions for invalid input
- [ ] API calls use new signatures (no timezone params)
- [ ] Visit history query runs fast (single query)
- [ ] No compile errors
- [ ] No runtime errors

---

## 💡 **How to Work with Me During Debugging**

### Best Practice:
1. **Run your debug session**
2. **Hit a breakpoint or error**
3. **Share with me**:
   - Error message (exact text)
   - Stack trace (full trace)
   - Variable values (from Watch window)
   - What you were trying to do
4. **I'll help you**:
   - Explain what went wrong
   - Suggest what to check
   - Provide fix if needed

### Example Interaction:

**You**: "I'm getting an error when pausing a visit. Here's the error: [paste error]"

**Me**: "That error indicates X is null. Check if visit was found. Set breakpoint before visit.Pause() and check visit variable."

**You**: "Checked, visit is null"

**Me**: "That means FindAsync returned null. The visitId doesn't exist in database. Check if you created any visits yet."

---

## 🎯 **Quick Test Scenarios**

### Test 1: Create and Pause Visit
```csharp
// 1. Create patient
var patient = new Patient("Test Patient", Sex.Male, new DateOnly(1990, 1, 1));

// 2. Start visit
var result = await _visitService.StartOrResumeVisitAsync(
    patient.PatientId, 
    "Headache", 
    "2 days", 
    "Testing");

// 3. Pause visit
await _visitService.PauseVisitAsync(result.VisitId);

// 4. Verify
var visit = await _visitService.GetActiveVisitForPatientAsync(patient.PatientId);
// visit should be null (no ACTIVE visit)
```

### Test 2: Invalid Input
```csharp
// Should throw ArgumentException
try 
{
    await _visitService.PauseVisitAsync(-1);
    // FAIL: Should have thrown exception!
}
catch (ArgumentException ex)
{
    // SUCCESS: Validation working!
    Console.WriteLine(ex.Message); // "Visit ID must be positive"
}
```

### Test 3: Performance
```csharp
var stopwatch = Stopwatch.StartNew();

// This should be FAST (1 query)
var history = await _visitService.GetVisitHistoryForPatientAsync(patientId);

// Access entries to trigger loading
var totalEntries = history.Sum(v => v.Entries.Count);

stopwatch.Stop();
Console.WriteLine($"Took {stopwatch.ElapsedMilliseconds}ms");
// Should be < 100ms for reasonable data sizes
```

---

## 🚑 **Emergency Debugging**

### If Everything Breaks:

1. **Revert Changes** (if needed):
```bash
git status
git diff
git checkout -- <filename>  # Revert specific file
```

2. **Clean Build**:
```bash
dotnet clean
dotnet build
```

3. **Reset Database** (last resort):
```bash
dotnet ef database drop
dotnet ef database update
```

---

## 📞 **Getting Help from Me**

**Share This Information**:
1. Exact error message
2. Stack trace (full)
3. What you were doing
4. Variable values at breakpoint
5. Expected vs actual behavior

**I'll Help By**:
1. Explaining the error
2. Identifying root cause
3. Suggesting fixes
4. Providing code examples
5. Walking through debug steps

---

**Ready to Debug?** Start Visual Studio, set some breakpoints, and let's test those fixes! 🚀

If you hit any issues, just share the error details and I'll help you debug them!
