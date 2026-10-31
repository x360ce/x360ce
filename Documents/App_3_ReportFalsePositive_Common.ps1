<#
.SYNOPSIS
    What every false positive report is made of, whoever it goes to.
.DESCRIPTION
    Dot-sourced by the App_3_ReportFalsePositive_{Vendor}.ps1 scripts, so the file's
    facts are gathered one way and the program is described in the same words to
    Microsoft and to Malwarebytes alike.
#>

# A path relative to this folder, which is where the release configuration and its
# output folders are named from.
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

# The certificate that signed a file, or the program beside an archive, which is the
# one that answers for it.
function Get-SigningCertificate {
    param([string]$FilePath)
    $item = Get-Item -LiteralPath $FilePath
    $signed = $FilePath
    if ($item.Extension -in @(".zip", ".cab")) {
        $beside = Join-Path $item.DirectoryName ($item.BaseName + ".exe")
        if (Test-Path -LiteralPath $beside) { $signed = $beside }
    }
    $signature = Get-AuthenticodeSignature -LiteralPath $signed
    if ($signature.Status -ne "Valid") { return $null }
    return $signature.SignerCertificate
}

# The file's own version, or the version of the program beside an archive.
function Get-ReportVersion {
    param([string]$FilePath)
    $item = Get-Item -LiteralPath $FilePath
    $version = $item.VersionInfo.FileVersion
    if (-not $version -and $item.Extension -in @(".zip", ".cab")) {
        $beside = Join-Path $item.DirectoryName ($item.BaseName + ".exe")
        if (Test-Path -LiteralPath $beside) {
            $version = (Get-Item -LiteralPath $beside).VersionInfo.FileVersion + " (the program inside)"
        }
    }
    if (-not $version) { $version = "not recorded in the file" }
    return $version
}

# Lookups are spaced out, as App_2_VirusTotal.ps1 does, so several files stay inside
# a free key's allowance of 4 requests a minute.
$script:VirusTotalLastRequest = [DateTime]::MinValue
$script:VirusTotalGap = [TimeSpan]::FromSeconds(15)

# What every engine on VirusTotal says. A hash lookup, so the file is not sent anywhere.
# Results is null when there is nothing to read, and Note says why.
function Get-VirusTotalResult {
    param([string]$Hash)
    if ([string]::IsNullOrWhiteSpace($env:VIRUSTOTAL_API_KEY)) {
        return [PSCustomObject]@{ Results = $null; Note = "Not asked: VIRUSTOTAL_API_KEY is not set." }
    }
    $wait = $script:VirusTotalLastRequest.Add($script:VirusTotalGap) - [DateTime]::UtcNow
    if ($wait.TotalMilliseconds -gt 0) { Start-Sleep -Milliseconds ([int]$wait.TotalMilliseconds) }
    $script:VirusTotalLastRequest = [DateTime]::UtcNow
    $response = Invoke-WebRequest -Uri "https://www.virustotal.com/api/v3/files/$Hash" `
        -Headers @{ "x-apikey" = $env:VIRUSTOTAL_API_KEY } -SkipHttpErrorCheck -TimeoutSec 120
    if ($response.StatusCode -eq 404) {
        return [PSCustomObject]@{ Results = $null; Note = "VirusTotal has never seen this file." }
    }
    if ($response.StatusCode -eq 429) {
        return [PSCustomObject]@{ Results = $null; Note = "VirusTotal refused the lookup for being too frequent; the free key allows 500 a day." }
    }
    if ($response.StatusCode -ne 200) {
        return [PSCustomObject]@{ Results = $null; Note = "VirusTotal answered $($response.StatusCode)." }
    }
    # As a hash table, because a report can carry a property with an empty name.
    $body = $response.Content | ConvertFrom-Json -AsHashtable
    $results = $body.data.attributes.last_analysis_results
    if (-not $results) {
        return [PSCustomObject]@{ Results = $null; Note = "VirusTotal has the file but has not analysed it yet." }
    }
    return [PSCustomObject]@{ Results = $results; Note = "" }
}

# The engines that flag the file, in one line.
function Format-VirusTotalVerdict {
    param($Result)
    if (-not $Result.Results) { return $Result.Note }
    $results = $Result.Results
    $flagged = @($results.Keys | Where-Object { $results[$_].category -in @("malicious", "suspicious") } | Sort-Object)
    if ($flagged.Count -eq 0) {
        return "$($results.Count) engines, none of which flags it."
    }
    $verb = if ($flagged.Count -eq 1) { "flags" } else { "flag" }
    return "$($results.Count) engines, $($flagged.Count) of which $verb it: $($flagged -join ', ')."
}

# What the program is and why a detection scores it highly, said the same way to
# every vendor.
function Get-ProgramStatement {
    return @"
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

Version 4 also has an optional AI assistant access, off by default. When the user
turns it on, a local MCP server listens on 127.0.0.1 only, asks for a token, and lets
an AI assistant read the program's settings, or change them if the user allows it.
An optional tunnel, also off by default and made with the user's own OpenAI key,
lets ChatGPT on the web reach the same access. A listener that takes commands and
a tunnel can look like remote-control software to a model, but neither runs unless
the user enables it.
"@
}
