using System;
using System.Runtime.InteropServices;

namespace x360ce.App.XnaInput
{
	[StructLayout(LayoutKind.Sequential)]
	public struct XINPUT_STATE
	{
		public int PacketNumber;
		public XINPUT_GAMEPAD GamePad;
	}
}

