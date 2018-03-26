using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
    public partial class DInputHelper
    {

        void UpdateDiStates(UserGame game)
        {
            // Get all mapped user instances.
            var instanceGuids = SettingsManager.Settings.Items
                .Where(x => x.MapTo > (int)MapTo.None)
                .Select(x => x.InstanceGuid).ToArray();
            // Get all connected devices.
            var userDevices = SettingsManager.UserDevices.Items
                .Where(x => instanceGuids.Contains(x.InstanceGuid) && x.IsOnline)
                .ToArray();

            // Acquire copy of feedbacks for processing.
            var feedbacks = CopyAndClearFeedbacks();

            for (int i = 0; i < userDevices.Count(); i++)
            {
                // Update direct input form and return actions (pressed Buttons/DPads, turned Axis/Sliders).
                var ud = userDevices[i];
                JoystickState state = null;
                // Allow if not testing or testing with option enabled.
                var o = SettingsManager.Options;
                var allow = !o.TestEnabled || o.TestGetDInputStates;
                var isOnline = ud != null && ud.IsOnline;
                if (isOnline && allow)
                {
                    var device = ud.Device;
                    if (device != null)
                    {
                        try
                        {
                            var isVirtual = ((EmulationType)game.EmulationType).HasFlag(EmulationType.Virtual);
                            var hasForceFeedback = device.Capabilities.Flags.HasFlag(DeviceFlags.ForceFeedback);

                            // Exclusive mode required only if force feedback is available and device is virtual there are no info about effects.
                            var exclusiveRequired = hasForceFeedback && (isVirtual || ud.DeviceEffects == null);
                            // If exclusive mode is required and mode is not exclusive then...
                            if (exclusiveRequired && (!ud.IsExclusiveMode.HasValue || !ud.IsExclusiveMode.Value))
                            {
                                var flags = CooperativeLevel.Background | CooperativeLevel.Exclusive;
                                // Reacquire device in exclusive mode.
                                device.Unacquire();
                                device.SetCooperativeLevel(detector.DetectorForm.Handle, flags);
                                device.Acquire();
                                ud.IsExclusiveMode = true;
                            }
                            // If current mode must be non exclusive...
                            else if (!exclusiveRequired && (!ud.IsExclusiveMode.HasValue || ud.IsExclusiveMode.Value))
                            {
                                var flags = CooperativeLevel.Background | CooperativeLevel.NonExclusive;
                                // Reacquire device in non exclusive mode so that xinput.dll can control force feedback.
                                device.Unacquire();
                                device.SetCooperativeLevel(detector.DetectorForm.Handle, flags);
                                device.Acquire();
                                ud.IsExclusiveMode = false;
                            }
                            state = device.GetCurrentState();
                            // Fill device objects.
                            if (ud.DeviceObjects == null)
                                ud.DeviceObjects = AppHelper.GetDeviceObjects(device);
                            if (ud.DeviceEffects == null)
                                ud.DeviceEffects = AppHelper.GetDeviceEffects(device);
                            // Get PAD index this device is mapped to.
                            var userIndex = SettingsManager.Settings.Items
                                .Where(x => x.MapTo > (int)MapTo.None)
                                .Where(x => x.InstanceGuid == ud.InstanceGuid)
                                .Select(x => x.MapTo).First();

                            if (hasForceFeedback)
                            {
                                // Get setting related to user device.
                                var setting = SettingsManager.Settings.Items
                                    .FirstOrDefault(x => x.MapTo == userIndex && x.InstanceGuid == ud.InstanceGuid);
                                if (setting != null)
                                {
                                    // Get pad setting attached to device.
                                    var ps = SettingsManager.GetPadSetting(setting.PadSettingChecksum);
                                    if (ps != null)
                                    {
                                        // If force is enabled then...
                                        if (ps.ForceEnable == "1")
                                        {
                                            if (ud.FFState == null)
                                                ud.FFState = new Engine.ForceFeedbackState(ud);
                                            // If force update supplied then...
                                            var force = feedbacks[userIndex - 1];
                                            if (force != null || ud.FFState.Changed(ps))
                                            {
                                                var v = new Vibration();
                                                if (force == null)
                                                {
                                                    v.LeftMotorSpeed = short.MinValue;
                                                    v.RightMotorSpeed = short.MinValue;
                                                }
                                                else
                                                {
                                                    v.LeftMotorSpeed = (short)ConvertHelper.ConvertRange(byte.MinValue, byte.MaxValue, short.MinValue, short.MaxValue, force.LargeMotor);
                                                    v.RightMotorSpeed = (short)ConvertHelper.ConvertRange(byte.MinValue, byte.MaxValue, short.MinValue, short.MaxValue, force.SmallMotor);
                                                }
                                                // For teh future: Investigate device states if force feedback is not working. 
                                                // var st = ud.Device.GetForceFeedbackState();
                                                //st == SharpDX.DirectInput.ForceFeedbackState
                                                // ud.Device.SendForceFeedbackCommand(ForceFeedbackCommand.SetActuatorsOn);
                                                ud.FFState.SetDeviceForces(ud.Device, ps, v);
                                            }
                                        }
                                        // If force state was created then...
                                        else if (ud.FFState != null)
                                        {
                                            // Stop device forces.
                                            ud.FFState.StopDeviceForces(ud.Device);
                                            ud.FFState = null;
                                        }
                                    }
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            ud.IsExclusiveMode = null;
                            var error = ex;
                        }
                    }
                    // If this is test device then...
                    else if (TestDeviceHelper.ProductGuid.Equals(ud.ProductGuid))
                    {
                        // Fill device objects.
                        if (ud.DeviceObjects == null)
                            ud.DeviceObjects = TestDeviceHelper.GetDeviceObjects();
                        if (ud.DeviceEffects == null)
                            ud.DeviceEffects = new DeviceEffectItem[0];
                        state = TestDeviceHelper.GetCurrentState(ud);
                    }
                }
                ud.JoState = state ?? new JoystickState();
                var newState = new CustomDiState(ud.JoState);
                var newTime = watch.ElapsedTicks;
                // Mouse needs special update.
                if (ud.Device != null && ud.Device.Information.Type == SharpDX.DirectInput.DeviceType.Mouse)
                {
                    var mouseState = new CustomDiState(ud.JoState);
                    if (ud.OldDiState == null)
                    {
                        // Make sure new state have zero values.
                        for (int a = 0; a < newState.Axis.Length; a++)
                            mouseState.Axis[a] = -short.MinValue;
                        // Update sliders with delta.
                        for (int s = 0; s < newState.Sliders.Length; s++)
                            mouseState.Sliders[s] = -short.MinValue;
                    }
                    else
                    {
                        // This parts needs to be worked on.
                        var ticks = (int)(newTime - ud.DiStateTime);
                        // Update axis with delta.
                        for (int a = 0; a < newState.Axis.Length; a++)
                            mouseState.Axis[a] = ticks * (newState.Axis[a] - ud.OldDiState.Axis[a]) - short.MinValue;
                        // Update sliders with delta.
                        for (int s = 0; s < newState.Sliders.Length; s++)
                            mouseState.Sliders[s] = ticks * (newState.Sliders[s] - ud.OldDiState.Sliders[s]) - short.MinValue;
                    }
                    // Assign unmodified state.
                    ud.OldDiState = newState;
                    ud.OldDiStateTime = ud.DiStateTime;
                    ud.DiState = mouseState;
                }
                else
                {
                    // Update state.
                    ud.DiState = newState;
                }
                ud.DiStateTime = newTime;
            }
        }

    }
}
