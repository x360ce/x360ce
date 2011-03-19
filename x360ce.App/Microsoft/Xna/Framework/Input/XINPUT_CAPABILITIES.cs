namespace Microsoft.Xna.Framework.Input
{
	using System;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential)]
	internal struct XINPUT_CAPABILITIES
	{
		public byte Type;
		public byte SubType;
		public ushort Flags;
		public XINPUT_GAMEPAD GamePad;
		public XINPUT_VIBRATION Vibration;
	}
}

