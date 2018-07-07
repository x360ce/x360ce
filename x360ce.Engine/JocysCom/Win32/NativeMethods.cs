using System.Security.Permissions;
using System.Runtime.InteropServices;
using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Security.Principal;
using System.ComponentModel;

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
		public static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);


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
		public static extern IntPtr WindowFromPoint(POINT Point);

		#endregion

		#region advapi32

		/// <summary>
		/// The LogonUser function attempts to log a user on to the local computer.
		/// </summary>
		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

		/// <summary>
		/// Opens the access token associated with a process.
		/// </summary>
		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern Boolean OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

		/// <summary>
		/// Retrieves a specified type of information about an access token.
		/// </summary>
		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetTokenInformation(
			IntPtr TokenHandle,
			TOKEN_INFORMATION_CLASS TokenInformationClass,
			IntPtr TokenInformation,
			UInt32 TokenInformationLength,
			out UInt32 ReturnLength
		);

		#endregion

		#region shell32

		[DllImport("shell32.dll")]
		public static extern IntPtr SHAppBarMessage(int dwMessage, [MarshalAs(UnmanagedType.Struct)] ref APPBARDATA pData);

		#endregion

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

		static IntPtr StartButtonHandle { get { return FindWindowEx((IntPtr)GetDesktopWindow(), IntPtr.Zero, "Button", "Start"); } }

		public static ABE GetPosition()
		{
			var data = new APPBARDATA();
			data.hWnd = ShellHandle;
			data.cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));
			var result = SHAppBarMessage((int)ABM.ABM_GETTASKBARPOS, ref data);
			if (result == IntPtr.Zero) throw new InvalidOperationException();
			return (ABE)data.uEdge;
		}

		public static Rectangle GetBounds()
		{
			var data = new APPBARDATA();
			data.hWnd = ShellHandle;
			// Get position and bounds.
			data.cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));
			var result = SHAppBarMessage((int)ABM.ABM_GETTASKBARPOS, ref data);
			if (result == IntPtr.Zero) throw new InvalidOperationException();
			return Rectangle.FromLTRB(data.rc.left, data.rc.top, data.rc.right, data.rc.bottom);
		}

		public static ABS GetState()
		{
			var data = new APPBARDATA();
			data.cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));
			data.hWnd = ShellHandle;
			var result = SHAppBarMessage((int)ABM.ABM_GETSTATE, ref data);
			return (ABS)result.ToInt32();
		}

		public static void SetAutoHide(bool enable)
		{
			var data = new APPBARDATA();
			data.cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));
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
			var r = new RECT();
			int WM_MOUSEMOVE = 0x200;
			NativeMethods.GetClientRect(na, ref r);
			//Now we've got the area, force it to update
			//by sending mouse messages to it.
			var x = 0;
			var y = 0;
			while (x < r.right)
			{
				while (y < r.bottom)
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

		#region UAC

		/// <summary>Enables the UAC shield icon for the given button control</summary>
		/// <param name="ButtonToEnable">Button to display shield icon on.</param>
		public static void EnableShieldIcon(Button button)
		{
			// See http://msdn2.microsoft.com/en-us/library/aa361904.aspx
			int BCM_FIRST = 0x1600; // Normal button
			int BCM_SETSHIELD = BCM_FIRST + 0x000C; // Shield button
													// Input validation
			if (button == null) return;
			button.FlatStyle = FlatStyle.System;
			// Send the BCM_SETSHIELD message to the control
			NativeMethods.SendMessage(button.Handle, BCM_SETSHIELD, new IntPtr(0), new IntPtr(1));
		}

		/// <summary>Disable the UAC shield icon for the given button control</summary>
		/// <param name="ButtonToEnable">Button to remove shield icon.</param>
		public static void DisableShieldIcon(Button button)
		{
			int BCM_FIRST = 0x1600; // Normal button
			int BCM_SETSHIELD = BCM_FIRST + 0x000C; // Shield button
													// Input validation
			if (button == null) return;
			button.FlatStyle = FlatStyle.System;
			// Send the BCM_SETSHIELD message to the control
			NativeMethods.SendMessage(button.Handle, BCM_SETSHIELD, new IntPtr(0), new IntPtr(0));
		}

		/// <summary>Check if current process is elevated.</summary>
		public static bool IsElevated
		{
			get
			{
				//if (!IsVista) throw new ApplicationException("Function requires Vista or higher");
				// METHOD 1
				//AppDomain myDomain = Thread.GetDomain();
				//myDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
				//WindowsPrincipal myPrincipal = (WindowsPrincipal)Thread.CurrentPrincipal;
				//return (myPrincipal.IsInRole(WindowsBuiltInRole.Administrator));
				// METHOD 2
				bool bRetVal = false;
				IntPtr hToken = IntPtr.Zero;
				IntPtr hProcess = NativeMethods.GetCurrentProcess();
				if (hProcess == IntPtr.Zero) throw new InvalidOperationException("Error getting current process handle");
				bRetVal = NativeMethods.OpenProcessToken(hProcess, WinNT.TOKEN_QUERY, out hToken);
				if (!bRetVal) throw new InvalidOperationException("Error opening process token");
				try
				{
					TOKEN_ELEVATION te;
					te.TokenIsElevated = 0;

					UInt32 dwReturnLength = 0;
					Int32 teSize = Marshal.SizeOf(te);
					IntPtr tePtr = Marshal.AllocHGlobal(teSize);
					try
					{
						Marshal.StructureToPtr(te, tePtr, true);
						bRetVal = NativeMethods.GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenElevation, tePtr, (UInt32)teSize, out dwReturnLength);
						if ((!bRetVal) | (teSize != dwReturnLength))
						{
							throw new InvalidOperationException("Error getting token information");
						}
						te = (TOKEN_ELEVATION)Marshal.PtrToStructure(tePtr, typeof(TOKEN_ELEVATION));
					}
					finally
					{
						Marshal.FreeHGlobal(tePtr);
					}
					return (te.TokenIsElevated != 0);
				}
				finally
				{
					NativeMethods.CloseHandle(hToken);
				}
			}
		}

		/// <summary>
		/// Get elevation type.
		/// </summary>
		/// <returns>TokenElevationType</returns>
		public static TOKEN_ELEVATION_TYPE GetElevationType()
		{
			bool bRetVal = false;
			IntPtr hToken = IntPtr.Zero;
			IntPtr hProcess = NativeMethods.GetCurrentProcess();
			if (hProcess == IntPtr.Zero) throw new ApplicationException("Error getting current process handle");
			bRetVal = NativeMethods.OpenProcessToken(hProcess, WinNT.TOKEN_QUERY, out hToken);
			if (!bRetVal) throw new ApplicationException("Error opening process token");
			try
			{
				TOKEN_ELEVATION_TYPE tet = TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;
				UInt32 dwReturnLength = 0;
				UInt32 tetSize = (uint)Marshal.SizeOf((int)tet);
				IntPtr tetPtr = Marshal.AllocHGlobal((int)tetSize);
				try
				{
					bRetVal = NativeMethods.GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenElevationType, tetPtr, tetSize, out dwReturnLength);
					if ((!bRetVal) | (tetSize != dwReturnLength))
					{
						throw new ApplicationException("Error getting token information");
					}
					tet = (TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(tetPtr);
				}
				finally
				{
					Marshal.FreeHGlobal(tetPtr);
				}
				return tet;
			}
			finally
			{
				NativeMethods.CloseHandle(hToken);
			}
		}

		public static bool IsInAdministratorRole
		{
			get
			{
				WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
				return pricipal.IsInRole(WindowsBuiltInRole.Administrator);
			}
		}

		public static bool AllowRead(params string[] files)
		{
			FileIOPermission f2 = new FileIOPermission(PermissionState.None);
			for (int i = 0; i < files.Length; i++)
			{
				f2.AddPathList(FileIOPermissionAccess.Read, files[i]);
			}
			try
			{
				f2.Demand();
				return true;
			}
			catch
			{
				return false;
			}
		}


		public static Process CreateElevatedProcess(string fileName, string arguments = null)
		{
			ProcessStartInfo psi = new ProcessStartInfo();
			psi.UseShellExecute = true;
			psi.WorkingDirectory = Environment.CurrentDirectory;
			psi.FileName = fileName;
			if (arguments != null) psi.Arguments = arguments;
			psi.CreateNoWindow = true;
			psi.Verb = "runas";
			var process = new Process();
			// Must enable Exited event for both sync and async scenarios.
			process.EnableRaisingEvents = true;
			process.StartInfo = psi;
			return process;
		}

		/// <summary>
		/// Start program in elevated mode.
		/// </summary>
		/// <param name="fileName"></param>
		public static int RunElevated(string fileName, string arguments, ProcessWindowStyle style)
		{
			int exitCode = -1;
			if (String.IsNullOrEmpty(fileName))
				throw new ArgumentNullException("Executable file name must be specified");
			using (Process process = CreateElevatedProcess(fileName, arguments))
			{
				try
				{
					process.StartInfo.WindowStyle = style;
					process.Start();
				}
				catch (Win32Exception)
				{
					// The user refused to allow privileges elevation
					// or other error happend. Do nothing and return...
					return exitCode;
				}
				process.WaitForExit();
				exitCode = process.ExitCode;
			}
			return exitCode;
		}

		public static void RunElevatedAsync(string fileName, EventHandler exitedEventHandler)
		{
			if (String.IsNullOrEmpty(fileName)) throw new ArgumentNullException("Executable file name must be specified");
			using (Process process = CreateElevatedProcess(fileName))
			{
				if (exitedEventHandler != null) process.Exited += exitedEventHandler;
				try
				{
					process.Start();
				}
				catch (Win32Exception)
				{
					// The user refused to allow privileges elevation
					// or other error happend. Do nothing and return...
				}
			}
		}

		// Restart curent app in elevated mode.
		public static void RunElevated()
		{
			if (IsElevated) throw new ApplicationException("Elevated already");
			RunElevatedAsync(Application.ExecutablePath, null);
			//Close this instance because we have an elevated instance
			Application.Exit();
		}

		#endregion

	}

}
