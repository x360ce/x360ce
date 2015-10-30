using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SP_DEVICE_INTERFACE_DATA
	{
		public UInt32 cbSize;
		public Guid InterfaceClassGuid;
		public UInt32 Flags;
		public IntPtr Reserved;
		public void Initialize()
		{
			this.cbSize = (UInt32)Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));
		}
	}
}
