using System;

namespace Nefarius.ViGEm.Client.Targets.Xbox360
{
    public class Xbox360FeedbackReceivedEventArgs : EventArgs
    {
        public Xbox360FeedbackReceivedEventArgs(byte largeMotor, byte smallMotor, byte ledNumber)
        {
            LargeMotor = largeMotor;
            SmallMotor = smallMotor;
            LedNumber = ledNumber;
        }

        public byte LargeMotor { get; }

        public byte SmallMotor { get; }

        public byte LedNumber { get; }
    }
}
