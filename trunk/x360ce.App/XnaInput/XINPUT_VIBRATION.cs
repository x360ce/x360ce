using System;
using System.Runtime.InteropServices;

namespace x360ce.App.XnaInput
{

    [StructLayout(LayoutKind.Sequential)]
    internal struct XINPUT_VIBRATION
    {
        public short LeftMotorSpeed;
        public short RightMotorSpeed;
    }

}

