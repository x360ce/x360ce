<#
.SYNOPSIS
	Make/Fix soft or hard file links to shared files.
.NOTES
    Author:     Evaldas Jocys <evaldas@jocys.com>
    Modified:   2022-10-25
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
[Console]::WriteLine("Current Path: {0}", $scriptPath);
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
# Deleting the target will cause soft links to stop working.
# What it points to is gone.
# Hard links will keep working until you delete the hard link itself.
# The hard link acts just like the original file, because for all intents and purposes, it is the original file.
# ----------------------------------------------------------------------------
function MakeSoftLink {
    param([string]$link, [string]$target);
    #----------------------------------------------------------
    $linkType = (Get-Item $link).LinkType;
    if ($linkType -eq "SymbolicLink"){
        Write-Host "Skip Link: $link" -ForegroundColor DarkGray;
        return;
    }
    Remove-Item $link;
    $result = New-Item -Path $link -ItemType SymbolicLink -Value $target;
    Write-Host "Make Link: $link";
}
# ----------------------------------------------------------------------------
function MakeHardLink {
    param([string]$link, [string]$target);
    #----------------------------------------------------------
    $linkType = (Get-Item $link).LinkType;
    if ($linkType -eq "HardLink"){
        Write-Host "Skip Link: $link" -ForegroundColor DarkGray;
        return;
    }
    Remove-Item $link;
    # Note: When hardlinked then there is no way to determine which file entry is the original.
    $result = New-Item -Path $link -ItemType HardLink -Value $target;
    Write-Host "Make Link: $link";
}
# ----------------------------------------------------------------------------
$libraryPath = FindExistingPath @("c:\Projects\Jocys.com\Class Library", "d:\Projects\Jocys.com\Class Library");
if ($null -eq $libraryPath){
    Write-Host "Library Path: Not Found!";
    return;
}
Write-Host "Library Path: $libraryPath";
Write-Host;;

$items = Get-ChildItem "." -Recurse -Force | Where-Object {$_ -is [FileInfo]};
foreach ($item in $items)
{
    $linkFullName = $item.FullName;
    # Get relative name.
    $linkRelativeName = $linkFullName.Substring($scriptPath.Length + 1);
    # If file is not in directory then continue.
    if (!$linkRelativeName.Contains("\")){
        continue;
    }
    # Original file for link to target.
    $target = "$libraryPath\$linkRelativeName";
    #MakeSoftLink $linkRelativeName $target;
    MakeHardLink $linkRelativeName $target;
    #Copy-Item $target -Destination $linkRelativeName;
}
# ----------------------------------------------------------------------------
# Execute.
# ----------------------------------------------------------------------------
pause;