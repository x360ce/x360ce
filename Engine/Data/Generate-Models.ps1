<#
.SYNOPSIS
    Regenerates the entity classes and the database context from the local x360ce database.
.DESCRIPTION
    Database-first: SQL Server is the source of truth, and this script makes the C# classes match it. A column the
    database has and Data\x360ce.Data.sqlproj does not is a defect of the SQL project.
    Runs the Jocys.com SQL Model Generator with x360ce.ModelGenerator.json, which sits next to this script. The entity
    classes go to Model\ and carry no Entity Framework reference, so the programs build without it; the hand-written
    partial classes in this folder extend them through the shared namespace. The context goes to Web\Data\, the only
    project that queries the database: Entity Framework 6 under NETFRAMEWORK, Entity Framework Core otherwise.
    Build the engine, the programs and the web site afterwards, and run EntityFormatTest: the settings files and the
    web service must keep what they write.
.PARAMETER ExePath
    Path to the published generator (single-file EXE with the SQL client bundled).
.EXAMPLE
    PS C:\> .\Engine\Data\Generate-Models.ps1
.OUTPUTS
    [void]
#>
[CmdletBinding()]
param(
    [string]$ExePath = "\Projects\Jocys.com\Sql\ModelGenerator\bin\publish\win-x64\JocysCom.Sql.ModelGenerator.exe"
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path $ExePath)) { throw "Generator not found: $ExePath" }
$config = Join-Path $PSScriptRoot 'x360ce.ModelGenerator.json'
if (-not (Test-Path $config)) { throw "Config not found: $config" }

# The generator resolves OutputPath and ContextOutputPath relative to the working folder, so run it from here.
Push-Location $PSScriptRoot
try {
    Write-Host "Generating from $(Split-Path $config -Leaf)..."
    & $ExePath $config
    if ($LASTEXITCODE -ne 0) { throw "Generator failed (exit $LASTEXITCODE)." }
} finally { Pop-Location }
