# Text-to-CAD Add-In Troubleshooting Guide

Comprehensive troubleshooting for common issues with the SolidWorks add-in.

## 📋 Table of Contents

- [Registration Issues](#registration-issues)
- [Build & Compilation Issues](#build--compilation-issues)
- [Runtime Errors](#runtime-errors)
- [Connection Problems](#connection-problems)
- [UI Issues](#ui-issues)
- [Performance Issues](#performance-issues)
- [Debugging Tips](#debugging-tips)

---

## Registration Issues

### Add-In Not Showing in Tools → Add-Ins

**Symptoms:**
- "Text-to-CAD" doesn't appear in SolidWorks Add-Ins list
- Registration script says "SUCCESS" but add-in missing

**Diagnosis:**
1. Check if SolidWorks was closed during registration
2. Verify registry entries exist
3. Check GUID consistency

**Solutions:**

#### Solution 1: Re-register with SolidWorks Closed
```bash
# 1. Close ALL SolidWorks instances (check Task Manager)
# 2. Run as Administrator:
unregister_addin.bat
register_addin.bat
# 3. Start SolidWorks
```

#### Solution 2: Verify Registry Entries
1. Open Registry Editor (`Win+R` → `regedit`)
2. Navigate to:
   ```
   HKEY_LOCAL_MACHINE\SOFTWARE\SolidWorks\Addins\{D8A3F12B-ABCD-4A87-8123-9876ABCDEF01}
   ```
3. Should have these values:
   - `(Default)` = `0` (REG_DWORD)
   - `Description` = `"Text-to-CAD: Natural Language to CAD Add-In"`
   - `Title` = `"Text-to-CAD"`

4. Also check:
   ```
   HKEY_CURRENT_USER\SOFTWARE\SolidWorks\Addins\{D8A3F12B-ABCD-4A87-8123-9876ABCDEF01}
   ```

If missing, manually create or re-run registration script.

#### Solution 3: Verify GUID Consistency
The GUID must match in three places:

**File 1: `src\Addin.cs`**
```csharp
[Guid("D8A3F12B-ABCD-4A87-8123-9876ABCDEF01")]
public class Addin : ISwAddin
```

**File 2: `src\Properties\AssemblyInfo.cs`**
```csharp
[assembly: Guid("D8A3F12B-ABCD-4A87-8123-9876ABCDEF01")]
```

**File 3: `register_addin.bat`**
```batch
set ADDIN_GUID={D8A3F12B-ABCD-4A87-8123-9876ABCDEF01}
```

If any mismatch, fix and rebuild/re-register.

### "Access Denied" During Registration

**Symptoms:**
- Registration script fails with "Access denied"
- RegAsm.exe returns error code 1

**Solution:**
1. Right-click `register_addin.bat`
2. Select **"Run as administrator"**
3. Click **Yes** on UAC prompt

If still fails:
- Check User Account Control (UAC) settings
- Ensure you have admin rights on the machine
- Try running Command Prompt as Administrator first, then run script

### "RegAsm.exe Not Found"

**Symptoms:**
```
ERROR: RegAsm.exe not found at C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe
```

**Solutions:**

#### Solution 1: Install .NET Framework 4.7.2
1. Download: https://dotnet.microsoft.com/download/dotnet-framework/net472
2. Install "Developer Pack"
3. Restart computer
4. Re-run registration script

#### Solution 2: Update Path in Script
Your .NET Framework might be in a different location:

1. Search for `RegAsm.exe` on your system
2. Common locations:
   ```
   C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe
   C:\Windows\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe
   ```
3. Edit `register_addin.bat` and `unregister_addin.bat`
4. Update `set REGASM64=...` line with correct path

---

## Build & Compilation Issues

### "Cannot Find SolidWorks.Interop.sldworks"

**Symptoms:**
```
Error CS0246: The type or namespace name 'SolidWorks' could not be found
```

**Solution:**
1. In Visual Studio, open **Solution Explorer**
2. Expand **References**
3. Remove broken references (yellow warning icons):
   - Right-click → **Remove**
4. Add correct references:
   - Right-click **References** → **Add Reference**
   - Click **Browse**
   - Navigate to SolidWorks API folder:
     ```
     C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\
     ```
   - Select these DLLs:
     - `SolidWorks.Interop.sldworks.dll`
     - `SolidWorks.Interop.swconst.dll`
     - `SolidWorks.Interop.swpublished.dll`
   - Click **Add** → **OK**
5. **Important:** Set `Embed Interop Types` to `False`:
   - Select each reference
   - In Properties window, set `Embed Interop Types` = `False`
6. Rebuild solution

**For Different SolidWorks Versions:**
- SolidWorks 2024: `C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\`
- SolidWorks 2023: Same path, different version
- Check your actual installation directory

### "Newtonsoft.Json Could Not Be Found"

**Symptoms:**
```
Error CS0246: The type or namespace name 'Newtonsoft' could not be found
```

**Solution:**
1. Restore NuGet packages:
   - **Tools** → **NuGet Package Manager** → **Manage NuGet Packages for Solution**
   - Click **Restore** button
   - Wait for completion

2. If restore fails, reinstall manually:
   - Open **Package Manager Console** (Tools → NuGet Package Manager → Package Manager Console)
   - Run:
     ```powershell
     Install-Package Newtonsoft.Json -Version 13.0.3
     ```

3. Verify `packages.config` exists:
   ```xml
   <packages>
     <package id="Newtonsoft.Json" version="13.0.3" targetFramework="net472" />
   </packages>
   ```

4. Rebuild solution

### "Platform Target Mismatch"

**Symptoms:**
```
Warning: Platform target 'AnyCPU' is not compatible with SolidWorks
```

**Solution:**
1. **Build** → **Configuration Manager**
2. **Active solution platform** → Select **x64**
3. Ensure all projects are set to **x64**
4. Click **Close**
5. Rebuild solution

**Set Default Platform:**
1. Right-click project → **Properties**
2. **Build** tab
3. **Platform target** → **x64**
4. Apply to both Debug and Release configurations

### "Register for COM Interop" Not Set

**Symptoms:**
- DLL builds but doesn't register
- No .tlb file generated

**Solution:**
1. Right-click project → **Properties**
2. **Build** tab
3. Check **☑ Register for COM interop**
4. Apply to both Debug and Release
5. Rebuild

---

## Runtime Errors

### Add-In Loads Then Immediately Crashes

**Symptoms:**
- Add-in appears in list
- Checking it causes SolidWorks to freeze or crash
- No Task Pane appears

**Diagnosis:**
Check log file: `%APPDATA%\TextToCad\logs\TextToCad_YYYYMMDD.log`

Look for exception messages.

**Common Causes & Solutions:**

#### Cause 1: Missing Dependencies
**Error in log:** `"Could not load file or assembly 'Newtonsoft.Json'"`

**Solution:**
1. Copy `Newtonsoft.Json.dll` to add-in folder:
   ```
   From: solidworks-addin\packages\Newtonsoft.Json.13.0.3\lib\net45\
   To: solidworks-addin\bin\Release\
   ```
2. Or install in GAC (Global Assembly Cache):
   ```bash
   gacutil /i Newtonsoft.Json.dll
   ```

#### Cause 2: Task Pane Creation Failed
**Error in log:** `"Failed to create Task Pane view"`

**Solution:**
1. Check SolidWorks version compatibility
2. Verify UserControl is properly designed
3. Try simplifying TaskPaneControl (remove complex controls temporarily)
4. Rebuild and test

#### Cause 3: API Connection Timeout
**Error in log:** `"API connection test failed"`

**Solution:**
- This is a warning, not a crash cause
- Ensure backend is running
- Add-in should still load

### "Type Library Registration Failed"

**Symptoms:**
```
Error: Type library registration failed
```

**Solution:**
1. Run unregister script first:
   ```bash
   unregister_addin.bat
   ```
2. Delete old .tlb file:
   ```
   solidworks-addin\bin\Release\TextToCad.SolidWorksAddin.tlb
   ```
3. Clean solution in Visual Studio
4. Rebuild
5. Re-register

### Task Pane Shows But UI is Blank

**Symptoms:**
- Task Pane window appears
- No controls visible
- White/gray empty panel

**Solutions:**

#### Solution 1: Check UserControl Loading
1. Open `TaskPaneControl.Designer.cs`
2. Verify `InitializeComponent()` is called in constructor
3. Check for designer errors

#### Solution 2: Rebuild Designer
1. Open `TaskPaneControl.cs` in designer view
2. Make a small change (move a control slightly)
3. Save
4. Rebuild
5. Re-register

#### Solution 3: Check .resx File
1. Verify `TaskPaneControl.resx` exists
2. Build action should be **Embedded Resource**
3. Right-click file → Properties → Build Action → Embedded Resource

---

## Connection Problems

### "● Disconnected" Status (Red)

**Symptoms:**
- Connection status shows red
- Test Connection fails
- Preview/Execute buttons don't work

**Diagnosis Steps:**

#### Step 1: Verify Backend is Running
```bash
# In PowerShell:
cd text-to-cad\backend
.venv\Scripts\Activate.ps1
uvicorn main:app --reload
```

Expected output:
```
INFO:     Uvicorn running on http://127.0.0.1:8000
INFO:     Application startup complete.
```

#### Step 2: Test Backend Directly
Open browser: http://localhost:8000/health

Expected response:
```json
{"status":"ok"}
```

If this fails, backend is not running correctly.

#### Step 3: Check Firewall
Windows Firewall might block localhost:

1. **Windows Security** → **Firewall & network protection**
2. **Allow an app through firewall**
3. Find **Python** or **uvicorn**
4. Check **Private** and **Public**
5. Click **OK**

#### Step 4: Verify API URL
In Task Pane Settings:
- URL should be: `http://localhost:8000`
- No trailing slash
- Click **Update** after changing
- Click **Test Connection**

#### Step 5: Check Port Conflicts
Backend might be on different port:

```bash
# Check what's running on port 8000:
netstat -ano | findstr :8000
```

If nothing, backend isn't running.
If something else, backend is on different port.

### "Request Timed Out"

**Symptoms:**
```
⏱️ Request timed out.
The backend took too long to respond.
```

**Solutions:**

#### Solution 1: Increase Timeout
Edit `app.config`:
```xml
<add key="ApiTimeoutSeconds" value="60" />
```
Rebuild and re-register.

#### Solution 2: Check Backend Performance
- Backend might be processing slowly
- Check backend logs for errors
- Try simpler instruction first

#### Solution 3: Network Issues
- If backend is remote, check network connection
- Try pinging the server
- Check VPN/proxy settings

### "Invalid JSON Response"

**Symptoms:**
```
📄 Invalid response from backend.
The server returned data in an unexpected format.
```

**Diagnosis:**
1. Check backend version matches add-in expectations
2. Verify `/dry_run` and `/process_instruction` endpoints exist
3. Test endpoints directly:
   ```bash
   curl -X POST http://localhost:8000/dry_run \
     -H "Content-Type: application/json" \
     -d '{"instruction":"test","use_ai":false}'
   ```

**Solutions:**

#### Solution 1: Update Backend
```bash
cd text-to-cad\backend
git pull
pip install -r requirements.txt --upgrade
```

#### Solution 2: Check Response Schema
Backend must return:
```json
{
  "schema_version": "1.0",
  "instruction": "...",
  "source": "rule",
  "plan": ["..."],
  "parsed_parameters": {...}
}
```

If schema is different, update `InstructionResponse.cs` model.

---

## UI Issues

### Controls Not Responding

**Symptoms:**
- Buttons don't click
- Textboxes don't accept input
- UI appears frozen

**Solutions:**

#### Solution 1: Check if Processing
- Look for "Processing..." status
- Wait for current operation to complete
- If stuck, restart SolidWorks

#### Solution 2: Thread Blocking
- Long operations might block UI thread
- Check if `async/await` is used properly in code
- Verify `SetUIEnabled()` is called correctly

### Log Text Not Showing Colors

**Symptoms:**
- All log text is same color
- No colored status messages

**Solution:**
- This is normal for some Windows themes
- Colors work in RichTextBox but may not be visible in high-contrast themes
- Functionality is not affected

### Task Pane Too Small/Large

**Solution:**
1. Drag Task Pane border to resize
2. SolidWorks remembers size per session
3. To reset: Close and reopen SolidWorks

---

## Performance Issues

### Slow API Responses

**Symptoms:**
- Preview/Execute takes many seconds
- UI becomes unresponsive during requests

**Solutions:**

#### Solution 1: Check Backend Performance
```bash
# In backend logs, look for slow requests
# Typical response time should be < 1 second
```

#### Solution 2: Use Rule-Based Parsing
- Uncheck "Use AI Parsing"
- Rule-based is much faster than AI
- AI requires OpenAI API call (network latency)

#### Solution 3: Optimize Backend
- Ensure backend is in Release mode, not Debug
- Check database performance
- Monitor CPU/memory usage

### High Memory Usage

**Symptoms:**
- SolidWorks uses excessive RAM
- System becomes slow

**Solutions:**
- Restart SolidWorks periodically
- Clear log frequently (Click "Clear Log" button)
- Check for memory leaks in custom code

---

## Debugging Tips

### Enable Debug Logging

Edit `app.config`:
```xml
<add key="LogLevel" value="Debug" />
```

Rebuild and re-register. Log file will have much more detail.

### View Real-Time Logs

**Option 1: Log File**
```bash
# Open log folder:
explorer %APPDATA%\TextToCad\logs

# Tail log file (PowerShell):
Get-Content -Path "$env:APPDATA\TextToCad\logs\TextToCad_$(Get-Date -Format 'yyyyMMdd').log" -Wait
```

**Option 2: DebugView**
1. Download DebugView from Microsoft Sysinternals
2. Run as Administrator
3. Capture → Capture Global Win32
4. See real-time debug output

### Attach Visual Studio Debugger

1. Build project in **Debug** configuration
2. Register debug DLL
3. Start SolidWorks
4. In Visual Studio: **Debug** → **Attach to Process**
5. Find `SLDWORKS.exe`
6. Click **Attach**
7. Set breakpoints in code
8. Trigger action in add-in

### Test API Independently

Test backend without add-in:

```bash
# Test /dry_run:
curl -X POST http://localhost:8000/dry_run \
  -H "Content-Type: application/json" \
  -d '{"instruction":"create cylinder 20mm diameter","use_ai":false}'

# Test /process_instruction:
curl -X POST http://localhost:8000/process_instruction \
  -H "Content-Type: application/json" \
  -d '{"instruction":"test instruction","use_ai":false}'
```

### Check SolidWorks API Logs

SolidWorks has its own logs:
```
C:\Users\[YourName]\AppData\Local\SolidWorks\SLDWORKS\logs\
```

Look for add-in related errors.

### Common Log Messages

**Normal:**
```
[Info] Text-to-CAD Add-In Loading...
[Info] Connected to SolidWorks (Cookie: 123)
[Info] Task Pane created successfully
[Info] ✓ Backend API is reachable at http://localhost:8000
```

**Warnings (Non-Critical):**
```
[Warning] ✗ Backend API is not reachable
[Warning] AI parsing failed, falling back to rules
```

**Errors (Critical):**
```
[Error] Failed to create Task Pane
[Error] API request failed with status 500
[Error] Exception: NullReferenceException
```

---

## Still Having Issues?

### Checklist

Before asking for help, verify:

- [ ] SolidWorks 2020 or later installed
- [ ] .NET Framework 4.7.2 Developer Pack installed
- [ ] Visual Studio 2019+ with .NET desktop development workload
- [ ] Project builds without errors
- [ ] Registration script runs as Administrator successfully
- [ ] SolidWorks was closed during registration
- [ ] Backend server is running on http://localhost:8000
- [ ] Backend `/health` endpoint returns `{"status":"ok"}`
- [ ] Log files checked for error messages
- [ ] All GUIDs match across files
- [ ] References to SolidWorks PIAs are correct

### Collect Diagnostic Info

1. **Log file:**
   ```
   %APPDATA%\TextToCad\logs\TextToCad_[date].log
   ```

2. **Build output:**
   - Visual Studio → View → Output
   - Copy build messages

3. **Registration output:**
   - Run `register_addin.bat`
   - Copy console output

4. **System info:**
   - SolidWorks version
   - Windows version
   - .NET Framework version

5. **Error messages:**
   - Screenshots of error dialogs
   - Exact error text

### Contact Support

Include all diagnostic info above when reporting issues.

---

**Remember:** Most issues are solved by:
1. Closing SolidWorks completely
2. Unregistering the add-in
3. Cleaning and rebuilding the project
4. Re-registering as Administrator
5. Restarting SolidWorks

**Good luck! 🚀**
