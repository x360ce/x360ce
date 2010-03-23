using System;
using System.Runtime.InteropServices;

namespace x360ce.App.XnaInput
{


	[StructLayout(LayoutKind.Sequential)]
	public struct GamePad
	{
		public ButtonValues Buttons;
		public byte LeftTrigger;
		public byte RightTrigger;
		public short ThumbLeftX;
		public short ThumbLeftY;
		public short ThumbRightX;
		public short ThumbRightY;
	}
}

