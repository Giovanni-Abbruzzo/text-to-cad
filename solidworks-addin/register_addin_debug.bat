@echo off
REM Debug version of registration script
echo.
echo ============================================================================
echo Text-to-CAD SolidWorks Add-In Registration (DEBUG)
echo ============================================================================
echo.

REM Configuration
set ADDIN_GUID={D8A3F12B-ABCD-4A87-8123-9876ABCDEF01}
set ADDIN_DLL=TextToCad.SolidWorksAddin.dll
echo [DEBUG] ADDIN_DLL=%ADDIN_DLL%

REM Determine .NET Framework path (64-bit)
set REGASM64=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe
echo [DEBUG] REGASM64=%REGASM64%

REM Check if RegAsm exists
echo [DEBUG] Checking if RegAsm exists...
if not exist "%REGASM64%" (
    echo ERROR: RegAsm.exe not found at %REGASM64%
    echo Please verify .NET Framework 4.7.2 is installed
    pause
    exit /b 1
)
echo [DEBUG] RegAsm found!

REM Determine DLL path (try Release first, then Debug)
echo [DEBUG] Current directory: %~dp0
set DLL_PATH=%~dp0bin\Release\%ADDIN_DLL%
echo [DEBUG] DLL_PATH=%DLL_PATH%

echo [DEBUG] Checking if Release DLL exists...
if not exist "%DLL_PATH%" (
    echo [DEBUG] Release build not found, trying Debug...
    set DLL_PATH=%~dp0bin\Debug\%ADDIN_DLL%
    echo [DEBUG] New DLL_PATH=%DLL_PATH%
)

REM Check if DLL exists
echo [DEBUG] Final check if DLL exists at: %DLL_PATH%
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
echo [DEBUG] Running: "%REGASM64%" "%DLL_PATH%" /codebase /tlb
"%REGASM64%" "%DLL_PATH%" /codebase /tlb

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Registration failed with error code: %ERRORLEVEL%
    echo Make sure you are running this script as Administrator.
    pause
    exit /b 1
)

echo.
echo ============================================================================
echo SUCCESS! Add-in registered successfully.
echo ============================================================================
echo.
pause
