param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$installerRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $installerRoot
$publishDir = Join-Path $projectRoot "publish\$Runtime"
$distDir = Join-Path $projectRoot "dist"
$payloadDir = Join-Path $installerRoot "Payload"
$outputDir = Join-Path $installerRoot "Output"
$payloadZip = Join-Path $payloadDir "PythonCoderGamePayload.zip"

Write-Host "== Python Coder Game release pack =="
Write-Host "Project:   $projectRoot"
Write-Host "Installer: $installerRoot"

Get-Process PythonCoderGame -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "Publishing game..."
dotnet publish $projectRoot -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "Game publish failed with exit code $LASTEXITCODE"
}

if (Test-Path $distDir) {
    Remove-Item -LiteralPath $distDir -Recurse -Force
}
New-Item -ItemType Directory -Path $distDir | Out-Null
Copy-Item -Path (Join-Path $publishDir "*") -Destination $distDir -Recurse -Force

Write-Host "Creating embedded installer payload..."
New-Item -ItemType Directory -Force -Path $payloadDir, $outputDir | Out-Null
if (Test-Path $payloadZip) {
    Remove-Item -LiteralPath $payloadZip -Force
}
Compress-Archive -Path (Join-Path $distDir "*") -DestinationPath $payloadZip -CompressionLevel Optimal

Write-Host "Building setup wizard..."
dotnet publish (Join-Path $installerRoot "PythonCoderGame.Setup.csproj") -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $outputDir
if ($LASTEXITCODE -ne 0) {
    throw "Installer publish failed with exit code $LASTEXITCODE"
}

$setupExe = Join-Path $outputDir "PythonCoderGame.Setup.exe"
if (-not (Test-Path $setupExe)) {
    throw "Installer build did not produce $setupExe"
}

Write-Host ""
Write-Host "Release pack complete."
Write-Host "Game executable:      $(Join-Path $distDir 'PythonCoderGame.exe')"
Write-Host "Installer executable: $setupExe"
