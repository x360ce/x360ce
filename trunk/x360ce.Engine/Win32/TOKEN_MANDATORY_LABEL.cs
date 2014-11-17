using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace x360ce.Engine.Win32
{
	/// <summary>
	/// The structure specifies the mandatory integrity level for a token.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct TOKEN_MANDATORY_LABEL
	{
		public SID_AND_ATTRIBUTES Label;
	}
}
