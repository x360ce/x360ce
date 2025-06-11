<#
.SYNOPSIS
		Removes temporary bin and obj folders.
		Kill and clear IIS Express configuration.
		Removes temporary and user specific solution files.
.NOTES
    Author:     Evaldas Jocys <evaldas@jocys.com>
    Modified:   2025-02-26
.LINK
    http://www.jocys.com
#>
using namespace System
using namespace System.IO

# ----------------------------------------------------------------------------
# Run as administrator.
If (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {   
	# Pass arguments: script path, original user profile path and local app data path.
	$argumentList = "& '" + $MyInvocation.MyCommand.Path + "' '$($env:USERNAME)' '$($env:USERPROFILE)' '$($env:LOCALAPPDATA)'"
	Start-Process PowerShell.exe -Verb Runas -ArgumentList $argumentList
	return
}	

# Add original user profile path and optionally admin user profile path to process.
$userNames = @( $args[0])
if ($args[0] -ne $env:USERNAME) { $userNames += $env:USERNAME }

# Add original user profile path and optionally admin user profile path to process.
$userProfilePaths = @( $args[1])
if ($args[1] -ne $env:USERPROFILE) { $userProfilePaths += $env:USERPROFILE }

# Add original user app data path and optionally admin user app data path to process.
$localAppDataPaths = @( $args[2])
if ($args[2] -ne $env:LOCALAPPDATA) { $localAppDataPaths += $env:LOCALAPPDATA }

# ----------------------------------------------------------------------------
# Get current command path.
[string]$current = $MyInvocation.MyCommand.Path
# Get calling command path.
[string]$calling = @(Get-PSCallStack)[1].InvocationInfo.MyCommand.Path
# If executed directly then...
if ($calling -ne "") {
	$current = $calling
}
# ----------------------------------------------------------------------------
[FileInfo]$file = New-Object FileInfo($current)
# Set public parameters.
$global:scriptName = $file.Basename
$global:scriptPath = $file.Directory.FullName
# Change current directory.
Write-Host "Script Path:    $scriptPath"
[Environment]::CurrentDirectory = $scriptPath
Set-Location $scriptPath
# ----------------------------------------------------------------------------
# Shot which profiles will be affected.
foreach ($p in $userProfilePaths) {
	Write-host "User Profile:   $p"
}
foreach ($d in $localAppDataPaths) {
	Write-host "Local App Data: $d"
}
# ----------------------------------------------------------------------------
Function KillProcess {
	param($pattern)
	# -------------------------
	# Function.
	$procs = Get-Process
	foreach ($proc in $procs) {
		if ($proc.Path) {
			$item = Get-Item $proc.Path
			if ($item.Name -eq $pattern) {
				Write-Output "  Stopping process: $($item.Name)"
				Stop-Process $proc
			}
		}
	}	
}
# ----------------------------------------------------------------------------
Function RemoveDirectories {
	param ($pattern, $mustBeInProject)
	# -------------------------
	# Function.
	$items = Get-ChildItem $wdir -Filter $pattern -Recurse -Force | Where-Object { $_ -is [DirectoryInfo] }
	foreach ($item in $items) {
		# If folder no longer exists then...
		if (-not [Directory]::Exists($item.FullName)) {
			continue
		}
		if (-not $mustBeInProject) {
			continue
		}
		# Get parent folder.
		[DirectoryInfo] $parent = $item.Parent
		$projects = $parent.GetFiles("*.*proj", [SearchOption]::TopDirectoryOnly)
		# If parent folder do not contain *.*proj file then...
		if ($projects.length -eq 0) {
			# Ignore node_modules.
			if ($item.FullName -like "*\node_modules\*") {
				continue
			}
			Write-Output "  Skip:   $($item.FullName)"
			$global:skipCount += 1
			continue
		}
		Write-Output "  Remove: $($item.FullName)"
		$global:removeCount += 1
		Remove-Item -LiteralPath $item.FullName -Force -Recurse
	}
}
# ----------------------------------------------------------------------------
function RemoveSubFoldersAndFiles {
	param($path, $onlyDirs)
	# -------------------------
	# Function.
	$dirs = Get-Item $path -ErrorAction SilentlyContinue
	foreach ($dir in $dirs) {
		Write-Output "  $($dir.FullName)"
		$items = Get-ChildItem -LiteralPath $dir.FullName -Force
		if ($onlyDirs -eq $true) {
			$items = $items | Where-Object { $_ -is [DirectoryInfo] }
		}
		foreach ($item in $items) {
			Write-Output "  - $($item.Name)"
			Remove-Item -LiteralPath $item.FullName -Force -Recurse
		}
	}
}
# ----------------------------------------------------------------------------
Function RemoveFiles {
	param($pattern)
	# -------------------------
	# Function.
	$items = Get-ChildItem $wdir -Filter $pattern -Recurse -Force | Where-Object { $_ -is [FileInfo] }
	foreach ($item in $items) {
		Write-Output "  $($item.FullName)"
		Remove-Item -LiteralPath $item.FullName -Force
	}
}
# ----------------------------------------------------------------------------
function ClearBuilds {
	$global:removeCount = 0
	$global:skipCount = 0
	# Remove 'obj' folders first, because it can contain 'bin' inside.
	Write-Host "Delete Intermediate Build Object Folders"
	RemoveDirectories "obj" $true
	Write-Host "Delete Final Build Binary Folders"
	RemoveDirectories "bin" $true
	#Write-Output "Skipped: $global:skipCount, Removed: $global:removeCount"
}
# ----------------------------------------------------------------------------
# Kill processes which could lock files in the project folders.
function KillDeveloperProcesses {
	# You can find the process locking file by searching for the locked file name using SysInternals Process Explorer.
	# 1. Open SysInternals Process Explorer.
	# 2. Go to the Menu and click on "Find". Then, choose "Find Handle or DLL...(CTRL+SHIFT+F)".
	#
	# List of process names to kill (without the .exe extension), with comments.
	$processNames = @(
		"MsBuild", # Microsoft Build Engine.
		"EndTask", # Microsoft Visual Studio Team Foundation Server End Task.
		"w3wp", # IIS Worker.
		"node", # Node.js JavaScript runtime environment.
		"WebViewHost", # Web View Host.
		"ChromeDriver", # ChromeDriver. Used for UI testing.
		"iisexpress", # IIS Express.
		"iisexpresstray", # IIS Express Tray Icon.
		"devenv", # Integrated Development Environment (IDE) for Microsoft's Visual Studio.
		"testhost", # Visual Studio Test Host.
		"TGitCache", # GIT Cache.
		"TSVNCache"         # SVN Cache.
	)
	Write-Host "Kill Developer Processes"
	foreach ($name in $processNames) {
		# Attempt to get the running processes by name.
		$processes = Get-Process -Name $name -ErrorAction SilentlyContinue
		if ($processes) {
			foreach ($process in $processes) {
				# Stop the process.
				Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
				# Output a message indicating the process was stopped.
				Write-Host "  Stopped process: $($process.Name) (PID: $($process.Id))"
			}
		}
	}
}
# ----------------------------------------------------------------------------
function ClearCache {
	Write-Host "Clear IIS Express configuration and remove temp files"
	Start-Sleep -Seconds 2.0
	foreach ($p in $userProfilePaths) {
		RemoveSubFoldersAndFiles "$p\Documents\My Web Sites" $true
	}
	Write-Host "Remove temp directories"
	RemoveDirectories ".vs" # Visual Studio
	RemoveDirectories ".vscode" # Visual Studio Code
	Write-Host "Remove temp files"
	RemoveFiles "*.dbmdl"
	RemoveFiles "*.user"
	RemoveFiles "*.suo"
	RemoveFiles "tsconfig.tsbuildinfo"
}
# ----------------------------------------------------------------------------
function ResetPermissions {
	param([string]$path)
	# -------------------------
	# Give read write permissions to local users.
	$di = new-Object System.IO.DirectoryInfo($path)
	Write-Host "Reset Permissions on $($di.FullName)"
	if ($di.Exists -eq $false) {
		Write-Host "Folder not found!"
		return
	}
	# Take ownership.
	& takeown.exe @("/F", $path)
	# Return ownership to TrustedInstaller.
	& icacls.exe @($path, "/setowner", "`"NT Service\TrustedInstaller`"", "/Q")
	# Replace ACL with default inherited acls for all matching files.
	& icacls.exe @($path, "/reset", "/T", "/C", "/Q")
	# Add modify (M) & write (W) permission.
	# Inherit: This folder and files (OI), This folder and subfolders (CI).
	#& icacls.exe @($path, "/grant", "`"Users`":(OI)(CI)MW")
}
# ----------------------------------------------------------------------------
function ClearCacheVS {
	# Fix Visual Studio "Windows Form Designer: Could not load file or assembly" designer error by 
	# clearing temporary compiled assemblies inside dynamically created folders by Visual Studio.
	# Visual studio must be closed for this batch script to succeed.
	#
	Write-Host "Clear Visual Studio Cache"
	foreach ($p in $userProfilePaths) {
		for ($i = 12; $i -le 20; $i++) {
			$vsPaths = @(
				"$p\AppData\Local\Microsoft\VisualStudio\$($i).*\ProjectAssemblies",
				"$p\AppData\Local\Microsoft\VisualStudio\$($i).*\ComponentModelCache",
				"$p\AppData\Local\Microsoft\VisualStudio\$($i).*\ItemTemplatesCache_{00000000-0000-0000-0000-000000000000}",
				"$p\AppData\Local\Microsoft\VisualStudio\$($i).*\ProjectTemplatesCache_{00000000-0000-0000-0000-000000000000}",
				"$p\AppData\Local\Microsoft\VisualStudio\$($i).*\MEFCacheBackup",
				"$p\AppData\Local\Microsoft\VisualStudio\$($i).*\PackageCache",
				"$p\AppData\Local\Microsoft\VisualStudio\$($i).*\TextMateCache",
				"$p\AppData\Local\Microsoft\VisualStudio\$($i).*\NpdProjectTemplateCache_en-US",
				"$p\AppData\Local\Microsoft\VisualStudio\$($i).*\ToolboxItemDiscoveryCache*",
				"$p\AppData\Local\Microsoft\VisualStudio\$($i).*\TunnelCache.json"
			)
			foreach ($vsPath in $vsPaths) {
				$paExpanded = Get-Item $vsPath -ErrorAction SilentlyContinue
				if ($paExpanded.Length -ne 0) {
					RemoveSubFoldersAndFiles $vsPath
				}
			}
		}
	}	
	return
	Write-Host "Clear IIS Express Cache"
	foreach ($p in $localAppDataPaths) {
		RemoveSubFoldersAndFiles "$p\Temp\iisexpress"
		RemoveSubFoldersAndFiles "$p\Temp\TFSTemp"
		RemoveSubFoldersAndFiles "$p\Temp\Temporary ASP.NET Files"
	}
	Write-Host "Clear Xamarin Cache"
	foreach ($p in $localAppDataPaths) {
		RemoveSubFoldersAndFiles "$p\Temp\Xamarin"
		RemoveSubFoldersAndFiles "$p\Xamarin\iOS\Provisioning"
	}
	Write-Host "Clear .NET Framework Cache"
	$netVersions = @("v2.0.50727", "v4.0.30319")
	foreach ($v in $netVersions) {
		RemoveSubFoldersAndFiles "$($env:SystemRoot)\Microsoft.NET\Framework\$v\Temporary ASP.NET Files"
		RemoveSubFoldersAndFiles "$($env:SystemRoot)\Microsoft.NET\Framework64\$v\Temporary ASP.NET Files"
	}
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
function ShowMainMenu {
	$m = ""
	do {
		$namesAffected = [String]::Join(", ", $userNames)
		# Clear screen.
		Clear-Host
		Write-Host
		Write-Host "User profiles will be affected: $namesAffected"
		Write-Host "Please close Visual Studio before starting cleanup."
		Write-Host "The 'Clear' option will kill all developer processes."
		Write-Host
		Write-Host "    1 - Clear Project builds"
		Write-Host "    2 - Clear IIS and temp files"
		Write-Host "    3 - Clear Visual Studio cache"
		Write-Host
		Write-Host "    0 - Clear all"
		Write-Host
		Write-Host "    R - Reset Permissions"
		Write-Host "    K - Kill Developer Processes"
		Write-Host
		$m = Read-Host -Prompt "Type option and press ENTER to continue"
		Write-Host
		# Options:
		IF ("$m" -eq "0" -or "$m" -eq "1" -or "$m" -eq "2" -or "$m" -eq "3" ) { KillDeveloperProcesses }
		IF ("$m" -eq "0" -or "$m" -eq "1") { ClearBuilds }
		IF ("$m" -eq "0" -or "$m" -eq "2") { ClearCache }
		IF ("$m" -eq "0" -or "$m" -eq "3") { ClearCacheVS }
		IF ("$m" -eq "R") { ResetPermissions "$scriptPath" }
		IF ("$m" -eq "K") { KillDeveloperProcesses; Start-Sleep -Seconds 2.0; }
		# If option was choosen.
		IF ("$m" -ne "") {
			pause
		}
	} until ("$m" -eq "")
	return $m
}
# ----------------------------------------------------------------------------
# Execute.
# ----------------------------------------------------------------------------
ShowMainMenu