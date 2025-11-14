# Text-to-CAD Add-In - Quick Start Guide

**Get up and running in 10 minutes!**

## ✅ Prerequisites Checklist

Before you start, make sure you have:

- [ ] Visual Studio 2019+ installed
- [ ] .NET Framework 4.7.2 Developer Pack installed
- [ ] SolidWorks 2020+ installed
- [ ] Administrator rights on your computer
- [ ] FastAPI backend running (see main README)

## 🚀 5-Step Setup

### Step 1: Open Project (1 minute)

```bash
cd text-to-cad/solidworks-addin
# Double-click: TextToCad.SolidWorksAddin.csproj
```

Visual Studio will open.

### Step 2: Update SolidWorks References (2 minutes)

1. In **Solution Explorer**, expand **References**
2. If you see yellow warning icons:
   - Remove the three SolidWorks references
   - Right-click **References** → **Add Reference** → **Browse**
   - Navigate to: `C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\`
   - Select all three DLLs:
     - `SolidWorks.Interop.sldworks.dll`
     - `SolidWorks.Interop.swconst.dll`
     - `SolidWorks.Interop.swpublished.dll`
   - Click **Add**
3. For each reference, set **Embed Interop Types** = **False**

### Step 3: Build (1 minute)

1. Select **Release** configuration (dropdown at top)
2. Select **x64** platform
3. Press **Ctrl+Shift+B** to build
4. Wait for "Build succeeded" message

### Step 4: Register (1 minute)

1. **Close SolidWorks** if running
2. Navigate to `solidworks-addin\` folder
3. **Right-click** `register_addin.bat`
4. Select **"Run as administrator"**
5. Wait for "SUCCESS!" message

### Step 5: Enable in SolidWorks (2 minutes)

1. **Start SolidWorks**
2. **Tools** → **Add-Ins**
3. Find **"Text-to-CAD"**
4. Check **both boxes** (Active + Start Up)
5. Click **OK**

**Done!** The Task Pane should appear on the right.

## 🧪 Test It

### Test 1: Connection

1. In Task Pane, click **🔌 Test Connection**
2. Should show **● Connected** (green)

If disconnected:
```bash
# Start backend:
cd text-to-cad/backend
.venv\Scripts\Activate.ps1
uvicorn main:app --reload
```

### Test 2: Preview

1. Enter: `"create a 20mm diameter cylinder 30mm tall"`
2. Click **🔍 Preview (Dry Run)**
3. Check **📋 Execution Plan** shows:
   ```
   • Create cylinder Ø20.0 mm × 30.0 mm height
   ```

### Test 3: Execute

1. Click **⚙️ Execute**
2. Confirm the dialog
3. Check **📝 Log** shows:
   ```
   ✓ Execution complete (saved to database)
   ```

## 🎉 Success!

You now have a working SolidWorks add-in that:
- ✅ Connects to your FastAPI backend
- ✅ Parses natural language instructions
- ✅ Shows execution plans
- ✅ Saves commands to database

## 📚 Next Steps

- Read **README_Addin.md** for detailed documentation
- Try different instructions (see examples in README)
- Check logs at: `%APPDATA%\TextToCad\logs\`
- Customize the UI in `TaskPaneControl.Designer.cs`

## ❌ Troubleshooting

**Add-in not in list?**
- Make sure SolidWorks was closed during registration
- Re-run `register_addin.bat` as Administrator

**Build errors?**
- Update SolidWorks references (Step 2)
- Restore NuGet packages: Tools → NuGet Package Manager → Restore

**Connection failed?**
- Start backend server (see Test 1 above)
- Check firewall isn't blocking localhost

**Still stuck?**
- See **TROUBLESHOOTING.md** for detailed solutions
- Check log files: `%APPDATA%\TextToCad\logs\`

---

**Total time: ~10 minutes** ⏱️

**Questions?** Check README_Addin.md or TROUBLESHOOTING.md
