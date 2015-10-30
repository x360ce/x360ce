using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	/// <summary>
	/// Contains information about a file system handle.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct DEV_BROADCAST_OEM
	{
		public int dbch_size;
		public DBCH_DEVICETYPE dbch_devicetype;
		public int dbch_reserved;
		public int dbco_identifier;
		public int dbco_suppfunc;
	}
}
