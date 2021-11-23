<#
.SYNOPSIS
    Convert Folder with SVG image files into XAML Resource file.
.NOTES
    Author:     Evaldas Jocys <evaldas@jocys.com>
    Modified:   2021-11-06
.LINK
    http://www.jocys.com

.REMARKS

    Requires Installation of InkScape from https://inkscape.org/release/

	How to include icons resource into App.xaml file:

		<Application
			x:Class="JocysCom.SomeApp"
			xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			StartupUri="MainWindow.xaml">
			<Application.Resources>
				<ResourceDictionary>
					<ResourceDictionary.MergedDictionaries>
						<ResourceDictionary Source="Resources/Icons/Icons_Default.xaml" />
					</ResourceDictionary.MergedDictionaries>
				</ResourceDictionary>
			</Application.Resources>
		</Application>
	
	How to display image inside the XAML with style:
	
		<ContentControl	x:Name="MyIcon" Width="24" Height="24" Content="{StaticResource Icon_IconFileName}" />	

	How to set image to Content control from code behind:

		MyIcon.Content = Icons_Default.Current[Icons_Default.Icon_IconFileName];
		
#>
using namespace System;
using namespace System.IO;
using namespace System.Linq;
using namespace System.Xml.Linq;
using namespace System.Text.RegularExpressions;
using namespace System.Collections.Generic;

[Reflection.Assembly]::LoadWithPartialName("System.Xml.Linq") | Out-Null;

Clear-Host;

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
[Console]::WriteLine("Script Path: {0}", $scriptPath);
[Environment]::CurrentDirectory = $scriptPath;
Set-Location $scriptPath;
# ----------------------------------------------------------------------------
[DirectoryInfo]$root = New-Object DirectoryInfo($scriptPath);
# ----------------------------------------------------------------------------
function RemoveAttributes
{
    param([XElement]$Node,[string]$Name);
    #----------------------------------------------------------
    foreach ($attr in $Node.Attributes())
    {
        if ($attr.Name -eq $Name)
        {
            $attr.Remove();
        }
    }
    foreach ($child in $Node.Descendants())
    {
        RemoveAttributes -Node $child -Name $Name;
    }
}
# ----------------------------------------------------------------------------
function FindParentFile
{
    [OutputType([FileInfo[]])] param([string]$pattern);
    #----------------------------------------------------------
    [DirectoryInfo]$di = new-Object DirectoryInfo $scriptPath;
    do
    {
        $files = $di.GetFiles($pattern);
        # Return if project files were found.
        if ($files.Count -gt 0)
        {
            return $files;
        }
        # Continue to parent.
        $di = $di.Parent;
    } while($null -ne $di);
    return $null;
}
# ----------------------------------------------------------------------------
function GetProjectValue
{
    [OutputType([string])] param([string]$path, [string]$name);
    #----------------------------------------------------------
    [string]$content = [File]::ReadAllText($path);
	[Regex]$rx = New-Object Regex("(?<p><$name>)(?<v>[^<]*)(?<s><\/$name>)");
	$match = $rx.Match($content);
	if ($match.Success -eq $true) {
		return $match.Groups["v"].Value;
	}
	return $null;
}
# ----------------------------------------------------------------------------
function FindExistingFile
{
    [OutputType([string])] param([string]$path);
    #----------------------------------------------------------
    [FileInfo]$fi = $null;
    # Paths to look for executable.
    $ps = @(
        $path,
        "${env:ProgramFiles}\$path",
        "${env:ProgramFiles(x86)}\$path",
        "D:\Program Files\$path",
        "D:\Program Files (x86)\$dfe"
    );
    foreach ($p in $ps) {
        # Fix dot notations.
        $fullPath = [Path]::GetFullPath($p);
        #Write-Host "Check... $fullPath";
        if ([File]::Exists($fullPath)) {
            $fi = new-Object FileInfo $fullPath;
            break;
        }
    }
    return $fi;
}
# ----------------------------------------------------------------------------
function SHA256CheckSum
{
    param($filePath);
    #----------------------------------------------------------
    $SHA256 = [System.Security.Cryptography.SHA256Managed]::Create();
    $fileStream = [System.IO.File]::OpenRead($filePath);
    $bytes = $SHA256.ComputeHash($fileStream);
    $hash = ($bytes|ForEach-Object ToString X2) -join '';
    $fileStream.Dispose();
    $SHA256.Dispose();
    return $hash;
}
# ----------------------------------------------------------------------------
function FindProjectFile
{
    [FileInfo[]]$list = FindParentFile "*.*proj";
    if ($list -ne $null -and $list.Count -gt 0){
        # Order by date descendign to most recent file.
        $list = [Enumerable]::OrderByDescending($list, [Func[object,object]]{ param($x) $x.LastWriteTime });
        $list = [Enumerable]::ToArray($list);
        return $list[0];
    }
    return $null;
}
# ----------------------------------------------------------------------------
# Show menu
# ----------------------------------------------------------------------------
function ShowOptionsMenu
{
    param($items);
    #----------------------------------------------------------
    # Get local configurations.
	$keys = "1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    Write-Host "Options:";
    Write-Host;
    for ($i = 0; $i -lt $items.Count; $i++)
    {
        $item = $items[$i];
        Write-Host "    $($keys[$i]) - $($item)";
    }
    Write-Host;
    $m = Read-Host -Prompt "Type option and press ENTER to continue";
    $m = $m.ToUpper();
    $keyIndex = $keys.IndexOf($m);
    # If wrong choice then...
    if ($keyIndex -eq -1)
    {
        return $null;
    }
    return $items[$keyIndex];
}
# ----------------------------------------------------------------------------

Write-Host;

#------------------------------
# Inkscape program location, which will be used for conversion from SVG format to XAML format.
#------------------------------

$inkscape = FindExistingFile "Inkscape\bin\inkscape.exe";
if ($null -eq $inkscape) {
    Write-Host "Inkscape program not found!";
    Write-Host "Download from https://inkscape.org/release/";
    return;
}
Write-Host "Inkscape:  $($inkscape.FullName)";

#------------------------------
# Get Project file.
#------------------------------

[FileInfo]$project = FindProjectFile;
if ($null -eq $project) {
    Write-Host "Project file not found.";
    return;
}
Write-Host "Project:   $($project.FullName)";

#------------------------------
# Get Default namespace.
#------------------------------

# Get from project file.
$defaultNamespace = GetProjectValue $project.FullName "RootNamespace";
# If default namespace not found.
if ("" -eq "$defaultNamespace") {
    # Visual studio use Project file name as default assembly and root namespace.
    $defaultNamespace = $project.BaseName;
}
# If namespace not found then...
if ("" -eq "$defaultNamespace") {
	Write-Host "Please provide default namespace";
    $defaultNamespace = Host-Read;
}
#Write-Host "Default Namespace: $defaultNamespace";

# Get Relative namespace.
$relativeNamespace = $scriptPath.Substring($project.Directory.FullName.Length).Replace("\", ".");
#Write-Host "Relative Namespace: $relativeNamespace";
$namespace = $defaultNamespace + $relativeNamespace;

Write-Host;
Write-Host "Namespace: $namespace";

#------------------------------
# Get Class Name
#------------------------------

# Get forders with *.svg files inside.
$dirs = $file.Directory.GetDirectories();
$dirNames = new-Object List[string];
foreach ($dir in $dirs) {
    $filePattern = "*.svg";
    # If folder contains images then...
    $dirFiles = $dir.GetFiles($filePattern);
    if ($dirFiles.Length -gt 0){
        $dirNames.Add($dir.Name);
    }
    #Write-Host "Source: $($dir.Name), $filePattern Files: $($dirFiles.Length)";
}

[string]$className = $null;

if ($dirNames.Count -eq 1){
    $className = $dirNames[0];
} elseif ($dirNames.Count -gt 1){
    $className = ShowOptionsMenu $dirNames;
}

if ($null -eq $className){
    Write-Host "Folder with images not found!";
    return;
}

$sourceDir = New-Object DirectoryInfo($root.FullName + "\" + $className);

Write-Host "Class:     $className";
Write-Host;
Write-Host "Source:    $($sourceDir.Name)\";
Write-Host "Target:    $className.xaml + $className.xaml.cs";
Write-Host;
pause;

#------------------------------
# Generate images.
#------------------------------

Write-Host;

#Write-Host "Done. Press any key to continue...";
#$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown");

# Get files.
$files = $sourceDir.GetFiles("*.svg");
# If no SVG images found then skip.
if ($files.Length -eq 0){
    continue;
}

# Create regular expressions for key and names generation.
$RxAllExceptNumbersAndLetters = New-Object Regex("[^a-zA-Z0-9]", [RegexOptions]::IgnoreCase);
$UsRx = New-Object Regex("_+");
# Crate output file name.
$fileName = $RxAllExceptNumbersAndLetters.Replace($sourceDir.Name, "_");
$fileName = $UsRx.Replace($fileName, "_");
$fileName = "$className.xaml";
$fileNameCs = "$className.xaml.cs";
if ($files.Length -eq 1){
    Write-Host "Convert  $($files.Length) image:";
}else{
    Write-Host "Convert $($files.Length) images:";
}
# Start <ResourceName>.xaml file.
$xNs = "http://schemas.microsoft.com/winfx/2006/xaml";
if ([File]::Exists($fileName) -ne $true)
{
    [File]::WriteAllText($fileName, "<ResourceDictionary xmlns=`"http://schemas.microsoft.com/winfx/2006/xaml/presentation`" xmlns:x=`"$xNs`"");
    [File]::AppendAllText($fileName,"`r`nx:Class=`"$($namespace).$($className)`"");
    [File]::AppendAllText($fileName,"`r`nx:ClassModifier=`"public`"");
    [File]::AppendAllText($fileName,'>');
    [File]::AppendAllText($fileName,"`r`n`r`n</ResourceDictionary>");
}
[XDocument]$xaml = [XDocument]::Load($fileName); 
# Create list from existing nodes.
$nodes = $xaml.Root.Nodes();
#$nodes = $xaml.Root.Elements("Viewbox").ToArray();
#$nodes = $xaml.DocumentElement.SelectNodes("/*[local-name() = 'ResourceDictionary']/*[local-name() = 'Viewbox']");

$nodeList = new-Object System.Collections.Generic.Dictionary[string`,object];
[XElement]$node = $null;
foreach ($node in $nodes)
{
    $nodeKey = $node.Attribute([XName]::Get("Key", $xNs)).Value;
    $nodeList.Add($nodeKey, $node);
    #Write-Host "Key $nodeKey";
}
# Cleanup old nodes.
$xaml.Root.RemoveNodes();

# Start <ResourceName>.xaml.cs file.
[File]::WriteAllText($fileNameCs, "using System.Windows;`r`n");
[File]::AppendAllText($fileNameCs, "`r`n");
[File]::AppendAllText($fileNameCs, "namespace $namespace`r`n");
[File]::AppendAllText($fileNameCs, "{`r`n");
[File]::AppendAllText($fileNameCs, "`tpartial class $className : ResourceDictionary`r`n");
[File]::AppendAllText($fileNameCs, "`t{`r`n");
[File]::AppendAllText($fileNameCs, "`t`tpublic $className()`r`n");
[File]::AppendAllText($fileNameCs, "`t`t{`r`n");
[File]::AppendAllText($fileNameCs, "`t`t`tInitializeComponent();`r`n");
[File]::AppendAllText($fileNameCs, "`t`t}`r`n");
[File]::AppendAllText($fileNameCs, "`r`n");
[File]::AppendAllText($fileNameCs, "`t`tpublic static $className Current => _Current = _Current ?? new $className();`r`n");
[File]::AppendAllText($fileNameCs, "`t`tprivate static $className _Current;`r`n");
[File]::AppendAllText($fileNameCs, "`r`n");

Write-Host;

# Process files.
for ($f = 0; $f -lt $files.Length; $f++) {
    $file = $files[$f];
    $fileHash = SHA256CheckSum -filePath $file.FullName;
    $fileHashNodeName = "Tag";
    $nodeXml = $null;
    # Create unique key.
    $key = "Icon_$($file.BaseName)";
    # Get existing node or create new.
	$action = "Insert:";
    if ($nodeList.ContainsKey($key))
    {
        $action = "Update:";
        [XElement]$oldNode = $nodeList[$key];
        # Get hash of current node.
        $oldHash = $oldNode.Attribute([XName]::Get($fileHashNodeName)).Value;
        if ($oldHash -eq "SHA256_$fileHash")
        {
            $nodeXml = $oldNode;
            $action = "Keep:  ";
        }
    }
    $isNew = ($null -eq $nodeXml);
    if ($isNew)
    {
        $nodeXml = Get-Content "$($file.FullName)" | & $inkscape --pipe --export-type=xaml | Out-String;
    }
    # Show file name.
    Write-Host "`t$action $($sourceDir.Name)\$($file.Name)";
    # Remove name attributes.
    [XDocument]$node = [XDocument]::Parse($nodeXml);
    # Remove "Name" attributes.
    RemoveAttributes -Node $node.Root -Name "Name";

    RemoveAttributes -Node $node.Root -Name "Key";
    RemoveAttributes -Node $node.Root -Name "Shared";

    # Add image XML to XAML document.
    $xaml.Root.Add($node.Root);
    # Remove old attributes.
    if ($isNew)
    {
        # Get node which was just added.
        $ln = $xaml.Root.LastNode;
        # Give node unique name.
        $ln.SetAttributeValue([XName]::Get("Key", $xNs), $key);
        # Make sure that image copy is made when it is used.
        $ln.SetAttributeValue([XName]::Get("Shared", $xNs), "False");
        # Set file hash.
        $ln.SetAttributeValue([XName]::Get($fileHashNodeName), "SHA256_$fileHash");
    }
    # Write unique name to code file.
    [File]::AppendAllText($fileNameCs, "`t`tpublic const string $key = nameof($key);`r`n");
}

# Save XAML file.
$xaml.Save($fileName);
# End <ResourceName>.xaml.cs file.
[File]::AppendAllText($fileNameCs, "`r`n");
[File]::AppendAllText($fileNameCs, "`t}`r`n");
[File]::AppendAllText($fileNameCs, "}`r`n");

Write-Host;
Write-Host "Done. Press any key to continue...";
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown");
