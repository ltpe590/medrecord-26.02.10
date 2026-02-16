# 🎨 MAJOR WPF REFACTORING PLAN

**Date**: February 14, 2026  
**Scope**: Complete UI/UX overhaul  
**Status**: 📋 PLANNING

---

## 🎯 OBJECTIVES

### **1. Separate Login Window**
- Remove login expander from main window
- Create dedicated `LoginWindow.xaml`
- Show on startup, blocks until logged in
- Add username/password login
- Add fingerprint authentication

### **2. Tab-Based Navigation**
- Replace expanders with TabControl
- Two main tabs: "Patients" and "Visit"
- Full-screen vertical layout
- Modern tab styling

### **3. Core Alignment**
- Match backend DTOs exactly
- Use proper enums (Sex, etc.)
- Follow backend architecture patterns
- Proper async/await throughout

---

## 📋 IMPLEMENTATION PLAN

### **Phase 1: Login Window** ✅
- [ ] Create `LoginWindow.xaml`
- [ ] Create `LoginViewModel.cs`
- [ ] Add fingerprint authentication service
- [ ] Update `App.xaml.cs` to show login first
- [ ] Pass auth token to main window

### **Phase 2: Main Window Redesign** 🎨
- [ ] Replace expanders with TabControl
- [ ] Create "Patients" tab layout
- [ ] Create "Visit" tab layout
- [ ] Full-screen responsive design
- [ ] Modern styling

### **Phase 3: Patient Tab** 👥
- [ ] Patient list on left (30%)
- [ ] Patient details on right (70%)
- [ ] Search functionality
- [ ] Add new patient button
- [ ] Patient history view

### **Phase 4: Visit Tab** 🏥
- [ ] Visit form (vitals, diagnosis, notes)
- [ ] Lab results section
- [ ] Prescription section
- [ ] Save/Complete visit buttons
- [ ] Visit history

### **Phase 5: Fingerprint Integration** 👆
- [ ] Windows Hello integration
- [ ] Fallback to username/password
- [ ] Biometric availability check
- [ ] Secure token storage

### **Phase 6: Core Alignment** 🔧
- [ ] Review all DTOs usage
- [ ] Ensure enum consistency (Sex, SaveType, etc.)
- [ ] Proper error handling
- [ ] Logging throughout

---

## 🏗️ NEW PROJECT STRUCTURE

```
WPF/
├── App.xaml
├── App.xaml.cs
├── Windows/
│   ├── LoginWindow.xaml          ← NEW
│   ├── LoginWindow.xaml.cs        ← NEW
│   ├── MainWindow.xaml            ← REDESIGNED
│   └── MainWindow.xaml.cs         ← SIMPLIFIED
├── ViewModels/
│   ├── LoginViewModel.cs          ← NEW
│   ├── MainWindowViewModel.cs     ← REFACTORED
│   ├── PatientTabViewModel.cs     ← NEW
│   └── VisitTabViewModel.cs       ← NEW
├── Services/
│   ├── FingerprintAuthService.cs  ← NEW
│   └── BiometricService.cs        ← NEW
├── Views/
│   ├── PatientListView.xaml       ← NEW (User Control)
│   ├── PatientDetailsView.xaml    ← NEW (User Control)
│   ├── VisitFormView.xaml         ← NEW (User Control)
│   └── LabResultsView.xaml        ← NEW (User Control)
└── Styles/
    ├── TabStyles.xaml             ← NEW
    └── ModernTheme.xaml           ← NEW
```

---

## 🎨 UI MOCKUP

### **Login Window**
```
┌─────────────────────────────────────┐
│     Medical Records System          │
│                                     │
│   [👤 Username Input]               │
│   [🔒 Password Input]               │
│                                     │
│   [👆 Login with Fingerprint]      │
│   [  Login with Password  ]        │
│                                     │
└─────────────────────────────────────┘
```

### **Main Window - Two Tabs**
```
┌─────────────────────────────────────────────────┐
│ Medical Records System    👤 Dr. Name  [Logout] │
├─────────────────────────────────────────────────┤
│ [  Patients  ] [   Visit   ]                    │
├─────────────────────────────────────────────────┤
│                                                 │
│  TAB CONTENT HERE (Full Screen)                │
│                                                 │
│                                                 │
└─────────────────────────────────────────────────┘
```

### **Patients Tab Layout**
```
┌──────────────────────┬──────────────────────────┐
│ PATIENT LIST (30%)   │ PATIENT DETAILS (70%)    │
├──────────────────────┼──────────────────────────┤
│ [Search: _______]    │ Name: John Doe           │
│ [+ Add Patient]      │ Age: 45, Sex: Male       │
│                      │ DOB: 1980-01-15          │
│ ☑ John Doe, 45, M    │                          │
│ □ Jane Smith, 32, F  │ Phone: 555-1234          │
│ □ Bob Wilson, 28, M  │ Address: 123 Main St     │
│ □ Alice Brown, 55, F │                          │
│ □ ...                │ Blood Group: O+          │
│                      │ Allergies: Penicillin    │
│                      │                          │
│                      │ === VISIT HISTORY ===    │
│                      │ • 2026-02-10: Checkup    │
│                      │ • 2026-01-15: Follow-up  │
└──────────────────────┴──────────────────────────┘
```

### **Visit Tab Layout**
```
┌──────────────────────────────────────────────┐
│ Visit for: John Doe (Age 45)                 │
├──────────────────────────────────────────────┤
│ VITALS                                       │
│ Temp: [___] °C   BP: [___]/[___] mmHg       │
│ GPA: G[_] P[_] A[_]                         │
│                                             │
│ DIAGNOSIS                                   │
│ [_____________________________________]     │
│                                             │
│ NOTES                                       │
│ [_____________________________________]     │
│ [_____________________________________]     │
│                                             │
│ LAB RESULTS                                 │
│ Test: [Select Test ▼]  Value: [____]       │
│ [Add Result]                                │
│ • HB: 13.5 g/dL                            │
│ • Blood Sugar: 95 mg/dL                    │
│                                             │
│ [Save Visit] [Complete Visit] [Pause]      │
└──────────────────────────────────────────────┘
```

---

## 🔐 FINGERPRINT AUTHENTICATION

### **Using Windows Hello**
```csharp
public class BiometricService
{
    public async Task<bool> IsAvailableAsync()
    {
        return await UserConsentVerifier.CheckAvailabilityAsync() 
            == UserConsentVerifierAvailability.Available;
    }

    public async Task<(bool Success, string Message)> AuthenticateAsync()
    {
        var result = await UserConsentVerifier.RequestVerificationAsync(
            "Login to Medical Records");
        
        return result == UserConsentVerificationResult.Verified
            ? (true, "Authenticated")
            : (false, "Authentication failed");
    }
}
```

---

## 📦 REQUIRED PACKAGES

```xml
<!-- For Windows Hello / Biometric -->
<PackageReference Include="Microsoft.Windows.SDK.Contracts" Version="10.0.*" />

<!-- For modern UI (optional) -->
<PackageReference Include="ModernWpfUI" Version="0.9.*" />
```

---

## 🎯 BENEFITS

### **User Experience**
✅ Faster login (fingerprint)  
✅ Cleaner UI (no expanders)  
✅ Better workflow (tab navigation)  
✅ Full-screen workspace  
✅ Modern look and feel  

### **Code Quality**
✅ Separation of concerns (login separate)  
✅ Better ViewModels (one per tab)  
✅ Reusable user controls  
✅ Aligned with Core backend  

### **Maintainability**
✅ Easier to test  
✅ Easier to extend  
✅ Better error handling  
✅ Proper async patterns  

---

## ⚠️ BREAKING CHANGES

1. **Login Flow**: Users must login via separate window
2. **Navigation**: No more expanders, use tabs instead
3. **Layout**: Complete UI redesign
4. **State Management**: Different ViewModel structure

---

## 🚀 EXECUTION PLAN

### **Approach: Incremental Migration**

**Option A: Big Bang** (All at once)
- ❌ Risky
- ❌ Hard to test incrementally
- ✅ Clean slate

**Option B: Feature Flags** (Gradual)
- ✅ Less risky
- ✅ Can test both UIs
- ✅ Rollback easily
- ✅ **RECOMMENDED**

**Option C: New Project** (Side by side)
- ✅ Keep old working
- ✅ Build new from scratch
- ❌ Duplication
- ❌ More work

---

## 📝 RECOMMENDED: INCREMENTAL APPROACH

### **Step 1: Add Login Window (Keep Old UI)**
- Create LoginWindow
- Show before MainWindow
- Pass auth token
- **Old UI still works**

### **Step 2: Add Tab Container (Keep Expanders)**
- Add TabControl
- Expanders inside tabs
- Test navigation
- **Gradual transition**

### **Step 3: Redesign One Tab at a Time**
- Patients tab first
- Then Visit tab
- **One feature at a time**

### **Step 4: Remove Expanders**
- Once tabs proven
- Clean up old code

### **Step 5: Polish & Optimize**
- Add fingerprint
- Modern styling
- Performance tuning

---

## ⏱️ TIME ESTIMATE

- Login Window: 2-3 hours
- Tab Layout: 2-3 hours
- Patient Tab: 3-4 hours
- Visit Tab: 3-4 hours
- Fingerprint Auth: 2-3 hours
- Testing & Polish: 2-3 hours

**Total: 14-20 hours of development**

---

## 🤔 DECISION POINT

**Should we proceed with this refactoring?**

**Questions:**
1. Do you want ALL changes at once, or incremental?
2. Is fingerprint auth a must-have, or nice-to-have?
3. Should we keep old UI accessible during transition?
4. Any specific UI preferences or requirements?

---

*Plan Created: February 14, 2026 11:10 PM*  
*Status: AWAITING APPROVAL*  
*Ready to Start: YES* 🚀
