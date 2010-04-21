namespace Microsoft.Xna.Framework.Input
{
    using System;

    [Flags]
    public enum Buttons
    {
        A = 0x1000,
        B = 0x2000,
        Back = 0x20,
        BigButton = 0x800,
        DPadDown = 2,
        DPadLeft = 4,
        DPadRight = 8,
        DPadUp = 1,
        LeftShoulder = 0x100,
        LeftStick = 0x40,
        LeftThumbstickDown = 0x20000000,
        LeftThumbstickLeft = 0x200000,
        LeftThumbstickRight = 0x40000000,
        LeftThumbstickUp = 0x10000000,
        LeftTrigger = 0x800000,
        RightShoulder = 0x200,
        RightStick = 0x80,
        RightThumbstickDown = 0x2000000,
        RightThumbstickLeft = 0x8000000,
        RightThumbstickRight = 0x4000000,
        RightThumbstickUp = 0x1000000,
        RightTrigger = 0x400000,
        Start = 0x10,
        X = 0x4000,
        Y = 0x8000
    }
}

