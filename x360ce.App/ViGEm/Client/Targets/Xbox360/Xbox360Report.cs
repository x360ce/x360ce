using System;

namespace Nefarius.ViGEm.Client.Targets.Xbox360
{
    [Flags]
    public enum Xbox360Buttons : ushort
    {
        Up = 0x0001,
        Down = 0x0002,
        Left = 0x0004,
        Right = 0x0008,
        Start = 0x0010,
        Back = 0x0020,
        LeftThumb = 0x0040,
        RightThumb = 0x0080,
        LeftShoulder = 0x0100,
        RightShoulder = 0x0200,
        Guide = 0x0400,
        A = 0x1000,
        B = 0x2000,
        X = 0x4000,
        Y = 0x8000
    }

    public enum Xbox360Axes
    {
        LeftTrigger,
        RightTrigger,
        LeftThumbX,
        LeftThumbY,
        RightThumbX,
        RightThumbY
    }

    public class Xbox360Report
    {
        public ushort Buttons { get; private set; }

        public byte LeftTrigger { get; private set; }

        public byte RightTrigger { get; private set; }

        public short LeftThumbX { get; private set; }

        public short LeftThumbY { get; private set; }

        public short RightThumbX { get; private set; }

        public short RightThumbY { get; private set; }

        public void SetButtons(params Xbox360Buttons[] buttons)
        {
            foreach (var button in buttons)
            {
                Buttons |= (ushort)button;
            }
        }

        public void SetButtonState(Xbox360Buttons button, bool state)
        {
            if (state)
            {
                Buttons |= (ushort)button;
            }
            else
            {
                Buttons &= (ushort)~button;
            }
        }

        public void SetAxis(Xbox360Axes axis, short value)
        {
            switch (axis)
            {
                case Xbox360Axes.LeftTrigger:
                    LeftTrigger = (byte)value;
                    break;
                case Xbox360Axes.RightTrigger:
                    RightTrigger = (byte) value;
                    break;
                case Xbox360Axes.LeftThumbX:
                    LeftThumbX = value;
                    break;
                case Xbox360Axes.LeftThumbY:
                    LeftThumbY = value;
                    break;
                case Xbox360Axes.RightThumbX:
                    RightThumbX = value;
                    break;
                case Xbox360Axes.RightThumbY:
                    RightThumbY = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }
    }
}
