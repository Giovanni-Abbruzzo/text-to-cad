@echo off
REM ============================================================================
REM Text-to-CAD SolidWorks Add-In Registration Script
REM ============================================================================
REM
REM This script registers the add-in with Windows COM and SolidWorks.
REM MUST BE RUN AS ADMINISTRATOR!
REM
REM Before running:
REM 1. Build the project in Visual Studio (Release configuration recommended)
REM 2. Verify the DLL path below matches your build output
REM 3. Right-click this file and select "Run as administrator"
REM
REM ============================================================================

echo.
echo ============================================================================
echo Text-to-CAD SolidWorks Add-In Registration
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
    echo ERROR: Add-in DLL not found!
    echo Expected location: %DLL_PATH%
    echo.
    echo Please build the project in Visual Studio first:
    echo 1. Open TextToCad.SolidWorksAddin.sln in Visual Studio
    echo 2. Build -^> Build Solution (or press Ctrl+Shift+B)
    echo 3. Run this script again as Administrator
    pause
    exit /b 1
)

echo Found DLL: %DLL_PATH%
echo.

REM Register the add-in
echo Registering add-in with COM...
"%REGASM64%" "%DLL_PATH%" /codebase /tlb

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Registration failed!
    echo Make sure you are running this script as Administrator.
    pause
    exit /b 1
)

echo.
echo ============================================================================
echo SUCCESS! Add-in registered successfully.
echo ============================================================================
echo.
echo Next steps:
echo 1. Start SolidWorks
echo 2. Go to Tools -^> Add-Ins
echo 3. Find "Text-to-CAD" in the list
echo 4. Check both boxes (active and startup)
echo 5. Click OK
echo.
echo The Task Pane should appear on the right side of SolidWorks.
echo.
echo If you don't see the add-in in the list:
echo - Make sure SolidWorks is closed when registering
echo - Try running this script as Administrator again
echo - Check the log file at: %%APPDATA%%\TextToCad\logs\
echo.
echo ============================================================================
pause
