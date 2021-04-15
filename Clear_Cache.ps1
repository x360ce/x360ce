<#
.SYNOPSIS
    Kill and clear IIS Express. Removes temporary and user specific solution files.
.NOTES
    Author:     Evaldas Jocys <evaldas@jocys.com>
    Modified:   2021-04-14
.LINK
    http://www.jocys.com
#>
# ----------------------------------------------------------------------------
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
Function RemoveDirectories
{
	# Parameters.
	param($pattern);
	# Function.
	$items = Get-ChildItem $wdir -Filter $pattern -Recurse -Force | Where-Object {$_ -is [System.IO.DirectoryInfo]};
	foreach ($item in $items)
	{
	  Write-Output $item.FullName;
	  Remove-Item -LiteralPath $item.FullName -Force -Recurse;
	}
}
# ----------------------------------------------------------------------------
Function RemoveSubDirectories
{
	# Parameters.
	param($path);
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
				Write-Output "Stopping process: $($item.Name)";
				Stop-Process $proc;
			}
		}
	}	
}
# ----------------------------------------------------------------------------
# Clear IIS Express configuration.
KillProcess "iisexpress.exe";
KillProcess "iisexpresstray.exe";
RemoveSubDirectories "$($env:USERPROFILE)\Documents\My Web Sites";
# Clear directories and files.
RemoveDirectories ".vs";
RemoveFiles "*.dbmdl";
RemoveFiles "*.user";
RemoveFiles "*.suo";
pause;
