using JocysCom.ClassLibrary.Win32;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Processes
{
	/// <summary>Contains information about a mouse event passed to a WH_MOUSE hook procedure, MouseProc.</summary>
	[StructLayout(LayoutKind.Sequential)]
	public class MouseHookStruct
	{
		/// <summary>The x- and y-coordinates of the cursor, in screen coordinates.</summary>
		public POINT pt;
		/// <summary>A handle to the window that will receive the mouse message corresponding to the mouse event.</summary>
		public int hwnd;
		/// <summary>The hit-test value. For a list of hit-test values, see the description of the</summary>
		public int wHitTestCode;
		/// <summary>Additional information associated with the message.</summary>
		public int dwExtraInfo;
	}
}
