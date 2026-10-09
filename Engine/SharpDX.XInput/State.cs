namespace SharpDX.XInput
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct State
    {
        public int PacketNumber;
        public SharpDX.XInput.Gamepad Gamepad;
    }
}

