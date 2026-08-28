<#
.SYNOPSIS
    Copies, signs and zips application release files.
.DESCRIPTION
    All project specific values are read from the JSON file which has the same
    name as this script, so the script itself is copied between projects
    unchanged. Only the JSON file is edited.

    The JSON file holds the solution to build, the build stages and an array of
    applications. An older file which is just the array of applications is read
    the same way, without the build information.

      Solution      - the solution built by the release script.
      Configuration - the build configuration, usually "Release".
      Stages        - the build stages, in the order they run. Each names the
                      platforms built before the files of that stage are signed.
      Apps          - the applications below.

    Each application has its own signature name, output folder and list of files:

      AppName    - the name written into the signature.
      Stage      - the build stage this application belongs to. Files embedded
                   into another application are signed in an earlier stage than
                   the application embedding them.
      AppLink    - the link written into the signature.
      SignModule - the shared signing module. Environment variables are expanded,
                   so the path is written as %X360CE_SIGN_MODULE% rather than one
                   maintainer's folder.
      ZipScript  - the shared zip script.
      FilesDir   - the folder receiving the signed copies and the zips.
                   Optional, defaults to "Files".
      Files      - the files to process.

    Every file says what is signed and, when needed, where it comes from and
    what zip it goes into:

      Source - copied over Target before signing. Optional, leave it out to
               sign a file where it already is.
      Target - the file which is signed. Optional, defaults to the name of the
               source file inside "FilesDir".
      Zip    - the zip built from Target, named inside "FilesDir". Optional,
               leave it out to sign without zipping.
      Cab    - the cabinet built from Target, named inside "FilesDir". Optional.
               A cabinet holds an Authenticode signature of its own, which a zip
               cannot, so the download itself can be checked before anything is
               unpacked. It is built after the file inside it is signed, and is
               then signed as well.

    Paths are relative to the folder of this script, except "Zip" which is a
    plain name inside "FilesDir". Two files may name the same Target, which
    packs one signed file into more than one zip. It is copied and signed once.

    Four steps are offered:

      Sign - copy every source over its target and sign the target.
      Zip  - build the zip of every file which names one.
      Cab  - build the cabinet of every file which names one, and sign it.
      Copy - copy every source over its target without signing.

    When more than one file is configured the step asks which one to work on,
    or all of them.

    The full release, menu item 1 and the "All" action, deletes the zip and the
    cabinet before packing so neither holds a file left over from an earlier
    release. The separate "Zip" step keeps the existing zip when nothing changed.
.PARAMETER Action
    Runs one action over every file and exits instead of showing the menu.
.PARAMETER Force
    Signs a file even when it already carries a trusted signature. Without it a file
    which is already signed is left alone, so a rerun only asks for the token once per
    file that actually changed, and a signature applied by somebody else is not
    overwritten.
.PARAMETER Stage
    Limits the run to the applications of one build stage. Without it every
    application is offered, which is what the menu does.
.EXAMPLE
    PS> .\App_1_Sign_and_Zip.ps1
    Shows the menu.
.EXAMPLE
    PS> .\App_1_Sign_and_Zip.ps1 All
    Signs and zips without asking. Used when another script drives the release.
.EXAMPLE
    PS> .\App_1_Sign_and_Zip.ps1 Sign -Stage Library
    Signs only the files embedded into the applications, before they are built.
.NOTES
    Paths inside the JSON file can use forward or back slashes. Signing requires
    the USB token or card reader supported by the signing module.
#>
param(
    [Parameter(Position = 0)]
    [ValidateSet("Sign", "Zip", "Cab", "Copy", "All")]
    [string]$Action,
    [switch]$Force,
    [string]$Stage
)

$ErrorActionPreference = "Stop"

#------------------------------------------------------------------------------
# Configuration.
#------------------------------------------------------------------------------

$configPath = [System.IO.Path]::ChangeExtension($PSCommandPath, ".json")
$json = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$config = @(if ($json.PSObject.Properties.Name -contains "Apps") { $json.Apps } else { $json })
if ($Stage) {
    $config = @($config | Where-Object { $_.Stage -eq $Stage })
    if ($config.Count -eq 0) {
        Write-Host "No applications in stage: $Stage"
        return
    }
}

# Importing the module changes the current folder to the folder of this script,
# so keep the import here rather than inside a function.
foreach ($module in ($config.SignModule | Select-Object -Unique)) {
    $module = [System.Environment]::ExpandEnvironmentVariables($module)
    if (Test-Path -LiteralPath $module) {
        Import-Module $module -Force
    }
}

#------------------------------------------------------------------------------
# Steps.
#------------------------------------------------------------------------------

function Resolve-Path2 {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, $Path))
}

# One record per configured file: what is signed, where it comes from and the
# zip built from it. "Label" names the record in the menus.
function Get-Targets {
    foreach ($app in $config) {
        $dir = $app.FilesDir
        if (-not $dir) {
            $dir = "Files"
        }
        foreach ($file in $app.Files) {
            $targetText = $file.Target
            if (-not $targetText) {
                $targetText = "$dir/" + [System.IO.Path]::GetFileName($file.Source)
            }
            $sourceText = $file.Source
            if (-not $sourceText) {
                $sourceText = $targetText
            }
            $label = $targetText
            $zip = ""
            if ($file.Zip) {
                $label = "$dir/$($file.Zip)"
                $zip = Resolve-Path2 $label
            }
            $cab = ""
            if ($file.Cab) {
                $cab = Resolve-Path2 "$dir/$($file.Cab)"
            }
            [PSCustomObject]@{
                App        = $app
                SourceText = $sourceText
                Source     = Resolve-Path2 $sourceText
                Target     = Resolve-Path2 $targetText
                Label      = $label
                Zip        = $zip
                Cab        = $cab
            }
        }
    }
}

# Each target is copied once, even when several zips are built from it.
function Copy-Sources {
    param($Targets, [switch]$OnlyIfMissing)
    $done = @{}
    foreach ($item in $Targets) {
        if ($item.Source -eq $item.Target -or $done.ContainsKey($item.Target)) {
            continue
        }
        $done[$item.Target] = $true
        if ($OnlyIfMissing -and (Test-Path -LiteralPath $item.Target)) {
            continue
        }
        if (-not (Test-Path -LiteralPath $item.Source)) {
            Write-Host "Source not found: $($item.Source)"
            continue
        }
        $targetDir = [System.IO.Path]::GetDirectoryName($item.Target)
        if (-not (Test-Path -LiteralPath $targetDir)) {
            New-Item -ItemType Directory -Path $targetDir | Out-Null
        }
        Copy-Item -LiteralPath $item.Source -Destination $item.Target -Force
        Write-Host "Copied: $($item.Target)"
    }
}

function Test-SignModule {
    if ((Get-Command Sign-Files -ErrorAction SilentlyContinue) -or (Get-Command Sign-File -ErrorAction SilentlyContinue)) {
        return $true
    }
    Write-Host "Signing module not found: $([System.Environment]::ExpandEnvironmentVariables(($config.SignModule | Select-Object -Unique)))"
    return $false
}

# One run of the signing tool for the files of one application. A module which can
# take several files signs them in that one run, which asks for the card PIN once
# instead of once per file. An older module without it still works, one at a time.
# The description and the link written into a signature belong to one application,
# which is why the caller groups the files by application before calling here.
function Invoke-SignModule {
    param($App, $Paths)
    $global:AppName = $App.AppName
    $global:AppLink = $App.AppLink
    foreach ($path in $Paths) {
        Write-Host "Signing file: $path"
    }
    if (Get-Command Sign-Files -ErrorAction SilentlyContinue) {
        Sign-Files -FilePath $Paths
        return
    }
    foreach ($path in $Paths) {
        Sign-File -FilePath $path
        # Brief delay so the USB token can process the next request.
        Start-Sleep -Seconds 2
    }
}

function Invoke-Sign {
    param($Targets)
    if (-not (Test-SignModule)) {
        return
    }
    Copy-Sources $Targets
    # Detect the signing hardware once per run.
    $global:SignProfileCache = $null
    $done = @{}
    foreach ($app in $config) {
        $paths = New-Object System.Collections.Generic.List[string]
        foreach ($item in $Targets | Where-Object { $_.App -eq $app }) {
            if ($done.ContainsKey($item.Target)) {
                continue
            }
            $done[$item.Target] = $true
            if (-not (Test-Path -LiteralPath $item.Target)) {
                Write-Host "Nothing to sign: $($item.Target)"
                continue
            }
            # Every signature costs a prompt on the token, so a file which is already
            # trusted is left as it is. Anything the build has just produced is unsigned
            # and so is always signed. It also keeps a signature applied by somebody else,
            # such as the supplier of a redistributed file, from being replaced.
            if (-not $Force -and (Get-AuthenticodeSignature -LiteralPath $item.Target).Status -eq "Valid") {
                Write-Host "Already signed: $($item.Target)"
                continue
            }
            $paths.Add($item.Target)
        }
        if ($paths.Count -eq 0) {
            continue
        }
        Invoke-SignModule -App $app -Paths $paths.ToArray()
    }
}

# One cabinet holding one file, under the name that file already has.
#
# Driven by a directive file rather than by "makecab source destination", because
# that short form stores the file under the name of the cabinet: the download then
# unpacks to x360ce.cab instead of x360ce.exe. The directive file names the entry
# explicitly, and it is the only way to say so.
#
# LZX with the largest window packs the executables well under the size of the zip
# of the same file. MaxDiskSize=0 keeps the result to one cabinet: the default
# splits at a media size, and half a release is not a download.
#
# makecab writes a report and an inf beside itself, so it runs in a folder of its
# own which is removed afterwards, and only the cabinet reaches the output folder.
function New-Cabinet {
    param([string]$Target, [string]$Cab)
    $work = Join-Path ([System.IO.Path]::GetTempPath()) ("makecab_" + [System.Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $work | Out-Null
    try {
        $directives = Join-Path $work "cabinet.ddf"
        Set-Content -LiteralPath $directives -Encoding ASCII -Value @(
            ".OPTION EXPLICIT",
            ".Set CabinetNameTemplate=$([System.IO.Path]::GetFileName($Cab))",
            ".Set DiskDirectory1=$([System.IO.Path]::GetDirectoryName($Cab))",
            ".Set CompressionType=LZX",
            ".Set CompressionMemory=21",
            ".Set MaxDiskSize=0",
            ".Set Cabinet=on",
            ".Set Compress=on",
            "`"$Target`" `"$([System.IO.Path]::GetFileName($Target))`""
        )
        # Its progress is a percentage repainted hundreds of times, which is noise in
        # a release log, so it goes to a file inside the folder that is thrown away.
        $log = Join-Path $work "makecab.log"
        $process = Start-Process makecab.exe -ArgumentList "/F", "`"$directives`"" `
            -WorkingDirectory $work -NoNewWindow -Wait -PassThru -RedirectStandardOutput $log
        if ($process.ExitCode -ne 0) {
            Get-Content -LiteralPath $log -Tail 5 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "  $_" }
            throw "makecab failed ($($process.ExitCode)) for: $Cab"
        }
        if (-not (Test-Path -LiteralPath $Cab)) {
            throw "makecab reported success but wrote no cabinet: $Cab"
        }
    }
    finally {
        Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# A cabinet is the archive Windows itself can check. Authenticode has nowhere to
# put a signature in a zip, so a zip can only be trusted after it is unpacked and
# the file inside it is examined; a cabinet carries the signature, so the download
# is verifiable as it stands, from its Properties page, with nothing installed.
#
# It is packed after the signing step because the file it holds has to carry its
# own signature first, and it is signed itself once it exists, which is a second
# request to the token.
function Invoke-Cab {
    param($Targets, [switch]$Fresh)
    $wanted = @($Targets | Where-Object { $_.Cab })
    if ($wanted.Count -eq 0) {
        return
    }
    if (-not (Test-SignModule)) {
        return
    }
    Copy-Sources $Targets -OnlyIfMissing
    foreach ($app in $config) {
        $paths = New-Object System.Collections.Generic.List[string]
        foreach ($item in $wanted | Where-Object { $_.App -eq $app }) {
            if (-not (Test-Path -LiteralPath $item.Target)) {
                Write-Host "Nothing to pack: $($item.Target)"
                continue
            }
            if ($Fresh -and (Test-Path -LiteralPath $item.Cab)) {
                Remove-Item -LiteralPath $item.Cab -Force
                Write-Host "Removed: $($item.Cab)"
            }
            $cabDir = [System.IO.Path]::GetDirectoryName($item.Cab)
            if (-not (Test-Path -LiteralPath $cabDir)) {
                New-Item -ItemType Directory -Path $cabDir | Out-Null
            }
            New-Cabinet -Target $item.Target -Cab $item.Cab
            Write-Host "Packed: $($item.Cab)"
            $paths.Add($item.Cab)
        }
        if ($paths.Count -eq 0) {
            continue
        }
        Invoke-SignModule -App $app -Paths $paths.ToArray()
    }
}

# The zip script keeps the existing zip when the content still matches, which
# leaves entries of an earlier release in place. Deleting the zip first forces
# it to be built from scratch.
function Invoke-Zip {
    param($Targets, [switch]$Fresh)
    Copy-Sources $Targets -OnlyIfMissing
    foreach ($item in $Targets) {
        if (-not $item.Zip) {
            continue
        }
        if (-not (Test-Path -LiteralPath $item.Target)) {
            Write-Host "Nothing to zip: $($item.Target)"
            continue
        }
        if ($Fresh -and (Test-Path -LiteralPath $item.Zip)) {
            Remove-Item -LiteralPath $item.Zip -Force
            Write-Host "Removed: $($item.Zip)"
        }
        $zipDir = [System.IO.Path]::GetDirectoryName($item.Zip)
        if (-not (Test-Path -LiteralPath $zipDir)) {
            New-Item -ItemType Directory -Path $zipDir | Out-Null
        }
        $zipScript = Resolve-Path2 $item.App.ZipScript
        # Packed by Explorer rather than by .NET. A scanner judges the compressed stream,
        # not the files inside it, and the two compressors produce entirely different
        # streams from the same input. Releases packed this way have not been flagged;
        # releases packed by .NET have, months after they shipped clean, when a new
        # signature happened to match their stream. Explorer also packs the same input to
        # the same bytes every time, so a release can be reproduced.
        & $zipScript `
            -sourceDir ([System.IO.Path]::GetDirectoryName($item.Target)) `
            -destFile $item.Zip `
            -searchPattern ([System.IO.Path]::GetFileName($item.Target)) `
            -UseShellToZipFiles $true `
            -IgnoreEmptyFolders $true
    }
}

function Invoke-Action {
    param([string]$Name, $Targets)
    switch ($Name) {
        "Sign" { Invoke-Sign $Targets }
        "Zip" { Invoke-Zip $Targets }
        "Cab" { Invoke-Cab $Targets }
        "Copy" { Copy-Sources $Targets }
        "All" { Invoke-Sign $Targets; Invoke-Zip $Targets -Fresh; Invoke-Cab $Targets -Fresh }
    }
}

#------------------------------------------------------------------------------
# Menu.
#------------------------------------------------------------------------------

# Asks which file the step works on. One file needs no question.
function Select-Targets {
    $all = @(Get-Targets)
    if ($all.Count -le 1) {
        return $all
    }
    while ($true) {
        Write-Host ""
        for ($i = 0; $i -lt $all.Count; $i++) {
            Write-Host "  $($i + 1) - $($all[$i].Label)"
        }
        Write-Host "  A - All"
        Write-Host "  C - Cancel"
        Write-Host ""
        $choice = Read-Host "  Which file"
        if ($choice -eq "A") { return $all }
        if ($choice -eq "C") { return @() }
        $index = 0
        if ([int]::TryParse($choice, [ref]$index) -and $index -ge 1 -and $index -le $all.Count) {
            return @($all[$index - 1])
        }
        Write-Host "  Unknown choice."
    }
}

if ($Action) {
    Invoke-Action $Action @(Get-Targets)
    return
}

while ($true) {
    $targets = @(Get-Targets)
    foreach ($app in $config) {
        Write-Host ""
        Write-Host "  $($app.AppName)"
        foreach ($item in $targets | Where-Object { $_.App -eq $app }) {
            $state = if (Test-Path -LiteralPath $item.Source) { "" } else { " (missing)" }
            if ($item.SourceText -eq $item.Label) {
                Write-Host "    $($item.Label)$state"
            }
            else {
                Write-Host "    $($item.SourceText)$state  ->  $($item.Label)"
            }
        }
    }
    Write-Host ""
    Write-Host "  1 - Full release.  Sign, then build the zip and the cabinet from scratch."
    Write-Host "  2 - Sign only.     Copy over the target and sign it."
    Write-Host "  3 - Zip only.      Rebuild the zip when the content changed."
    Write-Host "  4 - Cab only.      Rebuild the cabinet and sign it."
    Write-Host "  5 - Copy only.     Copy over the target without signing."
    Write-Host "  Q - Quit"
    Write-Host ""
    $choice = Read-Host "  Choice"
    if ($choice -eq "Q") {
        return
    }
    $name = switch ($choice) {
        "1" { "All" }
        "2" { "Sign" }
        "3" { "Zip" }
        "4" { "Cab" }
        "5" { "Copy" }
        default { "" }
    }
    if (-not $name) {
        Write-Host "  Unknown choice."
        continue
    }
    $selected = Select-Targets
    if ($selected.Count -gt 0) {
        Invoke-Action $name $selected
    }
}
