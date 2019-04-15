using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Processes
{
	public class MouseHelper
	{
		[Flags]
		public enum MouseEventFlags : uint
		{
			MOUSEEVENTF_MOVE = 0x0001,
			MOUSEEVENTF_LEFTDOWN = 0x0002,
			MOUSEEVENTF_LEFTUP = 0x0004,
			MOUSEEVENTF_RIGHTDOWN = 0x0008,
			MOUSEEVENTF_RIGHTUP = 0x0010,
			MOUSEEVENTF_MIDDLEDOWN = 0x0020,
			MOUSEEVENTF_MIDDLEUP = 0x0040,
			MOUSEEVENTF_XDOWN = 0x0080,
			MOUSEEVENTF_XUP = 0x0100,
			MOUSEEVENTF_WHEEL = 0x0800,
			MOUSEEVENTF_VIRTUALDESK = 0x4000,
			MOUSEEVENTF_ABSOLUTE = 0x8000
		}

		const Int32 CURSOR_SHOWING = 0x00000001;

		public class NativeMethods
		{
			//[DllImport("user32", EntryPoint = "FindWindowA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
			//public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

			[DllImport("user32.dll", SetLastError = true)]
			public static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

			[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
			public static extern bool SetCursorPos(int x, int y);

			[DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
			public static extern IntPtr SendMessage(IntPtr hwnd, uint wMsg, uint wParam, uint lParam);

			[DllImport("user32.dll", EntryPoint = "GetIconInfo", SetLastError = true)]
			public static extern bool GetIconInfo(IntPtr hIcon, out Win32.ICONINFO piconinfo);

			[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
			public static extern bool DestroyIcon(IntPtr handle);

			[DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
			public static extern bool GetCursorInfo(out CURSORINFO pci);

			[DllImport("user32.dll", EntryPoint = "CopyIcon", SetLastError = true)]
			public static extern IntPtr CopyIcon(IntPtr hIcon);

			[DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
			public static extern IntPtr DeleteObject(IntPtr hObject);
		}

		public static void MoveMouse(int x, int y)
		{
			NativeMethods.SetCursorPos(x, y);
			LastX = x;
			LastY = y;
		}

		public const uint WM_LBUTTONUP = 0x202;
		public const uint WM_RBUTTONUP = 0x205;
		public const uint WM_LBUTTONDOWN = 0x201;
		public const uint WM_RBUTTONDOWN = 0x204;
		public const uint MK_LBUTTON = 1;

		private static int LastX = 0;
		private static int LastY = 0;
		private static int LastRectX = 0;
		private static int LastRectY = 0;

		public static void SendRMouseClick(string processName)
		{
			SendMouseClick(processName, WM_RBUTTONDOWN, WM_RBUTTONUP);
		}

		public static void SendLMouseClick(string processName)
		{
			SendMouseClick(processName, WM_LBUTTONDOWN, WM_LBUTTONUP);
		}

		static void SendMouseClick(string processName, uint button1, uint button2)
		{
			var ps = System.Diagnostics.Process.GetProcessesByName(processName);
			var mainWindowHandle = IntPtr.Zero;
			if (ps.Length > 0)
				mainWindowHandle = ps[0].MainWindowHandle;
			else
			{
				ps = System.Diagnostics.Process.GetProcesses();
				foreach (var p in ps)
				{
					if (p.MainWindowTitle.IndexOf(processName, StringComparison.InvariantCulture) > -1)
					{
						mainWindowHandle = p.MainWindowHandle;
						break;
					}
				}
			}
			if (mainWindowHandle == IntPtr.Zero)
				return;
			uint dWord = MakeDWord((ushort)(LastX - LastRectX), (ushort)(LastY - LastRectY));
			NativeMethods.SendMessage(mainWindowHandle, button1, MK_LBUTTON, dWord);
			// Logical delay without blocking the current thread.
			System.Threading.Tasks.Task.Delay(100).Wait();
			NativeMethods.SendMessage(mainWindowHandle, button2, 0, dWord);

		}

		public static uint MakeDWord(ushort x, ushort y)
		{
			return ((uint)x << 16) | y;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct MouseKeybdhardwareInputUnion
		{
			[FieldOffset(0)]
			public MouseInputData mi;
		}

		public struct MouseInputData
		{
			public int dx;
			public int dy;
			public uint mouseData;
			public MouseEventFlags dwFlags;
			public uint time;
			public IntPtr dwExtraInfo;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct INPUT
		{
			public SendInputEventType type;
			public MouseKeybdhardwareInputUnion mkhi;
		}
		public enum SendInputEventType : int
		{
			InputMouse,
		}

		public static void Click(int x, int y, MouseButtons button = MouseButtons.Left)
		{
			INPUT mouseInput = new INPUT();
			mouseInput.type = SendInputEventType.InputMouse;
			mouseInput.mkhi.mi.dx = x;
			mouseInput.mkhi.mi.dy = y;
			mouseInput.mkhi.mi.mouseData = 0;
			MoveMouse(x, y);
			MouseEventFlags down = MouseEventFlags.MOUSEEVENTF_LEFTDOWN;
			MouseEventFlags up = MouseEventFlags.MOUSEEVENTF_LEFTUP;
			switch (button)
			{
				case MouseButtons.Middle:
					down = MouseEventFlags.MOUSEEVENTF_MIDDLEDOWN;
					up = MouseEventFlags.MOUSEEVENTF_MIDDLEUP;
					break;
				case MouseButtons.Right:
					down = MouseEventFlags.MOUSEEVENTF_RIGHTDOWN;
					up = MouseEventFlags.MOUSEEVENTF_RIGHTUP;
					break;
				case MouseButtons.XButton1:
					down = MouseEventFlags.MOUSEEVENTF_XDOWN;
					up = MouseEventFlags.MOUSEEVENTF_XUP;
					break;
				default:
					break;
			}
			mouseInput.mkhi.mi.dwFlags = down;
			NativeMethods.SendInput(1, ref mouseInput, Marshal.SizeOf(new INPUT()));
			mouseInput.mkhi.mi.dwFlags = up;
			NativeMethods.SendInput(1, ref mouseInput, Marshal.SizeOf(new INPUT()));
		}

		static object CursorLock = new object();

		public static Bitmap GetCurrentCursorImage(CURSORINFO? cursor = null)
		{

			lock (CursorLock)
			{
				Bitmap image = null;
				CURSORINFO ci;
				var success = true;
				if (cursor.HasValue)
				{
					ci = cursor.Value;
				}
				else
				{
					ci = default(CURSORINFO);
					ci.Size = Marshal.SizeOf(ci);
					success = NativeMethods.GetCursorInfo(out ci);
				}
				if (success && ci.Flags == CURSOR_SHOWING)
				{
					var iconHandle = NativeMethods.CopyIcon(ci.Handle);
					Win32.ICONINFO ii;
					// GetIconInfo created bitmaps for the hbmMask and hbmColor members of ICONINFO.
					// Must Dispose them when they are no longer necessary.
					if (NativeMethods.GetIconInfo(iconHandle, out ii))
					{
						if (ii.hbmColor != IntPtr.Zero)
						{
							var icon = Icon.FromHandle(iconHandle);
							var bmp = icon.ToBitmap();
							image = (Bitmap)bmp.Clone();
							bmp.Dispose();
							icon.Dispose();
							NativeMethods.DeleteObject(ii.hbmColor);
						}
						if (ii.hbmMask != IntPtr.Zero)
						{
							NativeMethods.DeleteObject(ii.hbmMask);
						}
					}
					// Dispose icon handle.
					NativeMethods.DestroyIcon(iconHandle);
				}
				return image;
			}
		}


	}
}
