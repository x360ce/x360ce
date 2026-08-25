<#
.SYNOPSIS
    Build and run the x360ce test suite.
.DESCRIPTION
    Uses Visual Studio MSBuild rather than `dotnet test`. The .NET SDK cannot resolve the
    Microsoft.mshtml COM reference in x360ce.Engine, so `dotnet test` fails at compile time
    while VS MSBuild succeeds. The MSTest adapter is passed explicitly because the package
    does not copy it into a net462 output folder.
.PARAMETER Interactive
    Also run tests tagged ui-interactive. These launch the applications and need a desktop
    session, so they are excluded by default and must be skipped on a headless agent.
.EXAMPLE
    .\Tests\Run-Tests.ps1
    Runs everything except the interactive UI tests.
.EXAMPLE
    .\Tests\Run-Tests.ps1 -Interactive
    Runs the whole suite, including application launch tests.
#>
[CmdletBinding()]
param(
    [switch]$Interactive,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) { throw "vswhere.exe not found. Visual Studio is required." }

$msbuild = & $vswhere -latest -prerelease -find '**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw "MSBuild.exe not found." }
$vstest = & $vswhere -latest -prerelease -find '**\TestWindow\vstest.console.exe' | Select-Object -First 1
if (-not $vstest) { throw "vstest.console.exe not found." }

$project = Join-Path $PSScriptRoot 'x360ce.Tests.csproj'
& $msbuild $project -t:restore,build -p:Configuration=$Configuration -v:minimal -nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

# The adapter version must match the MSTest.TestAdapter PackageReference in the csproj.
$version = ([xml](Get-Content $project)).Project.ItemGroup.PackageReference |
    Where-Object { $_.Include -eq 'MSTest.TestAdapter' } | Select-Object -ExpandProperty Version
$adapter = Join-Path $env:USERPROFILE ".nuget\packages\mstest.testadapter\$version\buildTransitive\net462"
if (-not (Test-Path $adapter)) { throw "MSTest adapter $version not found at $adapter" }

$assembly = Join-Path $PSScriptRoot "bin\$Configuration\net462\x360ce.Tests.dll"
$arguments = @($assembly, "/TestAdapterPath:$adapter", '/Logger:console;verbosity=normal')
if (-not $Interactive) {
    $arguments += '/TestCaseFilter:TestCategory!=ui-interactive'
    Write-Host 'Skipping ui-interactive tests. Pass -Interactive to include them.' -ForegroundColor DarkGray
}

& $vstest @arguments
exit $LASTEXITCODE
