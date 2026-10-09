using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	[StructLayout(LayoutKind.Sequential)]
	public class SP_PROPCHANGE_PARAMS
	{
		public SP_CLASSINSTALL_HEADER ClassInstallHeader = new SP_CLASSINSTALL_HEADER();
		public UInt32 StateChange;
		public UInt32 Scope;
		public UInt32 HwProfile;
	};
}
