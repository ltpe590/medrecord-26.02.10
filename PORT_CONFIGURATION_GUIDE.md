# 🌐 WebApi Port Configuration Guide

**Date**: February 13, 2026  
**WebApi Launch Profiles**: 3 options available

---

## 🔌 AVAILABLE PORTS & PROFILES

### **Profile 1: HTTP Only** (Non-SSL)
```json
Profile: "http"
URL: http://localhost:5258
SSL: No
Swagger: http://localhost:5258/swagger
Environment: Development
```

### **Profile 2: HTTPS (Recommended)** ⭐
```json
Profile: "https"
URLs: 
  - https://localhost:7012 (Primary - SSL)
  - http://localhost:5258  (Fallback - Non-SSL)
SSL: Yes (port 7012)
Swagger: 
  - https://localhost:7012/swagger
  - http://localhost:5258/swagger
Environment: Development
```

### **Profile 3: IIS Express**
```json
Profile: "IIS Express"
URL: http://localhost:16664
SSL Port: 44393
Swagger: http://localhost:16664/swagger
Environment: Development
```

---

## ⚙️ CURRENT WPF CONFIGURATION

**WPF is configured to use**: `http://localhost:5258`

**Configuration File**: `WPF/appsettings.json`
```json
{
  "AppSettings": {
    "ApiBaseUrl": "http://localhost:5258"
  }
}
```

**This means**:
- ✅ Works with "http" profile
- ✅ Works with "https" profile (uses fallback HTTP port)
- ❌ Won't work with "IIS Express" profile (different port)

---

## 🎯 WHICH PROFILE GETS USED?

### **When debugging in Visual Studio:**

**Default Profile**: Usually "https" (the default in launchSettings.json)

**You can change it**:
1. Right-click WebApi project
2. Properties → Debug → Launch Profiles
3. Select: http, https, or IIS Express

**Or in Visual Studio toolbar**:
- Click dropdown next to green "Start" button
- Select: "http", "https", or "IIS Express"

---

## 🔍 HOW TO TELL WHICH PORT IS ACTIVE

### **Check Visual Studio Output Window**:
```
Look for these messages when WebApi starts:

For HTTPS profile:
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7012
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5258

For HTTP profile:
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5258

For IIS Express:
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:16664
```

---

## ✅ RECOMMENDED CONFIGURATION

### **For Development (Current Setup)**: ⭐

**WebApi Profile**: "https"
```
Primary: https://localhost:7012 (SSL)
Fallback: http://localhost:5258 (Non-SSL)
```

**WPF Configuration**: 
```json
"ApiBaseUrl": "http://localhost:5258"
```

**Why This Works**:
- ✅ WebApi listens on BOTH ports
- ✅ WPF uses the non-SSL port (5258)
- ✅ You can test SSL if needed (port 7012)
- ✅ No certificate issues during development

---

## 🔒 USING SSL (HTTPS)

### **If you want WPF to use SSL**:

**Step 1**: Update WPF appsettings.json
```json
{
  "AppSettings": {
    "ApiBaseUrl": "https://localhost:7012"
  }
}
```

**Step 2**: Trust development certificate
```bash
dotnet dev-certs https --trust
```

**Step 3**: Restart both apps

**Benefits**:
- ✅ Encrypted communication
- ✅ More production-like
- ✅ Better security testing

**Drawbacks**:
- ⚠️ Certificate warnings possible
- ⚠️ Need to trust dev certificate
- ⚠️ Slightly more complex setup

---

## 🐛 TROUBLESHOOTING PORT ISSUES

### **Issue 1: "Port already in use"**

**Error**:
```
Unable to start Kestrel.
System.IO.IOException: Failed to bind to address http://localhost:5258: address already in use.
```

**Solution**:
```powershell
# Find what's using the port
netstat -ano | findstr :5258

# Kill the process (replace PID with actual PID)
taskkill /PID <PID> /F

# Or use a different port in launchSettings.json
```

---

### **Issue 2: WPF can't connect to WebApi**

**Symptoms**:
- WPF starts but shows connection errors
- Login fails
- "Connection refused" or "No such host" errors

**Check**:
1. ✅ Is WebApi running?
2. ✅ Which port is WebApi using?
3. ✅ Does WPF ApiBaseUrl match?

**Verify**:
```powershell
# Check if WebApi is listening
netstat -ano | findstr :5258
netstat -ano | findstr :7012

# Should show LISTENING
```

**Fix**:
Update `WPF/appsettings.json` to match WebApi's actual port:
```json
// If WebApi only on 7012:
"ApiBaseUrl": "https://localhost:7012"

// If WebApi on 5258:
"ApiBaseUrl": "http://localhost:5258"
```

---

### **Issue 3: SSL Certificate Errors**

**Error**:
```
The SSL connection could not be established
The remote certificate is invalid according to the validation procedure
```

**Solution**:
```bash
# Trust the development certificate
dotnet dev-certs https --trust

# Or switch WPF to use HTTP (port 5258) instead
```

---

## 📊 PORT SUMMARY TABLE

| Profile | HTTP Port | HTTPS Port | Swagger URL | WPF Compatible |
|---------|-----------|------------|-------------|----------------|
| **http** | 5258 | - | http://localhost:5258/swagger | ✅ Yes (current) |
| **https** | 5258 | 7012 | https://localhost:7012/swagger | ✅ Yes (current) |
| **IIS Express** | 16664 | 44393 | http://localhost:16664/swagger | ❌ No (wrong port) |

---

## 🎯 QUICK REFERENCE

### **Current Working Setup**:
```
WebApi: Using "https" profile
  - Listening on: https://localhost:7012 (SSL)
  - Listening on: http://localhost:5258 (Non-SSL)
  
WPF: Using HTTP connection
  - Configured for: http://localhost:5258
  - Matches: WebApi's non-SSL port ✅
```

### **To Test Swagger**:
```
Open browser:
- https://localhost:7012/swagger (if using https profile)
- http://localhost:5258/swagger (if using http or https profile)
```

### **To Check What's Running**:
```powershell
# Check both ports
netstat -ano | findstr :5258
netstat -ano | findstr :7012

# Should see LISTENING if WebApi is running
```

---

## 💡 BEST PRACTICES

### **Development (Current)**:
- ✅ Use "https" profile on WebApi (listens on both 5258 and 7012)
- ✅ Configure WPF to use http://localhost:5258 (no SSL hassle)
- ✅ This gives you flexibility to test both HTTP and HTTPS

### **Production**:
- ✅ Use HTTPS only (SSL/TLS encryption)
- ✅ Use proper SSL certificates (not dev certs)
- ✅ Configure firewall rules
- ✅ Use reverse proxy (IIS, nginx, etc.)

---

## 🔧 HOW TO CHANGE PORTS

### **Method 1: Edit launchSettings.json**
```json
// WebApi/Properties/launchSettings.json
{
  "https": {
    "applicationUrl": "https://localhost:7012;http://localhost:5258"
    // Change these URLs if needed
  }
}
```

### **Method 2: Environment Variable**
```bash
# Override at runtime
set ASPNETCORE_URLS=http://localhost:5000
dotnet run --project WebApi
```

### **Method 3: appsettings.json**
```json
// WebApi/appsettings.json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5258"
      }
    }
  }
}
```

---

## ✅ VERIFICATION CHECKLIST

When debugging, verify:

- [ ] WebApi is running
- [ ] Check Output window for "Now listening on..." messages
- [ ] Note which port(s) are active
- [ ] WPF ApiBaseUrl matches one of the active ports
- [ ] Can access Swagger UI at the port
- [ ] WPF can connect to WebApi

---

**Current Status**: ✅ Properly configured for development  
**WPF → WebApi**: http://localhost:5258  
**WebApi Listening**: Both 5258 (HTTP) and 7012 (HTTPS)  
**Everything Ready**: Press F5 to debug! 🚀

---

*Last Updated: February 13, 2026*
