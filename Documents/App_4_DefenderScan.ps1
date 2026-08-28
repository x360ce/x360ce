<#
.SYNOPSIS
    Updates Defender's definitions and scans the release with them.
.DESCRIPTION
    The cheapest gate there is. Defender is what most people who download this run,
    so what it says about a release is worth knowing before the release is
    published rather than from a user's report afterwards.

    Definitions are updated first because a scan is only as current as they are. A
    detection can appear when new definitions land, and one seen yesterday can be
    gone today: this release was flagged as Trojan:Script/Wacatac.H!ml at 1.457.375
    and clean at 1.457.382 a few hours later.

    A detection here is usually a false positive. A controller emulator presents a
    virtual device and reads real ones, which is what input-stealing software does,
    so behavioural and machine-learning families score it highly. Treat it as a
    reason to report the file, which App_3_ReportFalsePositive.ps1 does, rather than
    as evidence the build is unsafe.

    Nothing is ever removed or quarantined. The scan runs with remediation off, so
    a detection cannot delete the release that has just been built.
.PARAMETER Path
    Files or folders to scan. Without it the release output folders named in
    App_1_Sign_and_Zip.json are scanned.
.PARAMETER SkipUpdate
    Scans with the definitions already installed. Use it offline, or when you have
    just updated and want the same definitions twice.
.PARAMETER Quiet
    Says nothing unless something is flagged or could not be scanned, for a caller
    that has its own report to write.
.PARAMETER PassThru
    Writes one object per file so a caller can act on the result. Left out, the
    verdicts are printed and nothing is written to the pipeline, because a page of
    objects is not what a person running this wants to read.
.EXAMPLE
    PS> .\App_4_DefenderScan.ps1
    Updates definitions, scans everything the release publishes, and lists the
    verdict for each file.
.EXAMPLE
    PS> .\App_4_DefenderScan.ps1 -Path Files.v4\x360ce.zip -SkipUpdate
    Scans one file with the definitions already installed.
.OUTPUTS
    With -PassThru, one object per file scanned, with Name, Path, Clean, Threats and
    ExitCode, so a caller can act on the result rather than read the text.
.NOTES
    Exits non-zero when anything is flagged, so it can gate a release step.

    The scanner does not resolve a relative path against the current folder. It
    answers 0x80508023 instead, which reads like a scanning failure and means it
    could not find the file, so every path given to it here is a full one.
#>
[CmdletBinding()]
param(
    [string[]]$Path,
    [switch]$SkipUpdate,
    [switch]$Quiet,
    [switch]$PassThru
)

$ErrorActionPreference = "Stop"

#------------------------------------------------------------------------------
# The scanner.
#------------------------------------------------------------------------------

# Defender ships in two places: a stable path, and a versioned platform folder
# which is where updates actually live. The newest platform folder wins.
function Get-Scanner {
    $platform = Get-ChildItem "$env:ProgramData\Microsoft\Windows Defender\Platform" -Directory -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending | Select-Object -First 1
    if ($platform) {
        $inPlatform = Join-Path $platform.FullName "MpCmdRun.exe"
        if (Test-Path -LiteralPath $inPlatform) { return $inPlatform }
    }
    $inProgramFiles = Join-Path $env:ProgramFiles "Windows Defender\MpCmdRun.exe"
    if (Test-Path -LiteralPath $inProgramFiles) { return $inProgramFiles }
    return $null
}

function Get-DefinitionVersion {
    try { return (Get-MpComputerStatus).AntivirusSignatureVersion }
    catch { return "" }
}

#------------------------------------------------------------------------------
# What to scan.
#------------------------------------------------------------------------------

function Resolve-Path2 {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, $Path))
}

# The release output folders, which are what a user downloads. Read from the file
# the sign step is configured by, so there is no second list to keep in step.
function Get-DefaultTarget {
    $configPath = Join-Path $PSScriptRoot "App_1_Sign_and_Zip.json"
    if (-not (Test-Path -LiteralPath $configPath)) { return @() }
    $json = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
    $apps = @(if ($json.PSObject.Properties.Name -contains "Apps") { $json.Apps } else { $json })
    return @($apps.FilesDir | Where-Object { $_ } | Select-Object -Unique | ForEach-Object { Resolve-Path2 $_ })
}

# Every file behind whatever was asked for, whether that was a file or a folder.
function Get-FileToScan {
    param([string[]]$Targets)
    $scannable = @(".exe", ".dll", ".zip", ".cab", ".msi", ".sys")
    $found = New-Object System.Collections.Generic.List[object]
    $seen = @{}
    foreach ($target in $Targets) {
        $full = if ([System.IO.Path]::IsPathRooted($target)) { $target } else { Resolve-Path2 $target }
        if (-not (Test-Path -LiteralPath $full)) {
            Write-Host "  Not there: $full" -ForegroundColor Yellow
            continue
        }
        $items = if (Test-Path -LiteralPath $full -PathType Container) {
            Get-ChildItem -LiteralPath $full -File
        }
        else {
            Get-Item -LiteralPath $full
        }
        foreach ($item in $items) {
            if ($item.Extension -notin $scannable) { continue }
            if ($seen.ContainsKey($item.FullName)) { continue }
            $seen[$item.FullName] = $true
            $found.Add($item)
        }
    }
    return $found
}

#------------------------------------------------------------------------------
# Scan.
#------------------------------------------------------------------------------

# One file, one answer. The exit code alone cannot be trusted: 2 means both "found
# something" and "could not scan", and the two need different responses, so what
# the scanner printed decides which it was.
function Invoke-Scan {
    param([string]$Scanner, $Item)
    $output = & $Scanner -Scan -ScanType 3 -File $Item.FullName -DisableRemediation 2>&1 | Out-String
    $code = $LASTEXITCODE
    $threats = @($output -split "`r?`n" |
        Where-Object { $_ -match "^Threat\s+:" } |
        ForEach-Object { ($_ -split ":", 2)[1].Trim() })
    $finished = $output -match "Scan finished"
    return [PSCustomObject]@{
        Name     = $Item.Name
        Path     = $Item.FullName
        Clean    = ($code -eq 0)
        Scanned  = ($code -eq 0 -or $finished)
        Threats  = $threats
        ExitCode = $code
        Output   = $output.Trim()
    }
}

#------------------------------------------------------------------------------
# Run.
#------------------------------------------------------------------------------

$scanner = Get-Scanner
if (-not $scanner) {
    Write-Host "Defender is not installed on this machine, so nothing was scanned." -ForegroundColor Yellow
    exit 1
}

if (-not $SkipUpdate) {
    $before = Get-DefinitionVersion
    if (-not $Quiet) { Write-Host "Updating definitions..." }
    & $scanner -SignatureUpdate | Out-Null
    $after = Get-DefinitionVersion
    if (-not $Quiet) {
        if ($after -and $after -ne $before) {
            Write-Host "  Definitions: $before -> $after"
        }
        else {
            Write-Host "  Definitions: $after, already current"
        }
    }
}
elseif (-not $Quiet) {
    Write-Host "  Definitions: $(Get-DefinitionVersion), not updated"
}

$targets = if ($Path) { $Path } else { Get-DefaultTarget }
$files = @(Get-FileToScan $targets)
if ($files.Count -eq 0) {
    Write-Host "Nothing to scan. Build the release first, or name a file with -Path." -ForegroundColor Yellow
    exit 1
}

if (-not $Quiet) { Write-Host "Scanning $($files.Count) file(s)." }
$results = New-Object System.Collections.Generic.List[object]
foreach ($item in $files) {
    $result = Invoke-Scan -Scanner $scanner -Item $item
    $results.Add($result)
    if ($result.Clean) {
        if (-not $Quiet) { Write-Host ("  {0,-10}  {1}" -f "clean", $result.Name) }
        continue
    }
    if (-not $result.Scanned) {
        Write-Host ("  {0,-10}  {1}  (exit {2})" -f "NOT SCANNED", $result.Name, $result.ExitCode) -ForegroundColor Yellow
        $result.Output -split "`r?`n" | ForEach-Object { Write-Host "              $_" }
        continue
    }
    Write-Host ("  {0,-10}  {1}  {2}" -f "FLAGGED", $result.Name, ($result.Threats -join ", ")) -ForegroundColor Red
}

$flagged = @($results | Where-Object { -not $_.Clean -and $_.Scanned })
$unscanned = @($results | Where-Object { -not $_.Scanned })

if (-not $Quiet) { Write-Host "" }
if ($flagged.Count -gt 0) {
    Write-Host "$($flagged.Count) file(s) flagged. On this software that is usually a false" -ForegroundColor Yellow
    Write-Host "positive: check the same file on VirusTotal with App_2_VirusTotal.ps1, and if" -ForegroundColor Yellow
    Write-Host "other engines disagree, report it with App_3_ReportFalsePositive.ps1." -ForegroundColor Yellow
}
elseif ($unscanned.Count -eq 0 -and -not $Quiet) {
    Write-Host "Defender finds nothing in this release." -ForegroundColor Green
}

# Only when asked for. A person gets the lines above; a script gets the objects.
if ($PassThru) { $results }

if ($flagged.Count -gt 0 -or $unscanned.Count -gt 0) { exit 1 }
