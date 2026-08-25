using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	[StructLayout(LayoutKind.Sequential)]
	public struct SP_DEVINFO_DATA
	{

		public int cbSize;
		/// <summary>Class that the device belongs.</summary>
		public Guid ClassGuid;
		/// <summary>device instance handle that points to the device node for the device.</summary>
		public uint DevInst;
		public readonly IntPtr Reserved;
		public void Initialize()
		{
			cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
			DevInst = 0;
			ClassGuid = Guid.Empty;
		}
	}
}
