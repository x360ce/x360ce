using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace JocysCom.ClassLibrary.Win32
{
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public static partial class NativeMethods
	{

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern SafeFileHandle CreateFile(
			string lpFileName,
			uint dwDesiredAccess,
			uint dwShareMode,
			IntPtr lpSecurityAttributes,
			uint dwCreationDisposition,
			uint dwFlagsAndAttributes,
			IntPtr hTemplateFile
		);

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
		internal static extern bool CloseHandle(IntPtr handle);

		/// <summary>
		/// Loads the specified module into the address space of the calling process.
		/// The specified module may cause other modules to be loaded.
		/// </summary>
		[SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr LoadLibrary(string libFilename);

		/// <summary>
		/// Searches a directory for a file or subdirectory with a name that matches a specific name (or partial name if wildcards are used).
		/// </summary>
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern IntPtr FindFirstFile(string pFileName, ref WIN32_FIND_DATA pFindFileData);

		/// <summary>
		/// Closes a file search handle opened by the FindFirstFile, FindFirstFileEx, or FindFirstStreamW function.
		/// </summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool FindClose(IntPtr hndFindFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool SetSystemTime(ref SYSTEMTIME st);

		#region Helper Methods

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
		[SuppressUnmanagedCodeSecurity, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal static extern bool FreeLibrary(IntPtr hModule);

		/// <summary>
		/// Sets the current system time and date. 
		/// </summary>
		/// <param name="dt">The system time is expressed in Coordinated Universal Time(UTC).</param>
		public static bool SetSystemTime(DateTime dt)
		{
			SYSTEMTIME st = new SYSTEMTIME();
			st.wYear = (short)dt.Year;
			st.wMonth = (short)dt.Month;
			st.wDay = (short)dt.Day;
			st.wHour = (short)dt.Hour;
			st.wMinute = (short)dt.Minute;
			st.wSecond = (short)dt.Second;
			st.wMilliseconds = (short)dt.Millisecond;
			return SetSystemTime(ref st);
		}

		#endregion
	}
}
