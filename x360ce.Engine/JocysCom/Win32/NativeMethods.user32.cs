using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace JocysCom.ClassLibrary.Win32
{
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public static partial class NativeMethods
	{

		#region user32

		/// <summary>
		/// Sends the specified message to a window or windows.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		/// <summary>
		/// Sends the specified message to a window or windows.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		internal static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		/// <summary>
		/// Retrieves a handle to the top-level window whose class name and
		/// window name match the specified strings. This function does not
		/// search child windows. This function does not perform a case-sensitive search.
		/// </summary>
		/// <param name="lpClassName">The class name or a class atom created by a previous call to the RegisterClass or RegisterClassEx function.</param>
		/// <param name="lpWindowName">The window name (the window's title). If this parameter is NULL, all window names match.</param>
		/// <returns>
		/// If the function succeeds, the return value is a handle to the window that has the specified class name and window name.
		/// If the function fails, the return value is NULL. To get extended error information, call GetLastError.
		/// </returns>
		[DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
		public static extern IntPtr FindWindow([In] string lpClassName, [In] string lpWindowName);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr FindWindowEx(IntPtr parentHwnd, IntPtr childAfterHwnd, string className, string windowText);

		/// <summary>
		/// Sets the specified window's show state. 
		/// </summary>
		/// <param name="hWnd">A handle to the window. </param>
		/// <param name="nCmdShow">Controls how the window is to be shown.</param>
		/// <returns>
		/// If the window was previously visible, the return value is nonzero.
		/// If the window was previously hidden, the return value is zero.
		/// </returns>
		[DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
		public static extern bool ShowWindow([In] IntPtr hWnd, [In] int nCmdShow);

		/// <summary>
		/// Enables or disables mouse and keyboard input to the specified window or control.
		/// When input is disabled, the window does not receive input such as mouse clicks and key presses.
		/// When input is enabled, the window receives all input.
		/// </summary>
		/// <param name="hWnd">A handle to the window to be enabled or disabled. </param>
		/// <param name="bEnable">Indicates whether to enable or disable the window. </param>
		/// <returns>
		/// If the window was previously disabled, the return value is nonzero.
		/// If the window was not previously disabled, the return value is zero.
		/// </returns>
		[DllImport("user32.dll", EntryPoint = "EnableWindow", SetLastError = true)]
		public static extern bool EnableWindow([In] IntPtr hWnd, [In] bool bEnable);


		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetDesktopWindow();

		/// <summary>
		/// Retrieves the coordinates of a window's client area.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool GetClientRect(IntPtr hWnd, ref Rectangle lpRect);

		/// <summary>
		/// Registers the device or type of device for which a window will receive notifications.
		/// </summary>
		/// <param name="hRecipient">A handle to the window or service that will receive device events for the devices specified in the NotificationFilter parameter.</param>
		/// <param name="NotificationFilter">A pointer to a block of data that specifies the type of device for which notifications should be sent.</param>
		/// <param name="Flags">This parameter can be one of the following values.</param>
		/// <returns>If the function succeeds, the return value is a device notification handle. If the function fails, the return value is NULL. To get extended error information, call GetLastError.</returns>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		internal static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, uint Flags);

		/// <summary>
		/// Closes the specified device notification handle.
		/// </summary>
		/// <param name="Handle">Device notification handle returned by the RegisterDeviceNotification function.</param>
		/// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error information, call GetLastError.</returns>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		internal static extern uint UnregisterDeviceNotification(IntPtr Handle);

		/// <summary>
		/// Sends a message to the specified recipients. The recipients can be applications, installable drivers,
		/// network drivers, system-level device drivers, or any combination of these system components. 
		/// </summary>
		/// <param name="dwFlags">The broadcast option.</param>
		/// <param name="pdwRecipients">A pointer to a variable that contains and receives information about the recipients of the message.</param>
		/// <param name="uiMessage">The message to be sent.</param>
		/// <param name="wParam">Additional message-specific information.</param>
		/// <param name="lParam">Additional message-specific information.</param>
		/// <returns>Positive value if the function succeeds, -1 if the function is unable to broadcast the message.</returns>
		[DllImport("user32.dll", EntryPoint = "BroadcastSystemMessageA", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
		internal static extern int BroadcastSystemMessage(Int32 dwFlags, ref Int32 pdwRecipients, int uiMessage, int wParam, int lParam);

		public static int BroadcastSystemMessage(int dwFlags, ref int pdwRecipients, int uiMessage, int wParam, int lParam, out Exception error)
		{
			var result = BroadcastSystemMessage(dwFlags, ref pdwRecipients, uiMessage, wParam, lParam);
			error = (result < 0) ? new Exception(new Win32Exception().ToString()) : null;
			return result;
		}

		/// <summary>
		/// Defines a new window message that is guaranteed to be unique throughout the system.
		/// The message value can be used when sending or posting messages.
		/// </summary>
		/// <param name="pString">The message to be registered.</param>
		/// <returns>
		/// If the message is successfully registered, the return value is a message identifier in the range 0xC000 through 0xFFFF.
		/// If the function fails, the return value is zero. To get extended error information, call GetLastError.
		/// </returns>
		[DllImport("user32.dll", EntryPoint = "RegisterWindowMessageA", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
		internal static extern int RegisterWindowMessage(String pString);

		public static int RegisterWindowMessage(string pString, out Exception error)
		{
			var id = RegisterWindowMessage(pString);
			error = (id == 0) ? new Exception(new Win32Exception().ToString()) : null;
			return id;
		}

		/// <summary>
		/// Retrieves the value of one of the system-wide parameters.
		/// </summary>
		/// <param name="uiAction">The system-wide parameter to be retrieved.</param>
		/// <param name="uiParam">A parameter whose usage and format depends on the system parameter being queried.</param>
		/// <param name="pvParam">A parameter whose usage and format depends on the system parameter being queried.</param>
		/// <param name="fWinIni"></param>
		/// <returns>If the function succeeds, the return value is a nonzero value.</returns>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out bool pvParam, uint fWinIni);

		/// <summary>
		/// Sets the value of one of the system-wide parameters.
		/// This function can also update the user profile while setting a parameter.
		/// </summary>
		/// <param name="uiAction">The system-wide parameter to be set.</param>
		/// <param name="uiParam">A parameter whose usage and format depends on the system parameter being set.</param>
		/// <param name="pvParam">A parameter whose usage and format depends on the system parameter being set.</param>
		/// <param name="fWinIni">
		/// Specifies whether the user profile is to be updated,
		/// and if so, whether the WM_SETTINGCHANGE message is to be broadcast to all top-level windows to notify them
		/// of the change.
		/// </param>
		/// <returns>If the function succeeds, the return value is a nonzero value.</returns>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, bool pvParam, uint fWinIni);

		/// <summary>
		/// Retrieves a handle to the window that contains the specified point. 
		/// </summary>
		/// <param name="Point">The point to be checked. </param>
		/// <returns>
		/// The return value is a handle to the window that contains the point.
		/// If no window exists at the given point, the return value is NULL.
		/// If the point is over a static text control, the return value is a handle to the window under the static text control. </returns>
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr WindowFromPoint(Point point);

		//[DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		//private static extern bool GetWindowInfo(IntPtr hWnd, ref WINDOWINFO pwo);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowProc lpEnumFunc, IntPtr lParam);

		public delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

		[DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		public static extern IntPtr GetWindow(IntPtr hwnd, int wFlag);

		[DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		public static extern int GetWindowThreadProcessId(IntPtr hwnd, ref int lpdwProcessId);

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		public static extern bool IsWindowEnabled(IntPtr hwnd);

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		public static extern bool IsWindowVisible(IntPtr hwnd);

		[DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		private static extern int AttachThreadInput(int idAttach, int idAttachTo, int fAttach);

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		private static extern bool SetForegroundWindow(IntPtr hwnd);

		[DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		private static extern IntPtr SetFocus(IntPtr hwnd);

		#endregion

		#region Helper Methods

		private static void AppActivateHelper(IntPtr hwndApp)
		{
			int num = 0;
			//try
			//{
			new UIPermission(UIPermissionWindow.AllWindows).Demand();
			//}
			//catch (Exception ex)
			//{
			//	throw;
			//}
			if (!IsWindowEnabled(hwndApp) || !IsWindowVisible(hwndApp))
			{
				IntPtr window = GetWindow(hwndApp, 0);
				while (window != IntPtr.Zero)
				{
					if (GetWindow(window, 4) == hwndApp)
					{
						if (IsWindowEnabled(window) && IsWindowVisible(window))
						{
							break;
						}
						hwndApp = window;
						window = GetWindow(hwndApp, 0);
					}
					window = GetWindow(window, 2);
				}
				if (window == IntPtr.Zero)
				{
					throw new ArgumentException("Process not found");
				}
				hwndApp = window;
			}
			AttachThreadInput(0, GetWindowThreadProcessId(hwndApp, ref num), 1);
			SetForegroundWindow(hwndApp);
			SetFocus(hwndApp);
			AttachThreadInput(0, GetWindowThreadProcessId(hwndApp, ref num), 0);
		}


		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public static void AppActivate(int ProcessId)
		{
			int num = 0;
			IntPtr window = GetWindow(GetDesktopWindow(), 5);
			while (window != IntPtr.Zero)
			{
				GetWindowThreadProcessId(window, ref num);
				if (((num == ProcessId) && IsWindowEnabled(window)) && IsWindowVisible(window))
				{
					break;
				}
				window = GetWindow(window, 2);
			}
			if (window == IntPtr.Zero)
			{
				window = GetWindow(GetDesktopWindow(), 5);
				while (window != IntPtr.Zero)
				{
					GetWindowThreadProcessId(window, ref num);
					if (num == ProcessId)
					{
						break;
					}
					window = GetWindow(window, 2);
				}
			}
			if (window == IntPtr.Zero)
			{
				throw new ArgumentException(string.Format("Process not found: {0}", ProcessId));
			}
			AppActivateHelper(window);
		}


		#endregion

	}
}
