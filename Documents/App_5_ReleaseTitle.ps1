<#
.SYNOPSIS
    Checks that the application zip carries the version being released and prints
    the release title the program's updater reads.
.DESCRIPTION
    The updater finds a release by its title, "X360CE {version}", and downloads
    that release's own x360ce.zip, checking it against the size and SHA-256 that
    GitHub publishes for the asset. The version is read from the x360ce.exe inside
    the zip, never typed, so the title printed is the version that will run.

    A release announcing one version and shipping another installs nowhere, so
    when -ExpectedVersion is given the script stops if the zip's version differs.
    The release script passes the version from AssemblyInfo, which catches a zip
    built before the version was bumped.

    Title the GitHub release exactly as printed and attach x360ce.zip. Once it is
    uploaded, the release page shows the same SHA-256 as printed here.

    The updater takes the newest full release whose title carries its own major
    version, so pre-releases, drafts and v3 releases never reach v4 users.
.PARAMETER ZipPath
    The application zip. Defaults to Files.v4/x360ce.zip beside this script.
.PARAMETER ExpectedVersion
    The version the zip must carry; the script fails when it does not.
.EXAMPLE
    PS> .\App_5_ReleaseTitle.ps1 -ExpectedVersion 4.25.26.0
.OUTPUTS
    [string] The release title.
#>
[CmdletBinding()]
param(
    [string]$ZipPath = (Join-Path $PSScriptRoot "Files.v4\x360ce.zip"),
    [string]$ExpectedVersion
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (-not (Test-Path -LiteralPath $ZipPath)) { throw "Zip not found: $ZipPath" }
$zipFile = Get-Item -LiteralPath $ZipPath

# The version comes from the file that will run, read out of the zip that will ship.
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("x360ce-release-" + [System.Guid]::NewGuid().ToString("N") + ".exe")
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipFile.FullName)
try {
    $entry = $archive.Entries | Where-Object { $_.Name -eq "x360ce.exe" } | Select-Object -First 1
    if (-not $entry) { throw "The zip holds no x360ce.exe: $ZipPath" }
    [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $temp, $true)
}
finally { $archive.Dispose() }
try {
    $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($temp).FileVersion
}
finally { Remove-Item -LiteralPath $temp -Force -ErrorAction SilentlyContinue }

if (-not $version) { throw "x360ce.exe inside the zip carries no file version." }
if ($ExpectedVersion -and ([version]$version -ne [version]$ExpectedVersion)) {
    throw "The zip carries x360ce.exe $version but the release is $ExpectedVersion. Rebuild before publishing."
}

$sha256 = (Get-FileHash -LiteralPath $zipFile.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
$title = "X360CE $version"
Write-Host "Release title: $title"
Write-Host "Asset: $($zipFile.Name), $($zipFile.Length) bytes, SHA-256 $sha256"
return $title
