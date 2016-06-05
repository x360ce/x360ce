using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public class SP_DEVICE_INTERFACE_DETAIL_DATA
	{
		public UInt32 cbSize;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2048)] // const int BUFFER_SIZE = 2048;
		public string DevicePath;
		public void Initialize()
		{
			this.cbSize = (UInt32)Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DETAIL_DATA));
		}
	}

}
