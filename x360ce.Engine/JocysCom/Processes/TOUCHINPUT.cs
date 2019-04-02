using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Processes
{
	/// <summary>Contains information about a mouse event passed to a WH_MOUSE hook procedure, MouseProc.</summary>
	[StructLayout(LayoutKind.Sequential)]
	public class TOUCHINPUT
	{
		public int x;
		public int y;
		public readonly IntPtr hSource;
		public int dwID;
		public int dwFlags;
		public int dwMask;
		public int dwTime;
		public readonly IntPtr dwExtraInfo;
		public int cxContact;
		public int cyContact;
	}
}
