<#
.SYNOPSIS
  Publish self-contained win-x64 build, zip it, and optionally build an MSI (WiX v5).

.EXAMPLE
  .\scripts\pack-release.ps1 -Version 1.0.1
#>
param(
    [Parameter(Mandatory = $true)]
    [string] $Version,

    [string] $Configuration = "Release",

    [string] $Runtime = "win-x64",

    [switch] $SkipMsi
)

$ErrorActionPreference = "Stop"

function Assert-SemVer([string] $value) {
    if ($value -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') {
        throw "Version must look like 1.0.0 (got '$value')."
    }
}

function Assert-LastExitCode([string] $step) {
    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "$step failed with exit code $LASTEXITCODE"
    }
}

Assert-SemVer $Version

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $root

$publishDir = Join-Path $root "publish\$Runtime"
$outDir = Join-Path $root "artifacts"
$installerDir = Join-Path $root "installer"
$zipName = "AetherDownloader-v$Version-$Runtime.zip"
$msiName = "AetherDownloader-v$Version-$Runtime.msi"
$zipPath = Join-Path $outDir $zipName
$msiPath = Join-Path $outDir $msiName
$iconFile = Join-Path $root "src\AetherDownloader.App\Assets\app.ico"
$licenseRtf = Join-Path $installerDir "License.rtf"
$packageWxs = Join-Path $installerDir "Package.wxs"
$dotnetTools = Join-Path $env:USERPROFILE ".dotnet\tools"

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}
if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir | Out-Null
}

Write-Host "==> Publishing self-contained $Runtime ($Version)"
dotnet publish (Join-Path $root "src\AetherDownloader.App\AetherDownloader.App.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:Version=$Version `
    -p:FileVersion=$Version `
    -p:InformationalVersion=$Version `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir
Assert-LastExitCode "dotnet publish"

if (-not (Test-Path (Join-Path $publishDir "AetherDownloader.App.exe"))) {
    throw "Publish output missing AetherDownloader.App.exe"
}

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Write-Host "==> Creating $zipName"
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

if ($SkipMsi) {
    Write-Host "SkipMsi set - done. Zip: $zipPath"
    return
}

Write-Host "==> Ensuring WiX CLI"
if ($env:PATH -notlike "*$dotnetTools*") {
    $env:PATH = $dotnetTools + ";" + $env:PATH
}
if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
    dotnet tool install --global wix --version 5.0.2
    Assert-LastExitCode "dotnet tool install wix"
    $env:PATH = $dotnetTools + ";" + $env:PATH
}

Write-Host "==> Restoring WiX UI extension"
Push-Location $installerDir
try {
    & wix extension add "WixToolset.UI.wixext/5.0.2"
    # Non-zero is OK if already installed; verify with list.
    & wix extension list | Out-Host
}
finally {
    Pop-Location
}

$msiVersion = ($Version -split '\.')[0..2] -join '.'

Write-Host "==> Building MSI ($msiVersion)"
# WiX resolves extensions relative to the .wxs directory / wix.json there.
Push-Location $installerDir
try {
    & wix build $packageWxs `
        -ext WixToolset.UI.wixext `
        -d ("PublishDir=" + $publishDir) `
        -d ("Version=" + $msiVersion) `
        -d ("IconFile=" + $iconFile) `
        -d ("LicenseRtf=" + $licenseRtf) `
        -arch x64 `
        -o $msiPath
    Assert-LastExitCode "wix build"
}
finally {
    Pop-Location
}

if (-not (Test-Path $msiPath)) {
    throw "MSI was not created: $msiPath"
}

Write-Host ""
Write-Host "Artifacts:"
Write-Host ("  " + $zipPath)
Write-Host ("  " + $msiPath)
