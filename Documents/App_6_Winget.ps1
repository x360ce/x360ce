<#
.SYNOPSIS
    Writes the winget manifest for a published v4 release, checks it, and submits it when asked.
.DESCRIPTION
    winget installs x360ce as a portable program: it downloads the release's own
    x360ce.zip, checks it against the SHA-256 GitHub publishes for it, unpacks
    x360ce.exe and adds the command x360ce. ViGEm.ViGEmBus, the driver the emulated
    controllers need, is installed first as a dependency.

    Run it once the GitHub release is published: the manifest names the zip's address
    and hash, which exist only then. The release is found by its title,
    "X360CE {version}", as the program's updater finds it, so winget installs the
    zip the updater would.

    The version, installer and en-US locale files are written to
    Files.winget\{PackageIdentifier}\{version} beside this script and checked with
    winget validate. The names, the description and the copyright come from
    App.v4\Properties\AssemblyInfo.cs, as the program shows them. Nothing leaves the
    computer unless -Submit is given: then wingetcreate opens the pull request to
    microsoft/winget-pkgs, under the GitHub account it asks to sign in with.
.PARAMETER Version
    The release version. Defaults to AssemblyFileVersion in App.v4\Properties\AssemblyInfo.cs.
.PARAMETER Submit
    Opens the pull request to microsoft/winget-pkgs with wingetcreate, which
    winget install Microsoft.WingetCreate installs.
.EXAMPLE
    PS> .\App_6_Winget.ps1 -Version 4.27.53.0
    Writes and checks the manifest for 4.27.53.0.
.EXAMPLE
    PS> .\App_6_Winget.ps1 -Submit
    Writes, checks and submits the manifest for the version in AssemblyInfo.
.OUTPUTS
    [string] The folder holding the manifest.
#>
[CmdletBinding()]
param(
    [string]$Version,
    [switch]$Submit
)

$ErrorActionPreference = "Stop"

$packageId = "Jocys.x360ce"
$manifestVersion = "1.12.0"
$repository = "https://github.com/x360ce/x360ce"
$root = Split-Path -Parent $PSScriptRoot
$assemblyInfo = Get-Content -LiteralPath (Join-Path $root "App.v4\Properties\AssemblyInfo.cs") -Raw

function Get-AssemblyValue {
    param([string]$Name)
    $match = [regex]::Match($assemblyInfo, "Assembly$Name\(""([^""]*)""\)")
    if (-not $match.Success) { throw "Assembly$Name not found in AssemblyInfo.cs" }
    return $match.Groups[1].Value
}

if (-not $Version) { $Version = Get-AssemblyValue "FileVersion" }

# The newest releases, read as the updater reads them: one request, no account.
$title = "X360CE $Version"
$releases = Invoke-RestMethod -Uri "https://api.github.com/repos/x360ce/x360ce/releases?per_page=30" `
    -Headers @{ "User-Agent" = "x360ce-release"; "Accept" = "application/vnd.github+json" }
$release = $releases | Where-Object { $_.name -eq $title -and -not $_.draft -and -not $_.prerelease } | Select-Object -First 1
if (-not $release) { throw "No published release is titled '$title'. Publish it first, titled as App_5_ReleaseTitle.ps1 prints." }
$asset = $release.assets | Where-Object { $_.name -eq "x360ce.zip" } | Select-Object -First 1
if (-not $asset) { throw "The release '$title' carries no x360ce.zip." }
if ($asset.digest -notmatch '^sha256:([0-9a-f]{64})$') { throw "GitHub gives no SHA-256 for x360ce.zip of '$title'." }
$sha256 = $Matches[1].ToUpperInvariant()
$releaseDate = ([datetime]$release.published_at).ToUniversalTime().ToString("yyyy-MM-dd")

$folder = Join-Path $PSScriptRoot "Files.winget\$packageId\$Version"
New-Item -ItemType Directory -Force -Path $folder | Out-Null

function Write-Manifest {
    param([string]$Type, [string]$FileName, [string[]]$Lines)
    $text = @(
        "# yaml-language-server: `$schema=https://aka.ms/winget-manifest.$Type.$manifestVersion.schema.json"
        "PackageIdentifier: $packageId"
        "PackageVersion: $Version"
    ) + $Lines + @(
        "ManifestType: $Type"
        "ManifestVersion: $manifestVersion"
    )
    [System.IO.File]::WriteAllText((Join-Path $folder $FileName), (($text -join "`n") + "`n"), (New-Object System.Text.UTF8Encoding($false)))
}

Write-Manifest "version" "$packageId.yaml" @(
    "DefaultLocale: en-US"
)

# x360ce.exe is built for x86, and runs on x64 and ARM64 Windows as well.
Write-Manifest "installer" "$packageId.installer.yaml" @(
    "InstallerType: zip"
    "NestedInstallerType: portable"
    "NestedInstallerFiles:"
    "- RelativeFilePath: x360ce.exe"
    "  PortableCommandAlias: x360ce"
    "Dependencies:"
    "  PackageDependencies:"
    "  - PackageIdentifier: ViGEm.ViGEmBus"
    "ReleaseDate: $releaseDate"
    "Installers:"
    "- Architecture: x86"
    "  InstallerUrl: $($asset.browser_download_url)"
    "  InstallerSha256: $sha256"
)

# The first sentence of the description; the rest speaks to AI agents.
$description = (Get-AssemblyValue "Description") -replace '^(.*?\.)\s.*$', '$1'
Write-Manifest "defaultLocale" "$packageId.locale.en-US.yaml" @(
    "PackageLocale: en-US"
    "Publisher: $(Get-AssemblyValue "Company")"
    "PublisherUrl: https://www.jocys.com"
    "PublisherSupportUrl: $repository/issues"
    "PackageName: $(Get-AssemblyValue "Product")"
    "PackageUrl: $repository"
    "License: LGPL-3.0"
    "LicenseUrl: $repository/blob/master/App.v4/Documents/License.txt"
    "Copyright: $(Get-AssemblyValue "Copyright")"
    "ShortDescription: $description"
    "Moniker: x360ce"
    "Tags:"
    "- controller"
    "- gamepad"
    "- joystick"
    "- wheel"
    "- xbox"
    "- xinput"
    "ReleaseNotesUrl: $($release.html_url)"
)

& winget validate --manifest $folder --disable-interactivity
if ($LASTEXITCODE -ne 0) { throw "winget validate refused the manifest in $folder" }

if ($Submit) {
    $wingetcreate = Get-Command wingetcreate -ErrorAction SilentlyContinue
    if (-not $wingetcreate) { throw "wingetcreate is not installed. Install it with: winget install Microsoft.WingetCreate" }
    # No --token: wingetcreate asks to sign in, so no token is written to the console or a log.
    & $wingetcreate.Source submit --prtitle "New version: $packageId version $Version" $folder
    if ($LASTEXITCODE -ne 0) { throw "wingetcreate did not submit $folder" }
}

$folder
