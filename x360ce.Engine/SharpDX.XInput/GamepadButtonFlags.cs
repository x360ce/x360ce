namespace SharpDX.XInput
{
    using System;
    using System.ComponentModel;

    [Flags]
    public enum GamepadButtonFlags : ushort // short
    {
        [Description("")]
		None = 0,

		A = 0x1000,
        B = 0x2000,
        X = 0x4000,
        Y = 0x8000, // -32768 (short)

		Start = 0x10,
		Guide = 0x400,
		Back = 0x20,

		DPadUp = 1,
		DPadDown = 2,
        DPadLeft = 4,
        DPadRight = 8,

        LeftShoulder = 0x100,
		RightShoulder = 0x200,

		LeftThumb = 0x40,
        RightThumb = 0x80
    }
}

