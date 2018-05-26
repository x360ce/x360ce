using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{

#if WIN64
	[StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
#else
	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
#endif
	public struct SP_DEVINSTALL_PARAMS
	{
		public int cbSize;
		public int Flags;
		public int FlagsEx;
		public IntPtr hwndParent;
		public IntPtr InstallMsgHandler;
		public IntPtr InstallMsgHandlerContext;
		public IntPtr FileQueue;
		public IntPtr ClassInstallReserved;
		public UIntPtr Reserved;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string DriverPath;
		public void Initialize()
		{
			cbSize = Marshal.SizeOf(typeof(SP_DEVINSTALL_PARAMS));
		}
	}

}
