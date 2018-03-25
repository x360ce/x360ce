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
            PeriodicForceL = new PeriodicForce();
            PeriodicForceR = new PeriodicForce();
            ConstantForceL = new ConstantForce();
            ConstantForceR = new ConstantForce();
            GUID_Force = EffectGuid.ConstantForce;
            // Find and assign actuators.
            var actuators = ud.DeviceObjects.Where(x => x.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator)).ToList();

            // If actuator available then...
            if (actuators.Count > 0)
            {
                // Try to find left actuator.
                actuatorL = actuators.FirstOrDefault(x => x.Type == ObjectGuid.XAxis);
                //var actuator = actuators[0];
                // If default actuator not found then take default.
                if (actuatorL == null)
                    actuatorL = actuators[0];
                actuators.Remove(actuatorL);
            }
            // If actuator available then...
            if (actuators.Count > 0)
            {
                // Try to find right actuator.
                actuatorR = actuators.FirstOrDefault(x => x.Type == ObjectGuid.YAxis);
                //var actuator = actuators[0];
                // If default actuator not found then take default.
                if (actuatorR == null)
                    actuatorR = actuators[0];
                actuators.Remove(actuatorR);
            }
        }

        const uint INFINITE = 0xFFFFFFFF;
        const uint DIEB_NOTRIGGER = 0xFFFFFFFF;

        EffectParameters GetParameters()
        {
            var p = new EffectParameters();
            p.Flags = EffectFlags.Cartesian | EffectFlags.ObjectIds;
            p.StartDelay = 0;
            p.Duration = unchecked((int)INFINITE);
            p.SamplePeriod = 0;
            p.TriggerButton = unchecked((int)DIEB_NOTRIGGER);
            p.TriggerRepeatInterval = unchecked((int)INFINITE);
            return p;
        }

        // Left

        DeviceObjectItem actuatorL;
        EffectParameters paramsL;
        public PeriodicForce PeriodicForceL;
        public ConstantForce ConstantForceL;

        // Right

        DeviceObjectItem actuatorR;
        EffectParameters paramsR;
        public PeriodicForce PeriodicForceR;
        public ConstantForce ConstantForceR;

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

        public void StopDeviceForces(Joystick device)
        {
            for (int i = 0; i < device.CreatedEffects.Count; i++)
            {
                var effect = device.CreatedEffects[i];
                if (effect.Status == EffectStatus.Playing)
                    effect.Stop();
            }
        }

        public bool SetDeviceForces(Joystick device, PadSetting ps, Vibration v)
        {
            // Return if force feedback actuators not found.
            if (actuatorL == null)
                return false;

            Effect effectL = null;
            Effect effectR = null;

            // If device already have effects then...
            if (device.CreatedEffects.Count > 0)
                effectL = device.CreatedEffects[0];
            if (device.CreatedEffects.Count > 1)
                effectR = device.CreatedEffects[1];

            // Effect type changed.
            bool forceChanged = Changed(ref old_ForceType, ps.ForceType);

            if (forceChanged)
            {
                // Update values.
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
                // Force change requires to dispose old effects.
                // Stop old effects.
                if (effectL != null)
                {
                    effectL.Stop();
                    effectL.Dispose();
                    effectL = null;
                }
                // Stop old effects.
                if (effectR != null)
                {
                    effectR.Stop();
                    effectR.Dispose();
                    effectR = null;
                }
            }

            // If device already do not have effects then.
            if (paramsL != null && device.CreatedEffects.Count < 1)
                forceChanged = true;
            if (paramsR != null && device.CreatedEffects.Count < 2)
                forceChanged = true;

            // Tells which effect parameters to modify.
            var flagsL = EffectParameterFlags.None;
            var flagsR = EffectParameterFlags.None;

            if (forceChanged)
            {
                if (actuatorL != null)
                {
                    paramsL = GetParameters();
                    paramsL.Axes = new int[1] { actuatorL.ObjectId };
                    // There is no need to set this flag or DIERR_ALREADYINITIALIZED error will be thrown.
                    //flagsL |= EffectParameterFlags.Axes;
                }
                if (actuatorR != null)
                {
                    paramsR = GetParameters();
                    paramsR.Axes = new int[1] { actuatorR.ObjectId };
                    // There is no need to set this flag or DIERR_ALREADYINITIALIZED error will be thrown.
                    //flagsR |= EffectParameterFlags.Axes;
                }
            }

            // Direction changed.
            // Right-handed Cartesian direction:
            // x: -1 = left,     1 = right,   0 - no direction
            // y: -1 = backward, 1 = forward, 0 - no direction
            // z: -1 = down,     1 = up,      0 - no direction

            var directionLChanged = Changed(ref old_LeftDirection, ps.LeftMotorDirection);
            var directionRChanged = Changed(ref old_RightDirection, ps.RightMotorDirection);

            // Direction needs to be updated when force or direction change.
            if (forceChanged || directionLChanged)
            {
                var directionL = TryParse(old_LeftDirection);
                paramsL.Directions = new int[1] { directionL };
                flagsL |= EffectParameterFlags.Direction;
            }

            // Direction needs to be updated when force or direction change.
            if (actuatorR != null && (forceChanged || directionRChanged))
            {
                var directionR = TryParse(old_RightDirection);
                paramsR.Directions = new int[1] { directionR };
                flagsR |= EffectParameterFlags.Direction;
            }

            var strengthChanged = Changed(ref old_OveralStrength, ps.ForceOverall);
            var strengthLChanged = Changed(ref old_LeftStrength, ps.LeftMotorStrength);
            var strengthRChanged = Changed(ref old_RightStrength, ps.RightMotorStrength);

            if (forceChanged || strengthChanged || strengthLChanged)
            {
                int overalStrength = ConvertHelper.ConvertRange(0, 100, 0, DI_FFNOMINALMAX, ps.GetForceOverall());
                int leftGain = ConvertHelper.ConvertRange(0, 100, 0, overalStrength, ps.GetLeftMotorStrength());
                paramsL.Gain = leftGain;
                flagsL |= EffectParameterFlags.Gain;
            }

            if (actuatorR != null && (forceChanged || strengthChanged || strengthRChanged))
            {
                int overalStrength = ConvertHelper.ConvertRange(0, 100, 0, DI_FFNOMINALMAX, ps.GetForceOverall());
                int rightGain = ConvertHelper.ConvertRange(0, 100, 0, overalStrength, ps.GetRightMotorStrength());
                paramsR.Gain = rightGain;
                flagsR |= EffectParameterFlags.Gain;
            }

            var periodLChanged = Changed(ref old_LeftPeriod, ps.LeftMotorPeriod);
            var periodRChanged = Changed(ref old_RightPeriod, ps.RightMotorPeriod);

            var speedLChanged = Changed(ref old_LeftMotorSpeed, v.LeftMotorSpeed);
            var speedRChanged = Changed(ref old_RightMotorSpeed, v.RightMotorSpeed);

            // Convert speed into magnitude/amplitude.
            int leftMagnitudeAdjusted;
            int rightMagnitudeAdjusted = 0;

            int leftPeriod;
            int rightPeriod = 0;

            // If device have only one force feedback actuator (probably wheel).
            var combine = actuatorR == null;

            // Get right values first for possible combine later.
            if (forceChanged || periodRChanged || speedRChanged || combine)
            {
                rightMagnitudeAdjusted = ConvertHelper.ConvertRange(short.MinValue, short.MaxValue, 0, DI_FFNOMINALMAX, old_RightMotorSpeed);
                rightPeriod = TryParse(old_RightPeriod) * 1000;
                if (actuatorR != null)
                {
                    // Update force values.
                    if (GUID_Force == EffectGuid.ConstantForce)
                    {
                        ConstantForceR.Magnitude = rightMagnitudeAdjusted;
                    }
                    else
                    {
                        PeriodicForceR.Magnitude = rightMagnitudeAdjusted;
                        PeriodicForceR.Period = rightPeriod;
                    }
                    // Update flags to indicate that specific force parameters changed.
                    flagsR |= EffectParameterFlags.TypeSpecificParameters;
                }
            }

            // Calculate left later for possible combine.
            if (forceChanged || periodLChanged || speedLChanged || combine)
            {
                // Convert speed into magnitude/amplitude.
                leftMagnitudeAdjusted = ConvertHelper.ConvertRange(short.MinValue, short.MaxValue, 0, DI_FFNOMINALMAX, old_LeftMotorSpeed);
                leftPeriod = TryParse(old_LeftPeriod) * 1000;
                // If device have only one force feedback actuator (probably wheel).
                if (combine)
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
                    ConstantForceL.Magnitude = leftMagnitudeAdjusted;
                }
                else
                {
                    PeriodicForceL.Magnitude = leftMagnitudeAdjusted;
                    PeriodicForceL.Period = leftPeriod;
                }
                // Update flags to indicate that specific force parameters changed.
                flagsL |= EffectParameterFlags.TypeSpecificParameters;
            }
            // Recreate effects if force changed.
            if (forceChanged)
            {
                // Update Left force
                paramsL.Parameters = GUID_Force == EffectGuid.ConstantForce
                    ? ConstantForceL as TypeSpecificParameters : PeriodicForceL;
                // Note: Device must be acquired in exclusive mode before effect can be created.
                // try
                // {
                effectL = new Effect(device, GUID_Force, paramsL);
                if (actuatorR != null)
                {
                    // Update Right force
                    paramsR.Parameters = GUID_Force == EffectGuid.ConstantForce
                        ? ConstantForceR as TypeSpecificParameters : PeriodicForceR;
                    effectR = new Effect(device, GUID_Force, paramsR);
                }
            }
            if (flagsL != EffectParameterFlags.None)
            {
                SetParamaters(effectL, paramsL, flagsL);
            }
            if (flagsR != EffectParameterFlags.None)
                SetParamaters(effectR, paramsR, flagsR);
            return true;
        }

        void SetParamaters(Effect effect, EffectParameters parameters, EffectParameterFlags flags)
        {
            if (parameters == null)
                return;
            // Do not restart playing effect.
            flags |= effect.Status == EffectStatus.Playing
                ? EffectParameterFlags.NoRestart : EffectParameterFlags.Start;
            effect.SetParameters(parameters, flags);
        }

        int TryParse(string value)
        {
            int i;
            int.TryParse(value, out i);
            return i;
        }

        bool Changed(ref string oldValue, string newValue)
        {
            var changed = oldValue != newValue;
            oldValue = newValue;
            return changed;
        }

        bool Changed(ref short oldValue, short newValue)
        {
            var changed = oldValue != newValue;
            oldValue = newValue;
            return changed;
        }

        public bool Changed(PadSetting ps)
        {
            return
            old_ForceType != ps.ForceType ||
            old_LeftPeriod != ps.LeftMotorPeriod ||
            old_RightPeriod != ps.RightMotorPeriod ||
            old_LeftStrength != ps.LeftMotorStrength ||
            old_RightStrength != ps.RightMotorStrength ||
            old_LeftDirection != ps.LeftMotorDirection ||
           old_RightDirection != ps.RightMotorDirection ||
           old_OveralStrength != ps.ForceOverall;
        }


        #endregion


    }
}
