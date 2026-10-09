namespace SharpDX.XInput
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack=2)]
    public struct Capabilities
    {
        public DeviceType Type;
        public DeviceSubType SubType;
        public CapabilityFlags Flags;
        public SharpDX.XInput.Gamepad Gamepad;
        public SharpDX.XInput.Vibration Vibration;
    }
}

