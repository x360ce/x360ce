<#
.SYNOPSIS
    Removes temporary bin and obj folders.
	Kill and clear IIS Express configuration.
	Removes temporary and user specific solution files.
.NOTES
    Author:     Evaldas Jocys <evaldas@jocys.com>
    Modified:   2021-09-20
.LINK
    http://www.jocys.com
#>
# ----------------------------------------------------------------------------
# Get current command path.
[string]$current = $MyInvocation.MyCommand.Path
# Get calling command path.
[string]$calling = @(Get-PSCallStack)[1].InvocationInfo.MyCommand.Path
# If executed directly then...
if ($calling -ne "") {
    $current = $calling
}

$file = Get-Item $current
# Working folder.
$wdir = $file.Directory.FullName;
# ----------------------------------------------------------------------------
Function KillProcess
{
	# Parameters.
	param($pattern);
	# Function.
	$procs = Get-Process;
	foreach ($proc in $procs)
	{
		if ($proc.Path)
		{
			$item = Get-Item $proc.Path;
			if ($item.Name -eq $pattern)
			{
				Write-Output "  Stopping process: $($item.Name)";
				Stop-Process $proc;
			}
		}
	}	
}
# ----------------------------------------------------------------------------
Function RemoveDirectories
{
	# Parameters.
	Param ($pattern, $mustBeInProject)
	# Function.
	$items = Get-ChildItem $wdir -Filter $pattern -Recurse -Force | Where-Object {$_ -is [System.IO.DirectoryInfo]};
	foreach ($item in $items)
	{
		if ($mustBeInProject){
			# Get parent folder.
			[System.IO.DirectoryInfo] $parent = $item.Parent;
			$projects = $parent.GetFiles("*.*proj", [System.IO.SearchOption]::TopDirectoryOnly);
			# If parent folder do not contain *.*proj file then...
			if ($projects.length -eq 0)
			{
				Write-Output "  Skip:   $($item.FullName)";
				$global:skipCount += 1;
				continue;
			}
			Write-Output "  Remove: $($item.FullName)";
			$global:removeCount += 1;
			Remove-Item -LiteralPath $item.FullName -Force -Recurse
		}
	}
}
# ----------------------------------------------------------------------------
function RemoveSubFoldersAndFiles 
{
	# Parameters.
	param($path, $onlyDirs);
	# Function.
	$dirs = Get-Item $path -ErrorAction SilentlyContinue;
	foreach ($dir in $dirs)
	{
		Write-Output "  $($dir.FullName)";
		$items = Get-ChildItem -LiteralPath $dir.FullName -Force;
		if ($onlyDirs -eq $true)
		{
			$items = $items | Where-Object {$_ -is [System.IO.DirectoryInfo]};
		}
		foreach ($item in $items)
		{
			Write-Output "  - $($item.Name)";
			Remove-Item -LiteralPath $item.FullName -Force -Recurse;
		}
	}
}
# ----------------------------------------------------------------------------
Function RemoveFiles
{
	# Parameters.
	param($pattern);
	# Function.
	$items = Get-ChildItem $wdir -Filter $pattern -Recurse -Force | Where-Object {$_ -is [System.IO.FileInfo]};
	foreach ($item in $items)
	{
	  Write-Output $item.FullName;
	  Remove-Item -LiteralPath $item.FullName -Force;
	}
}
# ----------------------------------------------------------------------------
function ClearBuilds
{
	$global:removeCount = 0;
	$global:skipCount = 0;
	Write-Host "Clear Build Folders";
	# Remove 'obj' folders first, because it can contain 'bin' inside.
	RemoveDirectories "obj" $true;
	RemoveDirectories "bin" $true;
	#Write-Output "Skipped: $global:skipCount, Removed: $global:removeCount";
}
# ----------------------------------------------------------------------------
function ClearCache
{
	Write-Host "Clear IIS Express configuration and remove temp files";
	KillProcess "iisexpress.exe";
	KillProcess "iisexpresstray.exe";
	RemoveSubFoldersAndFiles "$($env:USERPROFILE)\Documents\My Web Sites" $true;
	Write-Host "Remove temp directories";
	RemoveDirectories ".vs";
	Write-Host "Remove temp files";
	RemoveFiles "*.dbmdl";
	RemoveFiles "*.user";
	RemoveFiles "*.suo";
}
# ----------------------------------------------------------------------------
function ClearCacheVS
{

	# Fix Visual Studio "Windows Form Designer: Could not load file or assembly" designer error by 
	# clearing temporary compiled assemblies inside dynamically created folders by Visual Studio.
	# Visual studio must be closed for this batch script to succeed.
	#
	Write-Host "Clear Visual Studio Cache";
	RemoveSubFoldersAndFiles "$($env:USERPROFILE)\AppData\Local\Microsoft\VisualStudio\12.*\ProjectAssemblies";
	RemoveSubFoldersAndFiles "$($env:USERPROFILE)\AppData\Local\Microsoft\VisualStudio\13.*\ProjectAssemblies";
	RemoveSubFoldersAndFiles "$($env:USERPROFILE)\AppData\Local\Microsoft\VisualStudio\14.*\ProjectAssemblies";
	RemoveSubFoldersAndFiles "$($env:USERPROFILE)\AppData\Local\Microsoft\VisualStudio\15.*\ProjectAssemblies";
	RemoveSubFoldersAndFiles "$($env:USERPROFILE)\AppData\Local\Microsoft\VisualStudio\16.*\ProjectAssemblies";
	Write-Host "Clear IIS Express Cache";
	RemoveSubFoldersAndFiles "$($env:LOCALAPPDATA)\Temp\iisexpress";
	RemoveSubFoldersAndFiles "$($env:LOCALAPPDATA)\Temp\Temporary ASP.NET Files";
	Write-Host "Clear Xamarin Cache";
	RemoveSubFoldersAndFiles "$($env:LOCALAPPDATA)\Temp\Xamarin";
	RemoveSubFoldersAndFiles "$($env:LOCALAPPDATA)\Xamarin\iOS\Provisioning";
	Write-Host "Clear .NET Framework Cache";
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
}
# ----------------------------------------------------------------------------
function ShowMainMenu
{
    $m = "";
    do {
        # Clear screen.
        Clear-Host;
        Write-Host;
		Write-Host "    1 - Clear builds";
		Write-Host "    2 - Clear IIS and temp files";
		Write-Host "    3 - Clear Visual Studio cache";
        Write-Host;
		Write-Host "    0 - Clear all";
        Write-Host;
        $m = Read-Host -Prompt "Type option and press ENTER to continue";
        Write-Host;
        # Options:
        IF ("$m" -eq "0" -or "$m" -eq "1") { ClearBuilds; };
        IF ("$m" -eq "0" -or "$m" -eq "2") { ClearCache; };
        IF ("$m" -eq "0" -or "$m" -eq "3") { ClearCacheVS; };
        # If option was choosen.
        IF ("$m" -ne "") {
            pause;
        }
    } until ("$m" -eq "");
    return $m;
}
# ----------------------------------------------------------------------------
# Execute.
# ----------------------------------------------------------------------------
ShowMainMenu;