using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace x360ce.App.Win32
{
	/// <summary>
	/// Represents a wrapper class for a token handle.
	/// </summary>
	public class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		private SafeTokenHandle()
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
			return Win32.NativeMethods.CloseHandle(base.handle);
		}
	}

}
