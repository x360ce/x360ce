using System;

namespace x360ce.Engine
{
	[Flags]
	public enum LayoutCode
	{
		ButtonA = 1 << 0,
		ButtonB = 1 << 1,
		ButtonBack = 1 << 2,
		ButtonGuide = 1 << 3,
		ButtonStart = 1 << 4,
		ButtonX = 1 << 5,
		ButtonY = 1 << 6,
		DPad = 1 << 7,
		DPadDown = 1 << 8,
		DPadLeft = 1 << 9,
		DPadRight = 1 << 10,
		DPadUp = 1 << 11,
		LeftShoulder = 1 << 12,
		LeftThumbAxisX = 1 << 13,
		LeftThumbAxisY = 1 << 14,
		LeftThumbButton = 1 << 15,
		LeftThumbDown = 1 << 16,
		LeftThumbLeft = 1 << 17,
		LeftThumbRight = 1 << 18,
		LeftThumbUp = 1 << 19,
		LeftTrigger = 1 << 20,
		RightShoulder = 1 << 21,
		RightThumbAxisX = 1 << 22,
		RightThumbAxisY = 1 << 23,
		RightThumbButton = 1 << 24,
		RightThumbDown = 1 << 25,
		RightThumbLeft = 1 << 26,
		RightThumbRight = 1 << 27,
		RightThumbUp = 1 << 28,
		RightTrigger = 1 << 29,
	}
}
