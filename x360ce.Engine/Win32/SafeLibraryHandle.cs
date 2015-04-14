using System.Security;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace x360ce.Engine.Win32
{
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		internal SafeLibraryHandle()
			: base(true)
		{
		}

		protected override bool ReleaseHandle()
		{
			return NativeMethods.FreeLibrary(base.handle);
		}
	}

 

}
