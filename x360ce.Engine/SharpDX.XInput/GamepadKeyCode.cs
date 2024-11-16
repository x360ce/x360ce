namespace SharpDX.XInput
{
    using System;

    [Flags]
    public enum GamepadKeyCode : short
    {
		None = 0,

		A = 0x5800,
        B = 0x5801,
		X = 0x5802,
		Y = 0x5803,

		Start = 0x5814,
		Back = 0x5815,

        DPadDown = 0x5811,
        DPadLeft = 0x5812,
        DPadRight = 0x5813,
        DPadUp = 0x5810,

        LeftShoulder = 0x5805,
		RightShoulder = 0x5804,

		LeftTrigger = 0x5806,
		RightTrigger = 0x5807,

		LeftThumbPress = 0x5816,
		LeftThumbUp = 0x5820,
		LeftThumbDown = 0x5821,
		LeftThumbLeft = 0x5823,
		LeftThumbRight = 0x5822,
		// LeftThumbUpright = 0x5825,
		// LeftThumbDownright = 0x5826,

		RightThumbPress = 0x5817,
		RightThumbUp = 0x5830,
		RightThumbDown = 0x5831,
		RightThumbLeft = 0x5833,
		RightThumbRight = 0x5832
		// RightThumbUpRight = 0x5835,
		// RightThumbDownRight = 0x5836,
		// RightThumbDownleft = 0x5837,
		// RightThumbDownLeft = 0x5827,
		// RightThumbUpleft = 0x5834,
		// RightThumbUpLeft = 0x5824,
	}
}

