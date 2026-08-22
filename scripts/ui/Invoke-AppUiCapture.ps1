<#
.SYNOPSIS
    Drives a running x360ce app window (tab selection) and captures it to PNG.
.DESCRIPTION
    Finds the first process with a main window matching -ProcessName, optionally
    resizes it (no focus stealing), selects tabs by posting TCM_SETCURFOCUS to the
    native SysTabControl32 children (works without UI Automation and without
    stealing the user's foreground window), then captures the window with
    PrintWindow(PW_RENDERFULLCONTENT), which works even when the window is
    occluded. Used to verify UI rendering during the 4.17.x reapply-fixes work.
.PARAMETER SelectTabs
    Flattened pairs: tabControlIndex, itemIndex, ... Tab controls are ordered by
    area, largest first (0 = main tab strip). Example: -SelectTabs 0,6 selects
    item 6 of the main tab strip.
.PARAMETER Capture
    Output PNG file name, written into -OutDir.
.PARAMETER OutDir
    Output folder. Default: scripts/ui/captures (git-ignored; captures are
    evidence, never committed).
.PARAMETER SettleMs
    Milliseconds to wait after each tab selection before capturing.
.PARAMETER NoResize
    Keep the window's current size. Without it the window is resized to
    1600x1200 physical pixels so all controls fit the capture.
.PARAMETER ProcessName
    Process to target. Default: x360ce.
.EXAMPLE
    ./scripts/ui/Invoke-AppUiCapture.ps1 -NoResize -Capture pad1.png
.EXAMPLE
    ./scripts/ui/Invoke-AppUiCapture.ps1 -SelectTabs 0,6 -Capture devices.png
.NOTES
    Run in a fresh PowerShell process (Add-Type types cannot be redefined).
#>
param(
    [int[]]$SelectTabs = @(),
    [string]$Capture = "shot.png",
    [string]$OutDir = "$PSScriptRoot\captures",
    [int]$SettleMs = 1500,
    [switch]$NoResize,
    [string]$ProcessName = "x360ce"
)
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System; using System.Text; using System.Collections.Generic; using System.Runtime.InteropServices;
public struct RECT { public int L, T, R, B; }
public static class W3 {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
  [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr h, EnumProc p, IntPtr l);
  [DllImport("user32.dll")] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  public static List<IntPtr> FindTabControls(IntPtr parent) {
    var list = new List<IntPtr>();
    EnumChildWindows(parent, (h, l) => {
      var sb = new StringBuilder(256); GetClassName(h, sb, 256);
      if (sb.ToString().Contains("SysTabControl32") && IsWindowVisible(h)) list.Add(h);
      return true;
    }, IntPtr.Zero);
    return list;
  }
}
"@
function Get-SortedTabs([IntPtr]$hwnd) {
    $tabs = [W3]::FindTabControls($hwnd)
    $tabs | Sort-Object {
        $r = New-Object RECT; [W3]::GetWindowRect($_, [ref]$r) | Out-Null
        -1 * ($r.R - $r.L) * ($r.B - $r.T)
    }
}
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }
$p = Get-Process $ProcessName -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not $p) { throw "$ProcessName is not running with a main window" }
# Raise (and optionally resize) without activating: SWP_NOACTIVATE|SHOWWINDOW.
if ($NoResize) {
    [W3]::SetWindowPos($p.MainWindowHandle, [IntPtr]::Zero, 0, 0, 0, 0, 0x0001 -bor 0x0002 -bor 0x0010 -bor 0x0040) | Out-Null
} else {
    [W3]::SetWindowPos($p.MainWindowHandle, [IntPtr]::Zero, 50, 50, 1600, 1200, 0x0010 -bor 0x0040) | Out-Null
}
Start-Sleep -Milliseconds 500
for ($i = 0; $i -lt $SelectTabs.Count; $i += 2) {
    $tabs = @(Get-SortedTabs $p.MainWindowHandle)
    $tc = $tabs[$SelectTabs[$i]]
    [W3]::SendMessage($tc, 0x1330, [IntPtr]$SelectTabs[$i + 1], [IntPtr]::Zero) | Out-Null  # TCM_SETCURFOCUS
    Start-Sleep -Milliseconds $SettleMs
}
$r = New-Object RECT; [W3]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
$w = $r.R - $r.L; $ht = $r.B - $r.T
$bmp = New-Object System.Drawing.Bitmap $w, $ht
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[W3]::PrintWindow($p.MainWindowHandle, $hdc, 2) | Out-Null   # 2 = PW_RENDERFULLCONTENT
$g.ReleaseHdc($hdc)
$bmp.Save("$OutDir\$Capture")
$g.Dispose(); $bmp.Dispose()
Write-Host "captured $OutDir\$Capture; tab controls:"
foreach ($t in @(Get-SortedTabs $p.MainWindowHandle)) {
    $r2 = New-Object RECT; [W3]::GetWindowRect($t, [ref]$r2) | Out-Null
    $cnt = [W3]::SendMessage($t, 0x1304, [IntPtr]::Zero, [IntPtr]::Zero)   # TCM_GETITEMCOUNT
    Write-Host "  hwnd=$t rect=$($r2.L),$($r2.T) $($r2.R - $r2.L)x$($r2.B - $r2.T) items=$cnt"
}
