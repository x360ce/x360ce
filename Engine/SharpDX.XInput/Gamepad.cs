namespace SharpDX.XInput
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack=2)]
    public struct Gamepad
    {
        public const byte TriggerThreshold = 30;
        public const short LeftThumbDeadZone = 0x1ea9;
        public const short RightThumbDeadZone = 0x21f1;
        public GamepadButtonFlags Buttons;
        public byte LeftTrigger;
        public byte RightTrigger;
        public short LeftThumbX;
        public short LeftThumbY;
        public short RightThumbX;
        public short RightThumbY;
        public override string ToString()
        {
            return string.Format("Buttons: {0}, LeftTrigger: {1}, RightTrigger: {2}, LeftThumbX: {3}, LeftThumbY: {4}, RightThumbX: {5}, RightThumbY: {6}", new object[] { this.Buttons, this.LeftTrigger, this.RightTrigger, this.LeftThumbX, this.LeftThumbY, this.RightThumbX, this.RightThumbY });
        }
    }
}

