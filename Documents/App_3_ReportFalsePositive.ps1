<#
.SYNOPSIS
    Fills Microsoft's false positive form for a release file, and stops before sending.
.DESCRIPTION
    Microsoft has no API for this. The developer route is a form at
    https://www.microsoft.com/en-us/wdsi/filesubmission behind a Microsoft account,
    so signing in stays a human act and sending stays your decision. Everything
    between the two is done here.

    A browser is opened with a debugging port and its own profile, which is how a
    person and a script share one window: you sign in, it fills the fields. The
    profile keeps you signed in between runs. Attaching the file is the part that
    cannot be done by hand from a page - clicking Select opens a chooser owned by
    Windows - so the file is put into the form directly.

    What it gathers for the chosen file:

      - Its SHA-256, which is how every party in this conversation names a file.
      - What Defender itself says about it, by running the scanner that ships with
        Windows, so the detection name in the report is the real one rather than
        one copied by hand from a message box.
      - What VirusTotal currently says, because other engines disagreeing with
        Defender is the strongest single argument a reviewer can be handed.
      - Who signed it and until when, since a signed file from a known publisher
        is treated differently from an anonymous one.

    The explanation it writes is a starting point, not a finished letter. Read it
    in the browser before sending and add anything specific to the detection you
    saw. Nothing is ever sent by this script.

    Filling needs Node and Playwright (npm install -g playwright). Without them the
    report is still written and put on the clipboard, and the page still opens, so
    the same job is done by hand rather than not at all.
.PARAMETER Path
    The file to report. Without it the release files are listed and one is chosen.
.PARAMETER Detection
    The name Defender gave the file, when you already know it. Without it the file
    is scanned and the name taken from what the scanner says.
.PARAMETER Company
    The company name the form asks for. Taken from the signing certificate when it
    is left out.
.PARAMETER Port
    The debugging port the browser listens on. A browser already listening there is
    used as it is, which is how you can sign in first and run this afterwards.
.PARAMETER SkipScan
    Does not run Defender over the file. Use it when the detection has already been
    removed locally, or when scanning would quarantine a file you still need.
.PARAMETER NoBrowser
    Writes the report and opens the page without filling anything.
.EXAMPLE
    PS> .\App_3_ReportFalsePositive.ps1
    Lists the release files, gathers everything about the one you pick, opens the
    form and fills it in. Sign in when the browser asks; it waits.
.EXAMPLE
    PS> .\App_3_ReportFalsePositive.ps1 -Path Files.v4\x360ce.exe -Detection "Trojan:Win32/Wacatac.B!ml"
    Prepares the report for one file without scanning it again.
.NOTES
    Report the file Defender objects to. That is whichever one it flags: the
    detection can be on the archive rather than on the program inside it.

    A correction applies to the bytes submitted, so a later build can be flagged
    again until the software earns a reputation.

    The form takes files up to 500 MB.
#>
param(
    [string]$Path,
    [string]$Detection,
    [string]$Company,
    [int]$Port = 9222,
    [switch]$SkipScan,
    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"

$submissionUrl = "https://www.microsoft.com/en-us/wdsi/filesubmission"
# What the page itself states.
$sizeLimit = 500MB
# The browser keeps its own profile so the Microsoft account is signed in once
# rather than every time, and separate from the everyday one so a browser already
# running does not refuse the debugging port.
$browserProfile = Join-Path $env:LOCALAPPDATA "x360ce\submission-profile"

#------------------------------------------------------------------------------
# Configuration.
#------------------------------------------------------------------------------

function Resolve-Path2 {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, $Path))
}

# The same list the release signs, so there is no second list to keep in step.
function Get-ReleaseFile {
    $configPath = Join-Path $PSScriptRoot "App_1_Sign_and_Zip.json"
    if (-not (Test-Path -LiteralPath $configPath)) { return @() }
    $json = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
    $apps = @(if ($json.PSObject.Properties.Name -contains "Apps") { $json.Apps } else { $json })
    $found = New-Object System.Collections.Generic.List[object]
    $seen = @{}
    foreach ($dir in ($apps.FilesDir | Where-Object { $_ } | Select-Object -Unique)) {
        $full = Resolve-Path2 $dir
        if (-not (Test-Path -LiteralPath $full)) { continue }
        foreach ($file in Get-ChildItem -LiteralPath $full -File) {
            if ($file.Extension -notin @(".exe", ".zip", ".cab", ".dll")) { continue }
            if ($seen.ContainsKey($file.FullName)) { continue }
            $seen[$file.FullName] = $true
            $found.Add([PSCustomObject]@{ Name = "$dir/$($file.Name)"; Path = $file.FullName })
        }
    }
    return $found
}

#------------------------------------------------------------------------------
# What the report is made of.
#------------------------------------------------------------------------------

# What Defender says right now, asked through the script that owns scanning so
# there is one place that knows how to run it.
function Get-DefenderVerdict {
    param([string]$FilePath)
    $scan = Join-Path $PSScriptRoot "App_4_DefenderScan.ps1"
    if (-not (Test-Path -LiteralPath $scan)) {
        return "The scanning script is missing, so Defender was not asked."
    }
    $result = & $scan -Path $FilePath -SkipUpdate -Quiet -PassThru | Select-Object -First 1
    if (-not $result) { return "Defender was not able to scan the file." }
    if ($result.Clean) { return "Clean on this machine, with the definitions installed today." }
    if (-not $result.Scanned) { return "The scanner exited $($result.ExitCode) without scanning the file." }
    return ($result.Threats -join "; ")
}

# What the other engines say. A hash lookup, so the file is not sent anywhere.
function Get-VirusTotalVerdict {
    param([string]$Hash)
    if ([string]::IsNullOrWhiteSpace($env:VIRUSTOTAL_API_KEY)) {
        return "Not asked: VIRUSTOTAL_API_KEY is not set."
    }
    $response = Invoke-WebRequest -Uri "https://www.virustotal.com/api/v3/files/$Hash" `
        -Headers @{ "x-apikey" = $env:VIRUSTOTAL_API_KEY } -SkipHttpErrorCheck -TimeoutSec 120
    if ($response.StatusCode -eq 404) {
        return "VirusTotal has never seen this file."
    }
    if ($response.StatusCode -ne 200) {
        return "VirusTotal answered $($response.StatusCode)."
    }
    # As a hash table, because a report can carry a property with an empty name.
    $body = $response.Content | ConvertFrom-Json -AsHashtable
    $results = $body.data.attributes.last_analysis_results
    if (-not $results) { return "VirusTotal has the file but has not analysed it yet." }
    $flagged = @($results.Keys | Where-Object { $results[$_].category -in @("malicious", "suspicious") } | Sort-Object)
    if ($flagged.Count -eq 0) {
        return "$($results.Count) engines, none of which flags it."
    }
    return "$($results.Count) engines, $($flagged.Count) of which flag it: $($flagged -join ', ')."
}

function Get-SignatureLine {
    param([string]$FilePath)
    # A zip has nowhere to keep an Authenticode signature, so reporting it as
    # unsigned reads like a fault when it is the format. Say which it is.
    if ([System.IO.Path]::GetExtension($FilePath) -eq ".zip") {
        return "A zip cannot carry a signature. The program inside it is signed."
    }
    $signature = Get-AuthenticodeSignature -LiteralPath $FilePath
    if ($signature.Status -ne "Valid") {
        return "Not signed, or the signature does not verify ($($signature.Status))."
    }
    $subject = ($signature.SignerCertificate.Subject -split ",")[0] -replace "^CN=", ""
    $issuer = ($signature.SignerCertificate.Issuer -split ",")[0] -replace "^CN=", ""
    return "Signed by $subject, issued by $issuer, valid to $($signature.SignerCertificate.NotAfter.ToString('yyyy-MM-dd'))."
}

#------------------------------------------------------------------------------
# The browser that fills the form.
#------------------------------------------------------------------------------

# Playwright is installed once, globally, rather than carried in this repository.
# Node resolves it through NODE_PATH, which is set from whatever is found here.
function Get-PlaywrightRoot {
    foreach ($root in @(
            (Join-Path $env:APPDATA "npm\node_modules\playwright"),
            (Join-Path $env:ProgramFiles "nodejs\node_modules\playwright"))) {
        if (Test-Path -LiteralPath $root) { return $root }
    }
    return $null
}

# A browser with a debugging port, which is what lets a person and a script share
# one window: the sign-in stays human, the typing does not. Its own profile keeps
# the account signed in between runs, and keeps it away from the everyday browser,
# which would refuse the port while it is already running.
function Start-SubmissionBrowser {
    param([int]$Port)
    $running = $null
    # Nothing listening is the ordinary case, and the answer to the question.
    try { $running = Invoke-RestMethod "http://127.0.0.1:$Port/json/version" -TimeoutSec 3 }
    catch { $running = $null }
    if ($running) {
        Write-Host "  Using the browser already listening on port $Port."
        return $true
    }
    $browsers = @(
        (Join-Path $env:ProgramFiles "Microsoft\Edge\Application\msedge.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Microsoft\Edge\Application\msedge.exe"),
        (Join-Path $env:ProgramFiles "Google\Chrome\Application\chrome.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Google\Chrome\Application\chrome.exe"))
    $exe = $browsers | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $exe) { return $false }
    New-Item -ItemType Directory -Path $browserProfile -Force | Out-Null
    Start-Process $exe -ArgumentList `
        "--remote-debugging-port=$Port", "--user-data-dir=`"$browserProfile`"",
    "--no-first-run", "--no-default-browser-check", $submissionUrl
    # Wait for the port rather than for a number of seconds, which is either too
    # long on a fast machine or too short on a slow one.
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Milliseconds 500
        try {
            Invoke-RestMethod "http://127.0.0.1:$Port/json/version" -TimeoutSec 3 | Out-Null
            return $true
        }
        catch {
            # Still starting. Ask again until the attempts run out.
            Write-Verbose "Port $Port is not answering yet."
        }
    }
    return $false
}

#------------------------------------------------------------------------------
# Choose the file.
#------------------------------------------------------------------------------

if ($Path) {
    $target = Resolve-Path2 $Path
    if (-not (Test-Path -LiteralPath $target)) { throw "File not found: $target" }
    $file = [PSCustomObject]@{ Name = [System.IO.Path]::GetFileName($target); Path = $target }
}
else {
    $files = @(Get-ReleaseFile)
    if ($files.Count -eq 0) {
        throw "No release files found. Build the release first, or name a file with -Path."
    }
    Write-Host ""
    Write-Host "  Which file is Defender objecting to?"
    Write-Host "  Report the program rather than the archive holding it: a correction"
    Write-Host "  applies to the bytes submitted."
    Write-Host ""
    for ($i = 0; $i -lt $files.Count; $i++) {
        Write-Host ("  {0,2} - {1}" -f ($i + 1), $files[$i].Name)
    }
    Write-Host "   Q - Nothing"
    Write-Host ""
    $choice = (Read-Host "  Choice").Trim()
    $index = 0
    if (-not ([int]::TryParse($choice, [ref]$index) -and $index -ge 1 -and $index -le $files.Count)) {
        Write-Host "Nothing prepared."
        return
    }
    $file = $files[$index - 1]
}

#------------------------------------------------------------------------------
# Gather.
#------------------------------------------------------------------------------

Write-Host ""
Write-Host "Preparing a report for $($file.Name)"
$item = Get-Item -LiteralPath $file.Path
if ($item.Length -gt $sizeLimit) {
    Write-Host ("  The form takes files up to {0:N0} MB and this one is {1:N0} MB." -f ($sizeLimit / 1MB), ($item.Length / 1MB)) -ForegroundColor Yellow
}

$hash = (Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "  SHA-256:    $hash"

$signatureLine = Get-SignatureLine $file.Path
Write-Host "  Signature:  $signatureLine"

if (-not $Detection) {
    if ($SkipScan) {
        $Detection = "Not recorded. Add the name Defender showed you."
    }
    else {
        Write-Host "  Asking Defender what it makes of the file..."
        $Detection = Get-DefenderVerdict $file.Path
    }
}
Write-Host "  Defender:   $Detection"

$virusTotalLine = Get-VirusTotalVerdict $hash
Write-Host "  VirusTotal: $virusTotalLine"

$definitionVersion = ""
# Recommended by the form rather than required, so a machine without Defender
# leaves it blank instead of stopping.
try { $definitionVersion = (Get-MpComputerStatus).AntivirusSignatureVersion }
catch { $definitionVersion = "" }
if ($definitionVersion) { Write-Host "  Definitions: $definitionVersion" }

# The company the form asks for is the one that signed the software, so it is read
# from the certificate rather than guessed. An archive carries no signature, so the
# program beside it answers for it.
if (-not $Company) {
    $signed = $file.Path
    if ($item.Extension -in @(".zip", ".cab")) {
        $beside = Join-Path $item.DirectoryName ($item.BaseName + ".exe")
        if (Test-Path -LiteralPath $beside) { $signed = $beside }
    }
    $certificate = (Get-AuthenticodeSignature -LiteralPath $signed).SignerCertificate
    if ($certificate) {
        $organisation = ($certificate.Subject -split "," | ForEach-Object { $_.Trim() } |
            Where-Object { $_ -like "O=*" } | Select-Object -First 1)
        if ($organisation) { $Company = $organisation -replace "^O=", "" }
    }
}
if ($Company) { Write-Host "  Company:    $Company" }

#------------------------------------------------------------------------------
# Write the explanation.
#------------------------------------------------------------------------------

$version = $item.VersionInfo.FileVersion
if (-not $version -and $item.Extension -in @(".zip", ".cab")) {
    $beside = Join-Path $item.DirectoryName ($item.BaseName + ".exe")
    if (Test-Path -LiteralPath $beside) {
        $version = (Get-Item -LiteralPath $beside).VersionInfo.FileVersion + " (the program inside)"
    }
}
if (-not $version) { $version = "not recorded in the file" }

# A report about a detection nobody can see wastes a reviewer's time and your own
# standing with them. It is still worth writing when the detection was seen
# somewhere else, which is why this says so rather than refusing.
$defenderObjects = $Detection -notmatch "^Clean on this machine"
$othersObject = $virusTotalLine -match "which flag it"
if (-not $defenderObjects -and -not $othersObject) {
    Write-Host ""
    Write-Host "  Nothing here objects to this file: Defender calls it clean and no engine" -ForegroundColor Yellow
    Write-Host "  on VirusTotal flags it. A report only makes sense if somebody else saw the" -ForegroundColor Yellow
    Write-Host "  detection. Name what they saw with -Detection so the report says what is" -ForegroundColor Yellow
    Write-Host "  being disputed." -ForegroundColor Yellow
}

$explanation = @"
This is a false positive report from the developer of the software.

File:      $($item.Name)
Version:   $version
SHA-256:   $hash
Signature: $signatureLine

What the program is
x360ce is an open source Xbox 360 controller emulator. It lets a game that only
supports Xbox controllers be played with any other controller, by presenting the
player's device to the game as an Xbox one. It has been published since 2010 and
its source is at https://github.com/x360ce/x360ce.

Why it is likely being flagged
To do its job the program presents a virtual controller to the system and reads
input from real ones. Games load its library, and it can install a driver for the
virtual device. Those are the same actions that input-stealing software performs,
so behavioural and machine-learning detections score it highly even though it does
exactly what its users install it to do. It does not collect data, contact any
service without being asked, or modify other programs.

What Defender reports
$Detection

What other engines report
$virusTotalLine
https://www.virustotal.com/gui/file/$hash

Request
Please review this detection and, if you agree it is a false positive, remove it.
The program is signed, published openly, and the same bytes are available from
the project's release page.
"@

$reportPath = Join-Path $item.DirectoryName ($item.BaseName + ".false-positive.txt")
Set-Content -LiteralPath $reportPath -Value $explanation -Encoding utf8
try {
    Set-Clipboard -Value $explanation
    $clipboard = "It is on the clipboard, ready to paste."
}
catch {
    $clipboard = "The clipboard was busy, so paste it from the file above."
}

#------------------------------------------------------------------------------
# Hand it over.
#------------------------------------------------------------------------------

Write-Host ""
Write-Host "Written: $reportPath"
Write-Host $clipboard

# Everything below fills the form for you. Without a browser it can drive, the
# report is still written and the page still opens, which is the same job done by
# hand rather than nothing at all.
$filler = [System.IO.Path]::ChangeExtension($PSCommandPath, ".js")
$canFill = -not $NoBrowser -and (Test-Path -LiteralPath $filler) -and
    (Get-Command node -ErrorAction SilentlyContinue) -and (Get-PlaywrightRoot)

if (-not $canFill) {
    Write-Host ""
    if ($NoBrowser) {
        Write-Host "Opening the page for you to fill in."
    }
    else {
        Write-Host "Filling the form needs Node and Playwright, and one of them is missing."
        Write-Host "Install them with: npm install -g playwright"
        Write-Host "Until then the page opens and the explanation is on the clipboard."
    }
    Write-Host "  1. Sign in, and choose Software developer."
    Write-Host "  2. Attach $($item.Name) from the folder that is opening."
    Write-Host "  3. Paste the explanation and say it is incorrectly detected."
    Start-Process explorer.exe -ArgumentList "/select,`"$($file.Path)`""
    Start-Process $submissionUrl
    return
}

$browser = Start-SubmissionBrowser -Port $Port
if (-not $browser) {
    Write-Host "No browser could be started, so the page is opening the ordinary way." -ForegroundColor Yellow
    Start-Process $submissionUrl
    return
}

Write-Host ""
Write-Host "A browser window is open at the form."
Write-Host "Sign in there if it asks. The filling waits for you and then takes over."
Write-Host ""

$request = [ordered]@{
    debuggerUrl       = "http://127.0.0.1:$Port"
    signInMinutes     = 10
    company           = $Company
    filePath          = $file.Path
    detectionName     = $Detection
    definitionVersion = $definitionVersion
    comments          = $explanation
    screenshot        = [System.IO.Path]::ChangeExtension($reportPath, ".png")
}
$requestPath = [System.IO.Path]::ChangeExtension($reportPath, ".json")
$request | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $requestPath -Encoding utf8

$env:NODE_PATH = Split-Path -Parent (Get-PlaywrightRoot)
& node $filler $requestPath
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "The form was not filled. What is on the clipboard still works by hand." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Read it over in the browser, then send it yourself. Nothing has been sent."
Write-Host "A correction applies to the bytes you send. Rebuilding changes the hash, so"
Write-Host "a later release can be flagged again until the software earns a reputation."
