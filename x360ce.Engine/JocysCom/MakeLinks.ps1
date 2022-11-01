<#
.SYNOPSIS

    Creating multiple hard links to the contents of the original file makes the same file appear to "exist in different locations".
    This allows editing "several" files at once. Same as property name refactoring in Visual Studio but with the file.

    All file names are just hard links (shortcuts) to the file contents on disk.
    All file name links must be removed for the contents of the file to be released as free disk space.
    All Visual Studio projects can reside in different source code controls and repositories.
    Hard links are used so that source controls are not aware of them. For other developers, linke files look like normal files.
    Note: There is no way to determine which file entry is the original when files are hardlinked.

    Original (maintaned by the owner)       Copies
    ---------------------------------       -----------------------------------------
    \Projects\Jocys.com\Class Library       \Projects\Company A\Product A\Jocys.com
        [linked file] <-----------------------> [linked file]
                                          |     MakeLinks.ps1    
                                          |
                                          |  \Projects\Company B\Product B\Jocys.com
                                          |---> [linked file]
                                          |     MakeLinks.ps1
                                          |
                                          |  \Projects\Company C\Product C\Jocys.com
                                          |---> [linked file]
                                          |     MakeLinks.ps1
                                          #
                            [file content on the disk]

.NOTES
    Author:     Evaldas Jocys <evaldas@jocys.com>
    Modified:   2022-10-31
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
# Load code behind.
$code = [File]::ReadAllText("$current.cs");
Add-Type -TypeDefinition $code -Language CSharp;
# ----------------------------------------------------------------------------
Remove-Variable $code

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
# Symbolic link types:
#
#   SoftLink - directories and files. Acts like shortcut to an exiting directory/file.
#              Deleting the target directory/file will cause SoftLinks to stop working.
#   HardLink - directories and files.
#              Points to the same file content on the disk. All hard inks must be deleted
#              for the contents of the file to be released as free disk space.
#   Junction - directories only. Don'o't support UNC paths.
#
# All hardlinks of the file can be listed with command: fsutil hardlink list <file> 
# Example: fsutil hardlink list "c:\windows\explorer.exe"
# ----------------------------------------------------------------------------
function MakeSoftLink {
    param([string]$link, [string]$target);
    #----------------------------------------------------------
    $linkType = (Get-Item $link).LinkType;
     if ([File]::Exists($target) -eq $false){
    	Write-Host "Error: $link - missing link target file: $target" -ForegroundColor Red;
    }
    elseif ($linkType -eq "SymbolicLink"){
        Write-Host "OK:    $link" -ForegroundColor DarkGray;
        return;
    }
    Remove-Item $link;
    $result = New-Item -Path $link -ItemType SymbolicLink -Value $target;
    # Update time of the link file to match the target file.
    [MakeLinks]::UpdateLinkTimeFromFile($link, $target);
    Write-Host "Link:  $link";
}
# ----------------------------------------------------------------------------
function MakeHardLink {
    param([string]$link, [string]$target);
    #----------------------------------------------------------
    $linkType = (Get-Item $link).LinkType;
    # If target (original) file doesn't exists then...
    if ([File]::Exists($target) -eq $false){
    	Write-Host "Error: $link - missing link target file: $target" -ForegroundColor Red;
    }
    # If the file is not hard linked (only one file points to the same content on disk) then...
    # $isHardLinked = ((fsutil hardlink list $linkType).count -gt 1);
    elseif ($linkType -ne "HardLink"){
        # Remove file and create link.
        Remove-Item $link;
        $result = New-Item -Path $link -ItemType HardLink -Value $target;
    	Write-Host "Link:  $link";
    }
    # If file is hard linked but to the wrong file then...
    elseif ([MakeLinks]::IsHardLinked($link, $target) -eq $false){
        # Remove file and create link. For example, original file was recreated.
        Remove-Item $link;
        $result = New-Item -Path $link -ItemType HardLink -Value $target;
        Write-Host "Fix:   $link" -ForegroundColor Red;
    } else {
    	Write-Host "OK:    $link" -ForegroundColor DarkGray;
	}
}
# ----------------------------------------------------------------------------
# Execute
# ----------------------------------------------------------------------------
$libraryPath = FindExistingPath @("c:\Projects\Jocys.com\Class Library", "d:\Projects\Jocys.com\Class Library");
if ($null -eq $libraryPath){
    Write-Host "Library Path: Not Found!";
    return;
}
Write-Host "Library Path: $libraryPath";
Write-Host;
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
    MakeHardLink $linkRelativeName $target;
    #MakeSoftLink $linkRelativeName $target;
    #Copy-Item $target -Destination $linkRelativeName;
}
Write-Host;
# ----------------------------------------------------------------------------
pause;
