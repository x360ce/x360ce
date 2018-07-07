using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

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
