<#
.SYNOPSIS
    Takes a picture of every page of x360ce 4 and x360ce 3, in each of their themes, and the advertisement pictures,
    driving each program only through its own AI door.
.DESCRIPTION
    Each program is copied into an emptied scratch folder and started there with empty settings and AI assistant access on
    at the Configure level, for this computer only. Everything after that goes through the program's door (its MCP
    server): no mouse, no keyboard and no AI. The pictures are taken from the script with PrintWindow, which needs
    neither the focus nor the mouse and works while the window is covered.

    For each program the script:
      1. Starts the copy and waits until its door answers and its pages and devices are loaded.
      2. Puts the devices named by -Devices on controllers. x360ce 4 maps them with device_map, by product name, to
         Controller 1, 2 and so on, with Show XInput State on in its settings. x360ce 3 sets up the devices it
         knows by itself, as on a person's first start, and the script reads where it put them.
      3. Waits until every device found works on its controller, read from the door: in x360ce 4 the device is
         online, the controller made from it holds its XInput place, and the controller's tab is green on both
         halves; in x360ce 3 the controller reads the device. x360ce 4's SDL page must show a mapping too, on the
         controller of the second device, which SDL's own database knows. When one does not within 30 seconds, the
         script names what does not work, closes the copy and stops, leaving no pictures of that program.
      4. For each theme the Theme list offers, except the one that follows Windows, checks again that the devices
         work, then pictures every page that ui_pages lists, nested pages included, then every window a button
         opens that changes nothing, then the advertisements, which point at a feature with a balloon as an
         assistant guiding a person would.
      5. Compares the pages it walked with the program's exported interface tree, and prints any page found in
         only one of the two, so a page added to the program is seen without changing this script.
      6. Closes the copy and checks that C:\ProgramData\X360CE, where the person's own settings are, was not changed.

    Pictures are named {Version}-{Theme}-{Tab}-{SubTab}.png, such as v4-Dark-Controller1-General.png, and replace the
    pictures of the last run. Advertisements go into the Ads folder beside them.

    The pictures are at 100 % display scale, 96 DPI, whatever this computer's scale, and the display's own settings are
    never changed:
      - x360ce lays itself out at the scale the main screen had when the person signed in. When that is not 100 %,
        each copy is started as a program that does not know the display scale (the __COMPAT_LAYER=DPIUNAWARE
        setting, given to that process alone), so Windows lays it out at 96 DPI and it draws itself, its balloons
        included, in 96-DPI pixels.
      - When a screen is at 100 %, the copy's window is moved onto it, where Windows shows it as drawn, and each
        picture is the whole window as Windows shows it.
      - With no screen at 100 %, Windows shows the window only stretched, title bar and all, and its own picture of
        the window is blurred. Each picture is then the window's inside, below its title bar, which the program draws
        into the picture itself, pixel for pixel what a 100 % screen shows.
    The script names the screens and the way it takes the pictures. Each PNG says 96 DPI in its pHYs chunk.

    Empty settings, and the person's own left alone:
      - x360ce 4 is started with /Profile on a new folder, which wins over every other settings folder.
      - x360ce 3 has no /Profile. Its copy gets the old portable layout, x360ce\x360ce.Options.xml beside the
        program, which x360ce 3 uses before any other folder, even one the person chose on the Options page.
      - Both copies also get a placeholder in x360ce\Settings beside them, so neither needs to test whether it can
        write to C:\ProgramData\X360CE when the person has chosen no folder. When the person chose that folder,
        x360ce 4 still tests it once at start, by writing and deleting a file there: the folder's own time changes
        and no file in it does. The check after each run fails on any changed file, and names a folder whose time
        changed.
      - The copies are closed by their own means: x360ce 4 by /Exit with the same /Profile, which reaches that copy
        alone. x360ce 3's /Exit would reach every x360ce 3 and every x360ce 4 started without /Profile, so its copy
        is closed by a close message to its own window, after its settings are saved through the door so it asks
        nothing on the way out. Ending the process by its ID is the last resort.

    Windows appear on the screen while the script runs. Leave them be: a click in the program takes the balloon of
    an advertisement away.

    Requires x360ce 4.31.1.0 and x360ce 3.12.1.0 or later, whose doors have ui_pages, ui_open, ui_wait and
    ui_close; the script says so and stops when a program lacks them. Runs in Windows PowerShell 5.1 and
    PowerShell 7.
.PARAMETER V4Exe
    x360ce 4 to picture. It is copied, never run in place. Defaults to Files.v4\x360ce.exe beside this script.
.PARAMETER V3Exe
    x360ce 3 to picture. It is copied, never run in place: the folder it is in may hold the person's x360ce.ini.
    Defaults to Files.v3\x360ce_x64.exe beside this script.
.PARAMETER V4Screenshots
    Folder for the x360ce 4 pictures. Defaults to Files.v4\Screenshots beside this script.
.PARAMETER V3Screenshots
    Folder for the x360ce 3 pictures. Defaults to Files.v3\Screenshots beside this script.
.PARAMETER Versions
    Which programs to picture: v4, v3 or both.
.PARAMETER Devices
    Product names of the devices the pictures show. x360ce 4 maps the first to Controller 1, the second to Controller
    2, and so on; x360ce 3 places them itself. A device is found by any part of its product name. With none, as
    -Devices @(), the pictures that need a device are left out, and the script says which.
.PARAMETER WorkFolder
    Folder for the scratch copies: each program runs from {WorkFolder}\v4 or {WorkFolder}\v3, emptied at the start of
    each run. Other folders in it are left alone.
.PARAMETER V4Port
    Port the x360ce 4 copy's door listens on.
.PARAMETER V3Port
    Port the x360ce 3 copy's door listens on.
.PARAMETER SquareCorners
    Keeps the pictures' corners square. Without it the corners are rounded as Windows 11 rounds a window's corners.
.PARAMETER TreeFolder
    Folder with the programs' exported interface trees, ui-tree-v4.json and ui-tree-v3.json, for the check that
    the pages walked match the program's own description of itself. Defaults to skills\x360ce\references in the
    repository.
.EXAMPLE
    PS> .\App_7_Screenshots.ps1
    Pictures both programs from Files.v4 and Files.v3 beside this script.
.EXAMPLE
    PS> .\App_7_Screenshots.ps1 -V4Exe ..\App.v4\bin\Release\x360ce.exe -V3Exe ..\App.v3\bin\Release_x64\x360ce.exe -SquareCorners
    Pictures this branch's own Release builds, with square corners.
.OUTPUTS
    [pscustomobject] One summary per program: pictures per theme, windows, advertisements and what was left out.
#>
[CmdletBinding()]
param(
    [string]$V4Exe,
    [string]$V3Exe,
    [string]$V4Screenshots,
    [string]$V3Screenshots,
    [ValidateSet('v4', 'v3')]
    [string[]]$Versions = @('v4', 'v3'),
    [string[]]$Devices = @('G27', 'RumblePad'),
    [string]$WorkFolder = 'D:\Temp\x360ce-screenshots',
    [ValidateRange(1024, 49151)]
    [int]$V4Port = 37901,
    [ValidateRange(1024, 49151)]
    [int]$V3Port = 37902,
    [switch]$SquareCorners,
    [string]$TreeFolder
)

$ErrorActionPreference = 'Stop'

# The defaults are worked out here: Windows PowerShell leaves $PSScriptRoot empty while it reads the parameters.
$here = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if (-not $V4Exe) { $V4Exe = Join-Path $here 'Files.v4\x360ce.exe' }
if (-not $V3Exe) { $V3Exe = Join-Path $here 'Files.v3\x360ce_x64.exe' }
if (-not $V4Screenshots) { $V4Screenshots = Join-Path $here 'Files.v4\Screenshots' }
if (-not $V3Screenshots) { $V3Screenshots = Join-Path $here 'Files.v3\Screenshots' }
if (-not $TreeFolder) { $TreeFolder = Join-Path (Split-Path -Parent $here) 'skills\x360ce\references' }

#region What the pictures show

# The advertisements: each opens the page an element is on, points at the element with a balloon in plain words, as
# an assistant guiding a person would, and is pictured in every theme. Find holds ui_find words for the element; a
# page's field name among them, such as Pad1 for Controller 1, keeps the search to that page. Open holds words for a
# page to bring to the front first, such as the page beside the element that shows what the picture should. Role keeps the
# search to one kind of element, such as the slider rather than the number box beside it. Needs names the device an
# advertisement shows, by its place in -Devices, so it is left out when that device is missing; {0} in its Find words
# stands for the number of the controller that device is on.
#
# ui_show puts the balloon below the element, or above it when the screen has no room below, so each element is one
# with empty space under it inside the window: a balloon covers no words and stays inside the window.
$Ads = @{
    v4 = @(
        @{ Name = 'Wheel'; Needs = 0; Find = 'Pad{0} Wheel message'
            Text = 'Your wheel works as an Xbox wheel, set up for you. Tick Centering spring to feel it pull back to the centre.' }
        @{ Name = 'ForceFeedback'; Needs = 0; Find = 'Pad{0} ForceDefaultsButton'; Role = 'Button'
            Text = 'Feel the road: the game''s force feedback reaches your wheel. Set how strong on this page.' }
        @{ Name = 'MouseStick'; Find = 'Pad1 PlayButton'; Role = 'Button'
            Text = 'No controller? A mouse can move a stick. Pick a style and watch how it moves.' }
        @{ Name = 'AiAssistant'; Open = 'AiAccessTabPage'; Find = 'AiAccessTabPage Door state'
            Text = 'AI assistant access is on. Your AI assistant can now set up your controller for you.' }
    )
    v3 = @(
        @{ Name = 'ReadyWithoutSetup'; Needs = 0; Find = 'Pad{0} Axes now'; Role = 'Grid'
            Text = 'Plug in and play: your wheel is read at once and mapped from the built-in controller database.' }
        @{ Name = 'AiAssistant'; Find = 'AiAccessTabPage Door'
            Text = 'AI assistant access is on. Your AI assistant can now set up x360ce for you.' }
    )
}

# A button whose name ends in "..." opens a window. It is pressed, pictured and closed again unless what it is
# described to do leaves a mark or leaves the program: writes, saves, downloads, installs or records, changes HID
# Hide, adds, removes, deletes, creates or sends something, sets something up, or opens a file, a folder, a web page
# or another program. A button with no description is left alone too, since nothing says what it does.
$SkipWhenDescribed = '\b(writes?|saves?|saving|download\w*|install\w*|record\w*|HID Hide|adds?|adding|remov\w*|delet\w*|creat\w*|send\w*|sets? [\w ]* up|web|forum|online|account|folder|file|Windows|Device Manager|separate tool)\b'

# Words that find the Theme list on the Options page, whose own items are the themes pictured.
$ThemeWords = 'Theme'

# The token the scratch doors are given, so the AI pages show a blank one rather than a secret. It opens nothing more
# than the door already lets in: the door listens on this computer only and lets this computer's programs in without
# a token, and it lives as long as the run.
$PictureToken = 'sk-x360ce-mcp-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX'

# x360ce 4 lights each controller tab in two halves, the device and the controller a game reads, and says in the
# tab's hint what the light means. Both halves are green when the hint, after "Controller N: ", is this sentence alone.
# Words that find the tab follow the controller's number, as in Pad1TabPage.
$WorkingTabHint = 'A mapped device is connected and Windows hands back a virtual controller.'
$ControllerTabWords = 'TabPage'

# x360ce 4's SDL page is checked before any picture on the controller of the device this names, by its place in
# -Devices: the RumblePad 2, which SDL's own database has a row for. The page draws itself while it is shown, and shows a
# mapping when its device line says a controller database knows the device, in these words. x360ce 3 has no SDL page.
$SdlPageNeeds = 1
$SdlPageWords = 'SdlTabPage'
$SdlLineWords = 'SdlPanel DeviceLabel'
$SdlMappedWords = 'knows this controller'

#endregion

#region Pictures

if (-not ('X360ceScreenshots.WindowPicture' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace X360ceScreenshots
{
    /// <summary>
    /// Pictures of a program's windows, taken with PrintWindow, which needs neither the focus nor the mouse and works while
    /// the window is covered. Pixels are 0xAARRGGBB, top row first, as a 32-bit ARGB bitmap holds them.
    /// </summary>
    public static class WindowPicture
    {
        [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)]
        struct BITMAPINFOHEADER
        {
            public int biSize, biWidth, biHeight;
            public short biPlanes, biBitCount;
            public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor, rcWork;
            public int dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szDevice;
        }
        delegate bool EnumWindowsProc(IntPtr window, IntPtr data);
        delegate bool MonitorEnumProc(IntPtr monitor, IntPtr dc, ref RECT rect, IntPtr data);

        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window, out RECT rect);
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr window, out RECT rect);
        [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr window, ref POINT point);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")] static extern IntPtr GetWindowDpiAwarenessContext(IntPtr window);
        [DllImport("user32.dll")] static extern bool PhysicalToLogicalPointForPerMonitorDPI(IntPtr window, ref POINT point);
        [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr window);
        [DllImport("user32.dll")] static extern uint GetDpiForSystem();
        [DllImport("user32.dll")] static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, MonitorEnumProc callback, IntPtr data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFOEX info);
        [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
        [DllImport("shcore.dll")] static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint dpiX, out uint dpiY);
        [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out RECT value, int size);
        [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr window, IntPtr dc, uint flags);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc callback, IntPtr data);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr window, int index);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr item);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr item);
        [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr dc, ref BITMAPINFOHEADER info, uint usage, out IntPtr bits, IntPtr section, uint offset);

        const uint PW_CLIENTONLY = 1;
        const uint PW_RENDERFULLCONTENT = 2;
        const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        const uint MONITOR_DEFAULTTONEAREST = 2;
        const uint SWP_NOSIZE = 0x1, SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10;
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x20;
        const int WS_EX_TOOLWINDOW = 0x80;
        const int WS_EX_LAYERED = 0x80000;
        const int KeyColour = 0xFF00FF;

        /// <summary>
        /// Measures in the window's own pixels: for a program that does not know the display scale, the 96-DPI pixels it
        /// draws, rather than the stretched ones Windows shows on a scaled screen.
        /// </summary>
        static void SeeOwnPixels(IntPtr window)
        {
            SetThreadDpiAwarenessContext(GetWindowDpiAwarenessContext(window));
        }

        /// <summary>Measures in screen pixels, whatever scale a screen is at.</summary>
        static void SeeScreenPixels()
        {
            SetThreadDpiAwarenessContext(new IntPtr(-4));
        }

        /// <summary>DWM gives a window's frame in screen pixels whatever the caller measures in; this turns it into the window's own.</summary>
        static RECT ToOwnPixels(IntPtr window, RECT screen)
        {
            var topLeft = new POINT { X = screen.Left, Y = screen.Top };
            var bottomRight = new POINT { X = screen.Right, Y = screen.Bottom };
            PhysicalToLogicalPointForPerMonitorDPI(window, ref topLeft);
            PhysicalToLogicalPointForPerMonitorDPI(window, ref bottomRight);
            return new RECT { Left = topLeft.X, Top = topLeft.Y, Right = bottomRight.X, Bottom = bottomRight.Y };
        }

        /// <summary>The window's dots per inch: 96 at 100 % display scale, and for a program that does not know the scale.</summary>
        public static int Dpi(IntPtr window)
        {
            var dpi = (int)GetDpiForWindow(window);
            return dpi == 0 ? 96 : dpi;
        }

        /// <summary>The dots per inch Windows lays out a program at that knows the display scale only as it was at sign-in, as x360ce does.</summary>
        public static int SystemDpi()
        {
            SeeScreenPixels();
            return (int)GetDpiForSystem();
        }

        /// <summary>A screen: its name, its work area in screen pixels, its dots per inch and whether it is the main one.</summary>
        public class Screen
        {
            public string Name;
            public int Left, Top, Right, Bottom, Dpi;
            public bool Main;
        }

        /// <summary>Every screen, as Windows has them now.</summary>
        public static Screen[] Screens()
        {
            SeeScreenPixels();
            var found = new List<Screen>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr dc, ref RECT rect, IntPtr data) =>
            {
                var info = new MONITORINFOEX { cbSize = Marshal.SizeOf(typeof(MONITORINFOEX)) };
                uint dpiX, dpiY;
                if (GetMonitorInfo(monitor, ref info) && GetDpiForMonitor(monitor, 0, out dpiX, out dpiY) == 0)
                    found.Add(new Screen
                    {
                        Name = info.szDevice, Left = info.rcWork.Left, Top = info.rcWork.Top, Right = info.rcWork.Right,
                        Bottom = info.rcWork.Bottom, Dpi = (int)dpiX, Main = (info.dwFlags & 1) != 0
                    });
                return true;
            }, IntPtr.Zero);
            return found.ToArray();
        }

        /// <summary>Moves a window, keeping its size, so its top left corner is at a point given in screen pixels.</summary>
        public static void MoveTo(IntPtr window, int left, int top)
        {
            SeeScreenPixels();
            SetWindowPos(window, IntPtr.Zero, left, top, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }

        /// <summary>True when Windows shows the window as the program draws it: the window's screen is at the scale it is drawn for.</summary>
        public static bool ShownUnscaled(IntPtr window)
        {
            SeeScreenPixels();
            uint dpiX, dpiY;
            return GetDpiForMonitor(MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST), 0, out dpiX, out dpiY) == 0
                && dpiX == GetDpiForWindow(window);
        }

        /// <summary>
        /// A window as the program draws it. On a screen at the scale the window is drawn for, the whole window as Windows
        /// shows it, from the top of its frame down, without the thin border Windows draws beside and below it, which
        /// PrintWindow leaves black. On another screen Windows shows the window stretched, title bar and all, and its
        /// picture of it is stretched and shrunk back, so blurred: then the picture is the window's inside, below its
        /// title bar, which the program draws into the picture itself. The window's own pixels are opaque.
        /// </summary>
        public static int[] Capture(IntPtr window, out int width, out int height, out int left, out int top)
        {
            var whole = ShownUnscaled(window);
            SeeOwnPixels(window);
            if (IsIconic(window))
                throw new InvalidOperationException("The window is minimised, so it has nothing to show.");
            RECT client;
            GetClientRect(window, out client);
            var origin = new POINT();
            ClientToScreen(window, ref origin);
            int[] pixels;
            if (whole)
            {
                RECT outer;
                GetWindowRect(window, out outer);
                var all = Print(window, outer.Right - outer.Left, outer.Bottom - outer.Top, PW_RENDERFULLCONTENT);
                RECT shown;
                if (DwmGetWindowAttribute(window, DWMWA_EXTENDED_FRAME_BOUNDS, out shown, Marshal.SizeOf(typeof(RECT))) == 0)
                    shown = ToOwnPixels(window, shown);
                else
                    shown = outer;
                shown.Left = Math.Max(shown.Left, origin.X);
                shown.Right = Math.Min(shown.Right, origin.X + client.Right);
                shown.Bottom = Math.Min(shown.Bottom, origin.Y + client.Bottom);
                pixels = Crop(all, outer, shown, out width, out height);
                left = shown.Left;
                top = shown.Top;
            }
            else
            {
                width = client.Right;
                height = client.Bottom;
                left = origin.X;
                top = origin.Y;
                pixels = Print(window, width, height, PW_CLIENTONLY);
            }
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] |= unchecked((int)0xFF000000);
            return pixels;
        }

        /// <summary>
        /// An overlay window, such as the frame and balloon an assistant points with, as the window paints itself: the
        /// colour Windows makes see-through stays in, so <see cref="Compose"/> can leave it out.
        /// </summary>
        public static int[] CaptureOverlay(IntPtr window, out int width, out int height, out int left, out int top)
        {
            SeeOwnPixels(window);
            RECT outer;
            GetWindowRect(window, out outer);
            width = outer.Right - outer.Left;
            height = outer.Bottom - outer.Top;
            left = outer.Left;
            top = outer.Top;
            // Without PW_RENDERFULLCONTENT the window paints itself with its background colour, the see-through key; with it,
            // Windows hands back the see-through part as black, the colour the balloon's words are written in.
            return Print(window, width, height, 0);
        }

        static int[] Print(IntPtr window, int width, int height, uint flags)
        {
            var info = new BITMAPINFOHEADER { biSize = 40, biWidth = width, biHeight = -height, biPlanes = 1, biBitCount = 32 };
            IntPtr bits;
            var dc = CreateCompatibleDC(IntPtr.Zero);
            var bitmap = CreateDIBSection(dc, ref info, 0, out bits, IntPtr.Zero, 0);
            var old = SelectObject(dc, bitmap);
            try
            {
                if (!PrintWindow(window, dc, flags))
                    throw new InvalidOperationException("Windows did not draw the window " + window + ".");
                var pixels = new int[width * height];
                Marshal.Copy(bits, pixels, 0, pixels.Length);
                return pixels;
            }
            finally
            {
                SelectObject(dc, old);
                DeleteObject(bitmap);
                DeleteDC(dc);
            }
        }

        static int[] Crop(int[] all, RECT outer, RECT shown, out int width, out int height)
        {
            var allWidth = outer.Right - outer.Left;
            width = shown.Right - shown.Left;
            height = shown.Bottom - shown.Top;
            var pixels = new int[width * height];
            for (var y = 0; y < height; y++)
                Array.Copy(all, (y + shown.Top - outer.Top) * allWidth + shown.Left - outer.Left, pixels, y * width, width);
            return pixels;
        }

        /// <summary>
        /// Cuts the four corners round, as Windows 11 rounds a window's corners: see-through outside a quarter circle of
        /// the radius given, with a soft edge. PrintWindow hands back square corners, so this is the only way a picture
        /// gets round ones.
        /// </summary>
        public static void RoundCorners(int[] pixels, int width, int height, int radius)
        {
            for (var y = 0; y < radius && y < height; y++)
            {
                for (var x = 0; x < radius && x < width; x++)
                {
                    var dx = radius - (x + 0.5);
                    var dy = radius - (y + 0.5);
                    var cover = Math.Max(0.0, Math.Min(1.0, radius - Math.Sqrt(dx * dx + dy * dy) + 0.5));
                    if (cover >= 1.0)
                        continue;
                    Fade(pixels, y * width + x, cover);
                    Fade(pixels, y * width + width - 1 - x, cover);
                    Fade(pixels, (height - 1 - y) * width + x, cover);
                    Fade(pixels, (height - 1 - y) * width + width - 1 - x, cover);
                }
            }
        }

        static void Fade(int[] pixels, int index, double cover)
        {
            var alpha = (int)(((uint)pixels[index] >> 24) * cover + 0.5);
            pixels[index] = (pixels[index] & 0xFFFFFF) | (alpha << 24);
        }

        /// <summary>
        /// The window with an overlay drawn over it where Windows shows it, leaving out the overlay's see-through colour.
        /// The picture grows to hold an overlay that reaches past the window, see-through around both.
        /// </summary>
        public static int[] Compose(int[] window, int width, int height, int left, int top,
            int[] overlay, int overlayWidth, int overlayHeight, int overlayLeft, int overlayTop,
            out int composedWidth, out int composedHeight)
        {
            var x0 = Math.Min(left, overlayLeft);
            var y0 = Math.Min(top, overlayTop);
            composedWidth = Math.Max(left + width, overlayLeft + overlayWidth) - x0;
            composedHeight = Math.Max(top + height, overlayTop + overlayHeight) - y0;
            var pixels = new int[composedWidth * composedHeight];
            for (var y = 0; y < height; y++)
                Array.Copy(window, y * width, pixels, (y + top - y0) * composedWidth + left - x0, width);
            for (var y = 0; y < overlayHeight; y++)
            {
                for (var x = 0; x < overlayWidth; x++)
                {
                    var pixel = overlay[y * overlayWidth + x];
                    if ((pixel & 0xFFFFFF) == KeyColour)
                        continue;
                    pixels[(y + overlayTop - y0) * composedWidth + x + overlayLeft - x0] = pixel | unchecked((int)0xFF000000);
                }
            }
            return pixels;
        }

        /// <summary>The see-through overlay windows a process shows, such as the frame and balloon of ui_show.</summary>
        public static IntPtr[] OverlayWindows(int processId)
        {
            var found = new List<IntPtr>();
            const int overlay = WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW;
            EnumWindows((window, data) =>
            {
                uint owner;
                GetWindowThreadProcessId(window, out owner);
                if (owner == (uint)processId && IsWindowVisible(window) && (GetWindowLong(window, GWL_EXSTYLE) & overlay) == overlay)
                    found.Add(window);
                return true;
            }, IntPtr.Zero);
            return found.ToArray();
        }
    }
}
'@
}
Add-Type -AssemblyName System.Drawing

#endregion

#region The door

#--------------------------------------------------------------------------------------------------
function Invoke-Door {
    <#
    .SYNOPSIS
        Sends one JSON-RPC message to a program's door and returns the answer's text.
    .DESCRIPTION
        The door listens on this computer only and trusts this computer's programs, as the scratch settings say, so
        no token is sent. A proxy is never used: it would carry a call to this computer out and back. A tool's
        refusal is thrown with the program's own words.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][int]$Port,
        [string]$Tool,
        [hashtable]$Arguments = @{},
        [string]$Method = 'tools/call',
        [int]$TimeoutSeconds = 120
    )
    $message = @{ jsonrpc = '2.0'; id = 1; method = $Method }
    if ($Method -eq 'tools/call') {
        $message.params = @{ name = $Tool; arguments = $Arguments }
    }
    $bytes = [System.Text.Encoding]::UTF8.GetBytes(($message | ConvertTo-Json -Depth 10 -Compress))
    $request = [System.Net.HttpWebRequest]::Create("http://localhost:$Port/mcp/")
    $request.Method = 'POST'
    $request.Proxy = $null
    $request.ContentType = 'application/json'
    $request.Timeout = $TimeoutSeconds * 1000
    $stream = $request.GetRequestStream()
    try { $stream.Write($bytes, 0, $bytes.Length) } finally { $stream.Dispose() }
    $response = $request.GetResponse()
    try {
        $reader = New-Object System.IO.StreamReader($response.GetResponseStream(), [System.Text.Encoding]::UTF8)
        $text = $reader.ReadToEnd()
    }
    finally { $response.Dispose() }
    if ($Method -ne 'tools/call') {
        return $text
    }
    $answer = $text | ConvertFrom-Json
    if ($answer.error) {
        throw "$Tool failed: $($answer.error.message)"
    }
    $said = $answer.result.content[0].text
    if ($answer.result.isError) {
        throw "$Tool refused: $said"
    }
    return $said
}

#--------------------------------------------------------------------------------------------------
function Invoke-DoorJson {
    <#
    .SYNOPSIS
        Calls a tool whose answer is JSON and returns it as objects.
    .DESCRIPTION
        The answer is read into a variable before it is returned, because Windows PowerShell passes an array read by
        ConvertFrom-Json down the pipeline as one item.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][int]$Port,
        [Parameter(Mandatory = $true)][string]$Tool,
        [hashtable]$Arguments = @{}
    )
    $text = Invoke-Door -Port $Port -Tool $Tool -Arguments $Arguments
    $value = $text | ConvertFrom-Json
    return $value
}

#--------------------------------------------------------------------------------------------------
function Wait-Condition {
    <#
    .SYNOPSIS
        Asks a question again and again until it answers, and returns the answer.
    .DESCRIPTION
        The question is a script block that returns nothing, $null or $false until what it waits for is so. A
        failure inside it counts as not yet, such as a door that does not answer while the program starts.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Condition,
        [Parameter(Mandatory = $true)][string]$What,
        [int]$TimeoutSeconds = 60,
        [int]$IntervalMilliseconds = 250
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastError = $null
    while ((Get-Date) -lt $deadline) {
        try {
            $value = & $Condition
            if ($value) { return $value }
        }
        catch { $lastError = $_.Exception.Message }
        Start-Sleep -Milliseconds $IntervalMilliseconds
    }
    $why = if ($lastError) { " The last attempt said: $lastError" } else { '' }
    throw "Waited $TimeoutSeconds seconds for $What.$why"
}

#--------------------------------------------------------------------------------------------------
function Find-DoorElement {
    <#
    .SYNOPSIS
        Finds an element by ui_find words and returns the best match, or $null.
    .DESCRIPTION
        The door ranks the matches, a word in the name counting most. A role, when given, keeps the matches to that
        kind of element, such as List or Button.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][int]$Port,
        [Parameter(Mandatory = $true)][string]$Words,
        [string]$Role
    )
    $hits = Invoke-DoorJson -Port $Port -Tool 'ui_find' -Arguments @{ query = $Words }
    foreach ($hit in $hits) {
        if ($hit.Path -and (-not $Role -or $hit.Role -eq $Role)) { return $hit }
    }
    return $null
}

#endregion

#region The person's own settings

#--------------------------------------------------------------------------------------------------
function Get-OwnerSnapshot {
    <#
    .SYNOPSIS
        Lists the files and folders of C:\ProgramData\X360CE and its Settings folder, with their sizes and times.
    #>
    [CmdletBinding()]
    param()
    $root = Join-Path $env:ProgramData 'X360CE'
    $snapshot = @{ Files = @{}; Folders = @{} }
    foreach ($folder in @($root, (Join-Path $root 'Settings'))) {
        if (-not (Test-Path -LiteralPath $folder)) { continue }
        foreach ($item in Get-ChildItem -LiteralPath $folder -Force) {
            if ($item.PSIsContainer) {
                $snapshot.Folders[$item.FullName] = $item.LastWriteTimeUtc.ToString('o')
            }
            else {
                $snapshot.Files[$item.FullName] = '{0} bytes, written {1}' -f $item.Length, $item.LastWriteTimeUtc.ToString('o')
            }
        }
    }
    return $snapshot
}

#--------------------------------------------------------------------------------------------------
function Assert-OwnerUnchanged {
    <#
    .SYNOPSIS
        Fails when a file of C:\ProgramData\X360CE or its Settings folder was added, removed or changed.
    .DESCRIPTION
        Returns the folders whose own time changed with no file in them changed: x360ce 4 tests once at start
        whether it can write there, by writing and deleting a file, when the person chose that folder for settings.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][hashtable]$Before,
        [Parameter(Mandatory = $true)][hashtable]$After
    )
    $changes = New-Object System.Collections.Generic.List[string]
    foreach ($name in $Before.Files.Keys) {
        if (-not $After.Files.ContainsKey($name)) { $changes.Add("removed: $name") }
        elseif ($After.Files[$name] -ne $Before.Files[$name]) { $changes.Add("changed: $name, was $($Before.Files[$name]), now $($After.Files[$name])") }
    }
    foreach ($name in $After.Files.Keys) {
        if (-not $Before.Files.ContainsKey($name)) { $changes.Add("added: $name") }
    }
    foreach ($name in $Before.Folders.Keys) {
        if (-not $After.Folders.ContainsKey($name)) { $changes.Add("removed folder: $name") }
    }
    foreach ($name in $After.Folders.Keys) {
        if (-not $Before.Folders.ContainsKey($name)) { $changes.Add("added folder: $name") }
    }
    if ($changes.Count -gt 0) {
        throw "The person's settings in C:\ProgramData\X360CE changed:`n  " + ($changes -join "`n  ")
    }
    $touched = @($Before.Folders.Keys | Where-Object { $After.Folders[$_] -ne $Before.Folders[$_] })
    return $touched
}

#endregion

#region Starting and closing a copy

#--------------------------------------------------------------------------------------------------
function New-ScratchCopy {
    <#
    .SYNOPSIS
        Copies a program into its scratch folder, emptied first, with empty settings and its AI door switched on at
        Configure.
    .DESCRIPTION
        The folder is the same on every run, {WorkFolder}\{Version}, so the paths the pictures show, such as the AI
        page's assistant snippet, read the same each time. The door listens on this computer only, on the port given,
        and trusts this computer's programs, so the script needs no token.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][ValidateSet('v4', 'v3')][string]$Version,
        [Parameter(Mandatory = $true)][string]$Exe,
        [Parameter(Mandatory = $true)][int]$Port
    )
    if (-not (Test-Path -LiteralPath $Exe)) { throw "x360ce $Version not found: $Exe" }
    $folder = Join-Path $WorkFolder $Version
    if (Test-Path -LiteralPath $folder) {
        try { Remove-Item -LiteralPath $folder -Recurse -Force }
        catch { throw "The scratch folder $folder could not be emptied, perhaps because a copy still runs from it: $($_.Exception.Message)" }
    }
    $settings = Join-Path $folder 'x360ce\Settings'
    New-Item -ItemType Directory -Path $settings -Force | Out-Null
    $copy = Join-Path $folder (Split-Path -Leaf $Exe)
    Copy-Item -LiteralPath $Exe -Destination $copy
    # A settings file beside the copy makes that folder the copy's choice over C:\ProgramData\X360CE, without the
    # copy testing whether it can write there. Its content is never read.
    Set-Content -LiteralPath (Join-Path $settings 'x360ce.Portable.xml') -Value '<?xml version="1.0" encoding="utf-8"?><Data />' -Encoding UTF8
    $copyInfo = @{ Version = $Version; Folder = $folder; Exe = $copy; Port = $Port; Arguments = @(); Profile = $null }
    if ($Version -eq 'v4') {
        $copyInfo.Profile = Join-Path $folder 'Profile'
        $profileSettings = Join-Path $copyInfo.Profile 'Settings'
        New-Item -ItemType Directory -Path $profileSettings -Force | Out-Null
        $ai = "<?xml version=`"1.0`" encoding=`"utf-8`"?>`r`n<Data Version=`"0`">`r`n  <Items>`r`n    <AiOptions>`r`n" +
            "      <AiAccessEnabled>true</AiAccessEnabled>`r`n      <AiAccess>Configure</AiAccess>`r`n" +
            "      <AiAccessTrustLocal>true</AiAccessTrustLocal>`r`n      <AiAccessPort>$Port</AiAccessPort>`r`n" +
            "      <AiAccessToken>$PictureToken</AiAccessToken>`r`n    </AiOptions>`r`n  </Items>`r`n</Data>`r`n"
        Set-Content -LiteralPath (Join-Path $profileSettings 'x360ce.AI.xml') -Value $ai -Encoding UTF8 -NoNewline
        # Show XInput State on, so x360ce reads back what a game receives and its controller tabs light green when that
        # works; without it the half that stands for the game's controller stays blue, "not checked".
        $options = "<?xml version=`"1.0`" encoding=`"utf-8`"?>`r`n<Data Version=`"0`">`r`n  <Items>`r`n    <Options>`r`n" +
            "      <GetXInputStates>true</GetXInputStates>`r`n    </Options>`r`n  </Items>`r`n</Data>`r`n"
        Set-Content -LiteralPath (Join-Path $profileSettings 'x360ce.Options.xml') -Value $options -Encoding UTF8 -NoNewline
        $copyInfo.Arguments = @('"/Profile=' + $copyInfo.Profile + '"')
    }
    else {
        # x360ce 3 uses the old portable layout before any other folder, even one the person chose.
        Set-Content -LiteralPath (Join-Path $folder 'x360ce\x360ce.Options.xml') -Value '<?xml version="1.0" encoding="utf-8"?><Data />' -Encoding UTF8
        # Its door settings live in x360ce.ini beside it, which Windows reads as UTF-16. Allowing more than one copy
        # keeps this one from handing over to an x360ce 3 the person has open.
        $ini = "[Options]`r`nAllowOnlyOneCopy=0`r`nAiAccessEnabled=1`r`nAiAccess=Configure`r`nAiAccessTrustLocal=1`r`nAiAccessPort=$Port`r`nAiAccessToken=$PictureToken`r`n"
        Set-Content -LiteralPath (Join-Path $folder 'x360ce.ini') -Value $ini -Encoding Unicode -NoNewline
    }
    return $copyInfo
}

#--------------------------------------------------------------------------------------------------
function Start-ScratchCopy {
    <#
    .SYNOPSIS
        Starts a scratch copy and returns its process once its door answers.
    .DESCRIPTION
        When Windows lays programs out at another scale than 100 %, the copy is started as a program that does not know
        the display scale, through the __COMPAT_LAYER setting given to its process alone, so Windows lays it out at
        96 DPI. The program's own manifest says dpiAware; a manifest file beside the copy is ignored, because the
        program carries its own, and editing the copy's own one gives the same pixels as this setting.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)][hashtable]$Copy)
    if (Wait-DoorAnswer -Port $Copy.Port -TimeoutSeconds 1) {
        throw "Something already answers on port $($Copy.Port). Close it, or choose another port with -$($Copy.Version.ToUpper())Port."
    }
    $start = New-Object System.Diagnostics.ProcessStartInfo $Copy.Exe
    $start.Arguments = $Copy.Arguments -join ' '
    $start.WorkingDirectory = $Copy.Folder
    $start.UseShellExecute = $false
    if ($StartUnaware) { $start.EnvironmentVariables['__COMPAT_LAYER'] = 'DPIUNAWARE' }
    $process = [System.Diagnostics.Process]::Start($start)
    Write-Host "  Started $($Copy.Exe), process $($process.Id)."
    Wait-Condition -What "the door of x360ce $($Copy.Version) on port $($Copy.Port)" -TimeoutSeconds 90 -Condition {
        if ($process.HasExited) { throw "x360ce $($Copy.Version) closed with exit code $($process.ExitCode) before its door opened." }
        Wait-DoorAnswer -Port $Copy.Port -TimeoutSeconds 2
    } | Out-Null
    return $process
}

#--------------------------------------------------------------------------------------------------
function Wait-DoorAnswer {
    <#
    .SYNOPSIS
        Returns $true when a door answers a ping on the port, $false otherwise.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][int]$Port,
        [int]$TimeoutSeconds = 2
    )
    try { return [bool]((Invoke-Door -Port $Port -Method 'ping' -TimeoutSeconds $TimeoutSeconds) -match '"result"') }
    catch { return $false }
}

#--------------------------------------------------------------------------------------------------
function Assert-DoorTools {
    <#
    .SYNOPSIS
        Stops with a clear message when the program's door lacks a tool the script needs.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][hashtable]$Copy,
        [Parameter(Mandatory = $true)][string[]]$Needed
    )
    $list = (Invoke-Door -Port $Copy.Port -Method 'tools/list') | ConvertFrom-Json
    $names = @($list.result.tools | ForEach-Object { $_.name })
    $missing = @($Needed | Where-Object { $names -notcontains $_ })
    if ($missing.Count -gt 0) {
        $version = (Get-Item -LiteralPath $Copy.Exe).VersionInfo.FileVersion
        throw ("x360ce $version ($($Copy.Exe)) lacks the door tools $($missing -join ', '). " +
            'They came with x360ce 4.31.1.0 and x360ce 3.12.1.0; picture a build that has them.')
    }
}

#--------------------------------------------------------------------------------------------------
function Complete-StartUp {
    <#
    .SYNOPSIS
        Answers what the program asks at start, then waits until its pages and devices are loaded.
    .DESCRIPTION
        x360ce 3 lists problems before it builds its controller pages, each row with a button that fixes it: the fix
        writes the XInput library into the scratch folder, as a person's first start does. Any other window waiting
        at start is closed as if cancelled. The program is ready when nothing waits and the devices are read.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)][hashtable]$Copy)
    $answered = New-Object System.Collections.Generic.List[string]
    Wait-Condition -What "x360ce $($Copy.Version) to finish starting" -TimeoutSeconds 120 -IntervalMilliseconds 500 -Condition {
        $windows = Invoke-DoorJson -Port $Copy.Port -Tool 'ui_windows'
        foreach ($window in $windows) {
            $tree = Invoke-DoorJson -Port $Copy.Port -Tool 'ui_read' -Arguments @{ path = $window.Path }
            $fixes = @(Get-TreeNodes -Node $tree | Where-Object { $_.Role -eq 'Button' -and $_.Path -match '/rows/\d+/' })
            if ($fixes.Count -gt 0) {
                foreach ($fix in $fixes) {
                    Invoke-Door -Port $Copy.Port -Tool 'ui_invoke' -Arguments @{ path = $fix.Path } | Out-Null
                    $answered.Add("$($window.Name): $($fix.Name)")
                }
            }
            else {
                Invoke-Door -Port $Copy.Port -Tool 'ui_close' -Arguments @{ path = $window.Path } | Out-Null
                $answered.Add("$($window.Name): closed")
            }
        }
        if (@($windows).Count -gt 0) { return $false }
        $listed = @(Invoke-DoorJson -Port $Copy.Port -Tool 'devices_list' | Where-Object { $_.Product })
        $pages = @(Invoke-DoorJson -Port $Copy.Port -Tool 'ui_pages')
        return ($listed.Count -gt 0 -and $pages.Count -gt 0)
    } | Out-Null
    foreach ($line in $answered) { Write-Host "  Answered at start: $line" }
    return , $answered.ToArray()
}

#--------------------------------------------------------------------------------------------------
function Get-TreeNodes {
    <#
    .SYNOPSIS
        Every node of a tree read with ui_read, with whether it or a node above it is hidden.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]$Node,
        [bool]$HiddenAbove = $false
    )
    $hidden = $HiddenAbove -or [bool]$Node.Hidden
    $Node | Add-Member -NotePropertyName 'ShownHidden' -NotePropertyValue $hidden -Force
    $Node
    foreach ($child in @($Node.Items)) {
        if ($child) { Get-TreeNodes -Node $child -HiddenAbove $hidden }
    }
}

#--------------------------------------------------------------------------------------------------
function Stop-ScratchCopy {
    <#
    .SYNOPSIS
        Closes a scratch copy by its own means and waits until it has gone; ends it by its ID only as a last resort.
    .DESCRIPTION
        x360ce 4 is asked by /Exit with the same /Profile, which reaches that copy alone. x360ce 3's /Exit would close
        every x360ce 3 and every x360ce 4 started without /Profile, so its settings are saved through the door, which
        keeps it from asking about them on the way out, and its own window is sent a close message.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][hashtable]$Copy,
        [System.Diagnostics.Process]$Process
    )
    if (-not $Process -or $Process.HasExited) { return }
    if ($Copy.Version -eq 'v4') {
        $exit = Start-Process -FilePath $Copy.Exe -ArgumentList (@($Copy.Arguments) + '/Exit') -WorkingDirectory $Copy.Folder -PassThru -Wait
        if ($exit.ExitCode -ne 0) { Write-Warning "x360ce 4's /Exit answered $($exit.ExitCode)." }
    }
    else {
        try { Invoke-Door -Port $Copy.Port -Tool 'settings_save' | Out-Null }
        catch { Write-Warning "x360ce 3 did not save its settings before closing: $($_.Exception.Message)" }
        $Process.Refresh()
        $Process.CloseMainWindow() | Out-Null
    }
    if (-not $Process.WaitForExit(30000)) {
        Write-Warning "x360ce $($Copy.Version) did not close within 30 seconds; ending process $($Process.Id)."
        Stop-Process -Id $Process.Id -Force
        $Process.WaitForExit(10000) | Out-Null
    }
    Write-Host "  Closed x360ce $($Copy.Version), process $($Process.Id)."
}

#endregion

#region Devices and themes

#--------------------------------------------------------------------------------------------------
function Set-DeviceMapping {
    <#
    .SYNOPSIS
        Puts each device named by -Devices on a controller, and returns where each one is.
    .DESCRIPTION
        Returns a dictionary keyed by the names in -Devices. Each value holds the device's Controller (1 to 4),
        InstanceGuid and Product, or is $null for a device not found. A device is found by any part of its product
        name in devices_list.

        x360ce 4 maps each device, read through DirectInput when it is listed both ways, with device_map: the first
        to Controller 1, the second to Controller 2, and so on.

        x360ce 3 sets up the devices it knows by itself, from its controller databases, as on a person's first start,
        so nothing is mapped: its devices_list names the device each of its four controllers reads.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)][hashtable]$Copy)
    # The program reads its devices in the background, so one plugged in may be listed a moment after the first.
    try {
        Wait-Condition -What 'every device named by -Devices to be listed' -TimeoutSeconds 30 -Condition {
            $listed = Invoke-DoorJson -Port $Copy.Port -Tool 'devices_list'
            @($Devices | Where-Object { $name = $_; -not ($listed | Where-Object { $_.Product -like "*$name*" }) }).Count -eq 0
        } | Out-Null
    }
    catch { Write-Verbose $_.Exception.Message }
    $list = Invoke-DoorJson -Port $Copy.Port -Tool 'devices_list'
    $places = [ordered]@{}
    for ($i = 0; $i -lt $Devices.Count; $i++) {
        $name = $Devices[$i]
        $places[$name] = $null
        $matching = @($list | Where-Object { $_.Product -and $_.Product -like "*$name*" })
        $device = @($matching | Where-Object { -not $_.Source -or $_.Source -eq 'DirectInput' }) + $matching | Select-Object -First 1
        if (-not $device) {
            $why = if ($Copy.Version -eq 'v4') { 'is connected' } else { 'was set up by x360ce 3, which sets up the devices it knows' }
            Write-Warning "No device named like '$name' $why, so the pictures that need it are left out."
            continue
        }
        if ($Copy.Version -eq 'v4') {
            $controller = $i + 1
            Invoke-Door -Port $Copy.Port -Tool 'device_map' -Arguments @{ instanceGuid = $device.InstanceGuid; controller = $controller } | Out-Null
            Write-Host "  $($device.Product) to Controller $controller."
        }
        else {
            $controller = [int]$device.Controller
            Write-Host "  x360ce 3 set up $($device.Product) on Controller $controller by itself."
        }
        $places[$name] = @{ Controller = $controller; InstanceGuid = $device.InstanceGuid; Product = $device.Product }
    }
    return $places
}

#--------------------------------------------------------------------------------------------------
function Get-ControllerFault {
    <#
    .SYNOPSIS
        What keeps each placed device from working on its controller, one line each; nothing when every one works.
    .DESCRIPTION
        Read from the door. x360ce 4: devices_list says the device is online and on its controller, and that the
        controller made from it holds an XInput place rather than waiting for one; and the controller's tab is green on
        both halves, which its hint says. x360ce 3: devices_list says the controller still reads the device.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][hashtable]$Copy,
        [Parameter(Mandatory = $true)][System.Collections.IDictionary]$Places
    )
    $list = Invoke-DoorJson -Port $Copy.Port -Tool 'devices_list'
    $working = '^[^:]+: ' + [regex]::Escape($WorkingTabHint) + '$'
    foreach ($place in @($Places.Values | Where-Object { $_ })) {
        $n = $place.Controller
        $device = $list | Where-Object { $_.InstanceGuid -eq $place.InstanceGuid } | Select-Object -First 1
        if ($Copy.Version -eq 'v3') {
            if (-not $device -or $device.Controller -ne $n) { "Controller ${n}: x360ce 3 no longer reads $($place.Product)." }
            continue
        }
        $xinput = [string]$device.XInputPlaces
        if (-not $device -or -not $device.Online) { "Controller ${n}: $($place.Product) is not online." }
        elseif (@($device.Controllers) -notcontains $n) { "Controller ${n}: $($place.Product) is not on it." }
        elseif (-not $xinput -or $xinput -match 'waiting') { "Controller ${n}: the controller made from $($place.Product) holds no XInput place ('$xinput')." }
        $tab = Find-DoorElement -Port $Copy.Port -Words "Pad$n$ControllerTabWords" -Role 'Tab'
        $hint = if ($tab) { [string]$tab.Value } else { 'no tab found' }
        if ($hint -notmatch $working) { "Controller ${n}: its tab is not green on both halves. Its hint says: $hint" }
    }
}

#--------------------------------------------------------------------------------------------------
function Wait-WorkingController {
    <#
    .SYNOPSIS
        Waits until every placed device works on its controller, as Get-ControllerFault reads it, and stops the run
        naming what does not work when one does not within 30 seconds.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][hashtable]$Copy,
        [Parameter(Mandatory = $true)][System.Collections.IDictionary]$Places
    )
    $last = @{ Faults = @() }
    try {
        Wait-Condition -What 'the controllers to work' -TimeoutSeconds 30 -Condition {
            $last.Faults = @(Get-ControllerFault -Copy $Copy -Places $Places)
            $last.Faults.Count -eq 0
        } | Out-Null
    }
    catch {
        $why = if ($last.Faults.Count -gt 0) { $last.Faults -join "`n  " } else { $_.Exception.Message }
        throw "A controller does not work, so no pictures are taken:`n  $why"
    }
}

#--------------------------------------------------------------------------------------------------
function Get-MissingNeed {
    <#
    .SYNOPSIS
        Why the device at a place in -Devices is not there, in words: on no controller, or not named at all.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)][int]$Needs)
    if ($Needs -lt $Devices.Count) { return "$($Devices[$Needs]), which is on no controller" }
    return "device $($Needs + 1) of -Devices, which names $($Devices.Count)"
}

#--------------------------------------------------------------------------------------------------
function Get-NeededPlace {
    <#
    .SYNOPSIS
        Where the device at a place in -Devices is, as Set-DeviceMapping says it; $null when -Devices names nothing there or
        that device is on no controller.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][System.Collections.IDictionary]$Places,
        [Parameter(Mandatory = $true)][int]$Needs
    )
    if ($Needs -ge $Devices.Count) { return $null }
    return $Places[$Devices[$Needs]]
}

#--------------------------------------------------------------------------------------------------
function Wait-SdlPageMapping {
    <#
    .SYNOPSIS
        Opens x360ce 4's SDL page on the controller of the device $SdlPageNeeds names, and waits until it shows a mapping.
    .DESCRIPTION
        The page draws itself while it is shown, and its device line says a controller database knows the device when it
        shows a mapping. When it does not within 30 seconds the run stops with the page's own words. A device -Devices does
        not name, or one on no controller, is said and passed over. x360ce 3 has no SDL page.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][hashtable]$Copy,
        [Parameter(Mandatory = $true)][System.Collections.IDictionary]$Places
    )
    if ($Copy.Version -ne 'v4') { return }
    $place = Get-NeededPlace -Places $Places -Needs $SdlPageNeeds
    if (-not $place) {
        Write-Warning "The SDL page is not checked: it needs $(Get-MissingNeed -Needs $SdlPageNeeds)."
        return
    }
    $n = $place.Controller
    $page = Find-DoorElement -Port $Copy.Port -Words "Pad$n $SdlPageWords" -Role 'Tab'
    if (-not $page) { throw "No SDL page is found on Controller $n." }
    Invoke-Door -Port $Copy.Port -Tool 'ui_open' -Arguments @{ path = $page.Path } | Out-Null
    $said = @{ Text = '' }
    try {
        Wait-Condition -What "the SDL page of Controller $n to show a mapping" -TimeoutSeconds 30 -Condition {
            $line = Find-DoorElement -Port $Copy.Port -Words "Pad$n $SdlLineWords"
            $said.Text = if ($line) { [string]$line.Value } else { '' }
            $said.Text.Contains($SdlMappedWords)
        } | Out-Null
    }
    catch {
        throw "The SDL page of Controller $n, $($place.Product), shows no mapping, so no pictures are taken. It says: $($said.Text)"
    }
    Write-Host "  The SDL page of Controller $n shows a mapping: $(($said.Text -split '\. ')[0])."
}

#--------------------------------------------------------------------------------------------------
function Get-ThemeChoices {
    <#
    .SYNOPSIS
        The Theme list's path and the themes it offers, leaving out the one its description says follows Windows.
    .DESCRIPTION
        The interface tree gives a list's value, not its items, and the door names the items when it refuses a value
        the list does not have, changing nothing. That refusal is the program's own list of themes.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)][int]$Port)
    $list = Find-DoorElement -Port $Port -Words $ThemeWords -Role 'List'
    if (-not $list) { throw "The door finds no list for the words '$ThemeWords'." }
    $items = $null
    try { Invoke-Door -Port $Port -Tool 'ui_set' -Arguments @{ path = $list.Path; value = '?' } | Out-Null }
    catch {
        $match = [regex]::Match($_.Exception.Message, 'Items: (.+?)\.?$')
        if ($match.Success) { $items = $match.Groups[1].Value -split ',\s*' }
    }
    if (-not $items) { throw "The Theme list at $($list.Path) did not name its items." }
    $themes = @($items | Where-Object { $list.Description -notmatch ('\b' + [regex]::Escape($_) + ' follows\b') })
    return @{ Path = $list.Path; Choices = $themes; Current = $list.Value }
}

#endregion

#region Pages and pictures

#--------------------------------------------------------------------------------------------------
function Get-PagesToPicture {
    <#
    .SYNOPSIS
        The pages of one window to picture, each with the names of the pages on the way to it, from ui_pages.
    .DESCRIPTION
        A page that holds two or more pages of its own is pictured through them, one picture each. A page that holds
        only one is pictured once, with the page it holds, under its own name. Hidden pages are left out.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][int]$Port,
        [string]$Window = ''
    )
    $all = @(Invoke-DoorJson -Port $Port -Tool 'ui_pages' | Where-Object { $_.Window -eq $Window })
    $shown = @($all | Where-Object { -not $_.Hidden })
    $byPath = @{}
    $children = @{}
    foreach ($page in $all) { $byPath[$page.Path] = $page; $children[$page.Path] = 0 }
    foreach ($page in $shown) {
        $way = @($page.Way)
        if ($way.Count -gt 0) { $children[$way[-1]]++ }
    }
    $result = New-Object System.Collections.Generic.List[object]
    foreach ($page in $shown) {
        $way = @($page.Way)
        if ($children[$page.Path] -ge 2) { continue }
        if ($way.Count -gt 0 -and $children[$way[-1]] -lt 2) { continue }
        $names = @($way | ForEach-Object { $byPath[$_].Name }) + $page.Name
        $result.Add([pscustomobject]@{ Path = $page.Path; Names = $names })
    }
    return , $result.ToArray()
}

#--------------------------------------------------------------------------------------------------
function ConvertTo-PictureName {
    <#
    .SYNOPSIS
        Joins names into a file name: each name's words run together, such as "Force Feedback" into ForceFeedback.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)][string[]]$Names)
    $parts = foreach ($name in $Names) {
        $words = [regex]::Matches($name, '[\p{L}\p{N}]+') | ForEach-Object { $_.Value.Substring(0, 1).ToUpperInvariant() + $_.Value.Substring(1) }
        $words -join ''
    }
    return ($parts | Where-Object { $_ }) -join '-'
}

#--------------------------------------------------------------------------------------------------
function Save-WindowPicture {
    <#
    .SYNOPSIS
        Saves a picture of a window as PNG, with an overlay the process shows drawn over it when asked.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][long]$Handle,
        [Parameter(Mandatory = $true)][string]$Path,
        [int]$OverlayOf = 0
    )
    $window = [IntPtr]$Handle
    $width = 0; $height = 0; $left = 0; $top = 0
    $pixels = [X360ceScreenshots.WindowPicture]::Capture($window, [ref]$width, [ref]$height, [ref]$left, [ref]$top)
    if (-not $SquareCorners) {
        $radius = [int][Math]::Round(8 * [X360ceScreenshots.WindowPicture]::Dpi($window) / 96)
        [X360ceScreenshots.WindowPicture]::RoundCorners($pixels, $width, $height, $radius)
    }
    if ($OverlayOf -gt 0) {
        $overlay = [X360ceScreenshots.WindowPicture]::OverlayWindows($OverlayOf) | Select-Object -First 1
        if (-not $overlay) { throw 'The balloon to picture is not on screen.' }
        $ow = 0; $oh = 0; $ol = 0; $ot = 0
        $over = [X360ceScreenshots.WindowPicture]::CaptureOverlay($overlay, [ref]$ow, [ref]$oh, [ref]$ol, [ref]$ot)
        $cw = 0; $ch = 0
        $pixels = [X360ceScreenshots.WindowPicture]::Compose($pixels, $width, $height, $left, $top, $over, $ow, $oh, $ol, $ot, [ref]$cw, [ref]$ch)
        $width = $cw; $height = $ch
    }
    $bitmap = New-Object System.Drawing.Bitmap $width, $height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        # The picture is at 96 DPI and says so; a new bitmap takes the dots per inch of whatever runs the script.
        $bitmap.SetResolution(96, 96)
        $area = New-Object System.Drawing.Rectangle 0, 0, $width, $height
        $data = $bitmap.LockBits($area, [System.Drawing.Imaging.ImageLockMode]::WriteOnly, $bitmap.PixelFormat)
        [System.Runtime.InteropServices.Marshal]::Copy($pixels, 0, $data.Scan0, $pixels.Length)
        $bitmap.UnlockBits($data)
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $bitmap.Dispose() }
    Set-PngDpi -Path $Path
}

#--------------------------------------------------------------------------------------------------
function Set-PngDpi {
    <#
    .SYNOPSIS
        Makes a PNG say 96 DPI as image editors write it: 3780 pixels per metre across and down in its pHYs chunk.
    .DESCRIPTION
        System.Drawing writes 96 DPI as 3779 pixels per metre, which some editors show as 95.99 DPI. Only the pHYs
        chunk's data and its CRC-32 change; the image data stays byte for byte as it was. A PNG chunk is a big-endian
        4-byte length, a 4-byte type, the data, and the CRC-32 of the type and the data.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)][string]$Path)
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $at = 8
    while ($at + 12 -le $bytes.Length) {
        $length = ([long]$bytes[$at] -shl 24) + ([long]$bytes[$at + 1] -shl 16) + ([long]$bytes[$at + 2] -shl 8) + $bytes[$at + 3]
        $type = [System.Text.Encoding]::ASCII.GetString($bytes, $at + 4, 4)
        if ($type -eq 'pHYs' -and $length -eq 9) {
            # 3780 is 0x00000EC4; the unit 1 is the metre.
            [Array]::Copy([byte[]](0, 0, 0x0E, 0xC4, 0, 0, 0x0E, 0xC4, 1), 0, $bytes, $at + 8, 9)
            $crc = 0xFFFFFFFFL
            for ($i = $at + 4; $i -lt $at + 17; $i++) {
                $crc = $crc -bxor $bytes[$i]
                for ($bit = 0; $bit -lt 8; $bit++) {
                    $crc = if ($crc -band 1) { ($crc -shr 1) -bxor 0xEDB88320L } else { $crc -shr 1 }
                }
            }
            $crc = $crc -bxor 0xFFFFFFFFL
            for ($k = 0; $k -lt 4; $k++) { $bytes[$at + 17 + $k] = [byte](($crc -shr (24 - 8 * $k)) -band 0xFF) }
            [System.IO.File]::WriteAllBytes($Path, $bytes)
            return
        }
        if ($type -eq 'IDAT') { break }
        $at += 12 + $length
    }
    throw "$Path has no pHYs chunk before its image data."
}

#--------------------------------------------------------------------------------------------------
function Get-WindowHandle {
    <#
    .SYNOPSIS
        A window's handle from the door: the main window's for an empty path.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][int]$Port,
        [string]$Window = ''
    )
    $current = Invoke-DoorJson -Port $Port -Tool 'ui_current'
    $found = @($current.Windows | Where-Object { $_.Path -eq $Window }) | Select-Object -First 1
    if (-not $found -or -not $found.Handle) { throw "The door gives no handle for the window '$Window'." }
    return [long]$found.Handle
}

#--------------------------------------------------------------------------------------------------
function Save-Pages {
    <#
    .SYNOPSIS
        Opens and pictures each page of one window, and returns the pages pictured.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][hashtable]$Copy,
        [Parameter(Mandatory = $true)][string]$Folder,
        [Parameter(Mandatory = $true)][string]$Prefix,
        [string]$Window = '',
        [string]$WindowName
    )
    $handle = Get-WindowHandle -Port $Copy.Port -Window $Window
    $pages = Get-PagesToPicture -Port $Copy.Port -Window $Window
    $saved = New-Object System.Collections.Generic.List[object]
    $taken = @{}
    if ($pages.Count -eq 0 -and $Window) {
        $pages = @([pscustomobject]@{ Path = $Window; Names = @() })
    }
    foreach ($page in $pages) {
        $names = @()
        if ($WindowName) { $names += $WindowName }
        $names += $page.Names
        $name = $Prefix + '-' + (ConvertTo-PictureName -Names $names)
        if ($taken.ContainsKey($name)) { $taken[$name]++; $name = "$name-$($taken[$name])" } else { $taken[$name] = 1 }
        Invoke-Door -Port $Copy.Port -Tool 'ui_open' -Arguments @{ path = $page.Path } | Out-Null
        $file = Join-Path $Folder "$name.png"
        Save-WindowPicture -Handle $handle -Path $file
        $saved.Add([pscustomobject]@{ Page = $page.Path; File = $file })
    }
    return , $saved.ToArray()
}

#--------------------------------------------------------------------------------------------------
function Save-OtherWindows {
    <#
    .SYNOPSIS
        Opens each window a button opens that changes nothing, pictures its pages, and closes it as if cancelled.
    .DESCRIPTION
        A button whose name ends in "..." opens a window. The buttons are read from the main window's tree, once each
        by field name, so a button every controller page has is pressed on the first. One left alone, and why, is
        reported.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][hashtable]$Copy,
        [Parameter(Mandatory = $true)][string]$Folder,
        [Parameter(Mandatory = $true)][string]$Prefix
    )
    $tree = Invoke-DoorJson -Port $Copy.Port -Tool 'ui_read'
    $buttons = @(Get-TreeNodes -Node $tree | Where-Object { $_.Role -eq 'Button' -and $_.Path -and $_.Name -match '(\.\.\.|\u2026)$' -and -not $_.ShownHidden })
    $seen = @{}
    $saved = New-Object System.Collections.Generic.List[object]
    $left = New-Object System.Collections.Generic.List[string]
    foreach ($button in $buttons) {
        if ($seen.ContainsKey($button.Id)) { continue }
        $seen[$button.Id] = $true
        $label = $button.Name -replace '(\.\.\.|\u2026)$', ''
        if (-not $button.Description) { $left.Add("$label (not described)"); continue }
        $why = [regex]::Match($button.Description, $SkipWhenDescribed, 'IgnoreCase')
        if ($why.Success) { $left.Add("$label ('$($why.Value)': $($button.Description))"); continue }
        $before = @(Invoke-DoorJson -Port $Copy.Port -Tool 'ui_windows' | ForEach-Object { $_.Path })
        try { Invoke-Door -Port $Copy.Port -Tool 'ui_invoke' -Arguments @{ path = $button.Path } | Out-Null }
        catch { $left.Add("$label (the press failed: $($_.Exception.Message))"); continue }
        Invoke-Door -Port $Copy.Port -Tool 'ui_wait' | Out-Null
        $opened = @(Invoke-DoorJson -Port $Copy.Port -Tool 'ui_windows' | Where-Object { $before -notcontains $_.Path })
        if ($opened.Count -eq 0) { $left.Add("$label (opened no window; it may need a selection first)"); continue }
        foreach ($window in $opened) {
            try {
                $windowName = $window.Path -replace 'Form$', ''
                $pictures = Save-Pages -Copy $Copy -Folder $Folder -Prefix $Prefix -Window $window.Path -WindowName $windowName
                foreach ($picture in $pictures) { $saved.Add($picture) }
            }
            finally {
                Invoke-Door -Port $Copy.Port -Tool 'ui_close' -Arguments @{ path = $window.Path } | Out-Null
            }
        }
        Wait-Condition -What "the windows $label opened to close" -TimeoutSeconds 15 -Condition {
            @(Invoke-DoorJson -Port $Copy.Port -Tool 'ui_windows' | Where-Object { $before -notcontains $_.Path }).Count -eq 0
        } | Out-Null
    }
    return @{ Saved = $saved.ToArray(); Left = $left.ToArray() }
}

#--------------------------------------------------------------------------------------------------
function Save-Ads {
    <#
    .SYNOPSIS
        Takes each advertisement: opens the element's page, points at it with its balloon, and pictures the window
        with the balloon drawn over it.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][hashtable]$Copy,
        [Parameter(Mandatory = $true)][System.Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)][string]$Folder,
        [Parameter(Mandatory = $true)][string]$Prefix,
        [Parameter(Mandatory = $true)][System.Collections.IDictionary]$Places
    )
    $saved = New-Object System.Collections.Generic.List[string]
    $left = New-Object System.Collections.Generic.List[string]
    foreach ($ad in $Ads[$Copy.Version]) {
        $words = $ad.Find
        if ($ad.ContainsKey('Needs')) {
            $place = Get-NeededPlace -Places $Places -Needs $ad.Needs
            if (-not $place) {
                $left.Add("$($ad.Name) (needs $(Get-MissingNeed -Needs $ad.Needs))")
                continue
            }
            $words = $ad.Find -f $place.Controller
        }
        try {
            if ($ad.Open) {
                $page = Find-DoorElement -Port $Copy.Port -Words $ad.Open -Role 'Tab'
                if (-not $page) { $left.Add("$($ad.Name) (no page found for '$($ad.Open)')"); continue }
                Invoke-Door -Port $Copy.Port -Tool 'ui_open' -Arguments @{ path = $page.Path } | Out-Null
            }
            $target = Find-DoorElement -Port $Copy.Port -Words $words -Role $ad.Role
            if (-not $target) { $left.Add("$($ad.Name) (nothing found for '$words')"); continue }
            Invoke-Door -Port $Copy.Port -Tool 'ui_open' -Arguments @{ path = $target.Path } | Out-Null
            Invoke-Door -Port $Copy.Port -Tool 'ui_show' -Arguments @{ path = $target.Path; text = $ad.Text; seconds = 0 } | Out-Null
            Invoke-Door -Port $Copy.Port -Tool 'ui_wait' | Out-Null
            $file = Join-Path $Folder ($Prefix + '-' + (ConvertTo-PictureName -Names $ad.Name) + '.png')
            Save-WindowPicture -Handle (Get-WindowHandle -Port $Copy.Port) -Path $file -OverlayOf $Process.Id
            $saved.Add($file)
        }
        catch { $left.Add("$($ad.Name) ($($_.Exception.Message))") }
        finally {
            try { Invoke-Door -Port $Copy.Port -Tool 'ui_hide' | Out-Null } catch { Write-Verbose "Nothing to hide: $($_.Exception.Message)" }
        }
    }
    return @{ Saved = $saved.ToArray(); Left = $left.ToArray() }
}

#--------------------------------------------------------------------------------------------------
function Compare-PagesWithExport {
    <#
    .SYNOPSIS
        Prints the pages found in only one of the walk and the program's exported interface tree.
    .DESCRIPTION
        The export, ui-tree-v4.json or ui-tree-v3.json, is the program's own description of its pages, written by
        x360ce.exe /ExportUi. A page in the walk and not the export is new since the export, or one that appears only
        with a device connected; one in the export and not the walk is one the program did not show this time.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][hashtable]$Copy
    )
    $file = Join-Path $TreeFolder "ui-tree-$($Copy.Version).json"
    if (-not (Test-Path -LiteralPath $file)) {
        Write-Host "  No exported tree at $file, so the walk is not compared with one."
        return @{ OnlyWalked = @(); OnlyExported = @() }
    }
    # Pages are told apart by their field names: the words on some tabs carry a count or a version that changes.
    $walked = @{}
    foreach ($page in @(Invoke-DoorJson -Port $Copy.Port -Tool 'ui_pages' | Where-Object { $_.Window -eq '' })) {
        $walked[$page.Path.Split('/')[-1]] = $page.Name
    }
    $exported = @{}
    $export = Get-Content -LiteralPath $file -Raw | ConvertFrom-Json
    foreach ($node in @(Get-TreeNodes -Node $export | Where-Object { $_.Role -eq 'Tab' -and $_.Id })) {
        $exported[$node.Id] = $node.Name
    }
    $onlyWalked = @($walked.Keys | Where-Object { -not $exported.ContainsKey($_) } | Sort-Object | ForEach-Object { '{0} ({1})' -f $walked[$_], $_ })
    $onlyExported = @($exported.Keys | Where-Object { -not $walked.ContainsKey($_) } | Sort-Object | ForEach-Object { '{0} ({1})' -f $exported[$_], $_ })
    Write-Host "  Pages walked: $($walked.Count) kinds; pages in $(Split-Path -Leaf $file): $($exported.Count) kinds."
    if ($onlyWalked.Count -gt 0) { Write-Host "  In the program, not in the export: $($onlyWalked -join ', ')" }
    if ($onlyExported.Count -gt 0) { Write-Host "  In the export, not shown by the program now: $($onlyExported -join ', ')" }
    if ($onlyWalked.Count -eq 0 -and $onlyExported.Count -eq 0) { Write-Host '  The walk and the export list the same pages.' }
    return @{ OnlyWalked = $onlyWalked; OnlyExported = $onlyExported }
}

#endregion

#region One program

#--------------------------------------------------------------------------------------------------
function Invoke-VersionRun {
    <#
    .SYNOPSIS
        Pictures one program from start to close, and returns what was taken and what was left out.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][ValidateSet('v4', 'v3')][string]$Version,
        [Parameter(Mandatory = $true)][string]$Exe,
        [Parameter(Mandatory = $true)][string]$Folder,
        [Parameter(Mandatory = $true)][int]$Port
    )
    Write-Host "x360ce $Version from $Exe"
    $clock = [System.Diagnostics.Stopwatch]::StartNew()
    $owner = Get-OwnerSnapshot
    $copy = New-ScratchCopy -Version $Version -Exe $Exe -Port $Port
    $adsFolder = Join-Path $Folder 'Ads'
    $summary = [ordered]@{
        Version = $Version; Program = (Get-Item -LiteralPath $Exe).VersionInfo.FileVersion; Pictures = [ordered]@{}
        Windows = 0; Ads = 0; AdsLeftOut = @(); WindowsLeftOut = @(); MissingDevices = @(); AnsweredAtStart = @()
        OnlyWalked = @(); OnlyExported = @(); FolderTimesChanged = @(); Seconds = 0
    }
    $process = $null
    $emptied = $false
    try {
        $process = Start-ScratchCopy -Copy $copy
        $needed = @('ui_pages', 'ui_open', 'ui_wait', 'ui_close', 'ui_current', 'ui_windows', 'ui_read', 'ui_find', 'ui_set',
            'ui_invoke', 'ui_show', 'ui_hide', 'devices_list')
        if ($Version -eq 'v4') { $needed += 'device_map' } else { $needed += 'settings_save' }
        Assert-DoorTools -Copy $copy -Needed $needed
        # The last run's pictures go only once this program can take new ones.
        New-Item -ItemType Directory -Path $adsFolder -Force | Out-Null
        Get-ChildItem -LiteralPath $Folder -Filter '*.png' -File | Remove-Item -Force
        Get-ChildItem -LiteralPath $adsFolder -Filter '*.png' -File | Remove-Item -Force
        $emptied = $true
        $summary.AnsweredAtStart = Complete-StartUp -Copy $copy
        if ($PictureScreen) {
            # The windows it opens later appear over this one, so on the same screen.
            $handle = [IntPtr](Get-WindowHandle -Port $Port)
            [X360ceScreenshots.WindowPicture]::MoveTo($handle, $PictureScreen.Left, $PictureScreen.Top)
            Wait-Condition -What "the window to show on $($PictureScreen.Name) as drawn" -TimeoutSeconds 10 -Condition {
                [X360ceScreenshots.WindowPicture]::ShownUnscaled($handle)
            } | Out-Null
        }
        # No picture is taken before every device is on its controller and works there, nor after a theme change
        # until it still does.
        $places = Set-DeviceMapping -Copy $copy
        $summary.MissingDevices = @($Devices | Where-Object { -not $places[$_] })
        Wait-WorkingController -Copy $copy -Places $places
        $what = if ($Version -eq 'v4') { 'online, in its XInput place, its tab green on both halves' } else { 'read by its controller' }
        Write-Host "  Every device found works: $what."
        Wait-SdlPageMapping -Copy $copy -Places $places
        $themeList = Get-ThemeChoices -Port $Port
        Write-Host "  Themes: $($themeList.Choices -join ', ')."
        foreach ($choice in $themeList.Choices) {
            Invoke-Door -Port $Port -Tool 'ui_set' -Arguments @{ path = $themeList.Path; value = $choice } | Out-Null
            Invoke-Door -Port $Port -Tool 'ui_wait' | Out-Null
            Wait-WorkingController -Copy $copy -Places $places
            $prefix = "$Version-$choice"
            # Named apart from the script's $Ads: PowerShell names ignore case, and a local $ads would hide it.
            $pagePictures = Save-Pages -Copy $copy -Folder $Folder -Prefix $prefix
            $windowPictures = Save-OtherWindows -Copy $copy -Folder $Folder -Prefix $prefix
            $adPictures = Save-Ads -Copy $copy -Process $process -Folder $adsFolder -Prefix $prefix -Places $places
            $summary.Pictures[$choice] = $pagePictures.Count
            $summary.Windows += $windowPictures.Saved.Count
            $summary.Ads += $adPictures.Saved.Count
            $summary.WindowsLeftOut = $windowPictures.Left
            $summary.AdsLeftOut += @($adPictures.Left | ForEach-Object { "${choice}: $_" })
            Write-Host ("  {0}: {1} pages, {2} window pictures, {3} advertisements." -f $choice, $pagePictures.Count, $windowPictures.Saved.Count, $adPictures.Saved.Count)
        }
        $compared = Compare-PagesWithExport -Copy $copy
        $summary.OnlyWalked = $compared.OnlyWalked
        $summary.OnlyExported = $compared.OnlyExported
    }
    catch {
        # A run that stops leaves no half set of pictures behind.
        if ($emptied) {
            Get-ChildItem -LiteralPath $Folder -Filter '*.png' -File | Remove-Item -Force
            Get-ChildItem -LiteralPath $adsFolder -Filter '*.png' -File | Remove-Item -Force
        }
        throw
    }
    finally {
        Stop-ScratchCopy -Copy $copy -Process $process
        $summary.FolderTimesChanged = Assert-OwnerUnchanged -Before $owner -After (Get-OwnerSnapshot)
        $summary.Seconds = [int]$clock.Elapsed.TotalSeconds
    }
    foreach ($line in $summary.WindowsLeftOut) { Write-Host "  Window not opened: $line" }
    foreach ($line in $summary.AdsLeftOut) { Write-Host "  Advertisement left out: $line" }
    foreach ($folderName in $summary.FolderTimesChanged) {
        Write-Host "  $folderName kept every file as it was; only its own time moved, from x360ce's test of whether it can write there."
    }
    Write-Host ("  Done in {0} seconds; C:\ProgramData\X360CE files unchanged." -f $summary.Seconds)
    return [pscustomobject]$summary
}

#endregion

# Where and how the pictures are taken, as the description says.
$StartUnaware = [X360ceScreenshots.WindowPicture]::SystemDpi() -ne 96
$screens = @([X360ceScreenshots.WindowPicture]::Screens())
$PictureScreen = @($screens | Where-Object { $_.Dpi -eq 96 } | Sort-Object { -not $_.Main })[0]
Write-Host ('Screens: ' + (@($screens | ForEach-Object {
                '{0} at {1} %{2}' -f $_.Name, [int]($_.Dpi * 100 / 96), $(if ($_.Main) { ', the main one' } else { '' })
            }) -join '; ') + '.')
if ($StartUnaware) { Write-Host 'x360ce is started as a program that does not know the display scale, so it lays itself out at 96 DPI.' }
if ($PictureScreen) { Write-Host "Pictures are taken on $($PictureScreen.Name), which is at 100 %: whole windows, as Windows shows them." }
else { Write-Host 'No screen is at 100 %, so each picture is a window''s inside, below its title bar, as the program draws it at 96 DPI.' }

$runs = @{
    v4 = @{ Exe = $V4Exe; Folder = $V4Screenshots; Port = $V4Port }
    v3 = @{ Exe = $V3Exe; Folder = $V3Screenshots; Port = $V3Port }
}
foreach ($version in $Versions) {
    $run = $runs[$version]
    New-Item -ItemType Directory -Path $run.Folder -Force | Out-Null
    Invoke-VersionRun -Version $version -Exe $run.Exe -Folder $run.Folder -Port $run.Port
}
