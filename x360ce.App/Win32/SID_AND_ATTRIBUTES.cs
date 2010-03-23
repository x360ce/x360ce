using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace x360ce.App.Win32
{
	/// <summary>
	/// The structure represents a security identifier (SID) and its 
	/// attributes. SIDs are used to uniquely identify users or groups.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct SID_AND_ATTRIBUTES
	{
		public IntPtr Sid;
		public Int32 Attributes;
	}
}
