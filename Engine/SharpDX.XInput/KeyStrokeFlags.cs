namespace SharpDX.XInput
{
    using System;

    [Flags]
    public enum KeyStrokeFlags : short
    {
        KeyDown = 1,
        KeyUp = 2,
        None = 0,
        Repeat = 4
    }
}

