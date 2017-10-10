namespace SharpDX.XInput
{
    using SharpDX;
    using SharpDX.Win32;
    using System;
    using System.Runtime.InteropServices;

    public class Controller
    {
        private readonly SharpDX.XInput.UserIndex userIndex;

        public Controller(SharpDX.XInput.UserIndex userIndex = UserIndex.Any)
        {
            this.userIndex = userIndex;
        }

        public BatteryInformation GetBatteryInformation(BatteryDeviceType batteryDeviceType)
        {
            BatteryInformation information;
            ErrorCodeHelper.ToResult(SharpDX.XInput.XInput.XInputGetBatteryInformation((int) this.userIndex, batteryDeviceType, out information)).CheckError();
            return information;
        }

        public Capabilities GetCapabilities(DeviceQueryType deviceQueryType)
        {
            Capabilities capabilities;
            ErrorCodeHelper.ToResult(SharpDX.XInput.XInput.XInputGetCapabilities((int) this.userIndex, deviceQueryType, out capabilities)).CheckError();
            return capabilities;
        }

        public Result GetKeystroke(DeviceQueryType deviceQueryType, out Keystroke keystroke)
        {
            Result result = ErrorCodeHelper.ToResult(SharpDX.XInput.XInput.XInputGetKeystroke((int) this.userIndex, (int) deviceQueryType, out keystroke));
            result.CheckError();
            return result;
        }

        public SharpDX.XInput.State GetState()
        {
            SharpDX.XInput.State state;
            ErrorCodeHelper.ToResult(SharpDX.XInput.XInput.XInputGetState((int) this.userIndex, out state)).CheckError();
            return state;
        }

        public bool GetState(out SharpDX.XInput.State state)
        {
            return (SharpDX.XInput.XInput.XInputGetState((int) this.userIndex, out state) == 0);
        }

        public static void SetReporting(bool enableReporting)
        {
            SharpDX.XInput.XInput.XInputEnable(enableReporting);
        }

        public Result SetVibration(Vibration vibration)
        {
            Result result = ErrorCodeHelper.ToResult(SharpDX.XInput.XInput.XInputSetState((int) this.userIndex, vibration));
            result.CheckError();
            return result;
        }

        public bool IsConnected
        {
            get
            {
                SharpDX.XInput.State state;
                return (SharpDX.XInput.XInput.XInputGetState((int) this.userIndex, out state) == 0);
            }
        }

        public Guid SoundCaptureGuid
        {
            get
            {
                Guid guid;
                Guid guid2;
                SharpDX.XInput.XInput.XInputGetDSoundAudioDeviceGuids((int) this.userIndex, out guid, out guid2);
                return guid2;
            }
        }

        public Guid SoundRenderGuid
        {
            get
            {
                Guid guid;
                Guid guid2;
                SharpDX.XInput.XInput.XInputGetDSoundAudioDeviceGuids((int) this.userIndex, out guid, out guid2);
                return guid;
            }
        }

        public SharpDX.XInput.UserIndex UserIndex
        {
            get
            {
                return this.userIndex;
            }
        }
    }
}

