using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
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

		public static IntPtr GetProcAddress(IntPtr hModule, string lpProcName, out Exception error)
		{
			var address = GetProcAddress(hModule, lpProcName);
			error = (address == IntPtr.Zero) ? new Exception(new Win32Exception().ToString()) : null;
			return address;
		}

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
		internal static extern IntPtr LoadLibrary(string libFilename);

		public static IntPtr LoadLibrary(string lpFileName, out Exception error)
		{
			var address = LoadLibrary(lpFileName);
			error = (address == IntPtr.Zero)
				? new Exception(new Win32Exception().ToString())
				: null;
			return address;
		}

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

		public static bool FreeLibrary(IntPtr lpFileName, out Exception error)
		{
			var success = FreeLibrary(lpFileName);
			error = (success)
				? null
				: new Exception(new Win32Exception().ToString());
			return success;
		}


		/// <summary>
		/// Adds a directory to the search path used to locate DLLs for the application.
		/// </summary>
		/// <param name="lpPathName">
		/// The directory to be added to the search path.
		/// If this parameter is an empty string (""), the call removes the current directory from the default DLL search order.
		/// If this parameter is NULL, the function restores the default search order.</param>
		/// <returns></returns>
		/// <remarks>
		/// After calling SetDllDirectory, the standard DLL search path is:
		/// 1. The directory from which the application loaded.
		/// 2. The directory specified by the lpPathName parameter.
		/// 3. The system directory.Use the GetSystemDirectory function to get the path of this directory.The name of this directory is System32.
		/// 4. The 16-bit system directory.There is no function that obtains the path of this directory, but it is searched.The name of this directory is System.
		/// 5. The Windows directory.Use the GetWindowsDirectory function to get the path of this directory.
		/// 6. The directories that are listed in the PATH environment variable.
		/// </remarks>
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool SetDllDirectory(string lpPathName);

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
		public static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, [Out] char[] lpReturnedString, int nSize, string lpFileName);

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
		public static extern int WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);

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

	}
}
