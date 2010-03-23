using System;
using System.Runtime.InteropServices;

namespace x360ce.App.XnaInput
{
	[StructLayout(LayoutKind.Sequential)]
	public struct GamePadState
	{
		public uint PacketNumber;
		public GamePad Gamepad;
	}
}

