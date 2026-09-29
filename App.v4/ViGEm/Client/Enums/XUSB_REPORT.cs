using System.Runtime.InteropServices;

namespace Nefarius.ViGEm.Client
{
	/// <summary>The state of an emulated Xbox 360 controller, as the bus takes it.</summary>
	/// <remarks>The same fields, in the same order and size, as an XInput gamepad state.</remarks>
	[StructLayout(LayoutKind.Sequential)]
	public struct XUSB_REPORT
	{
		/// <summary>The buttons, one bit each, on the same bits as XInput's, the guide button included.</summary>
		public ushort wButtons;
		public byte bLeftTrigger;
		public byte bRightTrigger;
		public short sThumbLX;
		public short sThumbLY;
		public short sThumbRX;
		public short sThumbRY;
	}
}
