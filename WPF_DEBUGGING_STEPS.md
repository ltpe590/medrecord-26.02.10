# 🔍 WPF Window Not Appearing - Debugging Steps

**Date**: February 14, 2026  
**Issue**: WPF process runs but window doesn't appear  
**Status**: Investigating - Exception being silently swallowed

---

## 🐛 THE PROBLEM

**Symptoms**:
- ✅ WPF.exe process running (PID visible)
- ✅ Process responding (not frozen)
- ❌ MainWindowHandle = 0 (window not created)
- ❌ No window visible on screen
- ❌ No crash logs being generated

**Root Cause Hypothesis**:
The exception handler is **silently catching and hiding** exceptions:
```csharp
this.DispatcherUnhandledException += (s, ev) =>
{
    LogUnhandledException(ev.Exception, "...");
    ev.Handled = true;  // ⚠️ Swallows exception!
};
```

This means:
1. Exception occurs during startup
2. Handler catches it and logs to file
3. Sets `Handled = true` (app continues running)
4. Window never gets created
5. App appears "running" but is broken

---

## ✅ FIX APPLIED

**Temporarily disabled exception swallowing**:
```csharp
this.DispatcherUnhandledException += (s, ev) =>
{
    LogUnhandledException(ev.Exception, "...");
    ev.Handled = false;  // ✅ Let it crash for debugging!
};
```

**Why**: This will let the exception bubble up so we can see it in Visual Studio debugger

---

## 🚀 NEXT STEPS - DO THIS NOW

### **Step 1**: Rebuild in Visual Studio
```
Build → Rebuild Solution
```

### **Step 2**: Start Debugging (F5)

### **Step 3**: Watch for Exception Dialog

**You should now see**:
- Exception dialog with **actual error message**
- Details about what's failing
- Stack trace showing where it failed

### **Step 4**: Share the Exception Details

**Copy and share with me**:
1. Exception type (e.g., NullReferenceException, InvalidOperationException)
2. Exception message
3. Stack trace (shows which line failed)

---

## 🔍 WHAT TO LOOK FOR

### **Common Exceptions in WPF Startup**:

**1. Dependency Injection Failure**:
```
Unable to resolve service for type 'X'
```
→ Missing service registration in ConfigureServices

**2. Configuration Error**:
```
Cannot find file 'appsettings.json'
The configuration file 'X' was not found
```
→ Missing or incorrect config file

**3. Database Connection**:
```
A network-related or instance-specific error occurred
Cannot open database
```
→ SQL Server not running or connection string wrong

**4. XAML Error**:
```
Cannot create instance of 'MainWindow'
The invocation of the constructor on type 'X' failed
```
→ MainWindow constructor or XAML has errors

---

## 📊 DEBUGGING FLOW

```
Start Debugging (F5)
        ↓
   WPF.exe starts
        ↓
   Exception thrown
        ↓
   VS Debugger breaks ← YOU SEE ERROR HERE
        ↓
   Share exception with me
        ↓
   I analyze and provide fix
```

---

## 💡 LIKELY CAUSES (In Order of Probability)

### **1. ISpecialtyProfile Still Not Resolved** (Most Likely)
Even though we moved the registration, there might be another issue

### **2. Missing IProfileService Implementation**
VisitService constructor needs this

### **3. Database Not Accessible**
LocalDB might not be running

### **4. appsettings.json Missing or Malformed**
Configuration can't be loaded

### **5. MainWindow Constructor Error**
Something in MainWindow.xaml.cs is failing

---

## 🎯 WHAT I NEED FROM YOU

**When the exception appears in Visual Studio, share**:

**Format**:
```
Exception Type: [e.g., InvalidOperationException]
Message: [full message]
Stack Trace: [copy entire stack trace]
```

**How to Copy**:
1. When debugger breaks, look at Exception Helper window
2. Click "View Detail" or "Copy Details to Clipboard"
3. Paste the details to share with me

**Or Screenshot**:
- Take screenshot of exception dialog
- Share with me

---

## 🔧 TEMPORARY CHANGE MADE

**File**: `WPF/App.xaml.cs`
**Line**: ~37
**Change**: `ev.Handled = true` → `ev.Handled = false`

**Purpose**: Allow exceptions to crash the app so we can see them

**After we fix**: We'll change it back to `true` to handle exceptions gracefully

---

## ✅ ACTION REQUIRED

**Do this now**:

1. ✅ **Stop debugging** (if still running)
2. ✅ **Rebuild solution** (Ctrl+Shift+B)
3. ✅ **Start debugging** (F5)
4. ✅ **Watch for exception dialog**
5. ✅ **Copy exception details**
6. ✅ **Share with me**

---

**I'm ready to analyze the exception and provide the fix!** 🚀

Just share what error appears when you press F5 now.

---

*Debugging Mode: ON*  
*Exception Swallowing: OFF*  
*Ready to See Real Error: YES*
