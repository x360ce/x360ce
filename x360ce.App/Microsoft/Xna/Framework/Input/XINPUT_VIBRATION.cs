namespace Microsoft.Xna.Framework.Input
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct XINPUT_VIBRATION
    {
        public short LeftMotorSpeed;
        public short RightMotorSpeed;
    }
}

