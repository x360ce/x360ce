<#
.SYNOPSIS
    Replace in multiple files.
.NOTES
    Author:     Evaldas Jocys <evaldas@jocys.com>
    Modified:   2021-04-14
.LINK
    http://www.jocys.com
#>

using namespace System;
using namespace System.IO;
using namespace System.Text;
using namespace System.Text.RegularExpressions;

# ----------------------------------------------------------------------------
# Get current command path.
[string]$current = $MyInvocation.MyCommand.Path;
# Get calling command path.
[string]$calling = @(Get-PSCallStack)[1].InvocationInfo.MyCommand.Path;
# If executed directly then...
if ($calling -ne "") {
    $current = $calling;
}
# ----------------------------------------------------------------------------
[FileInfo]$file = New-Object FileInfo($current);
# Set public parameters.
$global:scriptName = $file.Basename;
$global:scriptPath = $file.Directory.FullName;
# Change current directory.
[Console]::WriteLine("Path: {0}", $scriptPath);
[Environment]::CurrentDirectory = $scriptPath;
Set-Location $scriptPath;
# ----------------------------------------------------------------------------
Function DoReplace
{
	# Parameters.
	param ([string]$path, [string]$filesPattern, [string]$pattern, [string]$replacement);
	Write-Output "`"$path`" `"$filesPattern`" `"$pattern`" `"$replacement`"";
	# Function.
	$di = new-Object DirectoryInfo($path);
	[FileInfo[]]$files = $di.GetFiles($filesPattern, [SearchOption]::AllDirectories);
	$rx = new-Object Regex($pattern);
	foreach ($file in $files)
	{
		[string]$oldContent = [File]::ReadAllText($file.FullName);
		$newContent = $rx.Replace($oldContent, $replacement);
		if ($oldContent -ne $newContent)
		{
			$subName = $file.FullName.Substring($di.FullName.Length + 1);
			Write-Output "  $subName";
			[File]::WriteAllText($file.FullName, $newContent);
		}
	}
}
# Remove 'obj' folders first, because it can contain 'bin' inside.
DoReplace "." "*.xaml" "(4),([034]),([034]),([034])" "3,`$2,`$3,`$4";
DoReplace "." "*.xaml" "([034]),(4),([034]),([034])" "`$1,3,`$3,`$4";
DoReplace "." "*.xaml" "([034]),([034]),(4),([034])" "`$1,`$2,3,`$4";
DoReplace "." "*.xaml" "([034]),([034]),([034]),(4)" "`$1,`$2,`$3,3";
pause;
