<#
.SYNOPSIS
    Clear Visual Studio cache files.
.NOTES
    Author:     Evaldas Jocys <evaldas@jocys.com>
    Modified:   2021-04-14
.LINK
    http://www.jocys.com
#>
#----------------------------------------------------------------------------
# Run as administrator.
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{   
    $arguments = "& '" + $myinvocation.mycommand.definition + "'";
    Start-Process powershell -Verb runAs -ArgumentList $arguments;
    break;
}
#----------------------------------------------------------------------------
# Get current command path.
[string]$current = $MyInvocation.MyCommand.Path;
# Get calling command path.
[string]$calling = @(Get-PSCallStack)[1].InvocationInfo.MyCommand.Path;
# If executed directly then...
if ($calling -ne "") {
    $current = $calling;
}
$file = Get-Item $current;
# Working folder.
$wdir = $file.Directory.FullName;
# ----------------------------------------------------------------------------
function RemoveSubFoldersAndFiles
{
	# Parameters.
	param($path);
	# Function.
	$dirs = Get-Item $path -ErrorAction SilentlyContinue;
	foreach ($dir in $dirs)
	{
		Write-Output "Clear: $($dir.FullName)";
		$items = Get-ChildItem -LiteralPath $dir.FullName -Force;
		foreach ($item in $items)
		{
			Write-Output "  - $($item.Name)";
			Remove-Item -LiteralPath $item.FullName -Force -Recurse;
		}
	}
}
# ----------------------------------------------------------------------------
# Fix Visual Studio "Windows Form Designer: Could not load file or assembly" designer error by 
# clearing temporary compiled assemblies inside dynamically created folders by Visual Studio.
# Visual studio must be closed for this batch script to succeed.
#
# Clear Visual Studio.
RemoveSubFoldersAndFiles "$($env:USERPROFILE)\AppData\Local\Microsoft\VisualStudio\12.*\ProjectAssemblies";
RemoveSubFoldersAndFiles "$($env:USERPROFILE)\AppData\Local\Microsoft\VisualStudio\13.*\ProjectAssemblies";
RemoveSubFoldersAndFiles "$($env:USERPROFILE)\AppData\Local\Microsoft\VisualStudio\14.*\ProjectAssemblies";
RemoveSubFoldersAndFiles "$($env:USERPROFILE)\AppData\Local\Microsoft\VisualStudio\15.*\ProjectAssemblies";
RemoveSubFoldersAndFiles "$($env:USERPROFILE)\AppData\Local\Microsoft\VisualStudio\16.*\ProjectAssemblies";
# Clear IIS Express.
RemoveSubFoldersAndFiles "$($env:LOCALAPPDATA)\Temp\iisexpress";
RemoveSubFoldersAndFiles "$($env:LOCALAPPDATA)\Temp\Temporary ASP.NET Files";
# Clear Xamarin.
RemoveSubFoldersAndFiles "$($env:LOCALAPPDATA)\Temp\Xamarin";
RemoveSubFoldersAndFiles "$($env:LOCALAPPDATA)\Xamarin\iOS\Provisioning";
# Clear .NET Framework.
RemoveSubFoldersAndFiles "$($env:SystemRoot)\Microsoft.NET\Framework\v4.0.30319\Temporary ASP.NET Files";
RemoveSubFoldersAndFiles "$($env:SystemRoot)\Microsoft.NET\Framework64\v4.0.30319\Temporary ASP.NET Files";
#
# Solution Explorer, right-click Solution
#	Properties -> Common Properties -> Debug Source Files -> clean "Do not look for these source files" box.
#
# Tools -> Options -> Projects and Solutions -> Build and Run
# Set "On Run, when build or deployment errors occur:" Prompt to Launch
#
# .EditorConfig file.
# "charset=utf-8" option can trigger "The source file is different from when the module was built." warning when debugging.
pause;
