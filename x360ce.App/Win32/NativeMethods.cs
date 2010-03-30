using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Security;
using System;
using System.Runtime.ConstrainedExecution;

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
		public static extern Boolean IsUserAnAdmin();


		//[DllImport("coredll")]
		//private static extern bool GetFileVersionInfo(string filename, UInt32 handle, UInt32 len, IntPtr buffer);

		//[DllImport("coredll")]
		//private static extern UInt32 GetFileVersionInfoSize(string filename, out UInt32 handle);

		//[DllImport("coredll")]
		//private static extern bool VerQueryValue(IntPtr buffer, string subblock, out IntPtr blockbuffer, out int len);

		#endregion

		#region ole32

		[DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.Interface)]
		public static extern object CoGetObject(
		   string pszName,
		   [In] ref BIND_OPTS3 pBindOptions,
		   [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid);

		#endregion

		#region user32

		/// <summary>
		/// API used to send a message to another window
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		#endregion

		#region advapi32

		/// <summary>
		/// Opens the access token associated with a process.
		/// </summary>
		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern Boolean OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

		/// <summary>
		/// The function opens the access token associated with a process.
		/// </summary>
		[DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool OpenProcessToken(IntPtr hProcess, UInt32 desiredAccess, out SafeTokenHandle hToken);

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
			out UInt32 ReturnLength);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetTokenInformation(
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
		public static extern IntPtr GetSidSubAuthority(IntPtr pSid, UInt32 nSubAuthority);

		#endregion

		#region kernel32

		/// <summary>
		/// Retrieves a pseudo handle for the current process.
		/// </summary>
		[SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr GetCurrentProcess();

		/// <summary>
		/// Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
		/// </summary>
		[SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr GetProcAddress(IntPtr hModule, [In, MarshalAs(UnmanagedType.LPStr)] string lpProcName);

		/// <summary>
		/// Closes an open object handle.
		/// </summary>
		[return: MarshalAs(UnmanagedType.Bool)]
		[SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool CloseHandle(IntPtr handle);

		/// <summary>
		/// Loads the specified module into the address space of the calling process.
		/// The specified module may cause other modules to be loaded.
		/// </summary>
		[SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr LoadLibrary(string libFilename);

		/// <summary>
		/// Loads the specified module into the address space of the calling process.
		/// The specified module may cause other modules to be loaded.
		/// </summary>
		[SuppressUnmanagedCodeSecurity, SecurityCritical, DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern SafeLibraryHandle LoadLibraryEx(string libFilename, IntPtr reserved, int flags);

		/// <summary>
		/// Frees the loaded dynamic-link library (DLL) module and, if necessary, decrements its reference count.
		/// </summary>
		[return: MarshalAs(UnmanagedType.Bool)]
		[SuppressUnmanagedCodeSecurity, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		public static extern bool FreeLibrary(IntPtr hModule);

		#endregion

	}

}
