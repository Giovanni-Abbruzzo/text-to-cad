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

## 🛠️ Utility Helpers (Sprint SW-2)

The add-in includes a comprehensive set of utility classes to simplify SolidWorks API operations. These helpers handle common tasks like unit conversion, plane selection, face finding, and safe undo/rollback.

### Units - Dimension Conversion

**CRITICAL:** SolidWorks API expects dimensions in **METERS**, but users think in **MILLIMETERS**.

```csharp
using TextToCad.SolidWorksAddin.Utils;

// Convert user input (mm) to API format (meters)
double userDepth = 50;  // mm
double apiDepth = Units.MmToM(userDepth);  // 0.05 m

// Use in API calls
featureManager.FeatureExtrusion2(..., apiDepth, ...);

// Convert API output (meters) to display format (mm)
double featureDepth = feature.GetDepth();  // meters
double displayDepth = Units.MToMm(featureDepth);  // mm
Console.WriteLine($"Depth: {displayDepth} mm");
```

**Available Methods:**
- `Units.MmToM(double mm)` - Convert millimeters to meters
- `Units.MToMm(double m)` - Convert meters to millimeters

**Constants:**
- `Units.OneMm` = 0.001 (1mm in meters)
- `Units.OneCm` = 0.01 (1cm in meters)
- `Units.OneM` = 1000.0 (1m in millimeters)

### UndoScope - Safe Rollback

Provides RAII-style (Resource Acquisition Is Initialization) guard for automatic rollback if operations fail.

```csharp
using TextToCad.SolidWorksAddin.Utils;
using TextToCad.SolidWorksAddin.Interfaces;

ILogger logger = new Utils.Logger(msg => txtLog.AppendText(msg + "\r\n"));

// Wrap risky operations in UndoScope
using (var scope = new UndoScope(modelDoc, "Create Base Plate", logger))
{
    // Select plane
    Selection.SelectPlaneByName(swApp, modelDoc, "Top Plane");
    
    // Create sketch
    modelDoc.SketchManager.InsertSketch(true);
    modelDoc.SketchManager.CreateCenterRectangle(0, 0, 0, 
        Units.MmToM(50), Units.MmToM(50), Units.MmToM(0));
    modelDoc.SketchManager.InsertSketch(true);
    
    // Create extrude feature
    modelDoc.FeatureManager.FeatureExtrusion2(true, false, false,
        0, 0, Units.MmToM(10), 0, false, false, ...);
    
    // If all operations succeed, commit the changes
    scope.Commit();
}
// If Commit() was not called (e.g., exception thrown), 
// Dispose() automatically rolls back all changes
```

**How It Works:**
1. Constructor calls `model.SetUndoPoint()` at start
2. Perform your operations inside the `using` block
3. Call `scope.Commit()` if all operations succeed
4. If exception or early return, `Dispose()` calls `model.EditRollback()`

**Important Notes:**
- Always use with `using` statement for automatic disposal
- Call `Commit()` only after all operations succeed
- Rollback behavior varies by SolidWorks version
- Some operations cannot be undone programmatically

### Selection - Planes and Faces

High-level helpers for selecting geometry in SolidWorks.

#### Select Plane by Name

```csharp
using TextToCad.SolidWorksAddin.Utils;

// Select a reference plane to start a sketch
if (Selection.SelectPlaneByName(swApp, modelDoc, "Top Plane", logger: logger))
{
    modelDoc.SketchManager.InsertSketch(true);
    // Draw sketch entities...
    modelDoc.SketchManager.InsertSketch(true);
}
else
{
    logger.Error("Failed to select Top Plane");
}
```

**Common Plane Names:**
- `"Front Plane"` - XY plane (front view)
- `"Top Plane"` - XZ plane (top view)
- `"Right Plane"` - YZ plane (right view)

**Parameters:**
- `append` = `false` (default): Clears selection first
- `append` = `true`: Adds to current selection

#### Find Topmost Planar Face

Automatically finds the highest planar face in the part (useful for hole patterns on top surface).

```csharp
// Find the top face of a part
IFace2 topFace = Selection.GetTopMostPlanarFace(modelDoc, logger);

if (topFace != null)
{
    // Select the face
    Selection.SelectFace(modelDoc, topFace);
    
    // Start sketch on that face
    modelDoc.SketchManager.InsertSketch(true);
    
    // Create hole pattern...
}
else
{
    logger.Warn("No planar faces found, using Top Plane as fallback");
    Selection.SelectPlaneByName(swApp, modelDoc, "Top Plane");
}
```

**How It Works:**
1. Iterates through all solid bodies in the part
2. Examines all faces of each body
3. Filters to planar faces only
4. Calculates center Z-coordinate of each face
5. Returns face with maximum Z value (highest in model space)

**Limitations:**
- Only works for Part documents (not Assemblies or Drawings)
- Assumes standard orientation (+Z = up)
- Does not account for face area (small top face will still win)

### Logger - Lightweight Logging

Thread-safe logger that can forward messages to UI controls or debug output.

```csharp
using TextToCad.SolidWorksAddin.Utils;
using TextToCad.SolidWorksAddin.Interfaces;

// Create logger that forwards to Task Pane log
ILogger logger = new Utils.Logger(msg => 
{
    txtLog.AppendText(msg + "\r\n");
    txtLog.ScrollToCaret();
});

// Use in your code
logger.Info("Starting feature creation");
logger.Warn("Face selection returned null, using default");
logger.Error("Failed to create extrude: " + ex.Message);

// Alternative: Debug output only
ILogger debugLogger = Utils.Logger.Debug();

// Alternative: Null logger (discards all messages)
ILogger nullLogger = Utils.Logger.Null();
```

**Methods:**
- `Info(string message)` - Informational messages
- `Warn(string message)` - Non-critical warnings
- `Error(string message)` - Error messages

**Message Format:**
```
[HH:mm:ss.fff] [INFO] Starting feature creation
[HH:mm:ss.fff] [WARN] Face selection returned null
[HH:mm:ss.fff] [ERROR] Failed to create extrude
```

### Complete Example - Create Base Plate

Putting it all together:

```csharp
using System;
using TextToCad.SolidWorksAddin.Utils;
using TextToCad.SolidWorksAddin.Interfaces;
using SolidWorks.Interop.sldworks;

public bool CreateBasePlate(
    ISldWorks swApp, 
    IModelDoc2 modelDoc, 
    double widthMm, 
    double depthMm, 
    double heightMm,
    ILogger logger)
{
    // Use UndoScope for safe rollback on failure
    using (var scope = new UndoScope(modelDoc, "Create Base Plate", logger))
    {
        try
        {
            logger.Info($"Creating base plate: {widthMm}×{depthMm}×{heightMm} mm");
            
            // Clear any existing selection
            Selection.ClearSelection(modelDoc, logger);
            
            // Select Top Plane
            if (!Selection.SelectPlaneByName(swApp, modelDoc, "Top Plane", logger: logger))
            {
                logger.Error("Failed to select Top Plane");
                return false;
            }
            
            // Start sketch
            modelDoc.SketchManager.InsertSketch(true);
            
            // Create rectangle (centered at origin)
            // Convert dimensions to meters
            double widthM = Units.MmToM(widthMm);
            double depthM = Units.MmToM(depthMm);
            
            modelDoc.SketchManager.CreateCenterRectangle(
                0, 0, 0,  // Center point at origin
                widthM / 2, depthM / 2, 0  // Half-widths
            );
            
            // Exit sketch
            modelDoc.SketchManager.InsertSketch(true);
            
            // Create extrude feature
            double heightM = Units.MmToM(heightMm);
            
            IFeature feature = modelDoc.FeatureManager.FeatureExtrusion2(
                true,     // SD (single direction)
                false,    // Flip
                false,    // Dir
                0,        // T1 (extrude type: blind)
                0,        // T2
                heightM,  // D1 (depth in meters)
                0,        // D2
                false,    // DDir
                false,    // DDir2
                false,    // DDirBoth
                0,        // DDirAngle
                0,        // DDirAngle2
                false,    // Merge
                false,    // UseFeatScope
                false,    // UseAutoSelect
                false,    // AssemblyFeatureScope
                false,    // AutoSelectComponents
                false     // PropagateFeatureToParts
            );
            
            if (feature == null)
            {
                logger.Error("FeatureExtrusion2 returned null");
                return false;
            }
            
            logger.Info("✓ Base plate created successfully");
            
            // All operations succeeded - commit the changes
            scope.Commit();
            return true;
        }
        catch (Exception ex)
        {
            logger.Error($"Exception creating base plate: {ex.Message}");
            return false;
            // UndoScope will automatically rollback on exception
        }
    }
}
```

### Utility Best Practices

**1. Always Convert Units:**
```csharp
// ✓ CORRECT
double depthM = Units.MmToM(50);  // Convert before API call
feature.FeatureExtrusion2(..., depthM, ...);

// ✗ WRONG
feature.FeatureExtrusion2(..., 50, ...);  // 50 meters instead of 50mm!
```

**2. Use UndoScope for Multi-Step Operations:**
```csharp
// ✓ CORRECT - Wrapped in UndoScope
using (var scope = new UndoScope(model, "Operation", logger))
{
    CreateSketch();
    CreateFeature();
    scope.Commit();  // Only commits if both succeed
}

// ✗ RISKY - No rollback if second operation fails
CreateSketch();  // Succeeds
CreateFeature();  // Fails - now model is in inconsistent state
```

**3. Check Selection Results:**
```csharp
// ✓ CORRECT
if (!Selection.SelectPlaneByName(swApp, model, "Top Plane"))
{
    logger.Error("Selection failed");
    return;
}

// ✗ RISKY - Assumes selection always succeeds
Selection.SelectPlaneByName(swApp, model, "Top Plane");
modelDoc.SketchManager.InsertSketch(true);  // May fail if plane not selected
```

**4. Use Logger for Diagnostics:**
```csharp
// ✓ CORRECT - Detailed logging
logger.Info("Starting hole creation");
if (hole == null)
{
    logger.Error("Hole feature returned null");
    logger.Error($"Parameters: diameter={dia}, depth={dep}");
}

// ✗ MINIMAL - Hard to debug
// Silent failure, no diagnostics
```

### Utility Limitations

**UndoScope:**
- Rollback behavior varies by SolidWorks version (2015-2024)
- Some operations cannot be undone programmatically (e.g., file save)
- May not work perfectly for all feature types
- If `SetUndoPoint()` fails, rollback scope is limited

**Selection:**
- Plane names are case-sensitive and version-dependent
- Assembly plane selection requires fully qualified names
- Face finding assumes standard model orientation (+Z up)
- Works only for Part documents (not Assemblies/Drawings)

**Units:**
- Assumes metric units (mm/m)
- Does not handle imperial units (inches/feet)
- Some API methods may use different unit conventions

### Version Compatibility

These utilities are tested with:
- ✅ SolidWorks 2020-2024
- ✅ .NET Framework 4.7.2
- ✅ Windows 10/11 64-bit

For older SolidWorks versions:
- Some API methods may have different signatures
- `IFace2.Select4()` may need to be `Select()` or `Select2()`
- Undo/rollback mechanisms may differ
- Consult your version's API Help documentation

---

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
