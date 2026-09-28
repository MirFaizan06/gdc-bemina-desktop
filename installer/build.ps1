<#
.SYNOPSIS
    Builds the College Admin Windows installer (MSI) — Phase 1 Stage 17.

.DESCRIPTION
    Two steps, reproducible from a clean checkout:
      1. `dotnet publish` the desktop app self-contained for win-x64 (so a college PC that has
         never installed .NET can still run it — CLAUDE.md's "work reliably on ordinary Windows
         PCs" requirement, taken literally: no runtime prerequisite to assume is present).
      2. `wix build` Product.wxs against that publish output into a single MSI.

    Requires the WiX Toolset v5 dotnet tool (free, MIT-licensed — pinned to v5, not v6+, because
    v6 gates most commands behind an "Open Source Maintenance Fee" EULA that v5 does not have; see
    the comment at the top of Product.wxs and docs/claude/18_DEVLOG.md's Stage 17 entry for why
    that distinction matters here). One-time setup on a machine that doesn't have it yet:

        dotnet tool install --global wix --version 5.0.2
        wix extension add --global WixToolset.UI.wixext/5.0.2
        wix extension add --global WixToolset.Util.wixext/5.0.2

.PARAMETER Version
    Product version, e.g. "1.0.0". Must match desktop/Directory.Build.props's <Version> for the
    installed app to correctly report its own version to the updater — bump both together.

.EXAMPLE
    .\build.ps1 -Version 1.0.0
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"
$installerDir = $PSScriptRoot
$desktopDir = Split-Path $installerDir -Parent
$publishDir = Join-Path $installerDir "publish"
$outputMsi = Join-Path $installerDir "CollegeAdminSetup-$Version.msi"

Write-Host "== Publishing CollegeAdmin.Desktop (self-contained, win-x64, Release) ==" -ForegroundColor Cyan
dotnet publish (Join-Path $desktopDir "src\CollegeAdmin.Desktop\CollegeAdmin.Desktop.csproj") `
    -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

Write-Host "== Building MSI with WiX v5 ==" -ForegroundColor Cyan
wix build (Join-Path $installerDir "Product.wxs") `
    -d "ProductVersion=$Version" `
    -d "PublishDir=$publishDir" `
    -ext WixToolset.UI.wixext/5.0.2 `
    -ext WixToolset.Util.wixext/5.0.2 `
    -arch x64 `
    -o $outputMsi
if ($LASTEXITCODE -ne 0) { throw "wix build failed." }

Write-Host "== Validating the built MSI (standard ICE checks) ==" -ForegroundColor Cyan
wix msi validate $outputMsi
if ($LASTEXITCODE -ne 0) { throw "MSI validation failed." }

Write-Host "Built: $outputMsi" -ForegroundColor Green
Write-Host "Next: compute its SHA-256 and publish both the file and the hash — see" -ForegroundColor Yellow
Write-Host "  gdc-bemina-website/gdc-bemina-website-main/config/desktop_releases.json" -ForegroundColor Yellow
Write-Host "  (Get-FileHash $outputMsi -Algorithm SHA256).Hash" -ForegroundColor Yellow
