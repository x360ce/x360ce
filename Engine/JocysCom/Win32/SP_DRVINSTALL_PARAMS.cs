using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct SP_DRVINSTALL_PARAMS
	{
		public int cbSize;
		public int Rank;
		public int Flags;
		public int PrivateData;
		public int Reserved;
		public void Initialize()
		{
			cbSize = Marshal.SizeOf(typeof(SP_DRVINSTALL_PARAMS));
		}
	}
}
