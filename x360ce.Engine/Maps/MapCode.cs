using System;

namespace x360ce.Engine
{
	[Flags]
	public enum MapCode
	{
		ButtonA = 1 << 0,
		ButtonB = 1 << 1,
		ButtonBack = 1 << 2,
		ButtonGuide = 1 << 3,
		ButtonShare = 1 << 4,
		ButtonStart = 1 << 5,
		ButtonX = 1 << 6,
		ButtonY = 1 << 7,
		DPad = 1 << 8,
		DPadDown = 1 << 9,
		DPadLeft = 1 << 10,
		DPadRight = 1 << 11,
		DPadUp = 1 << 12,
		LeftShoulder = 1 << 13,
		LeftThumbAxisX = 1 << 14,
		LeftThumbAxisY = 1 << 15,
		LeftThumbButton = 1 << 16,
		LeftThumbDown = 1 << 17,
		LeftThumbLeft = 1 << 18,
		LeftThumbRight = 1 << 19,
		LeftThumbUp = 1 << 20,
		LeftTrigger = 1 << 21,
		RightShoulder = 1 << 22,
		RightThumbAxisX = 1 << 23,
		RightThumbAxisY = 1 << 24,
		RightThumbButton = 1 << 25,
		RightThumbDown = 1 << 26,
		RightThumbLeft = 1 << 27,
		RightThumbRight = 1 << 28,
		RightThumbUp = 1 << 29,
		RightTrigger = 1 << 30,
	}
}
