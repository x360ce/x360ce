using System;

namespace x360ce.Engine
{
	[Flags]
	public enum LayoutCode
	{
		None = 0,
		ButtonA = 2 ^ 0,
		ButtonB = 2 ^ 1,
		ButtonBack = 2 ^ 2,
		ButtonGuide = 2 ^ 3,
		ButtonStart = 2 ^ 4,
		ButtonX = 2 ^ 5,
		ButtonY = 2 ^ 6,
		DPad = 2 ^ 7,
		DPadDown = 2 ^ 8,
		DPadLeft = 2 ^ 9,
		DPadRight = 2 ^ 10,
		DPadUp = 2 ^ 11,
		LeftShoulder = 2 ^ 12,
		LeftThumbAxisX = 2 ^ 13,
		LeftThumbAxisY = 2 ^ 14,
		LeftThumbButton = 2 ^ 15,
		LeftThumbDown = 2 ^ 16,
		LeftThumbLeft = 2 ^ 17,
		LeftThumbRight = 2 ^ 18,
		LeftThumbUp = 2 ^ 19,
		LeftTrigger = 2 ^ 20,
		RightShoulder = 2 ^ 21,
		RightThumbAxisX = 2 ^ 22,
		RightThumbAxisY = 2 ^ 23,
		RightThumbButton = 2 ^ 24,
		RightThumbDown = 2 ^ 25,
		RightThumbLeft = 2 ^ 26,
		RightThumbRight = 2 ^ 27,
		RightThumbUp = 2 ^ 28,
		RightTrigger = 2 ^ 29,
	}
}
