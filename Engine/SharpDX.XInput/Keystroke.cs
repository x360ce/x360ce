namespace SharpDX.XInput
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack=2)]
    public struct Keystroke
    {
        public GamepadKeyCode VirtualKey;
        public char Unicode;
        public KeyStrokeFlags Flags;
        public SharpDX.XInput.UserIndex UserIndex;
        public byte HidCode;
    }
}

