using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SP_DEVINFO_DATA
	{
		public UInt32 cbSize;
		public Guid ClassGuid;
		public UInt32 DevInst;
		public IntPtr Reserved;
		public void Initialize()
		{
			this.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
		}
	}
}
