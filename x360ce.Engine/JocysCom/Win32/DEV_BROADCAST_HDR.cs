using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	/// <summary>
	/// Serves as a standard header for information related to a device event reported through the WM_DEVICECHANGE message.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct DEV_BROADCAST_HDR
	{
		public int dbch_size;
		public DBCH_DEVICETYPE dbch_devicetype;
		public int dbch_reserved;
	}
}
