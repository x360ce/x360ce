namespace Microsoft.Xna.Framework.Input
{
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential)]
	internal struct XINPUT_STATE
	{
		public int PacketNumber;
		public XINPUT_GAMEPAD GamePad;
	}
}

