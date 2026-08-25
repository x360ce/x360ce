
using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	public partial class NativeMethods
	{
		[DllImport("shell32.dll")]
		public static extern IntPtr SHAppBarMessage(int dwMessage, [MarshalAs(UnmanagedType.Struct)] ref APPBARDATA pData);

	}
}
