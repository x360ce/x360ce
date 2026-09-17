<#
.SYNOPSIS
    Writes the release manifest the program's updater reads.
.DESCRIPTION
    Produces latest.json beside the application zip. The program fetches it from
    the newest release on GitHub, compares the version with its own, and checks
    the zip it then downloads against the size and SHA-256 written here. The
    manifest is the single description of a release, so it is derived from the
    zip itself: the version is read from the x360ce.exe inside it, never typed.

    A release announcing one version and shipping another installs nowhere, so
    when -ExpectedVersion is given the script stops if the zip's version differs.
    The release script passes the version from AssemblyInfo, which catches a zip
    built before the version was bumped.

    Attach latest.json to the GitHub release beside x360ce.zip and title the
    release exactly as printed: "X360CE {version}". The updater reads the titles
    when a release has no manifest.
.PARAMETER ZipPath
    The application zip. Defaults to Files.v4/x360ce.zip beside this script.
.PARAMETER ExpectedVersion
    The version the zip must carry; the script fails when it does not.
.PARAMETER OutPath
    Where to write the manifest. Defaults to latest.json beside the zip.
.PARAMETER Notes
    Address written into the manifest for the release notes.
.EXAMPLE
    PS> .\App_5_Manifest.ps1 -ExpectedVersion 4.22.9.0
.OUTPUTS
    [string] The path of the manifest written.
#>
[CmdletBinding()]
param(
    [string]$ZipPath = (Join-Path $PSScriptRoot "Files.v4\x360ce.zip"),
    [string]$ExpectedVersion,
    [string]$OutPath,
    [string]$Notes = "https://github.com/x360ce/x360ce/releases/latest"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (-not (Test-Path -LiteralPath $ZipPath)) { throw "Zip not found: $ZipPath" }
$zipFile = Get-Item -LiteralPath $ZipPath
if (-not $OutPath) { $OutPath = Join-Path $zipFile.DirectoryName "latest.json" }

# The version comes from the file that will run, read out of the zip that will ship.
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("x360ce-manifest-" + [System.Guid]::NewGuid().ToString("N") + ".exe")
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
$manifest = [ordered]@{
    version   = $version
    file      = $zipFile.Name
    size      = $zipFile.Length
    sha256    = $sha256
    published = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
    notes     = $Notes
}
# UTF-8 without a signature: a byte order mark is not JSON.
[System.IO.File]::WriteAllText($OutPath, ($manifest | ConvertTo-Json), (New-Object System.Text.UTF8Encoding $false))

Write-Host "Manifest: $OutPath"
Write-Host "Release title: X360CE $version"
Write-Host "Assets: $($zipFile.Name), $([System.IO.Path]::GetFileName($OutPath))"
return $OutPath
