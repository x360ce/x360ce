using System;

namespace Nefarius.ViGEm.Client.Targets.DualShock4
{
    [Flags]
    public enum DualShock4Buttons : ushort
    {
        ThumbRight = 1 << 15,
        ThumbLeft = 1 << 14,
        Options = 1 << 13,
        Share = 1 << 12,
        TriggerRight = 1 << 11,
        TriggerLeft = 1 << 10,
        ShoulderRight = 1 << 9,
        ShoulderLeft = 1 << 8,
        Triangle = 1 << 7,
        Circle = 1 << 6,
        Cross = 1 << 5,
        Square = 1 << 4
    }

    [Flags]
    public enum DualShock4SpecialButtons : byte
    {
        Ps = 1 << 0,
        Touchpad = 1 << 1
    }

    public enum DualShock4Axes
    {
        LeftTrigger,
        RightTrigger,
        LeftThumbX,
        LeftThumbY,
        RightThumbX,
        RightThumbY
    }

    public enum DualShock4DPadValues
    {
        None = 0x8,
        Northwest = 0x7,
        West = 0x6,
        Southwest = 0x5,
        South = 0x4,
        Southeast = 0x3,
        East = 0x2,
        Northeast = 0x1,
        North = 0x0
    }

    public class DualShock4Report
    {
        public DualShock4Report()
        {
            Buttons &= unchecked((ushort)~0xF);
            Buttons |= 0x08;
        }

        public ushort Buttons { get; private set; }

        public byte SpecialButtons { get; private set; }

        public byte LeftTrigger { get; private set; }

        public byte RightTrigger { get; private set; }

        public byte LeftThumbX { get; private set; }

        public byte LeftThumbY { get; private set; }

        public byte RightThumbX { get; private set; }

        public byte RightThumbY { get; private set; }

        public void SetButtons(params DualShock4Buttons[] buttons)
        {
            foreach (var button in buttons)
            {
                Buttons |= (ushort)button;
            }
        }

        public void SetButtonState(DualShock4Buttons button, bool state)
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

        public void SetDPad(DualShock4DPadValues value)
        {
            Buttons &= unchecked((ushort)~0xF);
            Buttons |= (ushort)value;
        }

        public void SetSpecialButtons(params DualShock4SpecialButtons[] buttons)
        {
            foreach (var button in buttons)
            {
                SpecialButtons |= (byte)button;
            }
        }

        public void SetAxis(DualShock4Axes axis, byte value)
        {
            switch (axis)
            {
                case DualShock4Axes.LeftTrigger:
                    LeftTrigger = value;
                    break;
                case DualShock4Axes.RightTrigger:
                    RightTrigger = value;
                    break;
                case DualShock4Axes.LeftThumbX:
                    LeftThumbX = value;
                    break;
                case DualShock4Axes.LeftThumbY:
                    LeftThumbY = value;
                    break;
                case DualShock4Axes.RightThumbX:
                    RightThumbX = value;
                    break;
                case DualShock4Axes.RightThumbY:
                    RightThumbY = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }
    }
}
