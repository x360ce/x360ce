using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
	public struct SP_DRVINFO_DETAIL_DATA
	{
		public Int32 cbSize;
		public System.Runtime.InteropServices.ComTypes.FILETIME InfDate;
		public Int32 CompatIDsOffset;
		public Int32 CompatIDsLength;
		public IntPtr Reserved;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public String SectionName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public String InfFileName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public String DrvDescription;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
		public String HardwareID;
		public void Initialize()
		{
			cbSize = Marshal.SizeOf(typeof(SP_DRVINFO_DETAIL_DATA));
		}
	};
}
