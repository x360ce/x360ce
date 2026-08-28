<#
.SYNOPSIS
    Builds, signs and packs the whole release from a clean tree.
.DESCRIPTION
    One entry point from an empty tree to the final zips. Everything specific to
    the project is read from App_1_Sign_and_Zip.json, so this script is copied
    between projects unchanged in the same way its companion is.

    The order matters and is the reason this script exists. Several signed files
    are embedded into the applications rather than shipped beside them, so they
    have to be signed before the application that carries them is compiled.
    Signing everything after one build would leave the applications holding
    unsigned copies while the loose files on disk looked correct.

    Each stage in the JSON file names the platforms built before the files of
    that stage are signed, and the stages run in the order they are listed:

      Library - the native and redistributed files, signed where they are built.
      Engine  - the engine, built on its own so it carries a signature before the
                applications embed it.
      App     - the applications themselves, built once everything they embed is
                signed, then signed and zipped.

    A stage lists what it Builds, each entry a platform and optionally a project;
    without a project the solution is built. Signing skips any file which already
    carries a trusted signature, so a rerun only asks for the token for files the
    build has actually replaced.

    The clean step removes every bin, bin64 and obj folder beside a project of
    the solution, and the output folders holding the zips. Nothing outside the
    repository is touched and every removed path is printed.
.PARAMETER Configuration
    Build configuration. Defaults to the value in the JSON file.
.PARAMETER NoClean
    Keeps the existing build output. Use while iterating; a release should run
    the clean so no file survives from an earlier build.
.PARAMETER SkipSign
    Builds and zips without signing, for checking the build on a machine without
    the signing token. The zips it produces are not releasable.
.EXAMPLE
    PS> .\App_0_Release.ps1
    Asks whether to sign, then runs the full release: clean, build, sign, zip.
    The question is skipped when -SkipSign is given or nobody can answer.
.EXAMPLE
    PS> .\App_0_Release.ps1 -SkipSign -NoClean
    Rebuilds and repacks without the token, keeping existing output.
.NOTES
    Supports -WhatIf, which reports the build output the clean step would remove
    and then stops, so nothing is built or signed.

    Signing needs the USB token or card reader supported by the signing module,
    and SIGN_MODULE_PATH set to the path of that module. The release stops before
    it builds anything when the module is not there.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Configuration,
    [switch]$NoClean,
    [switch]$SkipSign
)

$ErrorActionPreference = "Stop"

#------------------------------------------------------------------------------
# Configuration.
#------------------------------------------------------------------------------

$signScript = Join-Path $PSScriptRoot "App_1_Sign_and_Zip.ps1"
$configPath = [System.IO.Path]::ChangeExtension($signScript, ".json")
if (-not (Test-Path -LiteralPath $configPath)) { throw "Configuration not found: $configPath" }
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json

if (-not $config.Stages) { throw "The configuration lists no build stages: $configPath" }
foreach ($stage in $config.Stages) {
    if (-not $stage.Builds) { throw "Stage $($stage.Name) lists nothing to build: $configPath" }
}
if (-not $Configuration) { $Configuration = $config.Configuration }
if (-not $Configuration) { $Configuration = "Release" }

function Resolve-Path2 {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, $Path))
}

$solution = Resolve-Path2 $config.Solution
if (-not (Test-Path -LiteralPath $solution)) { throw "Solution not found: $solution" }
$root = [System.IO.Path]::GetDirectoryName($solution)

# The signing module lives outside the repository and its path differs per machine,
# so the configuration names an environment variable rather than one maintainer's
# folder. Set SIGN_MODULE_PATH to the full path of the signing module.
#
#------------------------------------------------------------------------------
# Menu.
#------------------------------------------------------------------------------

# Asked only when the caller did not already say, and only when there is somebody
# to answer. Passing -SkipSign either way keeps the script silent for scripted
# builds, and so does a redirected input stream, which is how build agents run it.
$canAsk = [Environment]::UserInteractive -and -not [Console]::IsInputRedirected
if (-not $PSBoundParameters.ContainsKey("SkipSign") -and -not $WhatIfPreference -and $canAsk) {
    Write-Host ""
    Write-Host "  Release build" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "    1. Build everything and sign it"
    Write-Host "       Needs the signing token. This is what a release is built with."
    Write-Host ""
    Write-Host "    2. Build everything without signing"
    Write-Host "       No token needed. The zips it produces are not releasable."
    Write-Host ""
    Write-Host "    Q. Quit"
    Write-Host ""
    $chosen = $false
    while (-not $chosen) {
        switch ((Read-Host "Choose").Trim().ToUpperInvariant()) {
            "1" { $SkipSign = $false; $chosen = $true }
            "2" { $SkipSign = $true;  $chosen = $true }
            "Q" { Write-Host "Cancelled. Nothing was built."; return }
            default { Write-Host "Enter 1, 2 or Q." -ForegroundColor Yellow }
        }
    }
    Write-Host ""
}

# The signing module reports a missing token by carrying on unsigned, which would
# produce a release that looks finished and is not. Stop before building instead.
if (-not $SkipSign) {
    foreach ($module in ($config.Apps.SignModule | Select-Object -Unique)) {
        $module = [System.Environment]::ExpandEnvironmentVariables($module)
        if (-not (Test-Path -LiteralPath $module)) {
            throw "Signing module not found: $module`nSet SIGN_MODULE_PATH to the signing module path, or run with -SkipSign to build without signing."
        }
    }
}

# vswhere is asked first because it is the supported way to find an installation. It
# reports nothing when the installer's instance record is missing, which happens after
# some upgrades and after the package cache is cleaned, even though Visual Studio itself
# works. The folders it would have pointed at are then searched directly rather than
# failing on a machine which can build perfectly well.
function Find-VsTool {
    param(
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$Name
    )
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $found = & $vswhere -latest -prerelease -find $Pattern | Select-Object -First 1
        if ($found) { return $found }
    }
    $roots = @(
        "${env:ProgramFiles}\Microsoft Visual Studio",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio"
    ) | Where-Object { Test-Path $_ }
    # Newest edition first, so a side by side install picks the same one vswhere would,
    # and the processor specific copies are left out because they are not what it reports.
    $found = Get-ChildItem -Path $roots -Filter (Split-Path -Leaf $Pattern) -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.DirectoryName -notmatch 'amd64|arm64' } |
        Sort-Object FullName -Descending | Select-Object -First 1
    if (-not $found) { throw "$Name not found. Visual Studio is required." }
    return $found.FullName
}

$msbuild = Find-VsTool -Pattern '**\Bin\MSBuild.exe' -Name 'MSBuild.exe'

# The installation which owns the MSBuild being used. Derived from that path rather
# than looked up separately, so the tools checked here are certain to be the tools
# the build then runs with.
function Get-VsRoot {
    param([Parameter(Mandatory = $true)][string]$MsBuildPath)
    $dir = Split-Path -Parent $MsBuildPath
    while ($dir) {
        $parent = Split-Path -Parent $dir
        if ((Split-Path -Leaf $dir) -eq 'MSBuild') { return $parent }
        $dir = $parent
    }
    return $null
}

#------------------------------------------------------------------------------
# Steps.
#------------------------------------------------------------------------------

function Write-Step {
    param([string]$Text)
    Write-Host ""
    Write-Host "=== $Text" -ForegroundColor Cyan
}

# Build output only. The folders come from the solution rather than a list kept
# here, so a project added to the solution is cleaned without editing this file.
function Remove-BuildOutput {
    [CmdletBinding(SupportsShouldProcess = $true)]
    param()
    $paths = New-Object System.Collections.Generic.List[string]
    # Projects grouped into a solution folder are nested, so every Project node is
    # selected wherever it sits rather than only the ones at the top level.
    foreach ($project in ([xml](Get-Content -LiteralPath $solution -Raw)).SelectNodes("//Project")) {
        $dir = [System.IO.Path]::GetDirectoryName([System.IO.Path]::Combine($root, $project.Path))
        foreach ($name in "bin", "bin64", "obj") {
            $paths.Add([System.IO.Path]::Combine($dir, $name))
        }
    }
    foreach ($dir in ($config.Apps.FilesDir | Where-Object { $_ } | Select-Object -Unique)) {
        $paths.Add((Resolve-Path2 $dir))
    }
    foreach ($path in $paths) {
        if (-not (Test-Path -LiteralPath $path)) { continue }
        # Never step outside the repository, whatever the configuration says. The
        # separator is part of the test so a sibling folder whose name merely starts
        # with the repository name, such as "repo-backup" beside "repo", is outside.
        $inside = $root + [System.IO.Path]::DirectorySeparatorChar
        if (-not $path.StartsWith($inside, [StringComparison]::OrdinalIgnoreCase)) {
            Write-Host "Skipped, outside the repository: $path"
            continue
        }
        if (-not $PSCmdlet.ShouldProcess($path, "Remove build output")) { continue }
        Remove-Item -LiteralPath $path -Recurse -Force
        Write-Host "Removed: $path"
    }
}

# A C++ project states the build tools it compiles with. Building deletes the
# previous output before it fails, and the native output is not in source
# control, so missing tools have to be found before the clean, not after it.
function Assert-Toolsets {
    $vsRoot = Get-VsRoot $msbuild
    $installed = New-Object System.Collections.Generic.List[string]
    $vcDir = if ($vsRoot) { Join-Path $vsRoot "MSBuild\Microsoft\VC" } else { $null }
    if ($vcDir -and (Test-Path -LiteralPath $vcDir)) {
        foreach ($version in Get-ChildItem -LiteralPath $vcDir -Directory) {
            $platforms = Join-Path $version.FullName "Platforms"
            if (-not (Test-Path -LiteralPath $platforms)) { continue }
            foreach ($platform in Get-ChildItem -LiteralPath $platforms -Directory) {
                $toolsets = Join-Path $platform.FullName "PlatformToolsets"
                if (-not (Test-Path -LiteralPath $toolsets)) { continue }
                foreach ($toolset in Get-ChildItem -LiteralPath $toolsets -Directory) {
                    if (-not $installed.Contains($toolset.Name)) { $installed.Add($toolset.Name) }
                }
            }
        }
    }

    $missing = @{}
    foreach ($project in ([xml](Get-Content -LiteralPath $solution -Raw)).SelectNodes("//Project")) {
        $file = [System.IO.Path]::Combine($root, $project.Path)
        if (-not $file.EndsWith(".vcxproj", [StringComparison]::OrdinalIgnoreCase)) { continue }
        if (-not (Test-Path -LiteralPath $file)) { continue }
        foreach ($group in ([xml](Get-Content -LiteralPath $file -Raw)).Project.PropertyGroup) {
            # Only the groups belonging to the configuration being built.
            if ($group.Condition -notlike "*'$Configuration|*") { continue }
            $toolset = $group.PlatformToolset
            if (-not $toolset -or $installed.Contains($toolset)) { continue }
            if (-not $missing.ContainsKey($toolset)) {
                $missing[$toolset] = New-Object System.Collections.Generic.List[string]
            }
            $name = [System.IO.Path]::GetFileName($file)
            if (-not $missing[$toolset].Contains($name)) { $missing[$toolset].Add($name) }
        }
    }
    if ($missing.Count -eq 0) { return }

    $lines = foreach ($key in ($missing.Keys | Sort-Object)) { "  $key  -  $(($missing[$key]) -join ", ")" }
    throw ("These C++ build tools are needed by the $Configuration configuration and are not installed:`n" +
        ($lines -join "`n") +
        "`nInstalled: $(($installed | Sort-Object) -join ", ")" +
        "`nNothing has been removed. Install the missing tools, or run the release on a machine that has them.")
}

function Invoke-Build {
    param($Build, $Stage)
    $target = if ($Build.Project) { Resolve-Path2 $Build.Project } else { $solution }
    $name = [System.IO.Path]::GetFileName($target)
    Write-Host "Building $name $Configuration|$($Build.Platform)"
    $extra = @()
    if ($Stage.Properties) {
        foreach ($property in $Stage.Properties.PSObject.Properties) {
            $extra += "-p:$($property.Name)=$($property.Value)"
        }
    }
    & $msbuild $target -t:restore,build `
        -p:Configuration=$Configuration -p:Platform=$($Build.Platform) `
        @extra -v:minimal -nologo -m
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $name $Configuration|$($Build.Platform)" }
}

# "All" signs and then rebuilds the zip from scratch. A stage with no zip
# configured, such as the embedded libraries, only signs.
function Invoke-SignAndZip {
    param([string]$Stage)
    # A failure inside the sign script throws rather than setting an exit code,
    # and this script stops on errors, so the throw is what stops the release.
    $actions = if ($SkipSign) { "Copy", "Zip" } else { , "All" }
    foreach ($action in $actions) {
        & $signScript $action -Stage $Stage
    }
}

# The point of the release is the files it leaves behind, so name them and say
# whether each one is actually signed rather than assuming the step worked.
# A signed application carrying unsigned files inside it is the failure this pipeline
# exists to prevent, so the files are read back out of what was actually produced
# rather than trusting that each signing step ran.
function Get-EmbeddedFile {
    param([string]$Path)
    # Read straight from the compiled file. Loading it as an assembly instead would
    # fail on the 32 bit build, which a 64 bit process cannot load.
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $pe = [System.Reflection.PortableExecutable.PEReader]::new($stream)
        $reader = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
        $directory = $pe.PEHeaders.CorHeader.ResourcesDirectory
        $section = $pe.GetSectionData($directory.RelativeVirtualAddress)
        foreach ($handle in $reader.ManifestResources) {
            $resource = $reader.GetManifestResource($handle)
            $name = $reader.GetString($resource.Name)
            if ($name -notmatch "\.(dll|exe)$") { continue }
            $offset = [int]$resource.Offset
            $blob = $section.GetReader($offset, $section.Length - $offset)
            $length = $blob.ReadUInt32()
            [PSCustomObject]@{ Name = $name; Bytes = $blob.ReadBytes([int]$length) }
        }
    }
    finally { $stream.Dispose() }
}

function Test-EmbeddedSignatures {
    Write-Step "Check embedded signatures"
    $unsigned = New-Object System.Collections.Generic.List[string]
    $temp = Join-Path ([System.IO.Path]::GetTempPath()) ("x360ce-audit-" + [System.Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $temp | Out-Null
    try {
        foreach ($dir in ($config.Apps.FilesDir | Where-Object { $_ } | Select-Object -Unique)) {
            foreach ($file in Get-ChildItem -LiteralPath (Resolve-Path2 $dir) -Filter *.exe -File) {
                # The count is printed because a resource that stops being embedded
                # leaves no error behind, only a shorter list and a smaller file.
                $embeddedFiles = @(Get-EmbeddedFile $file.FullName)
                Write-Host ("  $dir/$($file.Name) carries $($embeddedFiles.Count) embedded file(s)")
                foreach ($embedded in $embeddedFiles) {
                    $path = Join-Path $temp ([System.Guid]::NewGuid().ToString("N") + [System.IO.Path]::GetExtension($embedded.Name))
                    [System.IO.File]::WriteAllBytes($path, $embedded.Bytes)
                    $status = (Get-AuthenticodeSignature -LiteralPath $path).Status
                    Write-Host ("    {0,-10}  {1}  in {2}/{3}" -f $status, $embedded.Name.Replace("x360ce.App.Resources.", ""), $dir, $file.Name)
                    if ($status -ne "Valid") { $unsigned.Add("$($embedded.Name) in $dir/$($file.Name)") }
                }
            }
        }
    }
    finally { Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue }

    if ($unsigned.Count -eq 0) {
        Write-Host "Every embedded file is signed."
        return
    }
    if ($SkipSign) {
        Write-Host "$($unsigned.Count) embedded file(s) are unsigned, which is expected with -SkipSign." -ForegroundColor Yellow
        return
    }
    throw ("These files are embedded into a signed application without a signature of their own:`n  " +
        ($unsigned -join "`n  "))
}

function Write-Result {
    Write-Step "Result"
    $missing = 0
    foreach ($dir in ($config.Apps.FilesDir | Where-Object { $_ } | Select-Object -Unique)) {
        $full = Resolve-Path2 $dir
        Write-Host ""
        Write-Host "  $dir"
        if (-not (Test-Path -LiteralPath $full)) {
            Write-Host "    nothing produced" -ForegroundColor Red
            $missing++
            continue
        }
        $files = @(Get-ChildItem -LiteralPath $full -File | Sort-Object Name)
        if ($files.Count -eq 0) {
            # A zip step that creates its folder and then produces nothing would
            # otherwise report a finished release holding no files.
            Write-Host "    nothing produced" -ForegroundColor Red
            $missing++
            continue
        }
        foreach ($file in $files) {
            $size = "{0,9:N0} KB" -f [math]::Ceiling($file.Length / 1KB)
            $state = ""
            # A cabinet carries a signature of its own, which is the reason it is
            # built, so the report says whether it actually got one.
            if ($file.Extension -in ".exe", ".dll", ".cab") {
                $state = (Get-AuthenticodeSignature -LiteralPath $file.FullName).Status
            }
            Write-Host "    $size  $($file.Name)  $state"
        }
    }
    if ($missing -gt 0) { throw "$missing output folder(s) are empty." }
}

# A release that a scanner objects to is worse than a release that is late, because
# the objection reaches users as a virus warning and the download stops. Defender
# ships a command line scanner, so the release checks its own output before it is
# published rather than finding out from a user report.
#
# The scanning itself belongs to App_4_DefenderScan.ps1, which also updates the
# definitions first: a scan is only as current as they are, and a detection can
# appear or disappear with them.
function Test-Detections {
    Write-Step "Scan"
    $scan = Join-Path $PSScriptRoot "App_4_DefenderScan.ps1"
    if (-not (Test-Path -LiteralPath $scan)) {
        Write-Host "  The scanning script is missing, so nothing was scanned." -ForegroundColor Yellow
        return
    }
    & $scan
}

#------------------------------------------------------------------------------
# Release.
#------------------------------------------------------------------------------

$started = Get-Date
Write-Host "Solution:      $solution"
Write-Host "Configuration: $Configuration"
Write-Host "Stages:        $(($config.Stages.Name) -join ', ')"
if ($SkipSign) { Write-Host "Signing:       skipped, the zips are not releasable" -ForegroundColor Yellow }

Write-Step "Check build tools"
Assert-Toolsets
Write-Host "All build tools required by the C++ projects are installed."

if ($NoClean) {
    Write-Step "Clean (skipped)"
}
else {
    Write-Step "Clean"
    Remove-BuildOutput
}

# Building and signing are not reversible in the way a file removal is, so a
# dry run stops here rather than reporting the clean and then doing the rest.
if ($WhatIfPreference) {
    Write-Host ""
    Write-Host "-WhatIf: stopping before the build."
    return
}

foreach ($stage in $config.Stages) {
    Write-Step "Build $($stage.Name)"
    foreach ($build in $stage.Builds) {
        Invoke-Build $build $stage
    }
    Write-Step "Sign and pack $($stage.Name)"
    Invoke-SignAndZip $stage.Name
}

Test-EmbeddedSignatures
Test-Detections
Write-Result
Write-Host ""
Write-Host "Done in $([int]((Get-Date) - $started).TotalMinutes) min $((((Get-Date) - $started).Seconds)) sec."
