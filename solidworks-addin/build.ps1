# Build script for Text-to-CAD SolidWorks Add-In
$msbuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
$projectFile = "TextToCad.SolidWorksAddin.csproj"

if (-not (Test-Path $msbuildPath)) {
    Write-Error "MSBuild not found at: $msbuildPath"
    exit 1
}

Write-Host "Building Text-to-CAD Add-In..." -ForegroundColor Cyan
# Skip COM registration during build - we'll do it separately with register_addin.bat
& $msbuildPath $projectFile /p:Configuration=Debug /p:RegisterForComInterop=false /t:Clean,Build /v:minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild succeeded!" -ForegroundColor Green
} else {
    Write-Host "`nBuild failed!" -ForegroundColor Red
    exit 1
}
