using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	[StructLayout(LayoutKind.Sequential)]
	public struct HIDD_ATTRIBUTES
	{
		public int Size;
		public ushort VendorID;
		public ushort ProductID;
		public ushort VersionNumber;
	}
}
