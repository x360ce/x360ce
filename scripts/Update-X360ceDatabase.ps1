<#
.SYNOPSIS
    Updates the built-in x360ce controller database, Engine\Resources\gamecontrollerdb_x360ce.windows.txt.gz.
.DESCRIPTION
    Run by hand when the built-in copy should be renewed, never in a build. By default it downloads the file
    x360ce's server writes (Web\ControllerDatabase.ashx), checks its header and that it holds as many whole rows as the
    header says, and only then replaces the built-in copy, gzip compressed as SDL's database beside it is. Both
    programs and the xinput library carry that file, so build them again afterwards.

    -FromPublicService makes the file without the server's own page, from x360ce's public settings service, through
    the engine's own writer (X360ceDatabase.Write): every controller SDL's database knows, the most popular list, the
    controllers connected now and some well-known wheels. That copy is partial, and its header says so. It needs
    Windows PowerShell and a built engine, Engine\bin\Release\x360ce.Engine.dll.
.PARAMETER Url
    The server's file.
.PARAMETER FromPublicService
    Make the file from x360ce's public settings service instead.
.PARAMETER ServiceUrl
    x360ce's public settings service, for -FromPublicService.
.EXAMPLE
    .\scripts\Update-X360ceDatabase.ps1
.EXAMPLE
    powershell.exe -File .\scripts\Update-X360ceDatabase.ps1 -FromPublicService
#>
[CmdletBinding()]
param(
	[string]$Url = 'https://www.x360ce.com/x360ce/ControllerDatabase.ashx',
	[switch]$FromPublicService,
	[string]$ServiceUrl = 'https://www.x360ce.com/x360ce/webservices/x360ce.asmx'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$target = Join-Path $root 'Engine\Resources\gamecontrollerdb_x360ce.windows.txt.gz'

#==============================================================================
# Function: Save-GzipFile
#==============================================================================
<#
.SYNOPSIS
    Writes bytes to a file, gzip compressed.
.DESCRIPTION
    At the smallest size .NET offers: gzip's level 9 where the runtime has it (PowerShell 7), otherwise its optimal
    level (Windows PowerShell). The programs and the xinput library read either.
#>
function Save-GzipFile {
	[CmdletBinding()]
	param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][byte[]]$Bytes)
	$level = [IO.Compression.CompressionLevel]::Optimal
	if ([enum]::GetNames([IO.Compression.CompressionLevel]) -contains 'SmallestSize') { $level = [IO.Compression.CompressionLevel]'SmallestSize' }
	$file = [IO.File]::Create($Path)
	try {
		$gzip = New-Object IO.Compression.GZipStream($file, $level)
		try { $gzip.Write($Bytes, 0, $Bytes.Length) } finally { $gzip.Dispose() }
	} finally { $file.Dispose() }
}

#==============================================================================
# Function: Test-X360ceDatabase
#==============================================================================
<#
.SYNOPSIS
    Checks a controller database's text: its header, and as many rows as the header says.
.OUTPUTS
    [int] The number of rows. Throws when the text is not the database.
#>
function Test-X360ceDatabase {
	[CmdletBinding()]
	param([Parameter(Mandatory = $true)][string]$Text)
	$lines = $Text -split "`r?`n"
	if ($lines[0] -notlike "# x360ce's controller database*") { throw 'The file does not start with the controller database header.' }
	$made = $lines | Where-Object { $_ -like '# Made: *' } | Select-Object -First 1
	$said = $lines | Where-Object { $_ -match '^# Rows: (\d+)$' } | Select-Object -First 1
	if (-not $made -or -not $said) { throw 'The header does not say when the file was made or how many rows it holds.' }
	$expected = [int]($said -replace '^# Rows: ', '')
	# Whole rows only: a line break inside a row leaves pieces that do not end with its platform.
	$rows = @($lines | Where-Object { $_ -match '^[0-9a-f]{32},.*,platform:Windows,$' }).Count
	if ($rows -ne $expected) { throw "The header says $expected rows; the file holds $rows." }
	if ($rows -lt 100) { throw "The file holds only $rows rows." }
	return $rows
}

#==============================================================================
# Function: Get-PublicServiceDatabase
#==============================================================================
<#
.SYNOPSIS
    Makes the controller database from x360ce's public settings service, through the engine's writer.
.OUTPUTS
    [string] The database's text, marked partial.
#>
function Get-PublicServiceDatabase {
	[CmdletBinding()]
	param()
	if ($PSVersionTable.PSEdition -eq 'Core') { throw '-FromPublicService needs Windows PowerShell: the engine is a .NET Framework library.' }
	$engine = Join-Path $root 'Engine\bin\Release\x360ce.Engine.dll'
	if (-not (Test-Path $engine)) { throw "Build the engine first: $engine is missing." }
	Add-Type -Path $engine
	# Product GUIDs, vendor and product in front, then "PIDVID".
	$guids = New-Object 'System.Collections.Generic.List[guid]'
	$seen = New-Object 'System.Collections.Generic.HashSet[guid]'
	$add = {
		param([int]$vendor, [int]$product)
		$g = [guid]('{0:x4}{1:x4}-0000-0000-0000-504944564944' -f $product, $vendor)
		if ($seen.Add($g)) { $guids.Add($g) }
	}
	# Every Windows controller of SDL's database the engine carries.
	foreach ($row in [x360ce.Engine.SdlSource]::Parse([x360ce.Engine.SdlDatabase]::ReadBuiltIn(), [x360ce.Engine.SdlSourceKind]::BuiltIn, $null).Mappings) {
		if ($row.Guid.HasIds) { & $add $row.Guid.VendorId $row.Guid.ProductId }
	}
	# Controllers connected now.
	foreach ($id in (Get-PnpDevice -PresentOnly -Class HIDClass -ErrorAction SilentlyContinue).InstanceId) {
		if ($id -match 'VID_([0-9A-F]{4})&PID_([0-9A-F]{4})') { & $add ([Convert]::ToInt32($Matches[1], 16)) ([Convert]::ToInt32($Matches[2], 16)) }
	}
	# Wheels and pedals many people use: Logitech, Thrustmaster, Fanatec, Microsoft.
	$wheels = @(
		0x046D, 0xC29B, 0x046D, 0xC299, 0x046D, 0xC24F, 0x046D, 0xC262, 0x046D, 0xC29A, 0x046D, 0xC298, 0x046D, 0xC294,
		0x046D, 0xC293, 0x046D, 0xCA03, 0x046D, 0xCA04, 0x046D, 0xC295, 0x046D, 0xC266, 0x046D, 0xC267, 0x046D, 0xC26D,
		0x046D, 0xC26E, 0x044F, 0xB66E, 0x044F, 0xB66D, 0x044F, 0xB677, 0x044F, 0xB67F, 0x044F, 0xB65E, 0x044F, 0xB696,
		0x044F, 0xB65D, 0x044F, 0xB664, 0x044F, 0xB668, 0x044F, 0xB689, 0x0EB7, 0x0E03, 0x0EB7, 0x0005, 0x0EB7, 0x0020,
		0x045E, 0x0034, 0x045E, 0x001A, 0x06A3, 0x0764)
	for ($i = 0; $i -lt $wheels.Count; $i += 2) { & $add $wheels[$i] $wheels[$i + 1] }
	$ws = New-Object x360ce.Engine.WebServiceClient
	$ws.Url = $ServiceUrl
	# Every setting of every controller: the writer keeps the one most people use, passing over a setting that sets nothing.
	$found = @{}
	$settings = @{}
	$take = {
		param($result)
		foreach ($ps in $result.PadSettings) { $settings[$ps.PadSettingChecksum] = $ps }
		$rows = @()
		if ($result.Summaries) { $rows += $result.Summaries | ForEach-Object { [pscustomobject]@{ Guid = $_.ProductGuid; Name = $_.ProductName; Users = $_.Users; Checksum = $_.PadSettingChecksum } } }
		if ($result.Presets) { $rows += $result.Presets | ForEach-Object { [pscustomobject]@{ Guid = $_.ProductGuid; Name = $_.ProductName; Users = $_.Users; Checksum = $_.PadSettingChecksum } } }
		foreach ($r in $rows) {
			$key = "$($r.Guid) $($r.Checksum)"
			if (-not $found.ContainsKey($key) -or $found[$key].Users -lt $r.Users) { $found[$key] = $r }
		}
	}
	# The most popular list: one empty search parameter.
	& $take ($ws.SearchSettings([x360ce.Engine.SearchParameter[]]@((New-Object x360ce.Engine.SearchParameter))))
	for ($i = 0; $i -lt $guids.Count; $i += 20) {
		$batch = $guids.GetRange($i, [Math]::Min(20, $guids.Count - $i)) | ForEach-Object { $p = New-Object x360ce.Engine.SearchParameter; $p.ProductGuid = $_; $p }
		for ($try = 1; ; $try++) {
			try { & $take ($ws.SearchSettings([x360ce.Engine.SearchParameter[]]@($batch))); break }
			catch { if ($try -ge 3) { throw }; Start-Sleep -Seconds 5 }
		}
		Write-Host ("Asked for {0} of {1} controllers." -f [Math]::Min($i + 20, $guids.Count), $guids.Count)
		Start-Sleep -Milliseconds 500
	}
	$entries = New-Object 'System.Collections.Generic.List[x360ce.Engine.X360ceDatabaseEntry]'
	foreach ($r in $found.Values) {
		if (-not $settings.ContainsKey($r.Checksum)) { continue }
		$e = New-Object x360ce.Engine.X360ceDatabaseEntry
		$e.ProductGuid = $r.Guid; $e.ProductName = $r.Name; $e.Users = $r.Users; $e.Setting = $settings[$r.Checksum]
		$entries.Add($e)
	}
	$note = 'Partial copy: made from x360ce''s public settings service for SDL''s controllers, the most popular list, the controllers connected and known wheels, until the server serves the whole file.'
	return [x360ce.Engine.X360ceDatabase]::Write($entries, 'x360ce''s public settings service', [DateTime]::UtcNow, $note)
}

if ($FromPublicService) {
	$text = Get-PublicServiceDatabase
} else {
	$response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 120
	$text = [Text.Encoding]::UTF8.GetString($response.RawContentStream.ToArray()).TrimStart([char]0xFEFF)
}
$rows = Test-X360ceDatabase -Text $text
Save-GzipFile -Path $target -Bytes (New-Object Text.UTF8Encoding $false).GetBytes(($text -replace "`r`n", "`n"))
Write-Host "Saved $rows rows to $target. Build the engine, both programs and the xinput library again."
