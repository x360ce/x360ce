# Convert Folder with SVG image files into XAML Resource file.
using namespace System;
using namespace System.IO;
using namespace System.Xml.Linq;
using namespace System.Text.RegularExpressions;

[Reflection.Assembly]::LoadWithPartialName("System.Xml.Linq") | Out-Null;

$namespace = "JocysCom.ClassLibrary.Controls.Themes";

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
[DirectoryInfo]$root = New-Object DirectoryInfo($scriptPath);
# ----------------------------------------------------------------------------
function RemoveAttributes
{
    param([XElement]$Node,[string]$Name);
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
function SHA256CheckSum
{
    param($filePath);
    $SHA256 = [System.Security.Cryptography.SHA256Managed]::Create();
    $fileStream = [System.IO.File]::OpenRead($filePath);
    $bytes = $SHA256.ComputeHash($fileStream);
    $hash = ($bytes|ForEach-Object ToString X2) -join '';
    $fileStream.Dispose();
    $SHA256.Dispose();
    return $hash;
}
# ----------------------------------------------------------------------------
# Create regular expressions for key and names generation.
$RxAllExceptNumbersAndLetters = New-Object Regex("[^a-zA-Z0-9]", [RegexOptions]::IgnoreCase);
$UsRx = New-Object Regex("_+");
# Inkscape program location, which will be used for conversion from SVG format to XAML format.
$inkscape = "d:\Program Files\Inkscape\bin\inkscape.exe";

# strip SVG_to_XAML
$className = [Path]::GetFileNameWithoutExtension($file.Basename);
$dir = New-Object DirectoryInfo($root.FullName + "\" + $className);
# Get files.
$files = $dir.GetFiles("*.svg");
# If no SVG images found then skip.
if ($files.Length -eq 0){
    continue;
}
# Crate output file name.
$fileName = $RxAllExceptNumbersAndLetters.Replace($dir.Name, "_");
$fileName = $UsRx.Replace($fileName, "_");
$fileName = "$className.xaml";
$fileNameCs = "$className.xaml.cs";
Write-Host "${dir} - $($files.Length) images -> $fileName";
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
$nodes = $xaml.Root.Nodes();
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

# Process files.
for ($f = 0; $f -lt $files.Length; $f++) {
    $file = $files[$f];
    #$hash = SHA256CheckSum -filePath $file.FullName;
    Write-Host "$($dir.Name)\$($file.Name)";
    #& $inkscape "$($file.FullName)" --export-filename="$scriptPath\$($file.BaseName).xaml";
    $nodeXml = Get-Content "$($file.FullName)" | & $inkscape --pipe --export-type=xaml | Out-String;
    # Remove name attributes.
    [XDocument]$node = [XDocument]::Parse($nodeXml);
    # Remove "Name" attributes.
    RemoveAttributes -Node $node.Root -Name "Name";
    # Create unique key.
    $key = "Icon_$($file.BaseName)";
    # Add image XML to XAML document.
    $xaml.Root.Add($node.Root);
    # Get node which was just added.
    $ln = $xaml.Root.LastNode;
    # Give node unique name.
    $ln.SetAttributeValue([XName]::Get("Key", $xNs), $key);
    # Make sure that image copy is made when it is used.
    $ln.SetAttributeValue([XName]::Get("Shared", $xNs), "False");
    # Set file hash.
    $ln.SetAttributeValue([XName]::Get("FileHash", $xNs), $hash);
    # Write unique name to code file.
    [File]::AppendAllText($fileNameCs, "`t`tpublic const string $key = nameof($key);`r`n");
}
# Save XAML file.
$xaml.Save($fileName);
# End <ResourceName>.xaml.cs file.
[File]::AppendAllText($fileNameCs, "`r`n");
[File]::AppendAllText($fileNameCs, "`t}`r`n");
[File]::AppendAllText($fileNameCs, "}`r`n");
