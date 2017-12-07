using System;
using Microsoft.Win32.SafeHandles;

namespace JocysCom.ClassLibrary.Win32
{
	/// <summary>
	/// Represents a wrapper class for a token handle.
	/// </summary>
	public class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		SafeTokenHandle()
			: base(true)
		{
		}

		internal SafeTokenHandle(IntPtr handle)
			: base(true)
		{
			base.SetHandle(handle);
		}

		protected override bool ReleaseHandle()
		{
			return NativeMethods.CloseHandle(base.handle);
		}
	}

}
