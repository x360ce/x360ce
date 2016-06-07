using System.Runtime.InteropServices;
using System;
using System.Drawing;

namespace JocysCom.ClassLibrary.Processes
{
	[StructLayout(LayoutKind.Sequential)]
	public struct CURSORINFO
	{
		public int Size;
		public int Flags;
		public IntPtr Handle;
		public Point Position;
	}
}
