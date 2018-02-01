using System.Runtime.InteropServices;

namespace Nefarius.ViGEm.Client
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct XUSB_REPORT
	{
		public ushort wButtons;
		public byte bLeftTrigger;
		public byte bRightTrigger;
		public short sThumbLX;
		public short sThumbLY;
		public short sThumbRX;
		public short sThumbRY;
	}
}
