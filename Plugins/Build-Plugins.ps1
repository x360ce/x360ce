<#
.SYNOPSIS
    Builds each plugin in this folder into one DLL.
.DESCRIPTION
    A plugin is a folder holding one .cs file named like it: MyDevice\MyDevice.cs. Each is built with
    dotnet build into Output\MyDevice\MyDevice.dll, the layout of the Plugins folder in the x360ce
    settings folder, so a built plugin's folder is copied there whole. Needs the .NET SDK 10 or newer.
.PARAMETER Output
    The folder the plugins' folders are written into. Default: bin beside this script.
.PARAMETER Configuration
    Debug or Release. Default: Release.
.PARAMETER Contract
    The x360ce.Plugins.dll to build against. Default: the one beside this script, which
    x360ce.exe /PluginKit writes; in the x360ce source, the one built from ..\PluginApi.
.EXAMPLE
    .\Build-Plugins.ps1
    Builds every plugin into bin; copy bin\MyDevice into C:\ProgramData\X360CE\Plugins.
#>
[CmdletBinding()]
param(
    [string]$Output = (Join-Path $PSScriptRoot 'bin'),
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$Contract
)

$ErrorActionPreference = 'Stop'
if (-not $Contract) {
    $Contract = Join-Path $PSScriptRoot 'x360ce.Plugins.dll'
    $project = Join-Path $PSScriptRoot '..\PluginApi\x360ce.Plugins.csproj'
    if (-not (Test-Path -LiteralPath $Contract) -and (Test-Path -LiteralPath $project)) {
        # Only the contract: the project also builds x360ce's own plugins, which this script is about to build.
        $Contract = & dotnet build $project -c $Configuration -nologo -v:q -t:Build -getProperty:TargetPath -p:BuildEmbeddedPlugins=false
        if ($LASTEXITCODE -ne 0) { throw 'The x360ce.Plugins contract did not build.' }
    }
}
if (-not (Test-Path -LiteralPath $Contract)) {
    throw "$Contract is missing. Write the plugin kit with x360ce.exe /PluginKit=<folder>, or name the DLL with -Contract."
}
foreach ($folder in Get-ChildItem -LiteralPath $PSScriptRoot -Directory) {
    $source = Join-Path $folder.FullName ($folder.Name + '.cs')
    if (-not (Test-Path -LiteralPath $source)) { continue }
    $arguments = @('build', $source, '-c', $Configuration, '-o', (Join-Path $Output $folder.Name), '-nologo', '-v:q',
        "-p:X360cePluginsDll=$Contract")
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "The $($folder.Name) plugin did not build." }
}
