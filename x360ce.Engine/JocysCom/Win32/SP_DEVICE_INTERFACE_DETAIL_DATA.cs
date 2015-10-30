using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SP_DEVICE_INTERFACE_DETAIL_DATA
	{
		public UInt32 cbSize;
		public short DevicePath;
		public void Initialize()
		{
			this.cbSize = (UInt32)Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DETAIL_DATA));
		}
	}

}
