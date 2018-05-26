using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
#if WIN64
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
#else
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
#endif
	public struct SP_DEVICE_INTERFACE_DATA
	{
		public uint cbSize;
		public Guid InterfaceClassGuid;
		public uint Flags;
		public IntPtr Reserved;
		public void Initialize()
		{
			this.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));
		}
	}
}
