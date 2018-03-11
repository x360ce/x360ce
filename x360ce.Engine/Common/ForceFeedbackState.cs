using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Linq;
using x360ce.Engine.Data;

namespace x360ce.Engine
{

    /// <summary>
    /// Class used to store current force feedback state of device.
    /// </summary>
    public class ForceFeedbackState
    {
        public ForceFeedbackState(UserDevice ud)
        {
            LeftPeriodicForce = new PeriodicForce();
            RightPeriodicForce = new PeriodicForce();
            LeftConstantForce = new ConstantForce();
            RightConstantForce = new ConstantForce();
            GUID_Force = EffectGuid.ConstantForce;
            // Find and assign actuators.
            var actuators = ud.DeviceObjects.Where(x => x.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator)).ToList();
            // If actuator available then...
            if (actuators.Count > 0)
            {
                // Try to find left actuator.
                var actuator = actuators.FirstOrDefault(x => x.Type == ObjectGuid.XAxis);
                //var actuator = actuators[0];
                // If default actuator not found then take default.
                if (actuator == null)
                    actuator = actuators[0];
                actuators.Remove(actuator);
                LeftParameters = GetParameters(actuator.Offset);
            }
            // If actuator available then...
            if (actuators.Count > 0)
            {
                // Try to find right actuator.
                var actuator = actuators.FirstOrDefault(x => x.Type == ObjectGuid.YAxis);
                //var actuator = actuators[0];
                // If default actuator not found then take default.
                if (actuator == null)
                    actuator = actuators[0];
                actuators.Remove(actuator);
                RightParameters = GetParameters(actuator.Offset);
            }
        }

        const uint INFINITE = 0xFFFFFFFF;
        const uint DIEB_NOTRIGGER = 0xFFFFFFFF;

        EffectParameters GetParameters(int offset)
        {
            var p = new EffectParameters();
            // Right-handed Cartesian direction:
            // x: -1 = left,     1 = right,   0 - no direction
            // y: -1 = backward, 1 = forward, 0 - no direction
            // z: -1 = down,     1 = up,      0 - no direction
            // Left motor.
            p.SetAxes(new int[1] { offset }, new int[1] { 1 });
            p.Flags = EffectFlags.Cartesian | EffectFlags.ObjectOffsets;
            p.StartDelay = 0;
            p.Duration = unchecked((int)INFINITE);
            p.SamplePeriod = 0;
            p.TriggerButton = unchecked((int)DIEB_NOTRIGGER);
            p.TriggerRepeatInterval = unchecked((int)INFINITE);
            return p;
        }

        // Left

        EffectParameters LeftParameters;
        public PeriodicForce LeftPeriodicForce;
        public ConstantForce LeftConstantForce;

        // Right

        EffectParameters RightParameters;
        public PeriodicForce RightPeriodicForce;
        public ConstantForce RightConstantForce;

        #region Force Feedback

        const int DI_FFNOMINALMAX = 10000;

        // Force type changed by settings.
        string old_ForceType = "-1";
        // Force parameters changed by settings.
        string old_LeftPeriod;
        string old_RightPeriod;
        string old_LeftStrength;
        string old_RightStrength;
        string old_LeftDirection;
        string old_RightDirection;
        string old_OveralStrength;
        // Speed sent by the game.
        short old_LeftMotorSpeed;
        short old_RightMotorSpeed;

        Guid GUID_Force;

        public bool SetDeviceForces(Joystick device, PadSetting ps, Vibration v)
        {
            // Return if force feedback actuators not found.
            if (LeftParameters == null)
                return false;

            bool forceChanged =
                old_ForceType != ps.ForceType;

            bool paramsChanged =
                // Left motor parameters.
                old_LeftPeriod != ps.LeftMotorPeriod ||
                old_LeftDirection != ps.LeftMotorDirection ||
                old_LeftStrength != ps.LeftMotorStrength ||
                // Right motor parameters.
                old_RightPeriod != ps.RightMotorPeriod ||
                old_RightDirection != ps.RightMotorDirection ||
                old_RightStrength != ps.RightMotorStrength ||
                // Shared motor parameters.
                old_OveralStrength != ps.ForceOverall;

            bool speedChanged =
                old_LeftMotorSpeed != v.LeftMotorSpeed ||
                old_RightMotorSpeed != v.RightMotorSpeed;

            // If nothing changed then return.
            if (!forceChanged && !paramsChanged && !speedChanged)
                return false;

            if (forceChanged)
            {
                // Update values.
                old_ForceType = ps.ForceType;
                var forceType = (ForceFeedBackType)TryParse(ps.ForceType);
                // Forces for vibrating motors (Game pads).
                // 0 - Constant. Good for vibrating motors.
                // Forces for torque motors (Wheels).
                // 1 - Periodic 'Sine Wave'. Good for car/plane engine vibration.
                // 2 - Periodic 'Sawtooth Down Wave'. Good for gun recoil.
                switch (forceType)
                {
                    case ForceFeedBackType.PeriodicSine: GUID_Force = EffectGuid.Sine; break;
                    case ForceFeedBackType.PeriodicSawtooth: GUID_Force = EffectGuid.SawtoothDown; break;
                    default: GUID_Force = EffectGuid.ConstantForce; break;
                }
            }

            // Tells which effect paramaters to modify.
            var flagsL = EffectParameterFlags.None;
            var flagsR = EffectParameterFlags.None;

            if (paramsChanged)
            {
                // Update values.
                old_LeftPeriod = ps.LeftMotorPeriod;
                old_LeftDirection = ps.LeftMotorDirection;
                old_LeftStrength = ps.LeftMotorStrength;
                old_RightPeriod = ps.RightMotorPeriod;
                old_RightDirection = ps.RightMotorDirection;
                old_RightStrength = ps.RightMotorStrength;
                old_OveralStrength = ps.ForceOverall;

                old_LeftMotorSpeed = v.LeftMotorSpeed;
                old_RightMotorSpeed = v.RightMotorSpeed;

                // Right-handed Cartesian direction:
                // x: -1 = left,     1 = right,   0 - no direction
                // y: -1 = backward, 1 = forward, 0 - no direction
                // z: -1 = down,     1 = up,      0 - no direction
                int overalStrength = ConvertHelper.ConvertRange(0, 100, 0, DI_FFNOMINALMAX, ps.GetForceOverall());
                int leftGain = ConvertHelper.ConvertRange(0, 100, 0, overalStrength, ps.GetLeftMotorStrength());
                int rightGain = ConvertHelper.ConvertRange(0, 100, 0, overalStrength, ps.GetRightMotorStrength());

                LeftParameters.Gain = leftGain;
                flagsL |= EffectParameterFlags.Gain;
                //int leftDirection = TryParse(ps.LeftMotorDirection);
                //LeftParameters.Directions = new int[1] { 1 }; // leftDirection;
                if (RightParameters != null)
                {
                    RightParameters.Gain = rightGain;
                    flagsR |= EffectParameterFlags.Gain;
                    //int rightDirection = TryParse(ps.RightMotorDirection);
                    //RightParameters.Directions = new int[1] { 1 }; // // rightDirection
                    // Update flags to indicate that gain changed.
                }
            }

            // Vibration speed changed will affect force magnitude and period when combined.
            if (speedChanged)
            {
                // Convert speed into magnitude/amplitude.
                var leftMagnitudeAdjusted = ConvertHelper.ConvertRange(short.MinValue, short.MaxValue, 0, DI_FFNOMINALMAX, v.LeftMotorSpeed);
                var rightMagnitudeAdjusted = ConvertHelper.ConvertRange(short.MinValue, short.MaxValue, 0, DI_FFNOMINALMAX, v.RightMotorSpeed);

                int leftPeriod = TryParse(ps.LeftMotorPeriod) * 1000;
                int rightPeriod = TryParse(ps.RightMotorPeriod) * 1000;

                // If device have only one force feedback actuator (probably wheel).
                if (RightParameters == null)
                {
                    // Forces must be combined.
                    var combinedMagnitudeAdjusted = Math.Max(leftMagnitudeAdjusted, rightMagnitudeAdjusted);
                    var combinedPeriod = 0;
                    // If at least one speed is specified then...
                    if (leftMagnitudeAdjusted > 0 || rightMagnitudeAdjusted > 0)
                    {
                        // Get combined period depending on magnitudes.
                        combinedPeriod =
                            ((leftPeriod * leftMagnitudeAdjusted) + (rightPeriod * rightMagnitudeAdjusted))
                            / (leftMagnitudeAdjusted + rightMagnitudeAdjusted);
                    }
                    // Update force properties.
                    leftMagnitudeAdjusted = combinedMagnitudeAdjusted;
                    leftPeriod = combinedPeriod;
                }

                // Update force values.
                if (GUID_Force == EffectGuid.ConstantForce)
                {
                    LeftConstantForce.Magnitude = leftMagnitudeAdjusted;
                    RightConstantForce.Magnitude = rightMagnitudeAdjusted;
                }
                else
                {
                    LeftPeriodicForce.Magnitude = leftMagnitudeAdjusted;
                    LeftPeriodicForce.Period = leftPeriod;
                    RightPeriodicForce.Magnitude = rightMagnitudeAdjusted;
                    RightPeriodicForce.Period = rightPeriod;
                }
                // Update flags to indicate that force properties changed.
                flagsL |= EffectParameterFlags.TypeSpecificParameters;
                flagsR |= EffectParameterFlags.TypeSpecificParameters;
            }

            Effect effectL = null;
            Effect effectR = null;

            // If device already have effects then...
            if (device.CreatedEffects.Count > 0)
                effectL = device.CreatedEffects[0];
            if (device.CreatedEffects.Count > 1)
                effectR = device.CreatedEffects[1];

            // Recreate effects if force changed.
            if (forceChanged)
            {
                // Stop old effects.
                if (effectL != null)
                {
                    effectL.Stop();
                    effectL.Dispose();
                }
                // Stop old effects.
                if (effectR != null)
                {
                    effectR.Stop();
                    effectR.Dispose();
                }
                // Update Left force
                LeftParameters.Parameters = GUID_Force == EffectGuid.ConstantForce
                    ? LeftConstantForce as TypeSpecificParameters : LeftPeriodicForce;
                // Note: Device must be acquired in exclusive mode before effect can be created.
                effectL = new Effect(device, GUID_Force, LeftParameters);
                if (RightParameters != null)
                {
                    // Update Right force
                    RightParameters.Parameters = GUID_Force == EffectGuid.ConstantForce
                        ? RightConstantForce as TypeSpecificParameters : RightPeriodicForce;
                    effectR = new Effect(device, GUID_Force, RightParameters);
                }
            }

            // Do not restart playing effect.
            flagsL |= effectL.Status == EffectStatus.Playing
                ? EffectParameterFlags.NoRestart : EffectParameterFlags.Start;
            effectL.SetParameters(LeftParameters, flagsL);

            if (RightParameters != null)
            {
                // Do not restart playing effect.
                flagsR |= effectR.Status == EffectStatus.Playing
                    ? EffectParameterFlags.NoRestart : EffectParameterFlags.Start;
                effectR.SetParameters(RightParameters, flagsR);
            }
            return true;
        }

        int TryParse(string value)
        {
            int i;
            int.TryParse(value, out i);
            return i;
        }

        #endregion


    }
}
