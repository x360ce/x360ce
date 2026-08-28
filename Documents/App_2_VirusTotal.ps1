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
    never seen has to be submitted and analysed, which is slow, so nothing is sent
    unless it is asked for.

    You are asked twice, over the same list. First, what to check: the whole
    release is more than twenty files, and at four requests a minute the free key
    spends about six of them looking up libraries nobody downloads on their own.
    Then, what to submit, from whatever came back unknown.

    Both offer the same three shapes: one file by number, every program and zip in
    one output folder by letter, or nothing. Checking also offers everything;
    submitting does not, because a submitted file is stored by VirusTotal and can
    be downloaded by anyone paying for its service, so it is a decision per file
    rather than a keypress. Rebuilding changes every hash, so a freshly built
    release is unknown every time.

    A detection is not automatically a failure. The baseline file records which
    engines are already known to flag a given file, so a rerun passes while those
    engines say the same thing, and fails when an engine which was not on the list
    starts flagging or when the number of detections grows. That is the difference
    between "the usual two heuristics" and "something changed".
.PARAMETER ApiKey
    VirusTotal API key. Defaults to the VIRUSTOTAL_API_KEY environment variable.
    The key is never written to the output.
.PARAMETER Upload
    Submits every file VirusTotal has not seen, without asking, and waits for each
    result. Off by default because a submission takes minutes while a lookup takes
    a second, and because sending a file is a decision worth making one at a time.
    Use it when the run is driven by another script and nobody is there to answer
    the menu.
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
    Asks what to check, looks those up by hash, reports anything unexpected, then
    offers to submit whatever VirusTotal has never seen.
.EXAMPLE
    PS> .\App_2_VirusTotal.ps1 -Upload
    Submits everything VirusTotal has never seen without asking. Slow, and it sends
    every unknown file rather than the ones you pick.
.EXAMPLE
    PS> .\App_2_VirusTotal.ps1 -UpdateBaseline
    Accepts the current detections, after you have looked at them.
.NOTES
    Exits non-zero when a file is still unknown to VirusTotal or carries a detection
    which is not in the baseline, so it can gate a deployment step. A run with no
    console to ask, such as one driven by another script, skips the menu and reports
    the unknown files instead.

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

# What a person downloads and runs. The menu offers these as a group per output
# folder, because they are what a release actually publishes; everything else is
# submitted one at a time or not at all.
$downloadTypes = @(".exe", ".zip")

# The folders a release is published from, which is what the menu groups by.
$outputFolders = @($apps.FilesDir | Where-Object { $_ } | Select-Object -Unique)

# The menu key of each output folder, fixed for the whole run. Numbering the keys
# afresh each time a menu is drawn would move a key onto a different folder as
# soon as one is finished with, so the key somebody just read would act on files
# they did not choose.
$letterFor = @{}
for ($i = 0; $i -lt $outputFolders.Count; $i++) {
    $letterFor[$outputFolders[$i]] = [string][char]([byte][char]"A" + $i)
}

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
        TimeoutSec         = 300
    }
    if ($Body) { $arguments.Body = $Body }
    if ($Form) { $arguments.Form = $Form }
    $response = Invoke-WebRequest @arguments
    # Read as a hash table rather than letting the answer be turned into an object.
    #
    # A VirusTotal report can carry a property whose name is an empty string, and
    # ConvertFrom-Json refuses one of those unless it is building a hash table.
    # Invoke-RestMethod, which converts for you, does not report that: it hands back
    # the raw text instead, every property reads as empty, and a file VirusTotal
    # knows all about is reported as never seen. A wrong answer nobody can see is
    # worse than an error, and it invites an upload the file did not need.
    $body = $null
    if ($response.Content) {
        $body = $response.Content | ConvertFrom-Json -AsHashtable
    }
    return [PSCustomObject]@{ Status = [int]$response.StatusCode; Body = $body }
}

function Get-Report {
    param([string]$Sha256, [string]$Name)
    $response = Invoke-VirusTotal "files/$Sha256"
    if ($response.Status -eq 200) {
        # An answer that arrived but cannot be read is not the same as a file
        # VirusTotal has never seen, and must never be reported as one. Saying so
        # here is what turns the next parsing surprise into a message instead of a
        # file wrongly called unknown and needlessly offered for submission.
        if ($null -eq $response.Body -or $null -eq $response.Body.data) {
            throw ("VirusTotal answered 200 for $Name but the report could not be read. " +
                "Treating it as unseen would be wrong, so the run stops here.")
        }
        return $response.Body.data
    }
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

# Where a person reads the same answer for themselves. VirusTotal indexes by hash,
# so this opens the file's page without the file ever being sent: looking a hash up
# tells VirusTotal only that somebody asked about it, while submitting hands over
# the file itself. It is also the way to check a file this script left unknown.
function Get-VirusTotalLink {
    param([string]$Hash)
    return "https://www.virustotal.com/gui/file/$Hash"
}

# The engines which flagged the file, which is the part worth reading. The counts
# on their own do not say whether anything changed.
function Get-Detection {
    param($Report)
    $results = $Report.attributes.last_analysis_results
    if (-not $results) { return @() }
    return @($results.Keys |
        Where-Object { $results[$_].category -in @("malicious", "suspicious") } |
        Sort-Object)
}

# How many engines answered at all, which is the number a count of detections is
# out of. Zero when the file has been seen but not yet analysed.
function Get-EngineCount {
    param($Report)
    $results = $Report.attributes.last_analysis_results
    if (-not $results) { return 0 }
    return $results.Count
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

# Send a file for analysis and wait for the answer. Null when it did not arrive.
function Send-ForAnalysis {
    param($File, [string]$Hash)
    Write-Host ("  {0,-10}  {1} (submitting)" -f "new", $File.Name)
    $id = Send-File $File.Path
    if (-not (Wait-Analysis $id)) {
        $problems.Add("$($File.Name) was submitted but the analysis did not finish within $WaitMinutes minute(s).")
        return $null
    }
    $report = Get-Report $Hash $File.Name
    if ($null -eq $report) {
        $problems.Add("$($File.Name) was submitted but no report came back.")
        return $null
    }
    $reports[$Hash] = $report
    return $report
}

# Say what the engines found, and whether it is what the baseline already accepts.
function Add-Result {
    param($File, $Report)
    $detections = Get-Detection $Report
    $total = Get-EngineCount $Report
    $known = @(if ($baseline.ContainsKey($File.Name)) { $baseline[$File.Name] } else { @() })
    $accepted[$File.Name] = $detections
    if ($detections.Count -eq 0) {
        Write-Host ("  {0,-10}  {1}  (0 of {2})" -f "clean", $File.Name, $total)
        # Under every file, whatever the verdict. A clean answer is the one most
        # worth being able to show somebody else, and what each engine actually
        # said is on that page while the count here is not.
        Write-Host ("              {0}" -f (Get-VirusTotalLink $File.Hash))
        return
    }
    $unexpected = @($detections | Where-Object { $_ -notin $known })
    $state = if ($unexpected.Count -gt 0) { "FLAGGED" } else { "known" }
    Write-Host ("  {0,-10}  {1}  ({2} of {3}: {4})" -f $state, $File.Name, $detections.Count, $total, ($detections -join ", "))
    Write-Host ("              {0}" -f (Get-VirusTotalLink $File.Hash))
    if ($unexpected.Count -gt 0) {
        $problems.Add("$($File.Name) is flagged by $($unexpected -join ', '), which the baseline does not accept." +
            [Environment]::NewLine + "    " + (Get-VirusTotalLink $File.Hash))
    }
    elseif ($detections.Count -gt $known.Count) {
        $problems.Add("$($File.Name) has $($detections.Count) detections but the baseline accepts $($known.Count).")
    }
}

# The output folder a listed file sits in, which is one per version of the program.
# Empty for anything else: the release also signs libraries where they were built,
# and those are not a download anybody groups together.
function Get-OutputFolder {
    param([string]$Name)
    $folder = (($Name -replace "\\", "/") -split "/")[0]
    if ($folder -in $outputFolders) {
        return $folder
    }
    return ""
}

# One menu, asked twice: once to choose what to check, once to choose what to
# submit. Both are the same question over the same list, so they are the same
# menu rather than two that drift apart.
#
# Choosing matters because neither is free. A lookup costs a request and the free
# key allows four a minute, so checking everything takes minutes of waiting on a
# release where only a handful of files are actually published. A submission is
# worse than slow: the file is stored by VirusTotal and can be downloaded by
# anyone paying for its service, which is why submitting everything is offered
# only through -Upload and never as a keypress.
function Select-Files {
    param($Files, [string]$Verb, [switch]$AllowAll)
    if ($Files.Count -eq 0) {
        return @()
    }
    # Cast rather than @(), which throws on a generic list in PowerShell 7 and
    # hands back nothing. Both a list and a plain array arrive here.
    $list = [object[]]$Files
    Write-Host ""
    Write-Host "  What should be $($Verb)ed?"
    Write-Host ""
    for ($i = 0; $i -lt $list.Count; $i++) {
        Write-Host ("  {0,2} - {1}" -f ($i + 1), $list[$i].Name)
    }
    Write-Host ""
    # The programs and zips of one output folder: what a release publishes.
    $groups = @($list |
        Where-Object {
            [System.IO.Path]::GetExtension($_.Name) -in $downloadTypes -and (Get-OutputFolder $_.Name)
        } |
        Group-Object { Get-OutputFolder $_.Name })
    foreach ($group in $groups | Sort-Object Name) {
        Write-Host ("   {0} - {1}: every program and zip in it ({2} file(s))" -f
            $letterFor[$group.Name], $group.Name, $group.Count)
    }
    if ($AllowAll) {
        Write-Host ("   * - Everything listed ({0} file(s))" -f $list.Count)
    }
    Write-Host "   Q - Nothing"
    Write-Host ""
    $choice = (Read-Host "  Choice").Trim()
    if ($choice -eq "" -or $choice -eq "Q" -or $choice -eq "q") {
        return @()
    }
    if ($AllowAll -and $choice -eq "*") {
        return $list
    }
    $index = 0
    if ([int]::TryParse($choice, [ref]$index) -and $index -ge 1 -and $index -le $list.Count) {
        return @($list[$index - 1])
    }
    $group = $groups | Where-Object { $letterFor[$_.Name] -eq $choice.ToUpperInvariant() }
    if ($group) {
        return @($group.Group)
    }
    Write-Host "  Unknown choice."
    return Select-Files $Files $Verb -AllowAll:$AllowAll
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
# Files VirusTotal has never seen. Nothing is known about a file until somebody
# submits it, and rebuilding changes every hash, so a fresh release is unknown
# every time. These are offered for submission once the check has finished.
$unknown = New-Object System.Collections.Generic.List[object]
$checked = 0

# Asked before the first lookup rather than after the last one. Checking every
# file of a release costs minutes at four requests a minute, and most of them are
# libraries nobody downloads on their own.
if (-not $ListOnly -and -not $Upload -and [Environment]::UserInteractive) {
    $files = @(Select-Files $files "check" -AllowAll)
    if ($files.Count -eq 0) {
        Write-Host ""
        Write-Host "Nothing checked."
        return
    }
}

Write-Host ""
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
    # Kept on the record so anything reported later can name the hash without
    # reading the file a second time.
    $file | Add-Member -NotePropertyName Hash -NotePropertyValue $hash -Force
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
            Write-Host ("              {0}" -f (Get-VirusTotalLink $hash))
            $unknown.Add($file)
            continue
        }
        $report = Send-ForAnalysis $file $hash
        if ($null -eq $report) {
            continue
        }
    }

    Add-Result $file $report
}

#------------------------------------------------------------------------------
# Submit.
#------------------------------------------------------------------------------

# Asked rather than assumed. Submitting is slow, it costs requests, and it puts
# the file in somebody else's hands, so it happens only when a person says so.
# A run driving another script answers no by never being asked.
if ($unknown.Count -gt 0 -and -not $Upload -and -not $ListOnly -and -not $UpdateBaseline -and
    [Environment]::UserInteractive) {
    Write-Host ""
    Write-Host "VirusTotal has never seen $($unknown.Count) of these files."
    Write-Host "A file you submit is stored by VirusTotal, where its customers can download it."
    while ($unknown.Count -gt 0) {
        $chosen = @(Select-Files $unknown "submit")
        if ($chosen.Count -eq 0) {
            break
        }
        foreach ($file in $chosen) {
            $hash = (Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash.ToLowerInvariant()
            $report = Send-ForAnalysis $file $hash
            if ($null -ne $report) {
                Add-Result $file $report
            }
            $unknown.Remove($file) | Out-Null
        }
    }
}
foreach ($file in $unknown) {
    $problems.Add("$($file.Name) has never been seen by VirusTotal. Submit it from the menu, run with" +
        " -Upload, or check the page yourself once somebody else has." +
        [Environment]::NewLine + "    " + (Get-VirusTotalLink $file.Hash))
}

#------------------------------------------------------------------------------
# Result.
#------------------------------------------------------------------------------

Write-Host ""
if ($skipped.Count -gt 0) {
    Write-Host "Not checked, nothing executable in them: $($skipped -join ', ')"
}
Write-Host "Checked $checked file(s)."

# Printed whatever the verdict, because the hash is what a person needs to look
# the file up themselves, to ask somebody else to, or to recognise this exact
# build later. Rebuilding changes it, so it is only true of what was just checked.
if (-not $ListOnly -and $checked -gt 0) {
    Write-Host ""
    Write-Host "SHA-256 of each file checked, which is how VirusTotal knows them:"
    foreach ($file in $files) {
        if ($file.PSObject.Properties.Name -contains "Hash" -and $file.Hash) {
            Write-Host ("  {0}  {1}" -f $file.Hash, $file.Name)
        }
    }
    Write-Host "Open one at $(Get-VirusTotalLink '<sha-256>')"
}

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
