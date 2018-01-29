namespace Nefarius.ViGEm.Client.Targets.DualShock4
{
    public class LightbarColor
    {
        public LightbarColor(byte red, byte green, byte blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        public byte Red { get; }
        public byte Green { get; }
        public byte Blue { get; }
    }
}
