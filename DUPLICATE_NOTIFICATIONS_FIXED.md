# ✅ ALL DUPLICATE/DOUBLE-NOTIFICATION BUGS FIXED

## 🐛 ROOT CAUSES

### Bug 1: Double Notifications
`OnShowErrorMessage` and `OnShowSuccessMessage` were subscribed TWICE:
- Once in Constructor (lines 43-44)
- Again in `SubscribeToViewModelEvents()` (lines 101-102)

### Bug 2: Duplicate Patient Creation
`AddNewPatientAsync` called `LoadAllPatientsAsync`, then `ShowNewPatientDialogAsync` called it again.

### Bug 3: Double Visit Start
When a new patient was saved and "Start Visit" pressed:
- `PatientListBox_SelectionChanged` fired → called `SelectPatientAsync`
- Then `ShowNewPatientDialogAsync` ALSO called `SelectPatientAsync`
- Two visits created!

### Bug 4: Double Error Messages
`StartVisitForPatientAsync` called `ShowError()` in its catch block,
then threw the exception, which propagated to `HandlePatientSelectionError`
which ALSO called `OnShowErrorMessage`.

---

## ✅ FIXES APPLIED

### Fix 1: Remove duplicate event subscriptions
`SubscribeToViewModelEvents()` no longer subscribes to
`OnShowErrorMessage` or `OnShowSuccessMessage` — those stay in constructor only.

### Fix 2: `SelectPatientAsync` guards
- Added `forceNewVisit` parameter
- Single-click (`forceNewVisit=false`): skips if same patient already selected
- Double-click / Save&StartVisit (`forceNewVisit=true`): resets `_currentVisitId` and forces new visit

### Fix 3: Single notification flow
- `AddNewPatientAsync` no longer shows "Patient added" success (removed)
- `StartVisitForPatientAsync` no longer calls `ShowError` (re-throws instead)
- `ShowNewPatientDialogAsync` shows ONE "registered" message when saving without visit
- Visit success message shown once by `ShowSuccess` in StartVisitForPatientAsync

### Fix 4: Clean dialog flow
`ShowNewPatientDialogAsync` is now a single clean flow:
```
Save patient → if StartVisit: switch tab → force new visit
           → else: show "Patient registered" once
```

---

## 🎯 EXPECTED BEHAVIOR

### "Save Patient" button:
- ONE "Patient registered successfully!" message ✅
- Patient appears in list once ✅

### "Save & Start Visit" button:
- No "Patient added" message
- Tab switches to Visit
- ONE "Visit #X started successfully" message ✅
- Visit form ready ✅

### Double-click patient in list:
- Tab switches to Visit
- ONE "Visit #X started successfully" message ✅
- New visit even if patient already selected ✅

---

## 🧪 TEST

1. Stop both apps
2. Rebuild (Ctrl+Shift+B)
3. Start WebApi, then WPF
4. Test each flow:
   - Add patient → Save only → ONE message
   - Add patient → Save & Start Visit → switches tab, ONE visit message
   - Double-click existing patient → ONE visit message
