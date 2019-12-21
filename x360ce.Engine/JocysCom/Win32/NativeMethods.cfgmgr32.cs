using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace JocysCom.ClassLibrary.Win32
{
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public static partial class NativeMethods
	{

		[DllImport("cfgmgr32", SetLastError = true)]
		public static extern CR CM_Open_DevNode_Key(int dnDevNode, int samDesired, int ulHardwareProfile, int Disposition, ref IntPtr phkDevice, int ulFlags);

		[DllImport("cfgmgr32", SetLastError = true)]
		public static extern CR CM_Locate_DevNode(out UInt32 dnDevInst, IntPtr pDeviceID, int ulFlags);

		[DllImport("cfgmgr32", SetLastError = true)]
		public static extern CR CM_Reenumerate_DevNode(UInt32 dnDevInst, int ulFlags);

	}
}
