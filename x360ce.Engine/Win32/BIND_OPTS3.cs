
using System.Runtime.InteropServices;
using System;

namespace x360ce.Engine.Win32
{
	[StructLayout(LayoutKind.Sequential)]
	public struct BIND_OPTS3
	{
		internal uint cbStruct;
		internal uint grfFlags;
		internal uint grfMode;
		internal uint dwTickCountDeadline;
		internal uint dwTrackFlags;
		internal uint dwClassContext;
		internal uint locale;
		object pServerInfo; // will be passing null, so type doesn't matter
		internal IntPtr hwnd;
	}

}
