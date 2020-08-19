# Removes temporary bin and obj folders.
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

Function RemoveDirectories
{
	# Parameters.
	Param ($pattern)
	# Function.
	$items = Get-ChildItem $wdir -Filter $pattern -Recurse -Force | Where-Object {$_ -is [System.IO.DirectoryInfo]};
	foreach ($item in $items)
	{
	  Write-Output $item.FullName;
	  Remove-Item -LiteralPath $item.FullName -Force -Recurse
	}
}
# Clear directories.
RemoveDirectories "bin"
RemoveDirectories "obj"
pause