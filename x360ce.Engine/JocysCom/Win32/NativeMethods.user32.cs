using System.Security.Permissions;
using System.Runtime.InteropServices;
using System;
using System.Drawing;

namespace JocysCom.ClassLibrary.Win32
{
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public static partial class NativeMethods
	{

		#region user32

		/// <summary>
		/// API used to send a message to another window
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

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
		public static extern int GetDesktopWindow();

		/// <summary>
		/// Retrieves the coordinates of a window's client area.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool GetClientRect(IntPtr hWnd, ref Rectangle lpRect);


		/// <summary>
		/// Determine if the application is already open.
		/// </summary>
		[DllImport("USER32.DLL", EntryPoint = "BroadcastSystemMessageA", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
		public static extern int BroadcastSystemMessage(Int32 dwFlags, ref Int32 pdwRecipients, int uiMessage, int wParam, int lParam);

		/// <summary>
		/// Defines a new window message that is guaranteed to be unique throughout the system.
		/// The message value can be used when sending or posting messages.
		/// </summary>
		/// <param name="pString">The message to be registered.</param>
		/// <returns>
		/// If the message is successfully registered, the return value is
		/// a message identifier in the range 0xC000 through 0xFFFF.
		/// If the function fails, the return value is zero.</returns>
		[DllImport("USER32.DLL", EntryPoint = "RegisterWindowMessageA", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
		public static extern int RegisterWindowMessage(String pString);

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

		#endregion

	}
}
