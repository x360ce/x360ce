# Install_BuildTools.ps1
# Ensures a Visual Studio install with the v141 (VS 2017) C++ toolset is
# available, then builds the full x360ce.sln (C++ DLLs + .NET apps).
#
# Target OS: Windows 10 or newer (Win 8/8.1/7 dropped). End users need
# "Visual C++ 2015-2022 Redistributable" installed to load the produced DLLs.
#
# Usage:
#   .\Install_BuildTools.ps1

$ErrorActionPreference = 'Stop'

# --- Configuration ---------------------------------------------------------

$RepoRoot    = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$ProjectFile = Join-Path $RepoRoot 'x360ce.sln'
$BuildConfig = 'Release'
# Build both architectures in one run.
# Native projects compile as Win32/x64; the .sln maps to x86/x64 for App.3
# and AnyCPU for Engine, App.4, Web.
$BuildPlats  = @('Win32', 'x64')

$Toolset     = 'v141'
$ToolsetComp = 'Microsoft.VisualStudio.Component.VC.v141.x86.x64'

# Fallback when no VS install exists at all
$FallbackWingetId = 'Microsoft.VisualStudio.2022.BuildTools'
$FallbackWorkload = 'Microsoft.VisualStudio.Workload.VCTools'

# --- Helpers ---------------------------------------------------------------

function Get-VsWhere {
    $p = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $p) { return $p }
    return $null
}

function Find-VsInstall([string]$vswhere) {
    if (-not $vswhere) { return $null }
    & $vswhere -all -prerelease -latest `
        -requires Microsoft.Component.MSBuild `
        -version '[17.0,19.0)' `
        -format json | ConvertFrom-Json | Select-Object -First 1
}

function Test-Toolset([string]$vsRoot, [string]$toolset) {
    # MSVC version prefixes that map to each PlatformToolset
    $prefix = switch ($toolset) {
        'v140' { '14.0' }
        'v141' { '14.1' }
        'v142' { '14.2' }
        'v143' { '14.3' }
        'v144' { '14.4' }
        'v145' { '14.5' }
        default { return $false }
    }
    $found = Get-ChildItem -Directory `
        -Path (Join-Path $vsRoot 'VC\Tools\MSVC') -ErrorAction SilentlyContinue |
        Where-Object Name -like "$prefix*" | Select-Object -First 1
    return [bool]$found
}

function Get-LatestWin10Sdk {
    $root = "${env:ProgramFiles(x86)}\Windows Kits\10\Include"
    if (-not (Test-Path $root)) { return $null }
    Get-ChildItem -Directory $root |
        Where-Object Name -match '^10\.0\.\d+\.\d+$' |
        Sort-Object Name |
        Select-Object -Last 1 -ExpandProperty Name
}

# --- 1. Pre-flight ---------------------------------------------------------

if (-not (Test-Path $ProjectFile)) {
    throw "Project not found: $ProjectFile"
}

$vswhere = Get-VsWhere
$vs = Find-VsInstall $vswhere

# --- 2. Install fallback if no VS exists ----------------------------------

if (-not $vs) {
    Write-Host "No Visual Studio install detected. Installing VS 2022 Build Tools with $Toolset..."
    $override = "--passive --wait --norestart --add $FallbackWorkload --add $ToolsetComp --includeRecommended"
    & winget install --id $FallbackWingetId --exact `
        --accept-source-agreements --accept-package-agreements `
        --override $override
    if ($LASTEXITCODE -ne 0) { throw "winget install failed: $LASTEXITCODE" }
    $vswhere = Get-VsWhere
    $vs = Find-VsInstall $vswhere
    if (-not $vs) { throw "Install completed but vswhere cannot find VS." }
}

Write-Host "Using: $($vs.displayName)  ($($vs.installationVersion))"
Write-Host "Path : $($vs.installationPath)"

# --- 3. Add v141 component if missing --------------------------------------

if (-not (Test-Toolset $vs.installationPath $Toolset)) {
    Write-Host "$Toolset toolset not present. Adding via VS Installer (UAC prompt expected)..."
    $setup = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\setup.exe"
    if (-not (Test-Path $setup)) {
        $setup = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vs_installer.exe"
    }
    if (-not (Test-Path $setup)) { throw "VS Installer setup.exe not found." }

    # Single string ensures the path with spaces is quoted correctly across
    # PowerShell -> Start-Process -> setup.exe argv parsing.
    $argLine = 'modify --installPath "{0}" --add {1} --passive --norestart' `
        -f $vs.installationPath, $ToolsetComp
    $proc = Start-Process -FilePath $setup -ArgumentList $argLine `
        -Verb RunAs -Wait -PassThru
    if ($proc.ExitCode -ne 0 -and $proc.ExitCode -ne 3010) {
        throw "VS Installer modify failed: exit $($proc.ExitCode)"
    }

    # Wait for any lingering installer child processes to exit
    Get-Process -Name 'setup','vs_installer','vs_installershell','vs_bootstrapper' `
        -ErrorAction SilentlyContinue | Wait-Process -Timeout 600 -ErrorAction SilentlyContinue

    if (-not (Test-Toolset $vs.installationPath $Toolset)) {
        throw "$Toolset still not present after install. Open the VS Installer GUI and add 'MSVC v141 - VS 2017 C++ x64/x86 build tools' manually, then re-run."
    }
}

# --- 4. Resolve SDK and MSBuild --------------------------------------------

$winSdk = Get-LatestWin10Sdk
if (-not $winSdk) { throw "No Windows 10/11 SDK found." }

$msbuild = & $vswhere -path $vs.installationPath `
    -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild -or -not (Test-Path $msbuild)) {
    throw "MSBuild.exe not found under $($vs.installationPath)"
}

Write-Host "PlatformToolset            : $Toolset"
Write-Host "WindowsTargetPlatformVersion: $winSdk"
Write-Host "MSBuild                    : $msbuild"

# --- 5. NuGet restore (for the .NET projects in the solution) -------------

Write-Host ""
Write-Host "Restoring NuGet packages..."
& $msbuild $ProjectFile /t:Restore /v:m /nologo
if ($LASTEXITCODE -ne 0) {
    Write-Warning "NuGet restore failed (exit $LASTEXITCODE). Continuing — projects without packages may still build."
}

# --- 6. Build both platforms ----------------------------------------------

foreach ($plat in $BuildPlats) {
    Write-Host ""
    Write-Host "Building $ProjectFile  ($BuildConfig|$plat)"
    Write-Host "----------------------------------------------------------------"

    & $msbuild $ProjectFile `
        /p:Configuration=$BuildConfig `
        /p:Platform=$plat `
        /p:PlatformToolset=$Toolset `
        /p:WindowsTargetPlatformVersion=$winSdk `
        /m /v:m /nologo

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Build failed for $BuildConfig|$plat (exit $LASTEXITCODE)."
        exit $LASTEXITCODE
    }
}

Write-Host ""
Write-Host "All platforms built successfully."
