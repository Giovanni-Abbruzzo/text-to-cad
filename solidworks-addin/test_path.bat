@echo off
set ADDIN_DLL=TextToCad.SolidWorksAddin.dll
set DLL_PATH=%~dp0bin\Release\%ADDIN_DLL%
echo DLL_PATH is: %DLL_PATH%
echo Testing if exists...
if exist "%DLL_PATH%" (
    echo YES - File exists!
) else (
    echo NO - File not found!
)
dir "%DLL_PATH%"
pause
