using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Security;
using System;
using System.Runtime.ConstrainedExecution;
using System.Text;

namespace x360ce.App.Win32
{
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public static class NativeMethods
	{

		// Conversion:
		// Handle - IntPtr
		//  DWORD - UInt32
		// PDWORD - UInt32
		// LPVOID - IntPtr

		#region shell32

		[DllImport("shell32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern Boolean IsUserAnAdmin();

		#endregion

		#region ole32

		[DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.Interface)]
		internal static extern object CoGetObject(
		   string pszName,
		   [In] ref BIND_OPTS3 pBindOptions,
		   [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid);

		#endregion

		#region user32

		/// <summary>
		/// Sends the specified message to a window or windows.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		/// <summary>
		/// Sends the specified message to a window or windows.
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="Msg"></param>
		/// <param name="wParam"></param>
		/// <param name="lParam"></param>
		/// <returns></returns>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		internal static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

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

		#endregion

		#region advapi32

		/// <summary>
		/// Opens the access token associated with a process.
		/// </summary>
		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern Boolean OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

		/// <summary>
		/// The function opens the access token associated with a process.
		/// </summary>
		[DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool OpenProcessToken(IntPtr hProcess, UInt32 desiredAccess, out SafeTokenHandle hToken);

		/// <summary>
		/// Retrieves a specified type of information about an access token.
		/// </summary>
		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetTokenInformation(
			IntPtr TokenHandle,
			TOKEN_INFORMATION_CLASS TokenInformationClass,
			IntPtr TokenInformation,
			UInt32 TokenInformationLength,
			out UInt32 ReturnLength);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetTokenInformation(
			SafeTokenHandle hToken,
			TOKEN_INFORMATION_CLASS tokenInfoClass,
			IntPtr pTokenInfo,
			Int32 tokenInfoLength,
			out Int32 returnLength);

		/// <summary>
		/// The function returns a pointer to a specified subauthority in a 
		/// security identifier (SID). The subauthority value is a relative 
		/// identifier (RID).
		/// </summary>
		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern IntPtr GetSidSubAuthority(IntPtr pSid, UInt32 nSubAuthority);

		#endregion

		#region kernel32

		/// <summary>
		/// Retrieves a pseudo handle for the current process.
		/// </summary>
		[SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern IntPtr GetCurrentProcess();

		/// <summary>
		/// Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
		/// </summary>
		[SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
		internal static extern IntPtr GetProcAddress(IntPtr hModule, [In, MarshalAs(UnmanagedType.LPStr)] string lpProcName);

		/// <summary>
		/// Closes an open object handle.
		/// </summary>
		[return: MarshalAs(UnmanagedType.Bool)]
		[SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool CloseHandle(IntPtr handle);

		/// <summary>
		/// Loads the specified module into the address space of the calling process.
		/// The specified module may cause other modules to be loaded.
		/// </summary>
		[SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", EntryPoint = "LoadLibraryA", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping=false, ThrowOnUnmappableChar=true)]
		internal static extern IntPtr LoadLibrary([In, MarshalAs(UnmanagedType.LPStr)] string lpFileName);

		/// <summary>
		/// Loads the specified module into the address space of the calling process.
		/// The specified module may cause other modules to be loaded.
		/// </summary>
		[SuppressUnmanagedCodeSecurity, SecurityCritical, DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern SafeLibraryHandle LoadLibraryEx(string libFilename, IntPtr reserved, int flags);

		/// <summary>
		/// Frees the loaded dynamic-link library (DLL) module and, if necessary, decrements its reference count.
		/// </summary>
		[return: MarshalAs(UnmanagedType.Bool)]
		[SuppressUnmanagedCodeSecurity, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success),
		DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal static extern bool FreeLibrary(IntPtr hModule);


		/// <summary>
		/// Retrieves a string from the specified section in an initialization file. http://msdn2.microsoft.com/en-us/library/ms724353.aspx
		/// </summary>
		/// <param name="lpAppName">The name of the section containing the key name. If this parameter is NULL, the GetPrivateProfileString function copies all section names in the file to the supplied buffer.</param>
		/// <param name="lpKeyName">The name of the key whose associated string is to be retrieved. If this parameter is NULL, all key names in the section specified by the lpAppName parameter are copied to the buffer specified by the lpReturnedString parameter.</param>
		/// <param name="lpDefault">A default string. If the lpKeyName key cannot be found in the initialization file, GetPrivateProfileString copies the default string to the lpReturnedString buffer. If this parameter is NULL, the default is an empty string, "". Avoid specifying a default string with trailing blank characters. The function inserts a null character in the lpReturnedString buffer to strip any trailing blanks.</param>
		/// <param name="lpReturnedString">[out] A pointer to the buffer that receives the retrieved string.</param>
		/// <param name="nSize">The size of the buffer pointed to by the lpReturnedString parameter, in characters.</param>
		/// <param name="lpFileName">The name of the initialization file. If this parameter does not contain a full path to the file, the system searches for the file in the Windows directory.</param>
		/// <returns>The return value is the number of characters copied to the buffer, not including the terminating null character.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"),
		DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);

		/// <summary>
		/// Copies a string into the specified section of an initialization file. http://msdn2.microsoft.com/en-us/library/ms725501.aspx
		/// </summary>
		/// <param name="lpAppName">The name of the section to which the string will be copied. If the section does not exist, it is created. The name of the section is case-independent; the string can be any combination of uppercase and lowercase letters.</param>
		/// <param name="lpKeyName">The name of the key to be associated with a string. If the key does not exist in the specified section, it is created. If this parameter is NULL, the entire section, including all entries within the section, is deleted.</param>
		/// <param name="lpString">A null-terminated string to be written to the file. If this parameter is NULL, the key pointed to by the lpKeyName parameter is deleted.</param>
		/// <param name="lpFileName">The name of the initialization file. If the file was created using Unicode characters, the function writes Unicode characters to the file. Otherwise, the function writes ANSI characters.</param>
		/// <returns>If the function successfully copies the string to the initialization file, the return value is nonzero. If the function fails, or if it flushes the cached version of the most recently accessed initialization file, the return value is zero.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "2#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"),
		DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern int WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);




		#endregion

	}

}
