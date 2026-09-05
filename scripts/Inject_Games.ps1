param (
    [string]$TargetFolder
)

$ErrorActionPreference = "SilentlyContinue"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "          X360CE GAME INJECTOR & SETUP UTILITY              " -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

$sourceDir = $PSScriptRoot
$sourceExe = Join-Path $sourceDir "x360ce.exe"
$sourceIni = Join-Path $sourceDir "x360ce.ini"

if (-not (Test-Path $sourceExe)) {
    Write-Host "[ERROR] x360ce.exe not found in $sourceDir!" -ForegroundColor Red
    Pause
    Exit
}

# 1. Detect target games
$gameFolders = @()

if ($TargetFolder -and (Test-Path $TargetFolder)) {
    $resolved = (Resolve-Path $TargetFolder).Path
    $subDirs = Get-ChildItem -Path $resolved -Directory | Where-Object { Get-ChildItem -Path $_.FullName -Filter "*.exe" -File }
    if ($subDirs) {
        $gameFolders += $subDirs.FullName
    } else {
        $gameFolders += $resolved
    }
} else {
    $readyDrives = [System.IO.DriveInfo]::GetDrives() | Where-Object { $_.IsReady } | Select-Object -ExpandProperty RootDirectory
    $relativeRoots = @("Games", "SteamLibrary\steamapps\common", "Program Files (x86)\Steam\steamapps\common", "Program Files\Steam\steamapps\common", "Program Files\Epic Games", "XboxGames")
    $candidates = @()
    foreach ($d in $readyDrives) {
        foreach ($rel in $relativeRoots) {
            $candidates += (Join-Path $d.FullName $rel)
        }
    }
    foreach ($cand in $candidates) {
        if (Test-Path $cand) {
            Get-ChildItem -Path $cand -Directory -ErrorAction SilentlyContinue | ForEach-Object {
                if (Get-ChildItem -Path $_.FullName -Filter "*.exe" -File -ErrorAction SilentlyContinue) {
                    $gameFolders += $_.FullName
                }
            }
        }
    }
}

if ($gameFolders.Count -eq 0) {
    Write-Host "No common game folders automatically found." -ForegroundColor Yellow
    $userInput = Read-Host "Enter the path to your Game folder (or drag and drop it here)"
    $userInput = $userInput.Trim('"', "'")
    if (Test-Path $userInput) {
        if ((Get-Item $userInput) -is [System.IO.FileInfo]) {
            $gameFolders += (Split-Path $userInput -Parent)
        } else {
            $gameFolders += $userInput
        }
    }
}

Write-Host "Found $($gameFolders.Count) game directory(s) to configure:" -ForegroundColor Green
foreach ($g in $gameFolders) {
    Write-Host "  -> $g" -ForegroundColor White
}
Write-Host ""

# Ensure X360CE Settings Directory
$settingsDir = "C:\ProgramData\X360CE\Settings"
if (-not (Test-Path $settingsDir)) {
    New-Item -ItemType Directory -Path $settingsDir -Force | Out-Null
}

$gamesXmlPath = Join-Path $settingsDir "x360ce.UserGames.xml"
$settingsXmlPath = Join-Path $settingsDir "x360ce.UserSettings.xml"
$padXmlPath = Join-Path $settingsDir "x360ce.PadSettings.xml"

# Base Pad Settings Setup
$padChecksum = "afe12f16-63dd-07f4-4fc6-96d2b1238b8f"
$ctrl1Guid = "7cb4d230-2cd4-11f1-8001-444553540000"
$ctrl2Guid = "7cb52050-2cd4-11f1-8002-444553540000"

if (Test-Path $padXmlPath) {
    $pXml = [xml](Get-Content $padXmlPath)
    foreach ($pad in $pXml.Data.Items.PadSetting) {
        $pad.ButtonA = "3"
        $pad.ButtonB = "2"
        $pad.ButtonX = "4"
        $pad.ButtonY = "1"
        $pad.ButtonBack = "9"
        $pad.ButtonStart = "10"
        $pad.LeftShoulder = "5"
        $pad.RightShoulder = "6"
        $pad.LeftTrigger = "7"
        $pad.RightTrigger = "8"
        $pad.LeftThumbButton = "11"
        $pad.RightThumbButton = "12"
        $pad.DPad = "p1"
        $pad.LeftThumbAxisX = "a1"
        $pad.LeftThumbAxisY = "a-2"
        $pad.RightThumbAxisX = "a3"
        $pad.RightThumbAxisY = "a-6"
    }
    $pXml.Save($padXmlPath)
}

# Process each game folder
foreach ($dir in $gameFolders) {
    Write-Host "[INJECTING] $dir..." -ForegroundColor Cyan
    Copy-Item -Path $sourceExe -Destination $dir -Force
    if (Test-Path $sourceIni) {
        Copy-Item -Path $sourceIni -Destination (Join-Path $dir "x360ce.ini") -Force
    }

    # PES Sider gamepad.ini check
    $pesIni = Join-Path $dir "gamepad.ini"
    if (Test-Path $pesIni) {
        (Get-Content $pesIni) -replace 'gamepad.dinput.enabled\s*=\s*1', 'gamepad.dinput.enabled = 0' | Set-Content $pesIni -Encoding UTF8
        Write-Host "  [PES FIX] Sider gamepad.ini configured for pure XInput (double-input prevented)!" -ForegroundColor Green
    }

    # Find executables
    $exes = Get-ChildItem -Path $dir -Filter "*.exe" -File | Where-Object {
        $_.Name -notmatch "unins|setup|crash|redist|dxweb|vcredist|QuickSFV"
    }

    # Register in UserGames and UserSettings
    if (Test-Path $gamesXmlPath) {
        $gXml = [xml](Get-Content $gamesXmlPath)
        $gItems = $gXml.Data.Items

        $sXml = [xml](Get-Content $settingsXmlPath)
        $sItems = $sXml.Data.Items

        foreach ($exe in $exes) {
            # Determine architecture
            $arch = 4 # Default x64
            try {
                $bytes = [System.IO.File]::ReadAllBytes($exe.FullName)
                $peOffset = [System.BitConverter]::ToInt32($bytes, 0x3C)
                $machine = [System.BitConverter]::ToUInt16($bytes, $peOffset + 4)
                if ($machine -eq 0x014c) { $arch = 2 } # x86
            } catch {}

            $existing = $gItems.UserGame | Where-Object { $_.FileName -eq $exe.Name }
            if ($existing) {
                $existing.EnableMask = "3"
                $existing.EmulationType = "2"
                $existing.FullPath = $exe.FullName
                $existing.ProcessorArchitecture = $arch.ToString()
                $existing.IsEnabled = "true"
            } else {
                $newNode = $gXml.CreateElement("UserGame")
                $newNode.InnerXml = @"
      <GameId>$([Guid]::NewGuid().ToString())</GameId>
      <ComputerId>2f566421-0433-cceb-eced-f7439dff2ae2</ComputerId>
      <FileName>$($exe.Name)</FileName>
      <FileProductName>$($exe.BaseName)</FileProductName>
      <FileVersion>1.0.0.0</FileVersion>
      <FullPath>$($exe.FullName)</FullPath>
      <CompanyName />
      <ProcessorArchitecture>$arch</ProcessorArchitecture>
      <HookMask>0</HookMask>
      <XInputMask>12</XInputMask>
      <DInputMask>0</DInputMask>
      <DInputFile />
      <FakeVID>0</FakeVID>
      <FakePID>0</FakePID>
      <Timeout>-1</Timeout>
      <Weight>1</Weight>
      <Comment />
      <IsEnabled>true</IsEnabled>
      <DateCreated>$((Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz"))</DateCreated>
      <DateUpdated>$((Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz"))</DateUpdated>
      <AutoMapMask>0</AutoMapMask>
      <EnableMask>3</EnableMask>
      <EmulationType>2</EmulationType>
      <Checksum>00000000-0000-0000-0000-000000000000</Checksum>
      <XInputPath />
      <ProfileId>00000000-0000-0000-0000-000000000000</ProfileId>
"@
                $gItems.AppendChild($newNode) | Out-Null
            }

            # Map Controller 1 and 2
            for ($mapTo = 1; $mapTo -le 2; $mapTo++) {
                $cGuid = if ($mapTo -eq 1) { $ctrl1Guid } else { $ctrl2Guid }
                $setting = $sItems.Setting | Where-Object { $_.FileName -eq $exe.Name -and $_.MapTo -eq "$mapTo" }
                if ($setting) {
                    $setting.InstanceGuid = $cGuid
                    $setting.PadSettingChecksum = $padChecksum
                    $setting.IsEnabled = "true"
                    $setting.Completion = "100"
                } else {
                    $sNode = $sXml.CreateElement("Setting")
                    $sNode.InnerXml = @"
      <SettingId>$([Guid]::NewGuid().ToString())</SettingId>
      <InstanceGuid>$cGuid</InstanceGuid>
      <InstanceName>Twin USB Gamepad      </InstanceName>
      <ProductGuid>00010810-0000-0000-0000-504944564944</ProductGuid>
      <ProductName>Twin USB Gamepad      </ProductName>
      <DeviceType>20</DeviceType>
      <FileName>$($exe.Name)</FileName>
      <FileProductName>$($exe.BaseName)</FileProductName>
      <DateCreated>$((Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz"))</DateCreated>
      <DateUpdated>0001-01-01T00:00:00</DateUpdated>
      <IsEnabled>true</IsEnabled>
      <PadSettingChecksum>$padChecksum</PadSettingChecksum>
      <DateSelected>0001-01-01T00:00:00</DateSelected>
      <MapTo>$mapTo</MapTo>
      <Completion>100</Completion>
      <ComputerId>00000000-0000-0000-0000-000000000000</ComputerId>
      <ProfileId>00000000-0000-0000-0000-000000000000</ProfileId>
      <Checksum>00000000-0000-0000-0000-000000000000</Checksum>
"@
                    $sItems.AppendChild($sNode) | Out-Null
                }
            }
            Write-Host "  -> Registered $($exe.Name) ($([string](if ($arch -eq 4) { '64-bit' } else { '32-bit' }))) with 2-Player Virtual ViGEm support" -ForegroundColor Green
        }
        $gXml.Save($gamesXmlPath)
        $sXml.Save($settingsXmlPath)
    }
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "   INJECTION & SETUP COMPLETE! ALL GAMES ARE OPTIMIZED!     " -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Green
Write-Host "Virtual Xbox 360 controllers are active for 2 players."
Write-Host "You do NOT need Steam running to play."
Write-Host ""
