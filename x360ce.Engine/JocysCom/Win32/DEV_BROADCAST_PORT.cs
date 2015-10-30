using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	/// <summary>
	/// Contains information about a class of devices.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct DEV_BROADCAST_PORT
	{
		public int dbch_size;
		public DBCH_DEVICETYPE dbch_devicetype;
		public int dbch_reserved;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
		public char[] dbcc_name;
	}
}
