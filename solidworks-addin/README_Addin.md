# Text-to-CAD SolidWorks Add-In

Natural language to CAD automation add-in for SolidWorks. Convert text instructions into CAD operations using AI or rule-based parsing.

## 🎯 Features

- **Task Pane UI** - Dockable panel integrated into SolidWorks
- **Dry Run Preview** - See what will happen before executing
- **AI & Rule-Based Parsing** - Choose between OpenAI GPT or regex parsing
- **Execution Plan Display** - Human-readable steps before execution
- **Comprehensive Logging** - File and UI logging for debugging
- **Connection Testing** - Verify backend API connectivity
- **Configurable API URL** - Easy backend server configuration

## 📋 Prerequisites

Before you begin, ensure you have:

### Required Software
- ✅ **Visual Studio 2019 or later** (Community Edition is fine)
  - Download: https://visualstudio.microsoft.com/downloads/
  - Required workload: ".NET desktop development"
  
- ✅ **.NET Framework 4.7.2 Developer Pack**
  - Download: https://dotnet.microsoft.com/download/dotnet-framework/net472
  
- ✅ **SolidWorks 2020 or later** (2024 recommended)
  - Student, Professional, or Premium edition
  
- ✅ **SolidWorks API SDK** (included with SolidWorks installation)
  - Located at: `C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\`

### Required Permissions
- ⚠️ **Administrator rights** for COM registration
- ⚠️ **Write access** to `%APPDATA%\TextToCad\` for logging

### Backend Requirements
- ✅ **FastAPI backend running** on `http://localhost:8000`
  - See main project README for backend setup
  - Must have `/dry_run` and `/process_instruction` endpoints

## 🚀 Quick Start

### Step 1: Open Project in Visual Studio

1. Navigate to `text-to-cad/solidworks-addin/`
2. Double-click `TextToCad.SolidWorksAddin.csproj`
3. Visual Studio will open the project

### Step 2: Configure SolidWorks References

The project references SolidWorks API DLLs. You may need to update the paths:

1. In Visual Studio, open **Solution Explorer**
2. Expand **References**
3. Look for these references (they may show warning icons):
   - `SolidWorks.Interop.sldworks`
   - `SolidWorks.Interop.swconst`
   - `SolidWorks.Interop.swpublished`

4. If they show warnings, remove and re-add them:
   - Right-click each → **Remove**
   - Right-click **References** → **Add Reference**
   - Click **Browse** → Navigate to:
     ```
     C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\
     ```
   - Select all three DLL files
   - Click **Add**

**For SolidWorks 2024:**
- Path: `C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\`
- Files: `SolidWorks.Interop.sldworks.dll`, `SolidWorks.Interop.swconst.dll`, `SolidWorks.Interop.swpublished.dll`

**For other versions:**
- The path is usually the same, but verify your SolidWorks installation directory

### Step 3: Restore NuGet Packages

1. In Visual Studio, go to **Tools** → **NuGet Package Manager** → **Manage NuGet Packages for Solution**
2. Click **Restore** (if prompted)
3. Verify `Newtonsoft.Json` (v13.0.3) is installed

Or use Package Manager Console:
```powershell
Update-Package -reinstall
```

### Step 4: Build the Project

1. Select **Release** configuration (dropdown at top)
2. Select **x64** platform
3. **Build** → **Build Solution** (or press `Ctrl+Shift+B`)
4. Verify build succeeds with no errors
5. Output DLL will be in: `bin\Release\TextToCad.SolidWorksAddin.dll`

**Common Build Errors:**
- **"Cannot find SolidWorks.Interop..."** → Update references (see Step 2)
- **"Newtonsoft.Json not found"** → Restore NuGet packages (see Step 3)
- **"Platform target mismatch"** → Ensure x64 is selected

### Step 5: Register the Add-In

1. **Close SolidWorks** if it's running
2. Navigate to `solidworks-addin\` folder
3. **Right-click** `register_addin.bat`
4. Select **"Run as administrator"**
5. Wait for success message

**Expected Output:**
```
============================================================================
SUCCESS! Add-in registered successfully.
============================================================================
```

If registration fails, see [Troubleshooting](#troubleshooting) section.

### Step 6: Enable Add-In in SolidWorks

1. **Start SolidWorks**
2. Go to **Tools** → **Add-Ins**
3. Find **"Text-to-CAD"** in the list
4. Check **both boxes**:
   - ☑️ Active (loads for this session)
   - ☑️ Start Up (loads automatically)
5. Click **OK**

The Task Pane should appear on the right side of SolidWorks!

### Step 7: Start Backend Server

The add-in needs the FastAPI backend running:

```bash
cd text-to-cad/backend
.venv\Scripts\Activate.ps1
uvicorn main:app --reload
```

Verify backend is running at: http://localhost:8000/docs

### Step 8: Test the Add-In

1. In the Task Pane, click **🔌 Test Connection**
2. Status should show **● Connected** (green)
3. Enter an instruction: `"create a 20mm diameter cylinder 30mm tall"`
4. Click **🔍 Preview (Dry Run)**
5. Review the execution plan
6. Click **⚙️ Execute** to process

## 📖 Usage Guide

### Task Pane Interface

```
┌─────────────────────────────────┐
│ Text-to-CAD                     │
├─────────────────────────────────┤
│ CAD Instruction:                │
│ ┌─────────────────────────────┐ │
│ │ Enter instruction here...   │ │
│ │                             │ │
│ └─────────────────────────────┘ │
│ ☐ Use AI Parsing                │
│ ┌──────────┐ ┌────────────────┐ │
│ │ Preview  │ │    Execute     │ │
│ └──────────┘ └────────────────┘ │
├─────────────────────────────────┤
│ 📋 Execution Plan               │
│ • Create cylinder Ø20 mm        │
│ • Height: 30 mm                 │
├─────────────────────────────────┤
│ 📝 Log                          │
│ ✓ Preview complete              │
│ Source: Rule-based parsing      │
├─────────────────────────────────┤
│ ⚙️ Settings                     │
│ Backend API URL:                │
│ http://localhost:8000           │
│ ● Connected                     │
└─────────────────────────────────┘
```

### Workflow

#### 1. Preview (Dry Run)
- Enter instruction in textbox
- Optionally check "Use AI Parsing"
- Click **🔍 Preview**
- Review the execution plan
- No changes are made to database or CAD model

#### 2. Execute
- After previewing, click **⚙️ Execute**
- Confirm the action
- Command is saved to database
- (Future: Will execute CAD operations in SolidWorks)

### Example Instructions

**Basic Shapes:**
```
create a 20mm diameter cylinder 30mm tall
extrude a 50mm cube
make a sphere with 15mm radius
```

**Holes:**
```
add a 6mm hole
create 4 holes in a circular pattern
drill 8 holes with 5mm diameter
```

**Patterns:**
```
pattern 6 features in a circle
create linear array of 5 holes
```

**With AI Parsing:**
- Check "Use AI Parsing" checkbox
- Use more natural language:
  ```
  I need four mounting holes equally spaced
  add some holes for M6 bolts
  make it 5mm thicker
  ```

### Settings Panel

**Backend API URL:**
- Default: `http://localhost:8000`
- Change if backend is on different port/server
- Click **Update** after changing
- Click **🔌 Test Connection** to verify

**Connection Status:**
- **● Connected** (green) - Backend reachable
- **● Disconnected** (red) - Cannot reach backend

**Log Files:**
- Click **📂 Open Log Folder** to view detailed logs
- Location: `%APPDATA%\TextToCad\logs\`
- Files named: `TextToCad_YYYYMMDD.log`

## 🔧 Configuration

### App.config Settings

Edit `app.config` to change defaults:

```xml
<appSettings>
  <!-- Backend URL -->
  <add key="ApiBaseUrl" value="http://localhost:8000" />
  
  <!-- Logging -->
  <add key="LogLevel" value="Info" />
  <!-- Options: Debug, Info, Warning, Error -->
  
  <add key="EnableFileLogging" value="true" />
  
  <!-- API timeout -->
  <add key="ApiTimeoutSeconds" value="30" />
</appSettings>
```

After changing, rebuild the project.

### Log Levels

- **Debug**: Verbose logging (HTTP requests/responses, detailed flow)
- **Info**: Normal operations (commands, API calls, status changes)
- **Warning**: Non-critical issues (connection problems, fallbacks)
- **Error**: Failures (exceptions, API errors, validation failures)

## 🐛 Troubleshooting

### Add-In Not Appearing in SolidWorks

**Problem:** "Text-to-CAD" not in Tools → Add-Ins list

**Solutions:**
1. Verify registration succeeded:
   - Re-run `register_addin.bat` as Administrator
   - Check for "SUCCESS" message
   
2. Check SolidWorks is closed during registration:
   - Close all SolidWorks instances
   - Re-register
   
3. Verify GUID matches:
   - Open `src\Addin.cs` - check `[Guid("...")]`
   - Open `src\Properties\AssemblyInfo.cs` - check `[assembly: Guid("...")]`
   - Open `register_addin.bat` - check `set ADDIN_GUID=...`
   - All three must be identical: `{D8A3F12B-ABCD-4A87-8123-9876ABCDEF01}`

4. Check registry:
   - Open Registry Editor (regedit.exe)
   - Navigate to: `HKLM\SOFTWARE\SolidWorks\Addins\{D8A3F12B-ABCD-4A87-8123-9876ABCDEF01}`
   - Should exist with "Title" = "Text-to-CAD"

### Task Pane Not Showing

**Problem:** Add-in is enabled but Task Pane doesn't appear

**Solutions:**
1. Check both boxes in Add-Ins dialog (Active + Start Up)
2. Restart SolidWorks completely
3. Check logs: `%APPDATA%\TextToCad\logs\`
4. Look for errors in log file

### Connection Failed

**Problem:** Status shows "● Disconnected" (red)

**Solutions:**
1. Verify backend is running:
   ```bash
   cd text-to-cad/backend
   uvicorn main:app --reload
   ```
   
2. Test backend directly:
   - Open browser: http://localhost:8000/health
   - Should return: `{"status":"ok"}`
   
3. Check firewall:
   - Windows Firewall may block localhost connections
   - Add exception for Python/uvicorn
   
4. Verify API URL in Settings panel matches backend

### Build Errors

**Problem:** "Cannot find SolidWorks.Interop.sldworks"

**Solution:**
1. Update references (see Step 2 in Quick Start)
2. Verify SolidWorks API SDK is installed
3. Check path: `C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\`

**Problem:** "Newtonsoft.Json could not be found"

**Solution:**
1. Restore NuGet packages:
   - Tools → NuGet Package Manager → Manage NuGet Packages
   - Click Restore
2. Or reinstall:
   ```powershell
   Install-Package Newtonsoft.Json -Version 13.0.3
   ```

**Problem:** "Platform target 'AnyCPU' is not compatible"

**Solution:**
1. Change platform to **x64**:
   - Build → Configuration Manager
   - Active solution platform → x64
   - Check all projects are x64
2. Rebuild solution

### COM Registration Errors

**Problem:** "Access denied" during registration

**Solution:**
- Run `register_addin.bat` as Administrator
- Right-click → "Run as administrator"

**Problem:** "RegAsm.exe not found"

**Solution:**
- Install .NET Framework 4.7.2 Developer Pack
- Verify path in batch file matches your system:
  ```
  C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe
  ```

### Runtime Errors

**Problem:** Add-in loads but crashes immediately

**Solutions:**
1. Check log file for exception details
2. Verify all dependencies are present:
   - Newtonsoft.Json.dll
   - SolidWorks Interop DLLs
3. Rebuild in Release mode (not Debug)
4. Unregister, clean, rebuild, re-register:
   ```bash
   unregister_addin.bat
   # Clean and rebuild in Visual Studio
   register_addin.bat
   ```

**Problem:** "Could not load file or assembly 'Newtonsoft.Json'"

**Solution:**
- Copy `Newtonsoft.Json.dll` to same folder as add-in DLL
- Or install Newtonsoft.Json in GAC:
  ```bash
  gacutil /i Newtonsoft.Json.dll
  ```

## 🔄 Updating the Add-In

When you make code changes:

1. Close SolidWorks
2. Rebuild project in Visual Studio
3. No need to re-register (unless GUID changed)
4. Restart SolidWorks
5. Changes will be active

If you change the GUID or assembly info:
1. Unregister old version: `unregister_addin.bat`
2. Rebuild
3. Register new version: `register_addin.bat`

## 🗑️ Uninstalling

1. Close SolidWorks
2. Run `unregister_addin.bat` as Administrator
3. Delete the `solidworks-addin\` folder
4. (Optional) Delete logs: `%APPDATA%\TextToCad\`

## 📁 Project Structure

```
solidworks-addin/
├── src/
│   ├── Addin.cs                    # Main ISwAddin entry point
│   ├── TaskPaneHost.cs             # Task pane manager
│   ├── TaskPaneControl.cs          # UI logic
│   ├── TaskPaneControl.Designer.cs # UI layout
│   ├── TaskPaneControl.resx        # UI resources
│   ├── ApiClient.cs                # HTTP client for backend
│   ├── Logger.cs                   # File & debug logging
│   ├── ErrorHandler.cs             # Exception handling
│   ├── Models/
│   │   ├── InstructionRequest.cs   # Request DTO
│   │   ├── InstructionResponse.cs  # Response DTO
│   │   └── ParsedParameters.cs     # Parameter models
│   └── Properties/
│       └── AssemblyInfo.cs         # COM attributes
├── TextToCad.SolidWorksAddin.csproj # Project file
├── app.config                      # Configuration
├── packages.config                 # NuGet packages
├── register_addin.bat              # Registration script
├── unregister_addin.bat            # Unregistration script
├── README_Addin.md                 # This file
└── TROUBLESHOOTING.md              # Detailed troubleshooting
```

## 🔐 Security Notes

- **Development Only**: This add-in uses HTTP (not HTTPS) for localhost development
- **Production**: Use HTTPS for production deployments
- **API Keys**: Store OpenAI API keys in backend, not in add-in
- **Permissions**: Add-in runs with SolidWorks user permissions

## 🚀 Next Steps

After getting the add-in working:

1. **Test with various instructions** - Try different CAD commands
2. **Check logs** - Review `%APPDATA%\TextToCad\logs\` for insights
3. **Customize UI** - Modify `TaskPaneControl.Designer.cs` for your needs
4. **Add CAD operations** - Implement actual SolidWorks API calls (Sprint SW-2+)
5. **Deploy to team** - Share DLL and registration scripts

## 📞 Support

**Issues:**
- Check TROUBLESHOOTING.md for detailed solutions
- Review log files in `%APPDATA%\TextToCad\logs\`
- Check backend logs for API errors

**Resources:**
- SolidWorks API Help: `C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\`
- FastAPI Docs: http://localhost:8000/docs
- Project Repository: [Your GitHub URL]

## 📝 Version Compatibility

| Component | Version | Notes |
|-----------|---------|-------|
| .NET Framework | 4.7.2 | Required for SolidWorks 2020+ |
| SolidWorks | 2020-2024 | Tested on 2024 Student Edition |
| Visual Studio | 2019+ | Community Edition supported |
| Windows | 10/11 | 64-bit required |

**Upgrading SolidWorks Versions:**
- Update PIA references to match your SolidWorks version
- Rebuild and re-register
- No code changes needed for minor version updates

---

**Built with ❤️ for engineers who want to design with language, not menus.**
