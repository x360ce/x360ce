using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	[StructLayout(LayoutKind.Sequential)]
	public class SP_CLASSINSTALL_HEADER
	{
		public UInt32 cbSize;
		public UInt32 InstallFunction;
	}
}
