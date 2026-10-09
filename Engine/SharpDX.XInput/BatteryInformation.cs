namespace SharpDX.XInput
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct BatteryInformation
    {
        public SharpDX.XInput.BatteryType BatteryType;
        public SharpDX.XInput.BatteryLevel BatteryLevel;
    }
}

