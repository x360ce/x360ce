using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace JocysCom.ClassLibrary.Win32
{
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public static partial class NativeMethods
	{

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern int RegCloseKey(IntPtr hkey);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern int RegQueryValueEx(IntPtr hKey, string valueName, int reserved, ref REG type, System.Text.StringBuilder data, ref int dataSize);

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

		/// <summary>
		/// The LogonUser function attempts to log a user on to the local computer.
		/// </summary>
		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

	}
}
