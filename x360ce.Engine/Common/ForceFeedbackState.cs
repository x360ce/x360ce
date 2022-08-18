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
        public ForceFeedbackState()
        {
            PeriodicForceL = new PeriodicForce();
            PeriodicForceR = new PeriodicForce();
            ConstantForceL = new ConstantForce();
            ConstantForceR = new ConstantForce();
            GUID_Force = EffectGuid.ConstantForce;
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
		string old_ForceSwapMotor = "-1";
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

        public void StopDeviceForces(Device device)
        {
            for (int i = 0; i < device.CreatedEffects.Count; i++)
            {
                var effect = device.CreatedEffects[i];
                if (effect.Status == EffectStatus.Playing)
                    effect.Stop();
            }
        }

        // Xbox One gamepads are equipped with a total of four independent vibration motors:
        // Two large motors located in the gamepad body:
        //	- Left  motor provides rough, high-amplitude vibration.
        //	- Right motor provides gentler, more subtle vibration.
        // Two small motors located inside each trigger,
        // that provide sharp bursts of vibration directly to the user's trigger fingers.

        public bool SetDeviceForces(UserDevice ud, Device device, PadSetting ps, Vibration v)
		{
			var motorsChanged = Changed(ref old_ForceSwapMotor, ps.ForceSwapMotor);
			bool swapMotor = false;

			if (motorsChanged)
			{
				// Find and assign actuators.
				var actuators = ud.DeviceObjects.Where(x => x.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator)).ToList();
				DeviceObjectItem xMotor = null;
				// If actuator available then...
				if (actuators.Count > 0)
				{
					// Try to find left actuator.
					xMotor = actuators.FirstOrDefault(x => x.Type == ObjectGuid.XAxis);
					//var actuator = actuators[0];
					// If default actuator not found then take default.
					if (xMotor == null)
						xMotor = actuators[0];
					actuators.Remove(xMotor);
				}
				DeviceObjectItem yMotor = null;
				// If actuator available then...
				if (actuators.Count > 0)
				{
					// Try to find right actuator.
					yMotor = actuators.FirstOrDefault(x => x.Type == ObjectGuid.YAxis);
					//var actuator = actuators[0];
					// If default actuator not found then take default.
					if (yMotor == null)
						yMotor = actuators[0];
					actuators.Remove(yMotor);
				}
				swapMotor = TryParse(ps.ForceSwapMotor) == 1;
				// Allow to swap if both motors exist.
				if (swapMotor && xMotor != null && yMotor != null)
				{
					actuatorL = yMotor;
					actuatorR = xMotor;
				}
				else
				{
					actuatorL = xMotor;
					actuatorR = yMotor;
				}
			}

			// Return if force feedback actuators not found.
			if (actuatorL == null)
                return false;
            Effect effectL = null;
            Effect effectR = null;

			// If device already have effects then...
			if (device.CreatedEffects.Count > 0)
			{
				effectL = device.CreatedEffects[0];
				effectL.Download();
			}
			if (device.CreatedEffects.Count > 1)
			{
				effectR = device.CreatedEffects[1];
				effectL.Download();
			}

			// Effect type changed.
			bool forceChanged =	Changed(ref old_ForceType, ps.ForceType);

			ForceEffectType forceType = 0;
			if (motorsChanged || forceChanged)
            {
                // Update values.
                forceType = (ForceEffectType)TryParse(ps.ForceType);
				if (forceType.HasFlag(ForceEffectType.PeriodicSine))
					GUID_Force = EffectGuid.Sine;
				else if (forceType.HasFlag(ForceEffectType.PeriodicSawtooth))
					GUID_Force = EffectGuid.SawtoothDown;
				else
					GUID_Force = EffectGuid.ConstantForce;
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

            if (motorsChanged || forceChanged)
            {
                // If 2 actuators available
                if (actuatorR != null)
                {
					paramsL = GetParameters();
					paramsR = GetParameters();
					// Unfortunately SpeedLink GamePad needs both axis specified in order to operate motors separately.
					// Which is counter-intuitive.
					if (forceType.HasFlag(ForceEffectType._Type2))
					{
						// Note: Second axis direction will be set to zero i.e. motor will be not used by effect.
						// Directions must be set to 'Positive' on both first axis via force feedback settings interface.
						paramsL.Axes = new int[2] { actuatorL.ObjectId, actuatorR.ObjectId };
						paramsR.Axes = new int[2] { actuatorR.ObjectId, actuatorL.ObjectId };
						// There is no need to set this flag or DIERR_ALREADYINITIALIZED error will be thrown.
						//flagsR |= EffectParameterFlags.Axes;
					}
					// Used for normal devices like Logitech. Use one axis per Effect/Parameter.
					else
					{
						paramsL.Axes = new int[1] { actuatorL.ObjectId };
						paramsR.Axes = new int[1] { actuatorR.ObjectId };
						// There is no need to set this flag or DIERR_ALREADYINITIALIZED error will be thrown.
						//flagsR |= EffectParameterFlags.Axes;
					}
				}
                // If one actuator available.
                else if (actuatorL != null)
                {
                    paramsL = GetParameters();
                    paramsL.Axes = new int[1] { actuatorL.ObjectId };
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
            if (motorsChanged || forceChanged || directionLChanged)
            {
                var directionL = TryParse(old_LeftDirection);
                var dirL = new int[paramsL.Axes.Length];
                dirL[0] = directionL;
                paramsL.Directions = dirL;
                flagsL |= EffectParameterFlags.Direction;
            }

            // Direction needs to be updated when force or direction change.
            if (actuatorR != null && (motorsChanged || forceChanged || directionRChanged))
            {
                var directionR = TryParse(old_RightDirection);
                var dirR = new int[paramsR.Axes.Length];
                dirR[0] = directionR;
                paramsR.Directions = dirR;
                flagsR |= EffectParameterFlags.Direction;
            }

            var strengthChanged = Changed(ref old_OveralStrength, ps.ForceOverall);
            var strengthLChanged = Changed(ref old_LeftStrength, ps.LeftMotorStrength);
            var strengthRChanged = Changed(ref old_RightStrength, ps.RightMotorStrength);

            if (motorsChanged || forceChanged || strengthChanged || strengthLChanged)
            {
                int overalStrength = ConvertHelper.ConvertRange(ps.GetForceOverall(), 0, 100, 0, DI_FFNOMINALMAX);
                int leftGain = ConvertHelper.ConvertRange(ps.GetLeftMotorStrength(), 0, 100, 0, overalStrength);
                paramsL.Gain = leftGain;
                flagsL |= EffectParameterFlags.Gain;
            }

            if (actuatorR != null && (motorsChanged || forceChanged || strengthChanged || strengthRChanged))
            {
                int overalStrength = ConvertHelper.ConvertRange(ps.GetForceOverall(), 0, 100, 0, DI_FFNOMINALMAX);
                int rightGain = ConvertHelper.ConvertRange(ps.GetRightMotorStrength(), 0, 100, 0, overalStrength );
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
            if (motorsChanged || forceChanged || periodRChanged || speedRChanged || combine)
            {
                rightMagnitudeAdjusted = ConvertHelper.ConvertRange(old_RightMotorSpeed, short.MinValue, short.MaxValue, 0, DI_FFNOMINALMAX);
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
            if (motorsChanged || forceChanged || periodLChanged || speedLChanged || combine)
            {
                // Convert speed into magnitude/amplitude.
                leftMagnitudeAdjusted = ConvertHelper.ConvertRange(old_LeftMotorSpeed, short.MinValue, short.MaxValue, 0, DI_FFNOMINALMAX);
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
            if (motorsChanged || forceChanged)
            {
                // Update Left force
                paramsL.Parameters = GUID_Force == EffectGuid.ConstantForce
                    ? ConstantForceL as TypeSpecificParameters : PeriodicForceL;
                // Note: Device must be acquired in exclusive mode before effect can be created.
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

		bool Changed(ref ForceEffectType? oldValue, ForceEffectType newValue)
		{
			var changed = oldValue != newValue;
			oldValue = newValue;
			return changed;
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
