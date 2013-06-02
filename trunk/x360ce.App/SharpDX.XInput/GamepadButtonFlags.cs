namespace SharpDX.XInput
{
    using System;

    [Flags]
    public enum GamepadButtonFlags : short
    {
        A = 0x1000,
        B = 0x2000,
        Back = 0x20,
        DPadDown = 2,
        DPadLeft = 4,
        DPadRight = 8,
        DPadUp = 1,
        LeftShoulder = 0x100,
        LeftThumb = 0x40,
        None = 0,
        RightShoulder = 0x200,
        RightThumb = 0x80,
        Start = 0x10,
        X = 0x4000,
        Y = -32768
    }
}

