<#
.SYNOPSIS
    Universal Build & Distribution Script for Modern x360ce Suite.
.DESCRIPTION
    Builds Engine, App.v4, Setup utility, runs tests, and packages Release_Portable.
.EXAMPLE
    .\Build.ps1 -Configuration Release -Package
#>

param (
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$RunTests,
    [switch]$Package
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "       X360CE MODERN SUITE BUILD & DEPLOY ENGINE            " -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Configuration : $Configuration" -ForegroundColor Gray
Write-Host "Root Directory: $repoRoot" -ForegroundColor Gray
Write-Host ""

# 1. Build Core Engine
Write-Host "[1/4] Building x360ce.Engine ($Configuration)..." -ForegroundColor Cyan
dotnet build (Join-Path $repoRoot "Engine\x360ce.Engine.csproj") -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed for x360ce.Engine" }

# 2. Build Modern App (App.v4)
Write-Host "[2/4] Building x360ce.App.v4 ($Configuration)..." -ForegroundColor Cyan
dotnet build (Join-Path $repoRoot "App.v4\x360ce.App.v4.csproj") -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed for x360ce.App.v4" }

# 3. Build Setup Utility
Write-Host "[3/4] Building Setup Utility ($Configuration)..." -ForegroundColor Cyan
dotnet build (Join-Path $repoRoot "Setup\Setup.csproj") -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed for Setup" }

# 4. Optional Unit Tests
if ($RunTests) {
    Write-Host "[4/4] Running Unit Test Suite ($Configuration)..." -ForegroundColor Cyan
    dotnet test (Join-Path $repoRoot "Tests\x360ce.Tests.csproj") -c $Configuration --filter "TestCategory!=ui-interactive" --nologo
    if ($LASTEXITCODE -ne 0) { throw "Unit tests failed!" }
} else {
    Write-Host "[4/4] Tests skipped (pass -RunTests to execute test suite)." -ForegroundColor DarkGray
}

# 5. Optional Package into Release_Portable
if ($Package) {
    Write-Host ""
    Write-Host "[PACKAGING] Synchronizing binaries to Release_Portable..." -ForegroundColor Yellow
    $portableDir = Join-Path $repoRoot "Release_Portable"
    if (-not (Test-Path $portableDir)) {
        New-Item -ItemType Directory -Path $portableDir -Force | Out-Null
    }

    $binApp = Join-Path $repoRoot "App.v4\bin\$Configuration\x360ce.exe"
    $binConfig = Join-Path $repoRoot "App.v4\bin\$Configuration\x360ce.exe.config"
    $binEngine = Join-Path $repoRoot "Engine\bin\$Configuration\x360ce.Engine.dll"
    $binSetup = Join-Path $repoRoot "Setup\bin\$Configuration\net462\Setup.exe"
    $syncScript = Join-Path $repoRoot "scripts\Inject_Games.ps1"

    Copy-Item $binApp -Destination (Join-Path $portableDir "x360ce.exe") -Force
    if (Test-Path $binConfig) { Copy-Item $binConfig -Destination (Join-Path $portableDir "x360ce.exe.config") -Force }
    Copy-Item $binEngine -Destination (Join-Path $portableDir "x360ce.Engine.dll") -Force
    Copy-Item $binSetup -Destination (Join-Path $portableDir "Setup.exe") -Force
    if (Test-Path $syncScript) {
        Copy-Item $syncScript -Destination (Join-Path $portableDir "Sync_Games.ps1") -Force
        Copy-Item $syncScript -Destination (Join-Path $portableDir "Inject_Games.ps1") -Force
    }

    Write-Host "Portable package updated successfully at: $portableDir" -ForegroundColor Green
    Get-ChildItem $portableDir | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
}

Write-Host "============================================================" -ForegroundColor Green
Write-Host "                 BUILD COMPLETED SUCCESSFULLY!              " -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
