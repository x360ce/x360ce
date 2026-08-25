using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	[StructLayout(LayoutKind.Sequential)]
	public struct APPBARDATA
	{
		public uint cbSize;
		internal IntPtr hWnd;
		public uint uCallbackMessage;
		public ABE uEdge;
		public RECT rc;
		public int lParam;

		public void Initialize()
		{
			// Get position and bounds.
			cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));
		}

	}
}
