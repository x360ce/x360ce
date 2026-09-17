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
.PARAMETER Width
    Window width to capture at, in logical pixels (the size at 100 % zoom).
    Multiplied by the zoom of the screen the window is on, so the picture shows
    the same amount of the program on every screen: 1100 is 1100 pixels at
    100 % and 1650 at 150 %. Default: the program's own size, 1100.
.PARAMETER Height
    Window height in logical pixels, scaled the same way. Default: 850.
.PARAMETER NoResize
    Keep the window's current size instead of -Width and -Height.
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
    [int]$Width = 1100,
    [int]$Height = 850,
    [string]$ProcessName = "x360ce"
)
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System; using System.Text; using System.Collections.Generic; using System.Runtime.InteropServices;
public struct RECT { public int L, T, R, B; }
public static class W3 {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr context);
  [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h);
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
# This process sees the screen in physical pixels, so a size given in logical pixels is scaled by
# the zoom of the screen the window is on. Left unaware, Windows would scale every coordinate
# for us, and the capture would be a blurred, stretched copy at the wrong size.
[W3]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null   # per-monitor aware, version 2
$dpi = [W3]::GetDpiForWindow($p.MainWindowHandle)
if ($dpi -eq 0) { $dpi = 96 }
$scale = $dpi / 96
# Raise (and optionally resize) without activating: SWP_NOACTIVATE|SHOWWINDOW.
if ($NoResize) {
    [W3]::SetWindowPos($p.MainWindowHandle, [IntPtr]::Zero, 0, 0, 0, 0, 0x0001 -bor 0x0002 -bor 0x0010 -bor 0x0040) | Out-Null
} else {
    [W3]::SetWindowPos($p.MainWindowHandle, [IntPtr]::Zero, 50, 50, [int]($Width * $scale), [int]($Height * $scale), 0x0010 -bor 0x0040) | Out-Null
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
