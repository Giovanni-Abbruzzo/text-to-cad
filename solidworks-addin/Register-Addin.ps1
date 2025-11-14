# ============================================================================
# Text-to-CAD SolidWorks Add-In Registration Script (PowerShell)
# ============================================================================
# MUST BE RUN AS ADMINISTRATOR!
# ============================================================================

Write-Host ""
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "Text-to-CAD SolidWorks Add-In Registration" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Please right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Configuration
$addinGuid = "{D8A3F12B-ABCD-4A87-8123-9876ABCDEF01}"
$addinDll = "TextToCad.SolidWorksAddin.dll"
$regasm = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"

# Get script directory
$scriptDir = $PSScriptRoot
if ([string]::IsNullOrEmpty($scriptDir)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
}
if ([string]::IsNullOrEmpty($scriptDir)) {
    $scriptDir = Get-Location
}

Write-Host "Script directory: $scriptDir" -ForegroundColor Gray

# Check if RegAsm exists
Write-Host "Checking for RegAsm.exe..." -ForegroundColor Yellow
if (-not (Test-Path $regasm)) {
    Write-Host "ERROR: RegAsm.exe not found at $regasm" -ForegroundColor Red
    Write-Host "Please verify .NET Framework 4.7.2 is installed" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "[OK] RegAsm.exe found" -ForegroundColor Green

# Determine DLL path (try Release first, then Debug)
$dllPath = Join-Path $scriptDir "bin\Release\$addinDll"
Write-Host "Checking Release path: $dllPath" -ForegroundColor Gray

if (-not (Test-Path $dllPath)) {
    Write-Host "[INFO] Release build not found, trying Debug..." -ForegroundColor Yellow
    $dllPath = Join-Path $scriptDir "bin\Debug\$addinDll"
    Write-Host "Checking Debug path: $dllPath" -ForegroundColor Gray
}

# Check if DLL exists
Write-Host "Looking for DLL..." -ForegroundColor Yellow
if (-not (Test-Path $dllPath)) {
    Write-Host ""
    Write-Host "ERROR: Add-in DLL not found!" -ForegroundColor Red
    Write-Host "Expected location: $dllPath" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please build the project in Visual Studio first:" -ForegroundColor Yellow
    Write-Host "1. Open TextToCad.SolidWorksAddin.csproj in Visual Studio"
    Write-Host "2. Build > Build Solution (press Ctrl+Shift+B)"
    Write-Host "3. Run this script again as Administrator"
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "[OK] DLL found: $dllPath" -ForegroundColor Green


# Register the add-in
Write-Host "Registering add-in with COM..." -ForegroundColor Yellow
Write-Host "Command: `"$regasm`" `"$dllPath`" /codebase" -ForegroundColor Gray
Write-Host ""

try {
    $output = & $regasm $dllPath /codebase 2>&1
    $exitCode = $LASTEXITCODE
    
    # Display RegAsm output
    $output | ForEach-Object { Write-Host $_ }
    
    if ($exitCode -ne 0) {
        Write-Host ""
        Write-Host "ERROR: Registration failed with exit code: $exitCode" -ForegroundColor Red
        Write-Host "Make sure you are running this script as Administrator." -ForegroundColor Yellow
        Read-Host "Press Enter to exit"
        exit 1
    }
}
catch {
    Write-Host ""
    Write-Host "ERROR: Registration failed!" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "============================================================================" -ForegroundColor Green
Write-Host "SUCCESS! Add-in registered successfully." -ForegroundColor Green
Write-Host "============================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Start SolidWorks"
Write-Host "2. Go to Tools > Add-Ins"
Write-Host "3. Find 'Text-to-CAD' in the list"
Write-Host "4. Check both boxes (active and startup)"
Write-Host "5. Click OK"
Write-Host ""
Write-Host "The Task Pane should appear on the right side of SolidWorks." -ForegroundColor Green
Write-Host ""
Write-Host "If you don't see the add-in in the list:" -ForegroundColor Yellow
Write-Host "- Make sure SolidWorks is closed when registering"
Write-Host "- Try running this script as Administrator again"
Write-Host "- Check the log file at: $env:APPDATA\TextToCad\logs\"
Write-Host ""
Write-Host "============================================================================" -ForegroundColor Cyan
Read-Host "Press Enter to exit"
