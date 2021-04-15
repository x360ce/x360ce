<#
.SYNOPSIS
    Removes temporary bin and obj folders.
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
	param ($pattern);
	# Function.
	[System.IO.DirectoryInfo[]]$items = Get-ChildItem $wdir -Filter $pattern -Recurse -Force | Where-Object {$_ -is [System.IO.DirectoryInfo]};
	foreach ($item in $items)
	{
		$item.Refresh();
		if ($item.Exists -eq $false){
			continue;
		}
		# If folder is not inside the project folder then continue.
		if ($item.Parent.GetFiles("*.*proj").Length -eq 0){
			continue;
		}
		Write-Output $item.FullName;
		Remove-Item -LiteralPath $item.FullName -Force -Recurse;
	}
}
# Remove 'obj' folders first, because it can contain 'bin' inside.
RemoveDirectories "obj";
RemoveDirectories "bin";
pause;
