using Microsoft.Win32.SafeHandles;
using System;
using System.Security.Permissions;

namespace JocysCom.ClassLibrary.Win32
{
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	internal sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		internal SafeLibraryHandle()
			: base(true)
		{
		}

		protected override bool ReleaseHandle()
		{
			if (base.handle == IntPtr.Zero) return true;
			return NativeMethods.FreeLibrary(base.handle);
		}
	}

}
