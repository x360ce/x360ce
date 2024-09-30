﻿namespace SharpDX.XInput
{
    using System;
    using System.ComponentModel;

    [Flags]
    public enum GamepadButtonFlags : ushort // short
    {
        [Description("")]
        A = 0x1000,
        B = 0x2000,
        X = 0x4000,
        Y = 0x8000, // -32768 (short)
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
        Guide = 0x400
    }
}

