# Clears Visual Studio cache files.
#----------------------------------------------------------------------------
# Run as administrator.
If (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{   
    $arguments = "& '" + $myinvocation.mycommand.definition + "'"
    Start-Process powershell -Verb runAs -ArgumentList $arguments
    Break
}
#----------------------------------------------------------------------------
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

Function RemoveSubDirectories
{
	# Parameters.
	Param ($path)
	# Function.
	$dirs = Get-Item $path -ErrorAction SilentlyContinue;
	foreach ($dir in $dirs)
	{
		Write-Output "Clear: $($dir.FullName)";
		$items = Get-ChildItem -LiteralPath $dir.FullName -Force | Where-Object {$_ -is [System.IO.DirectoryInfo]};
		foreach ($item in $items)
		{
			Write-Output "  - $($item.Name)";
			Remove-Item -LiteralPath $item.FullName -Force -Recurse;
		}
	}
}

# Fix Visual Studio "Windows Form Designer: Could not load file or assembly" designer error by 
# clearing temporary compiled assemblies inside dynamically created folders by Visual Studio.
# Visual studio must be closed for this batch script to succeed.
#
# Clear Visual Studio.
RemoveSubDirectories "$($env:USERPROFILE)\AppData\Local\Microsoft\VisualStudio\12.*\ProjectAssemblies"
RemoveSubDirectories "$($env:USERPROFILE)\AppData\Local\Microsoft\VisualStudio\13.*\ProjectAssemblies"
RemoveSubDirectories "$($env:USERPROFILE)\AppData\Local\Microsoft\VisualStudio\14.*\ProjectAssemblies"
RemoveSubDirectories "$($env:USERPROFILE)\AppData\Local\Microsoft\VisualStudio\15.*\ProjectAssemblies"
RemoveSubDirectories "$($env:USERPROFILE)\AppData\Local\Microsoft\VisualStudio\16.*\ProjectAssemblies"
# Clear IIS Express.
RemoveSubDirectories "$($env:LOCALAPPDATA)\Temp\iisexpress"
RemoveSubDirectories "$($env:LOCALAPPDATA)\Temp\Temporary ASP.NET Files"
# Clear .NET Framework.
RemoveSubDirectories "$($env:SystemRoot)\Microsoft.NET\Framework\v4.0.30319\Temporary ASP.NET Files"
RemoveSubDirectories "$($env:SystemRoot)\Microsoft.NET\Framework64\v4.0.30319\Temporary ASP.NET Files"

# Solution Explorer, right-click Solution
#	Properties -> Common Properties -> Debug Source Files -> clean "Do not look for these source files" box.

# Tools -> Options -> Projects and Solutions -> Build and Run
# Set "On Run, when build or deployment errors occur:" Prompt to Launch

# .EditorConfig file.
# "charset=utf-8" option can trigger "The source file is different from when the module was built." warning when debugging.

pause
