using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Security.Permissions;

namespace JocysCom.ClassLibrary.Win32
{
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public static partial class NativeMethods
	{

		// Conversion:
		// Handle - IntPtr
		//  DWORD - UInt32
		// PDWORD - UInt32
		// LPVOID - IntPtr

		public static int ERROR_INSUFFICIENT_BUFFER =  122;

		#region Disable/Enable UI Effects

		public static void EnableUiEffects(bool enable)
		{
			const uint SPI_SETUIEFFECTS = 0x103F;
			const uint SPIF_SENDCHANGE = 0x02;
			var success = SystemParametersInfo(SPI_SETUIEFFECTS, 0, enable, SPIF_SENDCHANGE);
			if (!success)
			{
				var ex = new Win32Exception();
				throw new Exception(ex.Message);
			}
		}

		public static bool IsUiEffectsEnabled()
		{
			bool enabled;
			const uint SPI_GETUIEFFECTS = 0x103E;
			var success = SystemParametersInfo(SPI_GETUIEFFECTS, 0, out enabled, 0);
			if (!success)
			{
				var ex = new Win32Exception();
				throw new Exception(ex.Message);
			}
			return enabled;
		}

		#endregion

		#region Hide/Show TaskbBar

		public static IntPtr ShellHandle { get { return FindWindow("Shell_TrayWnd", null); } }

		static IntPtr StartButtonHandle { get { return FindWindowEx(GetDesktopWindow(), IntPtr.Zero, "Button", "Start"); } }

		public static ABE GetPosition()
		{
			var data = new APPBARDATA();
			data.Initialize();
			data.hWnd = ShellHandle;
			var result = SHAppBarMessage((int)ABM.ABM_GETTASKBARPOS, ref data);
			if (result == IntPtr.Zero) throw new InvalidOperationException();
			return data.uEdge;
		}

		public static Rectangle GetBounds()
		{
			var data = new APPBARDATA();
			data.Initialize();
			data.hWnd = ShellHandle;
			// Get position and bounds.
			var result = SHAppBarMessage((int)ABM.ABM_GETTASKBARPOS, ref data);
			if (result == IntPtr.Zero) throw new InvalidOperationException();
			return Rectangle.FromLTRB(data.rc.left, data.rc.top, data.rc.right, data.rc.bottom);
		}

		public static ABS GetState()
		{
			var data = new APPBARDATA();
			data.Initialize();
			data.hWnd = ShellHandle;
			var result = SHAppBarMessage((int)ABM.ABM_GETSTATE, ref data);
			return (ABS)result.ToInt32();
		}

		public static void SetAutoHide(bool enable)
		{
			var data = new APPBARDATA();
			data.Initialize();
			data.hWnd = ShellHandle;
			data.lParam = (int)ABS.ABS_ALWAYSONTOP;
			if (enable) data.lParam |= (int)ABS.ABS_AUTOHIDE;
			var result = SHAppBarMessage((int)ABM.ABM_SETSTATE, ref data);
			if (result == IntPtr.Zero) throw new InvalidOperationException();
		}

		/// <summary>
		/// Hide Task bar and enable auto-hide.
		/// </summary>
		public static void HideTaskBar()
		{
			var explorers = Process.GetProcessesByName("explorer.exe");
			if (explorers.Length > 0)
			{
				SetAutoHide(true);
				EnableWindow(ShellHandle, false);
				ShowWindow(ShellHandle, (int)SW.SW_HIDE);
				ShowWindow(StartButtonHandle, (int)SW.SW_HIDE);
			}
		}

		/// <summary>
		/// Show Task bar and disable auto-hide.
		/// </summary>
		public static void ShowTaskBar()
		{
			var explorers = Process.GetProcessesByName("explorer.exe");
			if (explorers.Length > 0)
			{
				EnableWindow(ShellHandle, true);
				ShowWindow(ShellHandle, (int)SW.SW_SHOW);
				ShowWindow(StartButtonHandle, (int)SW.SW_SHOW);
				SetAutoHide(false);
			}
		}

		/// <summary>
		/// Clean dead icons from system tray/notification area.
		/// </summary>
		public static void CleanSystemTray()
		{
			var sh = ShellHandle;
			var tn = FindWindowEx(sh, IntPtr.Zero, "TrayNotifyWnd", null);
			var sp = FindWindowEx(tn, IntPtr.Zero, "SysPager", null);
			var na = FindWindowEx(sp, IntPtr.Zero, "ToolbarWindow32", null);
			var r = new Rectangle();
			int WM_MOUSEMOVE = 0x200;
			NativeMethods.GetClientRect(na, ref r);
			//Now we've got the area, force it to update
			//by sending mouse messages to it.
			var x = 0;
			var y = 0;
			while (x < r.Right)
			{
				while (y < r.Bottom)
				{
					// Make lParam.
					int val = ((y << 16) | (x & 0xFFFF));
					SendMessage(na, WM_MOUSEMOVE, IntPtr.Zero, (IntPtr)val);
					y += 5;
				}
				y = 0;
				x += 5;
			}
		}

		#endregion

	}

}
