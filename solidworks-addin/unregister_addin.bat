@echo off
REM ============================================================================
REM Text-to-CAD SolidWorks Add-In Unregistration Script
REM ============================================================================
REM
REM This script unregisters the add-in from Windows COM and SolidWorks.
REM MUST BE RUN AS ADMINISTRATOR!
REM
REM Use this when:
REM - Uninstalling the add-in
REM - Troubleshooting registration issues
REM - Before re-registering with a new build
REM
REM ============================================================================

echo.
echo ============================================================================
echo Text-to-CAD SolidWorks Add-In Unregistration
echo ============================================================================
echo.

REM Configuration
set ADDIN_GUID={D8A3F12B-ABCD-4A87-8123-9876ABCDEF01}
set ADDIN_DLL=TextToCad.SolidWorksAddin.dll

REM Determine .NET Framework path (64-bit)
set REGASM64=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe

REM Check if RegAsm exists
if not exist "%REGASM64%" (
    echo ERROR: RegAsm.exe not found at %REGASM64%
    echo Please verify .NET Framework 4.7.2 is installed
    pause
    exit /b 1
)

REM Determine DLL path (try Release first, then Debug)
set DLL_PATH=%~dp0bin\Release\%ADDIN_DLL%
if not exist "%DLL_PATH%" (
    echo Release build not found, trying Debug...
    set DLL_PATH=%~dp0bin\Debug\%ADDIN_DLL%
)

REM Check if DLL exists
if not exist "%DLL_PATH%" (
    echo WARNING: Add-in DLL not found at %DLL_PATH%
    echo Will attempt to unregister anyway using GUID...
    echo.
)

echo Unregistering add-in from COM...
"%REGASM64%" "%DLL_PATH%" /unregister

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo WARNING: Unregistration may have failed.
    echo This is normal if the add-in was not previously registered.
    echo.
) else (
    echo.
    echo SUCCESS! Add-in unregistered successfully.
    echo.
)

echo.
echo ============================================================================
echo Cleaning up registry entries...
echo ============================================================================
echo.

REM Clean up SolidWorks add-in registry keys
echo Removing HKLM registry key...
reg delete "HKLM\SOFTWARE\SolidWorks\Addins\%ADDIN_GUID%" /f 2>nul
if %ERRORLEVEL% EQU 0 (
    echo   - HKLM key removed
) else (
    echo   - HKLM key not found (already clean)
)

echo Removing HKCU registry key...
reg delete "HKCU\SOFTWARE\SolidWorks\Addins\%ADDIN_GUID%" /f 2>nul
if %ERRORLEVEL% EQU 0 (
    echo   - HKCU key removed
) else (
    echo   - HKCU key not found (already clean)
)

echo.
echo ============================================================================
echo Unregistration complete!
echo ============================================================================
echo.
echo The add-in has been removed from SolidWorks.
echo.
echo If SolidWorks is currently running:
echo 1. Close SolidWorks completely
echo 2. Restart SolidWorks
echo 3. The add-in should no longer appear in Tools -^> Add-Ins
echo.
echo To reinstall:
echo 1. Build the project in Visual Studio
echo 2. Run register_addin.bat as Administrator
echo.
echo ============================================================================
pause
