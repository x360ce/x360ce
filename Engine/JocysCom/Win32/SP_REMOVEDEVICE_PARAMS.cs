using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	[StructLayout(LayoutKind.Sequential)]
	public class SP_REMOVEDEVICE_PARAMS
	{
		public SP_CLASSINSTALL_HEADER ClassInstallHeader = new SP_CLASSINSTALL_HEADER();
		public UInt32 Scope;
		public UInt32 HwProfile;
	};
}
