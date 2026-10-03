namespace SharpDX.XInput
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack=2)]
    public struct Vibration
    {
        public short LeftMotorSpeed;
        public short RightMotorSpeed;
    }
}

