using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	/// <summary>
	/// Contains information about a file system handle.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct DEV_BROADCAST_HANDLE
	{
		public int dbch_size;
		public DBCH_DEVICETYPE dbch_devicetype;
		public int dbch_reserved;
		public readonly IntPtr dbch_handle;
		public readonly IntPtr dbch_hdevnotify;
		public Guid dbch_eventguid;
		public long dbch_nameoffset;
		public byte dbch_data;
		public byte dbch_data1;
	}
}
