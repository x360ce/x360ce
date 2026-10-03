namespace SharpDX.XInput
{
    using System;

    public enum BatteryType : byte
    {
        Alkaline = 2,
        Disconnected = 0,
        Nimh = 3,
        Unknown = 0xff,
        Wired = 1
    }
}

