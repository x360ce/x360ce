<#
.SYNOPSIS
    Export and Import SQL Data.
.NOTES
    Author:     Evaldas Jocys <evaldas@jocys.com>
    Modified:   2021-04-19

	Application Server Requirements (2012 R2):

	- To run applications compiled by Visual Studio 2019:
	  Microsoft .NET 5.0.1 Runtime - https://dotnet.microsoft.com/download 

	- To run PowerShell setup scripts:
	Windows Management Framework 5.1 - https://aka.ms/WMF5Download 
#>
#using assembly "System.Configuration";
#using assembly "System.Configuration.Install";
#using assembly "System.Xml";
using namespace System;
using namespace System.IO;
using namespace System.Linq;
using namespace System.Text;
using namespace System.Text.RegularExpressions;
using namespace System.Collections;
using namespace System.Collections.Generic;
using namespace System.Text.RegularExpressions;
using namespace System.Security.Cryptography;
using namespace System.Security.Cryptography.X509Certificates;

Clear-Host;
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
function ConfigureSettings
{
    [FileInfo]$global:scriptFile = New-Object FileInfo($current);
    # Set public parameters.
	$global:scriptName = $scriptFile.Basename;
	$global:scriptPath = $scriptFile.Directory.FullName;
    # Change current directory.
    [Environment]::CurrentDirectory = $scriptPath;
    # -------------------------
    # Determine configuration file.
    Write-Host "Path: $scriptPath";
    Write-Host;
}
# ----------------------------------------------------------------------------
function ConfigureTools
{
    $global:sqlExe = "sqlcmd.exe";
    if ([File]::Exists("Tools\sqlcmd.exe")){
        $global:sqlExe = "Tools\sqlcmd.exe";
    }
    $global:bcpExe = "bcp.exe";
    if ([File]::Exists("Tools\bcp.exe")){
        $global:bcpExe = "Tools\bcp.exe";
    }
    #Write-Host "$sqlExe";
    #Write-Host "$bcpExe";
    #pause;
}
# ----------------------------------------------------------------------------
function LoadCodeBehind
{
    # Load code behind.
    $assemblies = @(
        "System.Xml",
        "System.Security",
        "System.Security.Principal",
        "System.Runtime",
        "System.Data",
        "System.Data.Common",
        # https://www.nuget.org/packages/Microsoft.SqlServer.SqlManagementObjects
        # microsoft.sqlserver.sqlmanagementobjects.150.18208.0.nupkg.zip\lib\net45\
        "bin\Microsoft.SqlServer.ConnectionInfo.dll",
        "bin\Microsoft.SqlServer.Management.Sdk.Sfc.dll",
        "bin\Microsoft.SqlServer.ServiceBrokerEnum.dll",
        "bin\Microsoft.SqlServer.SqlEnum.dll",
        "bin\Microsoft.SqlServer.Smo.dll"
    );
    $code = [File]::ReadAllText("$current.cs");
    Add-Type -TypeDefinition $code -Language CSharp -ReferencedAssemblies $assemblies;
    foreach ($assembly in $assemblies) {
        if ($assembly -like "*.dll"){
            [void][System.Reflection.Assembly]::LoadFrom($assembly);
        }else{
            [void][System.Reflection.Assembly]::LoadWithPartialName($assembly);
        }
    }
}
# ----------------------------------------------------------------------------
# Functions
# ----------------------------------------------------------------------------
function GetOption
{
    param($name);
    $item = [Enumerable]::FirstOrDefault($global:configData.Options, [Func[object,bool]]{ param($x) $x.Name -eq $name});
    if ($null -eq $item){
        return $null;
    }
    return $item.Value;
}
# ----------------------------------------------------------------------------
function GetConnection
{
    param($name);
    $item = [Enumerable]::FirstOrDefault($configData.Connections, [Func[object,bool]]{ param($x) $x.Name -eq $name});
    if ($null -eq $item){
        return $null;
    }
    return $item.Value;
}
# ----------------------------------------------------------------------------
function ExportDataOrSchema
{
    param($action, $dataType);
    Write-Host "Exporting $action to $scriptPath\Data.$configName...";
    Write-Host;
    # Get configuration data.
    $connectionString = GetConnection("SourceConnection");
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $connectionString;
    $server = $builder.Server;
    $database = $builder.Database;
    $user = $builder.UserID;
    $password = $builder.Password;
    $items = $configData.Items;
    $log = "$current.log";
    "Export Log" > $log;
    $UseVarChar = GetOption("UseVarChar");
    $Compress = GetOption("Compress");
    # If set to OFF them spaces will be trimmed.
    $ANSI_PADDING = GetOption("ANSI_PADDING");
    $Return_Null = GetOption("Return_Null");
    Write-Host "  Compress: $Compress";
    Write-Host "  UseVarChar: $UseVarChar";
    Write-Host "  ANSI_PADDING: $ANSI_PADDING";
    Write-Host "  Return_Null: $Return_Null";
    Write-Host;
    if ("$Return_Null" -ne ""){
        $Return_Null_Regex = new-Object Regex($Return_Null, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase);
    }
    # Loop tables.
    for ($i = 0; $i -lt $items.Count; $i++) {
        $item =$items[$i];
        $schema = $item.Schema;
        $table = $item.Table;
        $query = $item.Query;
        Write-Host "  $schema.$table";
        "-------------------------------------------" >> $log;
        "-- ${$action}: $schema.$table" >> $log;
        "-------------------------------------------" >> $log;
        "Export [$database].[$schema].[$table]" >> $log;
        #pause
        if ($action -eq "Schema")
        {
            $fi = New-Object FileInfo "$scriptPath\Data.$configName\Tables\$schema.$table.sql";
            if ($fi.Directory.Exists -eq $false){
                $fi.Directory.Create();
            }
            $sql = [BackupAndRestoreData]::ScriptTable($connectionString, $schema, $table);
            if ($UseVarChar -eq "True"){
                # Convert char type to varchar type.
                $sql = $sql.Replace("[char]", "[varchar]").Replace("[nchar]", "[nvarchar]");
            }
            if ("$ANSI_PADDING" -ne ""){
                # Convert char type to varchar type.
                $sql =  $sql = $sql.Replace("CREATE TABLE", "SET ANSI_PADDING $ANSI_PADDING`r`nCREATE TABLE");
            }
            if ($fi.Exists -eq $true){
                $fi.Delete();
            }
            [File]::WriteAllText($fi.FullName, $sql);
        }
        if ($action -eq "Data")
        {
            # Export file in native format.
            if ($dataType -eq "csv2") {
                $fi = New-Object FileInfo "$scriptPath\Data.$configName\CSV\$schema.$table.csv";
                if ($fi.Directory.Exists  -eq $false){
                    $fi.Directory.Create();
                }
                $columns = [BackupAndRestoreData]::GetColumns($connectionString, $schema, $table);
                $sl = New-Object System.Collections.Generic.List[string];
                foreach ($column in $columns) {
                    if ($null -ne $Return_Null_Regex -and $Return_Null_Regex.IsMatch($column.Column) -eq $true){

                        $sl.Add("[$($Column.Column)] = NULL");
                    }
                    elseif ($column.Type -like "*char*"){
                        $sl.Add("[$($Column.Column)] = LTRIM(RTRIM([$($Column.Column)]))");
                        # QUOTENAME
                    }else{
                        $sl.Add("[$($Column.Column)]");
                    }
                }
                $s = [String]::Join(",`r`n  ", $sl);
                # Create new query which will trim all char columns.
                $newQuery = "SELECT`r`n  " + $s + "`r`nFROM (" + $query + ") Trimmed1";
                #Write-Host $newQuery;
                #pause;
                Invoke-sqlcmd -ConnectionString $connectionString  `
                    -Query $newQuery | `
                    Export-Csv -NoTypeInformation -path $fi.FullName -Encoding UTF8;
            }else{
                $fi = New-Object FileInfo "$scriptPath\Data.$configName\$schema.$table.$dataType";
                if ($fi.Directory.Exists  -eq $false){
                    $fi.Directory.Create();
                }
                # bcp.exe arguments.
                $b = @();
                # Table or query.
                if ($null -eq $query){
                $b += "[$database].[$schema].[$table]", "out";
                }else{
                $b += $query, "queryout";
                }
                # File.
                $b += $fi.FullName;
                # File type: csv, xml or dat.
                if ($dataType -eq "csv"){
                    $b += "-w", "-t,";
                }elseif ($dataType -eq "xml"){
                    $b += "-x";
                }else{
                    $b += "-n", "-N";
                }
                # Connection string.
                $b += "-d", $database, "-S", $server;
                if ($true -eq $builder.IntegratedSecurity){
                    $b += "-T";
                }else{
                    $b += "-U", $user, "-P", $password;
                
                }
                & $bcpExe @b >> $log;
            }
            [void]$fi.Refresh();
            if ($fi.Exists -eq $true -and $Compress -eq "True")
            {
                Compress-Archive -Path "$($fi.FullName)" -DestinationPath "$($fi.FullName).zip" -Force;
                $fi.Delete();
            }
        }
    }
    Write-Host;
}
# ----------------------------------------------------------------------------
function ImportDataOrSchema
{
	param($action, $dataType);
    Write-Host "Import $action...";
    Write-Host;
    # Get configuration data.
    $connectionString = GetConnection("TargetConnection");
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $connectionString;
    $server = $builder.Server;
    $database = $builder.Database;
    $user = $builder.UserID;
    $password = $builder.Password;
    $items = $configData.Items;
    $log = "$current.log";
    "Import Log" > $log;
    # Loop tables.
    for ($i = 0; $i -lt $items.Count; $i++) {
        $item =$items[$i];
        $schema = $item.Schema;
        $table = $item.Table;
        Write-Host "  $schema.$table";
        "-------------------------------------------" >> $log;
        "-- ${$action}: $schema.$table" >> $log;
        "-------------------------------------------" >> $log;
        "Import [$database].[$schema].[$table]" >> $log;
        #
        # https://docs.microsoft.com/en-us/sql/tools/sqlcmd-utility
        # sqlcmd.exe arguments:
        # -N encrypt connection.
        # -G use Azure Active Directory for authentication
        # -E trusted connection
        #
        # https://docs.microsoft.com/en-us/sql/tools/bcp-utility
        # bcp.exe arguments:
        # -n Native type (*.dat)
        # -N keep non-text native (*.dat)
        # -x XML format file (*.xml)
        # -c character type (*.csv)
        # -t field terminator (*.csv)
        # -T trusted connection
        # -E import identity values.
        # -q Executes the SET QUOTED_IDENTIFIERS ON statement.       
        if ($action -eq "Schema")
        {
            $fi = New-Object FileInfo "$scriptPath\Data.$configName\Tables\$schema.$table.sql";
            if ($fi.Directory.Exists -eq $false){
                continue;
            }
            $s = @("-i", $fi.FullName);
            $s += "-d", $database, "-S", $server;
            if ($true -eq $builder.IntegratedSecurity){
                $s += "-E";
            }else{
                $s += "-U", $user, "-P", $password;
            }
            & $sqlExe @s >> $log;
        }
        if ($action -eq "Data")
        {
            $fi = New-Object FileInfo "$scriptPath\Data.$configName\$schema.$table.$dataType";
            # If compressed file found then...
            if ([File]::Exists("$($fi.FullName).zip") -eq $true){
                # Extract data file.
                Expand-Archive -Path "$($fi.FullName).zip" -DestinationPath "$($fi.Directory.FullName)" -Force;
            }
            # Import file from native format.
            #
            # sqlcmd.exe arguments.
            $s = @("-Q", "TRUNCATE TABLE [$database].[$schema].[$table]");
            # bcp.exe arguments.
            $b = @("[$database].[$schema].[$table]", "in", $fi.FullName, "-E");
            # File type: csv, xml or dat.
            if ($dataType -eq "csv"){
                $b += "-w", "-t,";
            }elseif ($dataType -eq "xml"){
                $b += "-x";
            }else{
                $b += "-n", "-N";
            }
            # Connection string.
            $s += "-d", $database, "-S", $server;
            $b += "-S", $server;
            if ($true -eq $builder.IntegratedSecurity){
                $s += "-E";
                $b += "-T";
            }else{
                $s += "-U", $user, "-P", $password;
                $b += "-U", $user, "-P", $password;
            }
            & $sqlExe @s >> $log;
            & $bcpExe @b >> $log;
            $fi.Delete();
        }
    }
    Write-Host;
}
# ----------------------------------------------------------------------------
function CreateConfiguration
{
    $name = Read-Host -Prompt "Type configuration name and press ENTER to continue";
    Write-Host;
    Write-Host "Create Configuration...";
    Write-Host;
    [string]$global:configFile = "$scriptFile.$name.xml";
    $global:configData = new-Object Data;
    [Connection]$source = new-Object Connection;
    $source.Name = "SourceConnection";
    $source.Value = "Server=localhost;Database=dbImport;Trusted_Connection=yes";
    $configData.Connections.Add($source);
    [Connection]$target = new-Object Connection;
    $target.Name = "TargetConnection";
    $target.Value = "Server=localhost;Database=dbImport;Trusted_Connection=yes";
    $configData.Connections.Add($target);
    [BackupAndRestoreData]::Serialize($configData, $configFile);
    # Get configuration data.
    $sourceConnection =  [Enumerable]::First($configData.Connections, [Func[object,bool]]{ param($x) $x.Name -eq "SourceConnection" }).Value;
    $connections = $configData.Connections;
    $items = $configData.Items;
    $items.Clear();
    # Write configuration.
    $schemas = [BackupAndRestoreData]::GetSchemas($sourceConnection);
    for ($s = 0; $s -lt $schemas.Count; $s++) {
        $schema = $schemas[$s];
        $tables = [BackupAndRestoreData]::GetTables($sourceConnection, $schema.Schema);
        for ($t = 0; $t -lt $tables.Count; $t++) {
            $table = $tables[$t];
            $items.Add($table);
        }
    }
    # Save setting file.
    [BackupAndRestoreData]::SetSettings($configFile, $connections, $items);
}
# ----------------------------------------------------------------------------
# Show menu
# ----------------------------------------------------------------------------
function ShowConfigurationMenu
{
    # Get local configurations.
	$keys = "1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    $files = [Directory]::GetFiles(".", "$scriptName*.xml");
    $files = [Enumerable]::OrderBy($files, [Func[object,bool]]{ param($x) $x });
    $files = [Enumerable]::Take($files, $keys.Length);
    $files = [Enumerable]::Select($files, [Func[object,object]]{ param($x) New-Object FileInfo $x });
    $files = [Enumerable]::ToList($files);
    if ($files.Count -eq 0)
    {
        $global:configFile = $null;
        $global:configName = $null;
        $global:configData = $null;
        return;
    }
    if ($files.Count -eq 1)
    {
        $global:configFile = $files[0].FullName;
        $global:configName = [Path]::GetFileNameWithoutExtension($configFile).Substring($scriptFile.Name.Length + 1);
    }
    elseif ($files.Count -gt 1)
    {
        Write-Host "Select configuration file:";
        Write-Host;
        for ($i = 0; $i -lt $files.Count; $i++)
        {
            $file = $files[$i].FullName;
            $name = [Path]::GetFileNameWithoutExtension($file).Substring($scriptFile.Name.Length + 1);
            Write-Host "    $($keys[$i]) - $($name)";
        }
        Write-Host;
        $m = Read-Host -Prompt "Type option and press ENTER to continue";
        $keyIndex = $keys.IndexOf($m);
        # If wrong choice then...
        if ($keyIndex -eq -1)
        {
            # Exit application.
            exit;
        }
        $global:configFile = $files[$keyIndex].FullName;
        $global:configName = [Path]::GetFileNameWithoutExtension($configFile).Substring($scriptFile.Name.Length + 1);
    }
    $global:configData = [BackupAndRestoreData]::GetSettings($configFile);
}
# ----------------------------------------------------------------------------
function ShowImportExportMenu
{
    $m = "";
    do {
        # Clear screen.
        Clear-Host;
        Write-Host;
        if ($null -ne $configFile)
        {
            $sourceItem = GetConnection("SourceConnection");
            if ($null -ne $sourceItem){
                $sourceBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $sourceItem;
            }
            $targetItem =  GetConnection("TargetConnection");
            if ($null -ne $targetItem){
                $targetBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $targetItem;
            }
            Write-Host "Configuration: $configName";
            if ("$($sourceBuilder.Server)" -ne ""){
                Write-Host;
                Write-Host "  Source Server: $($sourceBuilder.Server), Database: $($sourceBuilder.Database)";
                Write-Host;
                Write-Host "    1 - Export Schema";
                Write-Host "    2 - Export Data (DAT)";
                Write-Host "    3 - Export Data (CSV)";
                Write-Host "    4 - Export Data (CSV) with Headers";
                #Write-Host "    4 - Export Data (XML)";
            }
            if ("$($targetBuilder.Server)" -ne ""){
                Write-Host;
                Write-Host "  Target Server: $($targetBuilder.Server), Database: $($targetBuilder.Database)";
                Write-Host;
                Write-Host "    5 - Import Schema";
                Write-Host "    6 - Import Data (DAT)";
                Write-Host "    7 - Import Data (CSV)";
                #Write-Host "    8 - Import Data (XML)";
            }
        }
        else
        {
            Write-Host;
            Write-Host "  C - Create Configuration";
        }
        Write-Host;
        $m = Read-Host -Prompt "Type option and press ENTER to continue";
        Write-Host;
        # Options:
        IF ("${m}" -eq "1") { ExportDataOrSchema "Schema"; };
        IF ("${m}" -eq "2") { ExportDataOrSchema "Data" "dat"; };
        IF ("${m}" -eq "3") { ExportDataOrSchema "Data" "csv"; };
        IF ("${m}" -eq "4") { ExportDataOrSchema "Data" "csv2"; };
        #IF ("${m}" -eq "4") { ExportDataOrSchema "Data" "xml"; };
        Write-Host;
        IF ("${m}" -eq "5") { ImportDataOrSchema "Schema"; };
        IF ("${m}" -eq "6") { ImportDataOrSchema "Data" "dat"; };
        IF ("${m}" -eq "7") { ImportDataOrSchema "Data" "csv"; };
        #IF ("${m}" -eq "8") { ImportDataOrSchema "Data" "xml"; };
        IF ("${m}" -eq "C") { CreateConfiguration; };
        # If option was choosen.
        IF ("${m}" -ne "") {
            pause;
        }
    } until ("${m}" -eq "");
    return $m;
}
# ----------------------------------------------------------------------------
# Execute.
# ----------------------------------------------------------------------------
# Configure settings.
ConfigureSettings;
ConfigureTools;
LoadCodeBehind;
ShowConfigurationMenu;
# ----------------------------------------------------------------------------
#pause;
# Show certificate menu.
ShowImportExportMenu;
# ----------------------------------------------------------------------------

# SIG # Begin signature block
# MIIpGgYJKoZIhvcNAQcCoIIpCzCCKQcCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCAdG9f4qwDpsbUq
# zUBEPCFkG1ha6W9TwQazcVsUbYDNx6CCEYUwggWBMIIEaaADAgECAhA5ckQ6+SK3
# UdfTbBDdMTWVMA0GCSqGSIb3DQEBDAUAMHsxCzAJBgNVBAYTAkdCMRswGQYDVQQI
# DBJHcmVhdGVyIE1hbmNoZXN0ZXIxEDAOBgNVBAcMB1NhbGZvcmQxGjAYBgNVBAoM
# EUNvbW9kbyBDQSBMaW1pdGVkMSEwHwYDVQQDDBhBQUEgQ2VydGlmaWNhdGUgU2Vy
# dmljZXMwHhcNMTkwMzEyMDAwMDAwWhcNMjgxMjMxMjM1OTU5WjCBiDELMAkGA1UE
# BhMCVVMxEzARBgNVBAgTCk5ldyBKZXJzZXkxFDASBgNVBAcTC0plcnNleSBDaXR5
# MR4wHAYDVQQKExVUaGUgVVNFUlRSVVNUIE5ldHdvcmsxLjAsBgNVBAMTJVVTRVJU
# cnVzdCBSU0EgQ2VydGlmaWNhdGlvbiBBdXRob3JpdHkwggIiMA0GCSqGSIb3DQEB
# AQUAA4ICDwAwggIKAoICAQCAEmUXNg7D2wiz0KxXDXbtzSfTTK1Qg2HiqiBNCS1k
# CdzOiZ/MPans9s/B3PHTsdZ7NygRK0faOca8Ohm0X6a9fZ2jY0K2dvKpOyuR+OJv
# 0OwWIJAJPuLodMkYtJHUYmTbf6MG8YgYapAiPLz+E/CHFHv25B+O1ORRxhFnRghR
# y4YUVD+8M/5+bJz/Fp0YvVGONaanZshyZ9shZrHUm3gDwFA66Mzw3LyeTP6vBZY1
# H1dat//O+T23LLb2VN3I5xI6Ta5MirdcmrS3ID3KfyI0rn47aGYBROcBTkZTmzNg
# 95S+UzeQc0PzMsNT79uq/nROacdrjGCT3sTHDN/hMq7MkztReJVni+49Vv4M0GkP
# Gw/zJSZrM233bkf6c0Plfg6lZrEpfDKEY1WJxA3Bk1QwGROs0303p+tdOmw1XNtB
# 1xLaqUkL39iAigmTYo61Zs8liM2EuLE/pDkP2QKe6xJMlXzzawWpXhaDzLhn4ugT
# ncxbgtNMs+1b/97lc6wjOy0AvzVVdAlJ2ElYGn+SNuZRkg7zJn0cTRe8yexDJtC/
# QV9AqURE9JnnV4eeUB9XVKg+/XRjL7FQZQnmWEIuQxpMtPAlR1n6BB6T1CZGSlCB
# st6+eLf8ZxXhyVeEHg9j1uliutZfVS7qXMYoCAQlObgOK6nyTJccBz8NUvXt7y+C
# DwIDAQABo4HyMIHvMB8GA1UdIwQYMBaAFKARCiM+lvEH7OKvKe+CpX/QMKS0MB0G
# A1UdDgQWBBRTeb9aqitKz1SA4dibwJ3ysgNmyzAOBgNVHQ8BAf8EBAMCAYYwDwYD
# VR0TAQH/BAUwAwEB/zARBgNVHSAECjAIMAYGBFUdIAAwQwYDVR0fBDwwOjA4oDag
# NIYyaHR0cDovL2NybC5jb21vZG9jYS5jb20vQUFBQ2VydGlmaWNhdGVTZXJ2aWNl
# cy5jcmwwNAYIKwYBBQUHAQEEKDAmMCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5j
# b21vZG9jYS5jb20wDQYJKoZIhvcNAQEMBQADggEBABiHUdx0IT2ciuAntzPQLszs
# 8ObLXhHeIm+bdY6ecv7k1v6qH5yWLe8DSn6u9I1vcjxDO8A/67jfXKqpxq7y/Nju
# o3tD9oY2fBTgzfT3P/7euLSK8JGW/v1DZH79zNIBoX19+BkZyUIrE79Yi7qkomYE
# doiRTgyJFM6iTckys7roFBq8cfFb8EELmAAKIgMQ5Qyx+c2SNxntO/HkOrb5RRMm
# da+7qu8/e3c70sQCkT0ZANMXXDnbP3sYDUXNk4WWL13fWRZPP1G91UUYP+1KjugG
# YXQjFrUNUHMnREd/EF2JKmuFMRTE6KlqTIC8anjPuH+OdnKZDJ3+15EIFqGjX5Uw
# ggXLMIIDs6ADAgECAhAablT/n5zg+OrL56CrVp0yMA0GCSqGSIb3DQEBDAUAMIGI
# MQswCQYDVQQGEwJVUzETMBEGA1UECBMKTmV3IEplcnNleTEUMBIGA1UEBxMLSmVy
# c2V5IENpdHkxHjAcBgNVBAoTFVRoZSBVU0VSVFJVU1QgTmV0d29yazEuMCwGA1UE
# AxMlVVNFUlRydXN0IFJTQSBDZXJ0aWZpY2F0aW9uIEF1dGhvcml0eTAeFw0xODA5
# MDYwMDAwMDBaFw0yODA5MDUyMzU5NTlaMFUxCzAJBgNVBAYTAkxWMQ0wCwYDVQQH
# EwRSaWdhMREwDwYDVQQKEwhHb0dldFNTTDEkMCIGA1UEAxMbR29HZXRTU0wgUlNB
# IENvZGVzaWduaW5nIENBMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA
# hYgR23Cnm85C1Bp/636bUuV1Mw5nvyjU9HWywhf4ZXJ0K2jb1U6e2EITKyCjUf/w
# /EsHrM4P+Wa/iQIVwFS2d7Frvvd1wCu5zx9HE/To6UM8GMRiT2lglw33KrL75nXC
# rVVrevF1aPZyCkRGancxV6nLCOYjunPO12LPIARf/VNDB2lgZA8NJYB53aUDx0FL
# 38JUyKGVjzjAMxYBYF88sZ6OO44pd57wDCl2rb8HfOv2FCmfp/iJMWbM1LnCJZOg
# 0nKl8Z0Lx+cHuV/VI8b5VTdBjWc5QShwmjRNnuG5XimOvxf0LXCEUcGr2iKGzcBC
# 5frXQPBZV06cA2lQUDb+fwIDAQABo4IBYTCCAV0wHwYDVR0jBBgwFoAUU3m/Wqor
# Ss9UgOHYm8Cd8rIDZsswHQYDVR0OBBYEFMOspdgTtbAVqNuvEHoZHNYapEcPMA4G
# A1UdDwEB/wQEAwIBhjASBgNVHRMBAf8ECDAGAQH/AgEAMBMGA1UdJQQMMAoGCCsG
# AQUFBwMDMBgGA1UdIAQRMA8wDQYLKwYBBAGyMQECAkAwUAYDVR0fBEkwRzBFoEOg
# QYY/aHR0cDovL2NybC51c2VydHJ1c3QuY29tL1VTRVJUcnVzdFJTQUNlcnRpZmlj
# YXRpb25BdXRob3JpdHkuY3JsMHYGCCsGAQUFBwEBBGowaDA/BggrBgEFBQcwAoYz
# aHR0cDovL2NydC51c2VydHJ1c3QuY29tL1VTRVJUcnVzdFJTQUFkZFRydXN0Q0Eu
# Y3J0MCUGCCsGAQUFBzABhhlodHRwOi8vb2NzcC51c2VydHJ1c3QuY29tMA0GCSqG
# SIb3DQEBDAUAA4ICAQA4nklEQpHLa1IvhRtwFFvADrxCnpdoRDljr0bCeL9w/SuI
# pMWRRJnzfthAttPLA0OB2Mj1D/+HrgnXJXH4hA2YMA2r4XnfevbNiMoIXrvG1J7j
# uTiNVPAJLL84tEJ1iFsNqqMryK19Z59roj8MwELzwJdB+skXnZwZ9xNh3C5xpBqH
# /ROucJl1XLdIQ0Tv/Luy1FIt/Bk+ziDFXnRrc6iUilTj3/uSpC5WLGp0ULgy2UzI
# txyp7xZsguLZ76RWTp4zB7QvX4l4WIW7lDXPzLgm4mU5ANNwI/DfQ+zc7ZTMpuV6
# w2mCbDdnPzvSHjn7MAe7OOA69de4RV9lDU74+9PpXi88dNI+vAqxiq0MuQktxpMc
# GpQhrJQ6og24ngnhHwRzBwW4og85bXBSWuEY4W+/z5yqaxJgp5xrNiSG5Iee54sn
# N2haDoyRTf0Z9ANLQQAR2qP8br7lkcuBFIiVQpYl34tmSz7Dnn0BlBdMXpPtXKo5
# iENDyG3aKQrTY3R3+m13QT9vHPMh2X057b+L59k7hCUIBdCYdZ9trZcnv+ZIRP0V
# aH5khavRIVzWw8m6iZ5bdBmTvPbptzTKpIly8K6I22dn+6NRL3rhGlijZiVD0wu0
# vpRKfM8LGPcvrdrCpt7iINQQ+lDsVbVMkTHAZF5wyw0gIPrWStsGRqgOB5qQZzCC
# Bi0wggUVoAMCAQICEGzHoo06b9M57OS/U1gDtRYwDQYJKoZIhvcNAQELBQAwVTEL
# MAkGA1UEBhMCTFYxDTALBgNVBAcTBFJpZ2ExETAPBgNVBAoTCEdvR2V0U1NMMSQw
# IgYDVQQDExtHb0dldFNTTCBSU0EgQ29kZXNpZ25pbmcgQ0EwHhcNMjAwODAxMDAw
# MDAwWhcNMjMwODAxMjM1OTU5WjCBlDELMAkGA1UEBhMCVVMxEDAOBgNVBBEMB1cx
# MiAwTFgxDzANBgNVBAcMBkxvbmRvbjEWMBQGA1UECQwNV29ybWhvbHQgUm9hZDEa
# MBgGA1UECQwRNSBDbGV2ZXJseSBFc3RhdGUxFjAUBgNVBAoMDUV2YWxkYXMgSm9j
# eXMxFjAUBgNVBAMMDUV2YWxkYXMgSm9jeXMwggIiMA0GCSqGSIb3DQEBAQUAA4IC
# DwAwggIKAoICAQC8mlJ5yB/BJylWgnuny5U6JSWznBiuulK4JK8l277uWb0WEwBq
# Cxlajep7ooAGJSPQDiZYT+vxy6/jLVPTzV2uCQ389upVwxdP5rWb/oNfwM7sV7td
# KSkfiuBVSmVD2x5k/OwVj6jpruYf2u0V3K/2OEDEq6EwP3L5QYy7G83hpsZcI0XK
# XAS/CC1sb79VbdMNZ2RJ0ZJwWonEPF5h76cnv1aH5OdIZXIh6GWq+S8tkSDd/Y27
# lukJfDMUgxcSX/HXyg+e9CuOARUsnwgW6aufcgtB5PlaykwR3kHjpeqKQmQJfdmX
# 2RFhm229qt3pdVAhHDPesIzbrwUdA/0P1n3E9W50M4RHx/cJfBYXSVfvWz2exvYH
# LT740ywbqFtMPsPDdlQkqkX4euLMbtXzRLGuva2kaiNoIFyH5Pz5/ql7yusbe6/d
# BWpKeAkgw+xEKGSEjW+Rme+MNfwm570Q09VrjLQ0UtXotS1c92bnPn1kvsgWxYv2
# jBG4y/i1Uh+n7DrzFL+jt4somAaShYm1HgZsC+JCCfAvQnuMGJJMX7u0s+rX2ipY
# 18wKLXzcqOmkBsK2Cttwed8lYAeBcusQTs8l94NHflaVHdGbtEyDAObvTJ963e2Z
# 8endj+/P81lJ0Xn6i5ixTP71nzxbaKnF6NhJK+eewNroIVdqiijm2/E/wQIDAQAB
# o4IBtzCCAbMwHwYDVR0jBBgwFoAUw6yl2BO1sBWo268Qehkc1hqkRw8wHQYDVR0O
# BBYEFCSIrHnrlEw/TBdVvJQX73fMj2WuMA4GA1UdDwEB/wQEAwIHgDAMBgNVHRMB
# Af8EAjAAMBMGA1UdJQQMMAoGCCsGAQUFBwMDMBEGCWCGSAGG+EIBAQQEAwIEEDBL
# BgNVHSAERDBCMDYGCysGAQQBsjEBAgJAMCcwJQYIKwYBBQUHAgEWGWh0dHBzOi8v
# Y3BzLnVzZXJ0cnVzdC5jb20wCAYGZ4EMAQQBMEYGA1UdHwQ/MD0wO6A5oDeGNWh0
# dHA6Ly9jcmwudXNlcnRydXN0LmNvbS9Hb0dldFNTTFJTQUNvZGVzaWduaW5nQ0Eu
# Y3JsMHgGCCsGAQUFBwEBBGwwajBBBggrBgEFBQcwAoY1aHR0cDovL2NydC51c2Vy
# dHJ1c3QuY29tL0dvR2V0U1NMUlNBQ29kZXNpZ25pbmdDQS5jcnQwJQYIKwYBBQUH
# MAGGGWh0dHA6Ly9vY3NwLnVzZXJ0cnVzdC5jb20wHAYDVR0RBBUwE4ERZXZhbGRh
# c0Bqb2N5cy5jb20wDQYJKoZIhvcNAQELBQADggEBACnqif6MY/7X3NX+19MOAYZ4
# dbwMpZD6rcVUjjZpozmskYTXy3IUDREXYMMhJyfPi71NvCJV5ytOg7xesjXIloaW
# L6x5GaSzku4v29IESOh9OaqpWONW6s87V9L3CXHP6Ec0pwRc/BvALtv5h8hnsVnt
# RTJMVWSwAUsPAcrhhyOLwu9s9OXBV9hKh2HOrskZQM/7h0kk0WlaywwXw6lcuHrw
# HQj3yYamSbZrRTn5vxHPyrz8GHu+iUUTjwerOicH3MPiTVKGgbA2X4SVUHwM98Yu
# cJNoOB4ml3Q/v84oHifyXXXFUxCtcqz0ZNQ2MeTPw0LuevO7vRqZCkc+wLBjhREx
# ghbrMIIW5wIBATBpMFUxCzAJBgNVBAYTAkxWMQ0wCwYDVQQHEwRSaWdhMREwDwYD
# VQQKEwhHb0dldFNTTDEkMCIGA1UEAxMbR29HZXRTU0wgUlNBIENvZGVzaWduaW5n
# IENBAhBsx6KNOm/TOezkv1NYA7UWMA0GCWCGSAFlAwQCAQUAoIHQMBkGCSqGSIb3
# DQEJAzEMBgorBgEEAYI3AgEEMBwGCisGAQQBgjcCAQsxDjAMBgorBgEEAYI3AgEV
# MC8GCSqGSIb3DQEJBDEiBCCDC0rRr16an0MDuhEVZSE7219gjHhgIUHozxOD5EAK
# nTBkBgorBgEEAYI3AgEMMVYwVKA4gDYASgBvAGMAeQBzAC4AYwBvAG0AIABQAG8A
# dwBlAHIAUwBoAGUAbABsACAAUwBjAHIAaQBwAHShGIAWaHR0cHM6Ly93d3cuam9j
# eXMuY29tIDANBgkqhkiG9w0BAQEFAASCAgCMq5Qd2yWMZSljoM37YKXrLKzyopVI
# QBJIWHSGIDqMkcASZ1zS6msMXpmvmSc1v7MYVoRYOmmkvR758pUGw62x/tfzKMF2
# 5G1L0PCIMmf3REaleXqal7V1B9wDXLoYzGmP0C5BxmZ4nAkWNk+dN6pUkLzlF0hi
# pI70lLwU7hQmvFy2LF7qlvH2szXgG6PKrQE5hOY5bmhWcHT5ZLKgw2LrWVy+QRLX
# TbtpsofE1EmGGUYni4r8dxrBuzMjx/KVPJAgWYuYZVDKdAe3MwN0ZL5yLhIq18Ph
# WtGAA/JSyJ20v/8uD4sn7C7/HHcRFLYZGQ3aZepXnkkwF7xjWVVt/jXd7Ji3zn9Z
# CBjNwau8pNhsO1z+rLQYHss3BZCn36wQJ8voYW/wDeX9KPKmupXy0/ZvdneT/FXn
# 7lCS4rFPf+q4ViNju2iHNmgCpcAIQLA+1LrokgY6o4bh4WjDtgLKZenwiDtsXQbz
# mDf2KkMZM1EOHeDlYVOg8tB80FBfyjKI3ho3iD9j8rmTgHFxPmZZrW6nFrMTEEV5
# yzIsWNbNUkwLwX3kpNEpHwfrSFE/QT21UHJszksXGfGPNg2i1EwpJfhG4x0jL02S
# 0lRf4sYphJpmdnPElVGsTFqYFHuUF40l/a8HLt7MRJPqKdSj3H3Vxa3ASwxUGndJ
# G3eVSHLBLwCQ16GCE4AwghN8BgorBgEEAYI3AwMBMYITbDCCE2gGCSqGSIb3DQEH
# AqCCE1kwghNVAgEDMQ8wDQYJYIZIAWUDBAICBQAwggENBgsqhkiG9w0BCRABBKCB
# /QSB+jCB9wIBAQYKKwYBBAGyMQIBATAxMA0GCWCGSAFlAwQCAQUABCDS0VKZtsnz
# wilVpm1+dNk+WfmrgGuT50aqhhT7MrnkSQIVAK8F+fv8jGFhpPQYpf5Hb13Cp9mb
# GA8yMDIxMDQxOTA5MjU0M1qggYqkgYcwgYQxCzAJBgNVBAYTAkdCMRswGQYDVQQI
# ExJHcmVhdGVyIE1hbmNoZXN0ZXIxEDAOBgNVBAcTB1NhbGZvcmQxGDAWBgNVBAoT
# D1NlY3RpZ28gTGltaXRlZDEsMCoGA1UEAwwjU2VjdGlnbyBSU0EgVGltZSBTdGFt
# cGluZyBTaWduZXIgIzKggg37MIIHBzCCBO+gAwIBAgIRAIx3oACP9NGwxj2fOkiD
# jWswDQYJKoZIhvcNAQEMBQAwfTELMAkGA1UEBhMCR0IxGzAZBgNVBAgTEkdyZWF0
# ZXIgTWFuY2hlc3RlcjEQMA4GA1UEBxMHU2FsZm9yZDEYMBYGA1UEChMPU2VjdGln
# byBMaW1pdGVkMSUwIwYDVQQDExxTZWN0aWdvIFJTQSBUaW1lIFN0YW1waW5nIENB
# MB4XDTIwMTAyMzAwMDAwMFoXDTMyMDEyMjIzNTk1OVowgYQxCzAJBgNVBAYTAkdC
# MRswGQYDVQQIExJHcmVhdGVyIE1hbmNoZXN0ZXIxEDAOBgNVBAcTB1NhbGZvcmQx
# GDAWBgNVBAoTD1NlY3RpZ28gTGltaXRlZDEsMCoGA1UEAwwjU2VjdGlnbyBSU0Eg
# VGltZSBTdGFtcGluZyBTaWduZXIgIzIwggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAw
# ggIKAoICAQCRh0ssi8HxHqCe0wfGAcpSsL55eV0JZgYtLzV9u8D7J9pCalkbJUzq
# 70DWmn4yyGqBfbRcPlYQgTU6IjaM+/ggKYesdNAbYrw/ZIcCX+/FgO8GHNxeTpOH
# uJreTAdOhcxwxQ177MPZ45fpyxnbVkVs7ksgbMk+bP3wm/Eo+JGZqvxawZqCIDq3
# 7+fWuCVJwjkbh4E5y8O3Os2fUAQfGpmkgAJNHQWoVdNtUoCD5m5IpV/BiVhgiu/x
# rM2HYxiOdMuEh0FpY4G89h+qfNfBQc6tq3aLIIDULZUHjcf1CxcemuXWmWlRx06m
# nSlv53mTDTJjU67MximKIMFgxvICLMT5yCLf+SeCoYNRwrzJghohhLKXvNSvRByW
# giKVKoVUrvH9Pkl0dPyOrj+lcvTDWgGqUKWLdpUbZuvv2t+ULtka60wnfUwF9/gj
# XcRXyCYFevyBI19UCTgqYtWqyt/tz1OrH/ZEnNWZWcVWZFv3jlIPZvyYP0QGE2Ru
# 6eEVYFClsezPuOjJC77FhPfdCp3avClsPVbtv3hntlvIXhQcua+ELXei9zmVN29O
# fxzGPATWMcV+7z3oUX5xrSR0Gyzc+Xyq78J2SWhi1Yv1A9++fY4PNnVGW5N2xIPu
# gr4srjcS8bxWw+StQ8O3ZpZelDL6oPariVD6zqDzCIEa0USnzPe4MQIDAQABo4IB
# eDCCAXQwHwYDVR0jBBgwFoAUGqH4YRkgD8NBd0UojtE1XwYSBFUwHQYDVR0OBBYE
# FGl1N3u7nTVCTr9X05rbnwHRrt7QMA4GA1UdDwEB/wQEAwIGwDAMBgNVHRMBAf8E
# AjAAMBYGA1UdJQEB/wQMMAoGCCsGAQUFBwMIMEAGA1UdIAQ5MDcwNQYMKwYBBAGy
# MQECAQMIMCUwIwYIKwYBBQUHAgEWF2h0dHBzOi8vc2VjdGlnby5jb20vQ1BTMEQG
# A1UdHwQ9MDswOaA3oDWGM2h0dHA6Ly9jcmwuc2VjdGlnby5jb20vU2VjdGlnb1JT
# QVRpbWVTdGFtcGluZ0NBLmNybDB0BggrBgEFBQcBAQRoMGYwPwYIKwYBBQUHMAKG
# M2h0dHA6Ly9jcnQuc2VjdGlnby5jb20vU2VjdGlnb1JTQVRpbWVTdGFtcGluZ0NB
# LmNydDAjBggrBgEFBQcwAYYXaHR0cDovL29jc3Auc2VjdGlnby5jb20wDQYJKoZI
# hvcNAQEMBQADggIBAEoDeJBCM+x7GoMJNjOYVbudQAYwa0Vq8ZQOGVD/WyVeO+E5
# xFu66ZWQNze93/tk7OWCt5XMV1VwS070qIfdIoWmV7u4ISfUoCoxlIoHIZ6Kvaca
# 9QIVy0RQmYzsProDd6aCApDCLpOpviE0dWO54C0PzwE3y42i+rhamq6hep4TkxlV
# jwmQLt/qiBcW62nW4SW9RQiXgNdUIChPynuzs6XSALBgNGXE48XDpeS6hap6adt1
# pD55aJo2i0OuNtRhcjwOhWINoF5w22QvAcfBoccklKOyPG6yXqLQ+qjRuCUcFubA
# 1X9oGsRlKTUqLYi86q501oLnwIi44U948FzKwEBcwp/VMhws2jysNvcGUpqjQDAX
# sCkWmcmqt4hJ9+gLJTO1P22vn18KVt8SscPuzpF36CAT6Vwkx+pEC0rmE4QcTesN
# tbiGoDCni6GftCzMwBYjyZHlQgNLgM7kTeYqAT7AXoWgJKEXQNXb2+eYEKTx6hkb
# gFT6R4nomIGpdcAO39BolHmhoJ6OtrdCZsvZ2WsvTdjePjIeIOTsnE1CjZ3HM5mC
# N0TUJikmQI54L7nu+i/x8Y/+ULh43RSW3hwOcLAqhWqxbGjpKuQQK24h/dN8nTfk
# KgbWw/HXaONPB3mBCBP+smRe6bE85tB4I7IJLOImYr87qZdRzMdEMoGyr8/fMIIG
# 7DCCBNSgAwIBAgIQMA9vrN1mmHR8qUY2p3gtuTANBgkqhkiG9w0BAQwFADCBiDEL
# MAkGA1UEBhMCVVMxEzARBgNVBAgTCk5ldyBKZXJzZXkxFDASBgNVBAcTC0plcnNl
# eSBDaXR5MR4wHAYDVQQKExVUaGUgVVNFUlRSVVNUIE5ldHdvcmsxLjAsBgNVBAMT
# JVVTRVJUcnVzdCBSU0EgQ2VydGlmaWNhdGlvbiBBdXRob3JpdHkwHhcNMTkwNTAy
# MDAwMDAwWhcNMzgwMTE4MjM1OTU5WjB9MQswCQYDVQQGEwJHQjEbMBkGA1UECBMS
# R3JlYXRlciBNYW5jaGVzdGVyMRAwDgYDVQQHEwdTYWxmb3JkMRgwFgYDVQQKEw9T
# ZWN0aWdvIExpbWl0ZWQxJTAjBgNVBAMTHFNlY3RpZ28gUlNBIFRpbWUgU3RhbXBp
# bmcgQ0EwggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAwggIKAoICAQDIGwGv2Sx+iJl9
# AZg/IJC9nIAhVJO5z6A+U++zWsB21hoEpc5Hg7XrxMxJNMvzRWW5+adkFiYJ+9Uy
# UnkuyWPCE5u2hj8BBZJmbyGr1XEQeYf0RirNxFrJ29ddSU1yVg/cyeNTmDoqHvzO
# WEnTv/M5u7mkI0Ks0BXDf56iXNc48RaycNOjxN+zxXKsLgp3/A2UUrf8H5VzJD0B
# KLwPDU+zkQGObp0ndVXRFzs0IXuXAZSvf4DP0REKV4TJf1bgvUacgr6Unb+0ILBg
# frhN9Q0/29DqhYyKVnHRLZRMyIw80xSinL0m/9NTIMdgaZtYClT0Bef9Maz5yIUX
# x7gpGaQpL0bj3duRX58/Nj4OMGcrRrc1r5a+2kxgzKi7nw0U1BjEMJh0giHPYla1
# IXMSHv2qyghYh3ekFesZVf/QOVQtJu5FGjpvzdeE8NfwKMVPZIMC1Pvi3vG8Aij0
# bdonigbSlofe6GsO8Ft96XZpkyAcSpcsdxkrk5WYnJee647BeFbGRCXfBhKaBi2f
# A179g6JTZ8qx+o2hZMmIklnLqEbAyfKm/31X2xJ2+opBJNQb/HKlFKLUrUMcpEmL
# QTkUAx4p+hulIq6lw02C0I3aa7fb9xhAV3PwcaP7Sn1FNsH3jYL6uckNU4B9+rY5
# WDLvbxhQiddPnTO9GrWdod6VQXqngwIDAQABo4IBWjCCAVYwHwYDVR0jBBgwFoAU
# U3m/WqorSs9UgOHYm8Cd8rIDZsswHQYDVR0OBBYEFBqh+GEZIA/DQXdFKI7RNV8G
# EgRVMA4GA1UdDwEB/wQEAwIBhjASBgNVHRMBAf8ECDAGAQH/AgEAMBMGA1UdJQQM
# MAoGCCsGAQUFBwMIMBEGA1UdIAQKMAgwBgYEVR0gADBQBgNVHR8ESTBHMEWgQ6BB
# hj9odHRwOi8vY3JsLnVzZXJ0cnVzdC5jb20vVVNFUlRydXN0UlNBQ2VydGlmaWNh
# dGlvbkF1dGhvcml0eS5jcmwwdgYIKwYBBQUHAQEEajBoMD8GCCsGAQUFBzAChjNo
# dHRwOi8vY3J0LnVzZXJ0cnVzdC5jb20vVVNFUlRydXN0UlNBQWRkVHJ1c3RDQS5j
# cnQwJQYIKwYBBQUHMAGGGWh0dHA6Ly9vY3NwLnVzZXJ0cnVzdC5jb20wDQYJKoZI
# hvcNAQEMBQADggIBAG1UgaUzXRbhtVOBkXXfA3oyCy0lhBGysNsqfSoF9bw7J/Ra
# oLlJWZApbGHLtVDb4n35nwDvQMOt0+LkVvlYQc/xQuUQff+wdB+PxlwJ+TNe6qAc
# Jlhc87QRD9XVw+K81Vh4v0h24URnbY+wQxAPjeT5OGK/EwHFhaNMxcyyUzCVpNb0
# llYIuM1cfwGWvnJSajtCN3wWeDmTk5SbsdyybUFtZ83Jb5A9f0VywRsj1sJVhGbk
# s8VmBvbz1kteraMrQoohkv6ob1olcGKBc2NeoLvY3NdK0z2vgwY4Eh0khy3k/ALW
# PncEvAQ2ted3y5wujSMYuaPCRx3wXdahc1cFaJqnyTdlHb7qvNhCg0MFpYumCf/R
# oZSmTqo9CfUFbLfSZFrYKiLCS53xOV5M3kg9mzSWmglfjv33sVKRzj+J9hyhtal1
# H3G/W0NdZT1QgW6r8NDT/LKzH7aZlib0PHmLXGTMze4nmuWgwAxyh8FuTVrTHurw
# ROYybxzrF06Uw3hlIDsPQaof6aFBnf6xuKBlKjTg3qj5PObBMLvAoGMs/FwWAKjQ
# xH/qEZ0eBsambTJdtDgJK0kHqv3sMNrxpy/Pt/360KOE2See+wFmd7lWEOEgbsau
# sfm2usg1XTN2jvF8IAwqd661ogKGuinutFoAsYyr4/kKyVRd1LlqdJ69SK6YMYIE
# LTCCBCkCAQEwgZIwfTELMAkGA1UEBhMCR0IxGzAZBgNVBAgTEkdyZWF0ZXIgTWFu
# Y2hlc3RlcjEQMA4GA1UEBxMHU2FsZm9yZDEYMBYGA1UEChMPU2VjdGlnbyBMaW1p
# dGVkMSUwIwYDVQQDExxTZWN0aWdvIFJTQSBUaW1lIFN0YW1waW5nIENBAhEAjHeg
# AI/00bDGPZ86SIONazANBglghkgBZQMEAgIFAKCCAWswGgYJKoZIhvcNAQkDMQ0G
# CyqGSIb3DQEJEAEEMBwGCSqGSIb3DQEJBTEPFw0yMTA0MTkwOTI1NDNaMD8GCSqG
# SIb3DQEJBDEyBDAyU8tGiT3Xu5vxFuK3QZ7vmvxhWih4ZoMvDq/uc/Pfw8xeKbg1
# piX0QzoKZsfhJccwge0GCyqGSIb3DQEJEAIMMYHdMIHaMIHXMBYEFJURNxAdiC8x
# vVE/lJraTGitjAj1MIG8BBQC1luV4oNwwVcAlfqI+SPdk3+tjzCBozCBjqSBizCB
# iDELMAkGA1UEBhMCVVMxEzARBgNVBAgTCk5ldyBKZXJzZXkxFDASBgNVBAcTC0pl
# cnNleSBDaXR5MR4wHAYDVQQKExVUaGUgVVNFUlRSVVNUIE5ldHdvcmsxLjAsBgNV
# BAMTJVVTRVJUcnVzdCBSU0EgQ2VydGlmaWNhdGlvbiBBdXRob3JpdHkCEDAPb6zd
# Zph0fKlGNqd4LbkwDQYJKoZIhvcNAQEBBQAEggIAhYY5mKOyA5zfWWCTkOPmoeQ+
# 65CyROcwOrccKEcmtvv8K/dRryyNl8CRpGWWsWcaV/PQRkrt0HVfCWu8Sz2vnWzm
# zodt8VqpmhGDX0RHcg13ZATFge2eIpbSkQwOSU3mQ3qxXu/+IrVt7qOHgC4fmwYd
# NFgF1fBCquukgaVXDxQO+S+IpWxZfFx2yRx0z3N7jpRMUGWAZeiyLsjU+QK6hwhT
# 9Pm2kPRxoyC51CIvvB/5kwWy3hCC+su1CNqqaM+2063KBZATB80e9HZa8rWLltaY
# n6NmOjPLDGNR+bbfy3ZSEb7Dm9cCtE50rfEgzJjgOXVuk3P5N1FTm/5vKvkAZO7/
# eu+R4mqnq0oUnu/0bR0iAaB0pt84V8W5Modj6nNFjlJ722C+usjQf2uZ1Yh7dCJa
# mYPjqrNPzapMhQSuzNwDviHiBzYnQHxJJLO+5zVZmDxb0w6c2/WcEwXqMszOyK8R
# 7NKtLeMMCsfvC+VBU/zBiavr7OR3IWUBDRdKPlUZiJsL9jVGsyPt+qIZfIz3gdXr
# fj3V+qY/2wW0xSuipb1o2aOvKUqu9kDJcE5nQXZb+S3E2qblbl6OQjx1XmDnGc9W
# Iywz7XtZsZYQLIoNJ3k7igKO5/sPHo1O+0xgMmr0s/4ZYutUDw4O1W7NXbAL2bLo
# 6RfslUlX2Xn3BttkvSk=
# SIG # End signature block
