<#
.SYNOPSIS
	Make/Fix soft file links to shared files.
.NOTES
    Author:     Evaldas Jocys <evaldas@jocys.com>
    Modified:   2022-08-11
.LINK
    http://www.jocys.com
#>
using namespace System;
using namespace System.IO;
using namespace System.Security.Principal;
# ----------------------------------------------------------------------------
# Get current command path.
[string]$current = $MyInvocation.MyCommand.Path;
# Get calling command path.
[string]$calling = @(Get-PSCallStack)[1].InvocationInfo.MyCommand.Path;
# If executed directly then...
if ($calling -ne "") {
    $current = $calling;
}
#----------------------------------------------------------------------------
# Run as Administrator is required to make hard or soft file links.
#----------------------------------------------------------------------------
# Run as administrator.
if (-NOT ([WindowsPrincipal][WindowsIdentity]::GetCurrent()).IsInRole([WindowsBuiltInRole] "Administrator"))
{  
    # Preserve working directory.
    $arguments = "`"cd '$pwd'; & '$current';`"";
    Start-Process powershell -Verb runAs -ArgumentList $arguments;
    break;
}
# ----------------------------------------------------------------------------
[FileInfo]$file = New-Object FileInfo($current);
# Set public parameters.
$global:scriptName = $file.Basename;
$global:scriptPath = $file.Directory.FullName;
# Change current directory.
[Console]::WriteLine("Script Path: {0}", $scriptPath);
[Environment]::CurrentDirectory = $scriptPath;
Set-Location $scriptPath;
# ----------------------------------------------------------------------------
function FindExistingPath
{
    [OutputType([string])] param([string[]]$ps);
    #----------------------------------------------------------
    foreach ($p in $ps) {
        if (Test-Path -Path $p) {
            return $p;
        }
    }
    return $null;
}
# ----------------------------------------------------------------------------
#soft links (also called symlinks, or symbolic links)
#hard links
#junctions (a type of soft link only for directories)
function MakeLink ($target, $link) {
    # Deleting the target will cause soft links to stop working.
    # What it points to is gone.
    # Hard links will keep working until you delete the hard link itself.
    # The hard link acts just like the original file, because for all intents and purposes, it is the original file.
}

# ----------------------------------------------------------------------------
$libraryPath = FindExistingPath @("c:\Projects\Jocys.com\Class Library", "d:\Projects\Jocys.com\Class Library");
if ($null -eq $libraryPath){
    Write-Host "Library Path: Not Found!";
    return;
}

Write-Host "Library Path: $libraryPath";

[DirectoryInfo]$dir = New-Object DirectoryInfo ".";
$items = Get-ChildItem "." -Recurse -Force | Where-Object {$_ -is [FileInfo]};
foreach ($item in $items)
{
    #{ $_.Attributes -match "ReparsePoint" }
    $fullName = $item.FullName;
    $isLink = ((Get-Item $fullName).Attributes.ToString() -match "ReparsePoint");
    $linkType = (Get-Item "$($item.FullName)").LinkType;
    # Get relative name.
    $relativeName = $fullName.Substring($dir.FullName.Length + 1);
    # File must be in directory.
    if (!$relativeName.Contains("\")){
        continue;
    }
    Write-Host "  $relativeName";
    # If normal file then remove.
    if (!$isLink){
        Remove-Item $fullName;
        $target = "$libraryPath\$relativeName";
        New-Item -Path $fullName -ItemType SymbolicLink -Value $target;

    }
}
# ----------------------------------------------------------------------------
# Execute.
# ----------------------------------------------------------------------------
pause;