<#
.SYNOPSIS
    Checks every file the release ships against VirusTotal before it is published.
.DESCRIPTION
    The last gate before deployment. A controller emulator loads itself into games,
    which is behaviour several engines score on heuristics, so a release can pick up
    a detection without anything being wrong with it. Publishing one of those costs
    reputation that takes a long time to earn back, and the damage is done before
    anyone notices. This reports what VirusTotal currently says about each file so
    that decision is made before the upload rather than after it.

    The files come from App_1_Sign_and_Zip.json, so the list is the same one the
    release signs and there is no second list to keep in step. Everything the sign
    step targets is checked, along with the executables and archives in the output
    folders. Files which are neither executable nor an archive are skipped.

    Each file is looked up by its SHA-256 first. VirusTotal keeps results by hash,
    so a file it has seen answers immediately and costs one request. A file it has
    never seen has to be uploaded and analysed, which is slow, so uploading is
    off unless it is asked for.

    A detection is not automatically a failure. The baseline file records which
    engines are already known to flag a given file, so a rerun passes while those
    engines say the same thing, and fails when an engine which was not on the list
    starts flagging or when the number of detections grows. That is the difference
    between "the usual two heuristics" and "something changed".
.PARAMETER ApiKey
    VirusTotal API key. Defaults to the VIRUSTOTAL_API_KEY environment variable.
    The key is never written to the output.
.PARAMETER Upload
    Uploads files VirusTotal has not seen and waits for the result. Off by default
    because an upload takes minutes, while a lookup takes a second.
.PARAMETER RequestsPerMinute
    Rate limit to stay under. The free public key allows 4 requests a minute; a
    paid key allows far more. Raise it only as far as your key permits.
.PARAMETER WaitMinutes
    How long to wait for an uploaded file to finish being analysed.
.PARAMETER ListOnly
    Lists what would be checked, with each file's SHA-256, and contacts nobody.
    The hash is what VirusTotal indexes by, so it can also be pasted into the
    website to look a single file up by hand.
.PARAMETER UpdateBaseline
    Records what the engines currently say as the accepted result. Use it after
    looking at a new detection and deciding it is a false positive.
.EXAMPLE
    PS> .\App_2_VirusTotal.ps1
    Looks up every release file by hash and reports anything unexpected.
.EXAMPLE
    PS> .\App_2_VirusTotal.ps1 -Upload
    Also uploads files VirusTotal has never seen. Slow, needed the first time a
    freshly built file is checked.
.EXAMPLE
    PS> .\App_2_VirusTotal.ps1 -UpdateBaseline
    Accepts the current detections, after you have looked at them.
.NOTES
    Exits non-zero when a file is unknown to VirusTotal or carries a detection
    which is not in the baseline, so it can gate a deployment step.

    Rebuilding changes every hash, so the baseline records engine names rather
    than hashes. A new build with the same known detections passes; a new engine,
    or more detections than were accepted, does not.
#>
[CmdletBinding()]
param(
    [string]$ApiKey = $env:VIRUSTOTAL_API_KEY,
    [switch]$Upload,
    [int]$RequestsPerMinute = 4,
    [int]$WaitMinutes = 10,
    [switch]$ListOnly,
    [switch]$UpdateBaseline
)

$ErrorActionPreference = "Stop"

#------------------------------------------------------------------------------
# Configuration.
#------------------------------------------------------------------------------

if (-not $ListOnly -and [string]::IsNullOrWhiteSpace($ApiKey)) {
    throw "No API key. Set VIRUSTOTAL_API_KEY, or pass -ApiKey."
}

$configPath = Join-Path $PSScriptRoot "App_1_Sign_and_Zip.json"
if (-not (Test-Path -LiteralPath $configPath)) { throw "Configuration not found: $configPath" }
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$apps = @(if ($config.PSObject.Properties.Name -contains "Apps") { $config.Apps } else { $config })

$baselinePath = [System.IO.Path]::ChangeExtension($PSCommandPath, ".baseline.json")

# Only files which can carry code or hide it. An ini or a log costs a request and
# tells nobody anything.
$scanned = @(".exe", ".dll", ".zip", ".msi", ".sys", ".cab")

$base = "https://www.virustotal.com/api/v3"
$headers = @{ "x-apikey" = $ApiKey }

function Resolve-Path2 {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, $Path))
}

#------------------------------------------------------------------------------
# Files.
#------------------------------------------------------------------------------

# The signed files plus whatever the output folders hold, which is what a user
# actually downloads.
function Get-ReleaseFile {
    $seen = @{}
    $result = New-Object System.Collections.Generic.List[object]
    function Add-Path {
        param([string]$Relative)
        $full = Resolve-Path2 $Relative
        if ($seen.ContainsKey($full)) { return }
        $seen[$full] = $true
        $result.Add([PSCustomObject]@{ Name = $Relative; Path = $full })
    }
    foreach ($app in $apps) {
        $dir = if ($app.FilesDir) { $app.FilesDir } else { "Files" }
        foreach ($file in $app.Files) {
            $target = if ($file.Target) { $file.Target } else { "$dir/" + [System.IO.Path]::GetFileName($file.Source) }
            Add-Path $target
        }
    }
    foreach ($dir in ($apps.FilesDir | Where-Object { $_ } | Select-Object -Unique)) {
        $full = Resolve-Path2 $dir
        if (-not (Test-Path -LiteralPath $full)) { continue }
        foreach ($file in Get-ChildItem -LiteralPath $full -File) {
            Add-Path "$dir/$($file.Name)"
        }
    }
    return $result
}

#------------------------------------------------------------------------------
# VirusTotal.
#------------------------------------------------------------------------------

$script:LastRequest = [DateTime]::MinValue

# Requests are spaced out rather than counted, which keeps a free key inside its
# allowance without having to model the window.
function Wait-Turn {
    if ($RequestsPerMinute -le 0) { return }
    $gap = [TimeSpan]::FromSeconds(60.0 / $RequestsPerMinute)
    $due = $script:LastRequest.Add($gap)
    $wait = $due - [DateTime]::UtcNow
    if ($wait.TotalMilliseconds -gt 0) {
        Start-Sleep -Milliseconds ([int]$wait.TotalMilliseconds)
    }
    $script:LastRequest = [DateTime]::UtcNow
}

function Invoke-VirusTotal {
    param([string]$Path, [string]$Method = "Get", $Body, $Form)
    Wait-Turn
    # An upload address for a large file points at a different host, so an absolute
    # address is used as it is rather than being appended to the API root.
    $uri = if ($Path -like "http*") { $Path } else { "$base/$Path" }
    $arguments = @{
        Uri                = $uri
        Method             = $Method
        Headers            = $headers
        SkipHttpErrorCheck = $true
        StatusCodeVariable = "status"
        TimeoutSec         = 300
    }
    if ($Body) { $arguments.Body = $Body }
    if ($Form) { $arguments.Form = $Form }
    $response = Invoke-RestMethod @arguments
    return [PSCustomObject]@{ Status = $status; Body = $response }
}

function Get-Report {
    param([string]$Sha256, [string]$Name)
    $response = Invoke-VirusTotal "files/$Sha256"
    if ($response.Status -eq 200) { return $response.Body.data }
    # Not seen before, which is normal for a file which has just been built.
    if ($response.Status -eq 404) { return $null }
    # The two answers worth naming, because the fix for each is a different one.
    if ($response.Status -eq 401) {
        throw "VirusTotal rejected the API key. Check VIRUSTOTAL_API_KEY."
    }
    if ($response.Status -eq 429) {
        throw ("VirusTotal refused the request for being too frequent. The free key allows " +
            "4 a minute and 500 a day; lower -RequestsPerMinute, or wait for the daily allowance.")
    }
    throw "VirusTotal returned $($response.Status) for $Name. $($response.Body.error.message)"
}

function Send-File {
    param([string]$Path)
    $length = (Get-Item -LiteralPath $Path).Length
    $endpoint = "files"
    # The plain endpoint takes files up to 32 MB; anything larger is given its own
    # single use address.
    if ($length -gt 32MB) {
        $upload = Invoke-VirusTotal "files/upload_url"
        if ($upload.Status -ne 200) { throw "Could not get an upload address: $($upload.Status)" }
        $endpoint = $upload.Body.data
    }
    $response = Invoke-VirusTotal $endpoint "Post" -Form @{ file = Get-Item -LiteralPath $Path }
    if ($response.Status -ne 200) {
        throw "Upload of $Path failed with $($response.Status). $($response.Body.error.message)"
    }
    return $response.Body.data.id
}

function Wait-Analysis {
    param([string]$Id)
    $until = [DateTime]::UtcNow.AddMinutes($WaitMinutes)
    while ([DateTime]::UtcNow -lt $until) {
        $response = Invoke-VirusTotal "analyses/$Id"
        if ($response.Status -eq 200 -and $response.Body.data.attributes.status -eq "completed") {
            return $true
        }
        Write-Host "    still being analysed..."
    }
    return $false
}

# The engines which flagged the file, which is the part worth reading. The counts
# on their own do not say whether anything changed.
function Get-Detection {
    param($Report)
    $results = $Report.attributes.last_analysis_results
    if (-not $results) { return @() }
    return @($results.PSObject.Properties |
        Where-Object { $_.Value.category -in @("malicious", "suspicious") } |
        ForEach-Object { $_.Name } | Sort-Object)
}

#------------------------------------------------------------------------------
# Baseline.
#------------------------------------------------------------------------------

function Read-Baseline {
    if (-not (Test-Path -LiteralPath $baselinePath)) { return @{} }
    $loaded = Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json
    $map = @{}
    foreach ($property in $loaded.Files.PSObject.Properties) {
        $map[$property.Name] = @($property.Value)
    }
    return $map
}

function Write-Baseline {
    param($Accepted)
    $files = [ordered]@{}
    foreach ($key in ($Accepted.Keys | Sort-Object)) {
        $files[$key] = @($Accepted[$key])
    }
    $document = [ordered]@{
        Comment = "Engines already known to flag each file. A rerun passes while they say the same thing, and fails when an engine which is not listed starts flagging or the number of detections grows. Rebuilding changes every hash, so this records names rather than hashes. Only add an entry after looking at the detection and deciding it is a false positive."
        Files   = $files
    }
    $document | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $baselinePath -Encoding utf8
    Write-Host "Baseline written: $baselinePath"
}

#------------------------------------------------------------------------------
# Check.
#------------------------------------------------------------------------------

$files = Get-ReleaseFile
$baseline = Read-Baseline
$accepted = @{}
$problems = New-Object System.Collections.Generic.List[string]
# VirusTotal indexes by hash, so two files with the same content share one answer.
# The release ships several: a zip built twice under different names, and a native
# library which also sits beside the application under its runtime name.
$reports = @{}
$skipped = New-Object System.Collections.Generic.List[string]
$checked = 0

Write-Host "Files listed:  $($files.Count)"
Write-Host "Baseline:      $(if (Test-Path -LiteralPath $baselinePath) { $baselinePath } else { 'none yet' })"
Write-Host "Rate limit:    $RequestsPerMinute request(s) per minute"
Write-Host ""

foreach ($file in $files) {
    if (-not (Test-Path -LiteralPath $file.Path)) {
        $problems.Add("$($file.Name) is missing. Build the release first.")
        continue
    }
    if ([System.IO.Path]::GetExtension($file.Path) -notin $scanned) {
        $skipped.Add($file.Name)
        continue
    }

    $checked++
    $hash = (Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($ListOnly) {
        Write-Host ("  {0}  {1}" -f $hash, $file.Name)
        continue
    }
    if ($reports.ContainsKey($hash)) {
        # Including a file VirusTotal has never seen: asking twice costs a second
        # request and gets the same answer.
        $report = $reports[$hash]
    }
    else {
        $report = Get-Report $hash $file.Name
        $reports[$hash] = $report
    }

    if ($null -eq $report) {
        if (-not $Upload) {
            Write-Host ("  {0,-10}  {1}" -f "unknown", $file.Name)
            $problems.Add("$($file.Name) has never been seen by VirusTotal. Run with -Upload to submit it.")
            continue
        }
        Write-Host ("  {0,-10}  {1} (uploading)" -f "new", $file.Name)
        $id = Send-File $file.Path
        if (-not (Wait-Analysis $id)) {
            $problems.Add("$($file.Name) was uploaded but the analysis did not finish within $WaitMinutes minute(s).")
            continue
        }
        $report = Get-Report $hash $file.Name
        if ($null -eq $report) {
            $problems.Add("$($file.Name) was uploaded but no report came back.")
            continue
        }
        $reports[$hash] = $report
    }

    $detections = Get-Detection $report
    $total = @($report.attributes.last_analysis_results.PSObject.Properties).Count
    $known = @(if ($baseline.ContainsKey($file.Name)) { $baseline[$file.Name] } else { @() })
    $accepted[$file.Name] = $detections

    if ($detections.Count -eq 0) {
        Write-Host ("  {0,-10}  {1}  (0 of {2})" -f "clean", $file.Name, $total)
        continue
    }

    $unexpected = @($detections | Where-Object { $_ -notin $known })
    $state = if ($unexpected.Count -gt 0) { "FLAGGED" } else { "known" }
    Write-Host ("  {0,-10}  {1}  ({2} of {3}: {4})" -f $state, $file.Name, $detections.Count, $total, ($detections -join ", "))
    if ($unexpected.Count -gt 0) {
        $problems.Add("$($file.Name) is flagged by $($unexpected -join ', '), which the baseline does not accept.")
    }
    elseif ($detections.Count -gt $known.Count) {
        $problems.Add("$($file.Name) has $($detections.Count) detections but the baseline accepts $($known.Count).")
    }
}

#------------------------------------------------------------------------------
# Result.
#------------------------------------------------------------------------------

Write-Host ""
if ($skipped.Count -gt 0) {
    Write-Host "Not checked, nothing executable in them: $($skipped -join ', ')"
}
Write-Host "Checked $checked file(s)."

if ($ListOnly) {
    return
}

if ($UpdateBaseline) {
    Write-Baseline $accepted
    Write-Host "Detections above are now the accepted result. Rerun without -UpdateBaseline to gate on them."
    return
}

if ($problems.Count -eq 0) {
    Write-Host "Nothing unexpected. Safe to publish." -ForegroundColor Green
    return
}

Write-Host ""
Write-Host "$($problems.Count) thing(s) to look at before publishing:" -ForegroundColor Yellow
foreach ($problem in $problems) {
    Write-Host "  $problem"
}
Write-Host ""
Write-Host "A detection you have looked at and decided is a false positive is recorded with -UpdateBaseline."
exit 1
