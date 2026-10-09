using JocysCom.ClassLibrary.Win32;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Processes
{
	/// <summary>Contains information about a mouse event passed to a WH_MOUSE hook procedure, MouseProc.</summary>
	[StructLayout(LayoutKind.Sequential)]
	public class MSLLHOOKSTRUCT
	{
		/// <summary>The x- and y-coordinates of the cursor, in screen coordinates.</summary>
		public POINT pt;
		/// <summary>
		/// If the message is WM_MOUSEWHEEL, the high-order word of this member is the wheel delta.
		/// The low-order word is reserved. A positive value indicates that the wheel was rotated forward,
		/// away from the user; a negative value indicates that the wheel was rotated backward, toward the user.
		/// One wheel click is defined as WHEEL_DELTA, which is 120.
		/// </summary>
		public int mouseData;
		/// <summary>The event-injected flag. An application can use the following value to test the mouse flags.</summary>
		public int flags;
		/// <summary>The time stamp for this message.</summary>
		public int time;
		/// <summary>Additional information associated with the message.</summary>
		public int dwExtraInfo;
	}
}
