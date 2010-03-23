using System;

namespace x360ce.App.XnaInput
{
	[Flags]
	public enum Buttons
	{
		A = 4096,
		B = 8192,
		Back = 32,
		BigButton = 2048,
		DPadDown = 2,
		DPadLeft = 4,
		DPadRight = 8,
		DPadUp = 1,
		LeftShoulder = 256,
		LeftStick = 64,
		LeftThumbstickDown = 536870912,
		LeftThumbstickLeft = 2097152,
		LeftThumbstickRight = 1073741824,
		LeftThumbstickUp = 268435456,
		LeftTrigger = 8388608,
		RightShoulder = 512,
		RightStick = 128,
		RightThumbstickDown = 33554432,
		RightThumbstickLeft = 134217728,
		RightThumbstickRight = 67108864,
		RightThumbstickUp = 16777216,
		RightTrigger = 4194304,
		Start = 16,
		X = 16384,
		Y = 32768,
	}
}
