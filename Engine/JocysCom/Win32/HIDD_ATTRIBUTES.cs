using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	[StructLayout(LayoutKind.Sequential)]
	public struct HIDD_ATTRIBUTES
	{
		internal int Size;
		internal ushort VendorID;
		internal ushort ProductID;
		internal ushort VersionNumber;
	}
}
