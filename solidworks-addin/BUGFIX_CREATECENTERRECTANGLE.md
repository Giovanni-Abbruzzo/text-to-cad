# 🐛 Bug Fix: CreateCenterRectangle Returning Null

## Issue Description

**Symptom:**
```
[INFO] Creating center rectangle (150×150 mm)...
[ERROR] CreateCenterRectangle returned null
[WARN] Undo scope not committed, attempting rollback
```

Sketch starts but rectangle creation fails, causing rollback.

---

## Root Cause

`SketchManager.CreateCenterRectangle()` returns null when:

1. **Sketch not fully active** - `InsertSketch(true)` called but sketch not ready for geometry
2. **Selection conflicts** - Plane still selected when trying to create sketch geometry
3. **View orientation** - View not properly oriented to sketch plane
4. **Timing issue** - API called before SolidWorks is ready

---

## The Fix

### What Was Changed

**File:** `BasePlateBuilder.cs`

**Added after `InsertSketch(true)`:**

```csharp
// Verify sketch is active and clear selections
if (model.SketchManager.ActiveSketch == null)
{
    _log.Error("Failed to activate sketch");
    return false;
}

// Clear any selections to avoid conflicts
model.ClearSelection2(true);

_log.Info("✓ Sketch active and ready");
```

**Why this works:**
- ✅ Explicitly checks `ActiveSketch` is not null
- ✅ Clears plane selection before drawing
- ✅ Ensures SolidWorks is in proper state
- ✅ Better error detection

### Fallback Strategy Added

If `CreateCenterRectangle` still fails, now tries `CreateCornerRectangle`:

```csharp
object rectObj = model.SketchManager.CreateCenterRectangle(...);

if (rectObj == null)
{
    _log.Error("CreateCenterRectangle returned null");
    _log.Error("Attempting alternative: CreateCornerRectangle...");
    
    // Try corner rectangle as fallback
    rectObj = model.SketchManager.CreateCornerRectangle(
        -halfSizeM, -halfSizeM, 0,  // Bottom-left corner
        halfSizeM, halfSizeM, 0     // Top-right corner
    );
    
    if (rectObj == null)
    {
        _log.Error("CreateCornerRectangle also returned null");
        return false;
    }
    
    _log.Info("✓ Rectangle created using corner method (fallback)");
}
```

**Why this helps:**
- ✅ Different API method may work when center method fails
- ✅ Same geometric result (centered rectangle)
- ✅ Provides redundancy

### Enhanced Logging

Added detailed logging to help diagnose issues:

```csharp
_log.Info($"Creating center rectangle ({sizeMm}×{sizeMm} mm)...");
_log.Info($"  Center: (0, 0, 0), Half-size: {halfSizeM:F6} meters");
```

Shows exact values being passed to API for debugging.

---

## Expected Behavior After Fix

### Successful Creation

```
[INFO] Starting sketch...
[INFO] ✓ Sketch active and ready
[INFO] Creating center rectangle (150×150 mm)...
[INFO]   Center: (0, 0, 0), Half-size: 0.075000 meters
[INFO] ✓ Center rectangle created successfully
[INFO] Exiting sketch...
[INFO] Creating boss-extrude (thickness=12 mm)...
[INFO] ✓ Base plate created successfully: 'Boss-Extrude1'
```

### Using Fallback (if needed)

```
[INFO] Starting sketch...
[INFO] ✓ Sketch active and ready
[INFO] Creating center rectangle (150×150 mm)...
[INFO]   Center: (0, 0, 0), Half-size: 0.075000 meters
[ERROR] CreateCenterRectangle returned null
[ERROR] Attempting alternative: CreateCornerRectangle...
[INFO] ✓ Rectangle created using corner method (fallback)
[INFO] Exiting sketch...
[INFO] Creating boss-extrude (thickness=12 mm)...
[INFO] ✓ Base plate created successfully: 'Boss-Extrude1'
```

### Complete Failure (improved error info)

```
[INFO] Starting sketch...
[ERROR] Failed to activate sketch
[WARN] Undo scope not committed, attempting rollback
```

or

```
[INFO] Starting sketch...
[INFO] ✓ Sketch active and ready
[INFO] Creating center rectangle (150×150 mm)...
[ERROR] CreateCenterRectangle returned null
[ERROR] Attempting alternative: CreateCornerRectangle...
[ERROR] CreateCornerRectangle also returned null - sketch geometry creation failed
[ERROR] Possible causes:
[ERROR]   - Sketch plane not properly selected
[ERROR]   - Invalid dimensions (too small or too large)
[ERROR]   - SolidWorks in unexpected state
```

---

## Testing the Fix

### Test 1: Same Command That Failed

**Instruction:**
```
create 150mm base plate 12mm thick
```

**Expected:**
- ✅ Sketch activates
- ✅ Rectangle created (either center or corner method)
- ✅ Extrude successful
- ✅ Boss-Extrude1 appears in FeatureManager

### Test 2: Various Sizes

Try different dimensions to ensure robustness:

```
base plate                          (80×6mm - default)
create 50mm base plate              (small)
create 100mm base plate 8mm thick   (medium)
create 200mm base plate 15mm thick  (large)
```

All should work now!

### Test 3: Edge Cases

```
create 10mm base plate              (very small - 10×10mm)
create 500mm base plate             (very large - 500×500mm)
```

Should either succeed or give clear error messages.

---

## SolidWorks API Insights

### Why CreateCenterRectangle Can Fail

**Common causes in SolidWorks API:**

1. **Selection state**
   - Plane still selected after `InsertSketch`
   - Solution: `ClearSelection2(true)`

2. **Sketch not ready**
   - `InsertSketch` returns before fully ready
   - Solution: Check `ActiveSketch != null`

3. **View orientation**
   - View not normal to sketch plane
   - Usually auto-handled by SolidWorks
   - Could add: `model.ShowNamedView2("*Normal To", 7)` if needed

4. **Timing/threading**
   - Rare, but API call too fast after plane selection
   - ClearSelection2 helps synchronize

5. **Invalid parameters**
   - Dimensions too small (< 0.000001m) or too large (> 1000m)
   - Our values (0.04m to 0.25m typical) are safe

### Alternative Methods Comparison

| Method | Parameters | When to Use |
|--------|-----------|-------------|
| `CreateCenterRectangle` | Center + corner | Symmetric parts, centered features |
| `CreateCornerRectangle` | Two opposite corners | Asymmetric, positioned features |
| `CreateLine` (4×) | Manual lines | Maximum control, complex shapes |

All produce same result for our use case!

---

## Code Changes Summary

### Modified Methods

**`BasePlateBuilder.EnsureBasePlate()`:**
- Added sketch activation check
- Added `ClearSelection2(true)`
- Added detailed value logging
- Added fallback to `CreateCornerRectangle`
- Enhanced error messages

### Lines Changed
- **Before:** ~15 lines for sketch + rectangle
- **After:** ~35 lines (added validation + fallback)
- **Net:** +20 lines

### Robustness Improvements
- ✅ Explicit validation of sketch state
- ✅ Selection cleanup before drawing
- ✅ Two methods to create rectangle (redundancy)
- ✅ Detailed logging for debugging
- ✅ Clear error messages with causes

---

## How to Apply the Fix

### Step 1: Rebuild

```powershell
# In Visual Studio
Ctrl+Shift+B
```

Verify: 0 errors

### Step 2: Re-register (if needed)

```powershell
# Run as Administrator
cd C:\WindsurfProjects\Engen\text-to-cad\solidworks-addin
.\register_addin.bat
```

### Step 3: Test

1. Close SolidWorks (if open)
2. Open SolidWorks
3. File → New → Part
4. Tools → Add-Ins → Enable "Text-to-CAD"
5. Try the failed command:
   ```
   create 150mm base plate 12mm thick
   ```

### Expected Result

```
✓ Sketch active and ready
✓ Center rectangle created successfully
✓ Base plate created successfully: 'Boss-Extrude1'
✓ CAD geometry created successfully!
```

And **Boss-Extrude1** appears in FeatureManager! 🎉

---

## Additional Debugging

If it **still** fails after these changes:

### Check Log for Exact Error

Look for:
```
[ERROR] Failed to activate sketch
```
→ Issue before CreateCenterRectangle

```
[ERROR] CreateCornerRectangle also returned null
```
→ Issue with sketch state or parameters

### Try Manual Test

In SolidWorks:
1. Manually select Top Plane
2. Insert → Sketch
3. Tools → Sketch Tools → Rectangle
4. Try to draw manually

If manual drawing also fails → SolidWorks configuration issue

### Check SolidWorks Settings

- Tools → Options → System Options → Sketching
- Ensure "Automatic sketch solve" is enabled
- Ensure "Display plane when shaded" is enabled

### Check Document Template

Some templates have different default planes. Verify:
- "Top Plane" exists in FeatureManager tree
- Plane is not hidden or suppressed

---

## Prevention for Future Builders

When creating new builders (Sprint SW-4+), always:

1. **Check sketch activation:**
   ```csharp
   model.SketchManager.InsertSketch(true);
   if (model.SketchManager.ActiveSketch == null)
       return false;
   ```

2. **Clear selections:**
   ```csharp
   model.ClearSelection2(true);
   ```

3. **Log detailed values:**
   ```csharp
   _log.Info($"Creating geometry: {value:F6} meters");
   ```

4. **Provide fallbacks:**
   ```csharp
   if (primaryMethod == null)
   {
       _log.Error("Primary method failed, trying fallback...");
       fallbackMethod();
   }
   ```

5. **Use UndoScope:**
   Already in place! Ensures rollback on failure.

---

## Status

✅ **Fix Applied**  
✅ **Code Updated**  
✅ **Logging Enhanced**  
✅ **Fallback Added**  
⏳ **Testing Required** - Please rebuild and test!

---

**This fix makes base plate creation much more robust!** 🛡️

The combination of validation, cleanup, fallback, and detailed logging should resolve the CreateCenterRectangle issue and provide better diagnostics if other issues arise.

**Please rebuild and try again with your 150mm base plate command!**
