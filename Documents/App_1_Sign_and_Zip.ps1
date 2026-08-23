<#
.SYNOPSIS
    Copies, signs and zips application release files.
.DESCRIPTION
    All project specific values are read from the JSON file which has the same
    name as this script, so the script itself is copied between projects
    unchanged. Only the JSON file is edited.

    The JSON file holds an array of applications, each with its own signature
    name, output folder and list of files:

      AppName    - the name written into the signature.
      AppLink    - the link written into the signature.
      SignModule - the shared signing module.
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

    Paths are relative to the folder of this script, except "Zip" which is a
    plain name inside "FilesDir". Two files may name the same Target, which
    packs one signed file into more than one zip. It is copied and signed once.

    Three steps are offered:

      Sign - copy every source over its target and sign the target.
      Zip  - build the zip of every file which names one.
      Copy - copy every source over its target without signing.

    When more than one file is configured the step asks which one to work on,
    or all of them.

    The full release, menu item 1 and the "All" action, deletes the zip before
    compressing so it never holds a file left over from an earlier release. The
    separate "Zip" step keeps the existing zip when nothing changed.
.PARAMETER Action
    Runs one action over every file and exits instead of showing the menu.
.EXAMPLE
    PS> .\App_1_Sign_and_Zip.ps1
    Shows the menu.
.EXAMPLE
    PS> .\App_1_Sign_and_Zip.ps1 All
    Signs and zips without asking. Used when another script drives the release.
.NOTES
    Paths inside the JSON file can use forward or back slashes. Signing requires
    the USB token or card reader supported by the signing module.
#>
param(
    [Parameter(Position = 0)]
    [ValidateSet("Sign", "Zip", "Copy", "All")]
    [string]$Action
)

$ErrorActionPreference = "Stop"

#------------------------------------------------------------------------------
# Configuration.
#------------------------------------------------------------------------------

$configPath = [System.IO.Path]::ChangeExtension($PSCommandPath, ".json")
$config = @(Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json)

# Importing the module changes the current folder to the folder of this script,
# so keep the import here rather than inside a function.
foreach ($module in ($config.SignModule | Select-Object -Unique)) {
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
            [PSCustomObject]@{
                App        = $app
                SourceText = $sourceText
                Source     = Resolve-Path2 $sourceText
                Target     = Resolve-Path2 $targetText
                Label      = $label
                Zip        = $zip
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

function Invoke-Sign {
    param($Targets)
    if (-not (Get-Command Sign-File -ErrorAction SilentlyContinue)) {
        Write-Host "Signing module not found: $($config.SignModule | Select-Object -Unique)"
        return
    }
    Copy-Sources $Targets
    # Detect the signing hardware once per run.
    $global:SignProfileCache = $null
    $done = @{}
    foreach ($item in $Targets) {
        if ($done.ContainsKey($item.Target)) {
            continue
        }
        $done[$item.Target] = $true
        if (-not (Test-Path -LiteralPath $item.Target)) {
            Write-Host "Nothing to sign: $($item.Target)"
            continue
        }
        $global:AppName = $item.App.AppName
        $global:AppLink = $item.App.AppLink
        Write-Host "Signing file: $($item.Target)"
        Sign-File -FilePath $item.Target
        # Brief delay so the USB token can process the next request.
        Start-Sleep -Seconds 2
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
        & $zipScript `
            -sourceDir ([System.IO.Path]::GetDirectoryName($item.Target)) `
            -destFile $item.Zip `
            -searchPattern ([System.IO.Path]::GetFileName($item.Target)) `
            -IgnoreEmptyFolders $true
    }
}

function Invoke-Action {
    param([string]$Name, $Targets)
    switch ($Name) {
        "Sign" { Invoke-Sign $Targets }
        "Zip" { Invoke-Zip $Targets }
        "Copy" { Copy-Sources $Targets }
        "All" { Invoke-Sign $Targets; Invoke-Zip $Targets -Fresh }
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
    Write-Host "  1 - Sign and zip.  Sign, then build the zip from scratch."
    Write-Host "  2 - Sign only.     Copy over the target and sign it."
    Write-Host "  3 - Zip only.      Rebuild the zip when the content changed."
    Write-Host "  4 - Copy only.     Copy over the target without signing."
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
        "4" { "Copy" }
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
