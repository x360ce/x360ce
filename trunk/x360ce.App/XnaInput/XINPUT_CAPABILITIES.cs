using System.Runtime.InteropServices;
namespace x360ce.App.XnaInput
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct XINPUT_CAPABILITIES
    {
        public byte Type;
        public byte SubType;
        public ushort Flags;
        public XINPUT_GAMEPAD GamePad;
        public XINPUT_VIBRATION Vibration;
    }

}
