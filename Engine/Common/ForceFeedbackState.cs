using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
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

        // Effects this program made on the device are held here by reference. The device's own
        // list of created effects is shared by all three, so a position in it names nothing.

        // Left

        DeviceObjectItem actuatorL;
        EffectParameters paramsL;
        Effect effectL;

        /// <summary>Effects the device said it will never take. Asked once, then left alone, or every poll would ask again and every answer would be reported.</summary>
        readonly HashSet<Effect> unsupported = new HashSet<Effect>();
        public PeriodicForce PeriodicForceL;
        public ConstantForce ConstantForceL;

        // Right

        DeviceObjectItem actuatorR;
        EffectParameters paramsR;
        Effect effectR;
        public PeriodicForce PeriodicForceR;
        public ConstantForce ConstantForceR;

        // Centering spring: a constant force on the same actuator as the left motor, turned
        // towards the centre by this program as the wheel moves.

        EffectParameters paramsS;
        Effect effectS;
        readonly ConstantForce springForce = new ConstantForce();
        string old_SpringEnable;
        string old_SpringStrength;
        /// <summary>The spring strength in percent, read once when the setting changes rather than parsed every poll.</summary>
        int springStrength;
        /// <summary>The force the device holds now, so a poll that would set the same value sets nothing.</summary>
        int springMagnitude;
        /// <summary>The wheel is at rest inside the dead band, and stays let go of until it is well outside it.</summary>
        bool springResting;
        /// <summary>The device refused the spring effect. Asked once, or every poll would ask and throw.</summary>
        bool springRefused;

        // Damping that goes with the spring: the device itself resists the wheel's speed, so a
        // constant force does not swing it from one end to the other.

        EffectParameters paramsD;
        Effect effectD;
        int damperCoefficient;
        /// <summary>Which axis in the device state the spring's actuator moves, or -1 while unknown.</summary>
        public int SpringAxisIndex = -1;

        // Steering range, sent to the wheel itself rather than through DirectInput.

        string old_WheelRange;
        Joystick rangeDevice;

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

        public void StopDeviceForces(Joystick device)
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

        public bool SetDeviceForces(UserDevice ud, Joystick device, PadSetting ps, Vibration v)
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
				SpringAxisIndex = actuatorL == null ? -1 : actuatorL.DiIndex;
			}

			// Return if force feedback actuators not found.
			if (actuatorL == null)
                return false;

			// A new state on a device still carrying effects from an earlier one starts clean, so
			// the effects it makes are the only ones the device holds.
			if (effectL == null && effectR == null && effectS == null && effectD == null)
				DisposeDeviceEffects(device);
			effectL?.Download();
			effectR?.Download();
			effectS?.Download();
			effectD?.Download();

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

            // If the effects this state made are gone then they are made again.
            if (paramsL != null && effectL == null)
                forceChanged = true;
            if (paramsR != null && effectR == null)
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
                int overalStrength = ConvertHelper.ConvertRange(0, 100, 0, DI_FFNOMINALMAX, ps.GetForceOverall());
                int leftGain = ConvertHelper.ConvertRange(0, 100, 0, overalStrength, ps.GetLeftMotorStrength());
                paramsL.Gain = leftGain;
                flagsL |= EffectParameterFlags.Gain;
            }

            if (actuatorR != null && (motorsChanged || forceChanged || strengthChanged || strengthRChanged))
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
            if (motorsChanged || forceChanged || periodRChanged || speedRChanged || combine)
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
            if (motorsChanged || forceChanged || periodLChanged || speedLChanged || combine)
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
            if (motorsChanged || forceChanged)
            {
                // Update Left force
                paramsL.Parameters = GUID_Force == EffectGuid.ConstantForce
                    ? ConstantForceL as TypeSpecificParameters : PeriodicForceL;
                // Note: Device must be acquired in exclusive mode before effect can be created.
                effectL = CreateEffect(device, GUID_Force, paramsL);
                if (actuatorR != null)
                {
                    // Update Right force
                    paramsR.Parameters = GUID_Force == EffectGuid.ConstantForce
                        ? ConstantForceR as TypeSpecificParameters : PeriodicForceR;
                    effectR = CreateEffect(device, GUID_Force, paramsR);
                }
            }
            if (flagsL != EffectParameterFlags.None)
            {
                SetParamaters(effectL, paramsL, flagsL);
            }
            if (flagsR != EffectParameterFlags.None)
                SetParamaters(effectR, paramsR, flagsR);

            var springEnableChanged = Changed(ref old_SpringEnable, ps.ForceSpringEnable);
            if (springEnableChanged || Changed(ref old_SpringStrength, ps.ForceSpringStrength))
                springStrength = ps.ForceSpringEnable == "1" ? Math.Max(0, Math.Min(100, ps.GetForceSpringStrength())) : 0;
            // The actuator the spring sits on may have changed, so the effect is made again on the next poll.
            if (motorsChanged)
                DropSpring();
            // The range goes to the wheel once per setting and once per device, because a wheel
            // plugged in again has forgotten it.
            if (Changed(ref old_WheelRange, ps.WheelRange) || rangeDevice != device)
            {
                rangeDevice = device;
                var degrees = ps.GetWheelRange();
                if (degrees > 0 && LogitechWheel.SupportsRange(ud.DevVendorId, ud.DevProductId))
                    LogitechWheel.SetRange(ud.HidDevicePath, degrees);
            }
            return true;
        }

        /// <summary>How near the centre the spring lets go, as DirectInput counts the axis: one percent of the travel.</summary>
        public const int SpringDeadBand = SpringCalibration.AxisMax / 100;

        /// <summary>The spring's force for a wheel position, in percent, positive towards the high end of the axis.</summary>
        /// <remarks>
        /// The force is the same at every angle, so the weakest setting that brings the wheel home
        /// from one place brings it home from everywhere, and a wheel in a 200 degree range feels
        /// the same as one in 900. Inside the dead band there is no force, so the wheel comes to
        /// rest at the centre instead of being pushed back and forth across it, and a wheel at rest
        /// there is let alone until it is twice the dead band away, so a jitter at the edge does
        /// not switch the force on and off.
        /// </remarks>
        /// <param name="resting">Whether the wheel was at rest in the dead band. Set on the way out to what it is now.</param>
        public static int SpringForce(int position, int strengthPercent, ref bool resting)
        {
            if (strengthPercent <= 0)
                return 0;
            var offset = position - SpringCalibration.Center;
            var distance = Math.Abs(offset);
            if (distance <= SpringDeadBand || (resting && distance <= SpringDeadBand * 2))
            {
                resting = true;
                return 0;
            }
            resting = false;
            return offset < 0 ? strengthPercent : -strengthPercent;
        }

        /// <summary>How much the device resists the wheel's speed while the spring is on, per percent of spring strength, in DirectInput's units.</summary>
        /// <remarks>
        /// A constant force alone swings a wheel from one end to the other: it arrives at the centre
        /// at speed, the force turns round, and it goes back just as far. Resistance in proportion
        /// to speed takes that energy out, and the device computes it itself between polls.
        /// </remarks>
        public const int DamperPerPercent = 100;

        /// <summary>How far from the centre the damping reaches, as a share of the travel to one side. Beyond it the wheel returns unbraked.</summary>
        /// <remarks>
        /// Measured on a G27: damping the whole travel brought the wheel home in 1.1 to 1.5 seconds at
        /// 60 and 100 percent, damping the inner quarter in 0.3 to 0.45, both settling after one crossing.
        /// </remarks>
        public const int DamperZonePercent = 25;

        /// <summary>The damping to ask of the device at a wheel position, for a spring strength in percent.</summary>
        public static int DamperFor(int position, int strengthPercent)
        {
            if (strengthPercent <= 0)
                return 0;
            var reach = (long)SpringCalibration.Center * DamperZonePercent / 100;
            return Math.Abs(position - SpringCalibration.Center) <= reach ? strengthPercent * DamperPerPercent : 0;
        }

        /// <summary>
        /// A DirectInput direction names where a force comes from, so a positive magnitude along the
        /// positive axis pushes towards the low end. A force towards the high end is the negative one.
        /// </summary>
        const int TowardsHighEnd = -1;

        /// <summary>Holds a wheel at its centre with a spring the game never asked for. Called every poll.</summary>
        /// <remarks>
        /// A game made for a gamepad sends rumble and nothing else, so a wheel driven from that
        /// rumble has no force at all between bumps and turns freely. While a calibration is on, it
        /// decides the force instead of the setting. The device is only spoken to when the force
        /// changes, which is when the wheel crosses the centre or the dead band, so a poll usually
        /// compares two numbers and returns.
        /// </remarks>
        /// <param name="position">The steering axis, 0 to <see cref="SpringCalibration.AxisMax"/>.</param>
        /// <param name="calibration">The Auto button's run, or null.</param>
        /// <param name="nowMs">The time, for the calibration.</param>
        public void UpdateSpring(Joystick device, int position, SpringCalibration calibration, long nowMs)
        {
            var percent = calibration != null
                ? calibration.Update(position, nowMs)
                : SpringForce(position, springStrength, ref springResting);
            // The damping follows the setting, not the calibration, whose pushes must move the wheel freely.
            UpdateDamper(device, calibration != null ? 0 : DamperFor(position, springStrength));
            var magnitude = TowardsHighEnd * percent * (DI_FFNOMINALMAX / 100);
            if (magnitude == springMagnitude && (magnitude == 0 || effectS != null))
                return;
            if (magnitude == 0 && effectS == null)
            {
                springMagnitude = 0;
                return;
            }
            if (springRefused || actuatorL == null)
                return;
            springForce.Magnitude = magnitude;
            if (effectS == null)
            {
                paramsS = GetParameters();
                paramsS.Axes = new int[1] { actuatorL.ObjectId };
                paramsS.Directions = new int[1] { 1 };
                paramsS.Gain = DI_FFNOMINALMAX;
                paramsS.Parameters = springForce;
                effectS = CreateEffect(device, EffectGuid.ConstantForce, paramsS);
                if (effectS == null)
                {
                    springRefused = true;
                    return;
                }
            }
            SetParamaters(effectS, paramsS, EffectParameterFlags.TypeSpecificParameters);
            springMagnitude = magnitude;
        }

        /// <summary>Sets the resistance to the wheel's speed, once per change. A device without a damper gets none, and is not asked again.</summary>
        void UpdateDamper(Joystick device, int coefficient)
        {
            if (coefficient == damperCoefficient && (coefficient == 0 || effectD != null))
                return;
            if (coefficient == 0 && effectD == null)
            {
                damperCoefficient = 0;
                return;
            }
            if (springRefused || actuatorL == null)
                return;
            var conditions = new ConditionSet();
            conditions.Conditions = new Condition[]
            {
                new Condition
                {
                    Offset = 0,
                    PositiveCoefficient = coefficient,
                    NegativeCoefficient = coefficient,
                    PositiveSaturation = DI_FFNOMINALMAX,
                    NegativeSaturation = DI_FFNOMINALMAX,
                    DeadBand = 0,
                }
            };
            if (effectD == null)
            {
                paramsD = GetParameters();
                paramsD.Axes = new int[1] { actuatorL.ObjectId };
                paramsD.Directions = new int[1] { 0 };
                paramsD.Gain = DI_FFNOMINALMAX;
                paramsD.Parameters = conditions;
                effectD = CreateEffect(device, EffectGuid.Damper, paramsD);
                if (effectD == null)
                {
                    // Remembered as set, so a device with no damper is not asked on every poll.
                    damperCoefficient = coefficient;
                    return;
                }
            }
            else
            {
                paramsD.Parameters = conditions;
            }
            SetParamaters(effectD, paramsD, EffectParameterFlags.TypeSpecificParameters);
            damperCoefficient = coefficient;
        }

        /// <summary>Takes the spring and its damping off the device, to be made again when next needed.</summary>
        void DropSpring()
        {
            if (effectS != null)
            {
                effectS.Stop();
                effectS.Dispose();
                effectS = null;
                paramsS = null;
            }
            if (effectD != null)
            {
                effectD.Stop();
                effectD.Dispose();
                effectD = null;
                paramsD = null;
            }
            springMagnitude = 0;
            damperCoefficient = 0;
            springResting = false;
            springRefused = false;
        }

        /// <summary>Takes down every effect on the device, whichever state made it.</summary>
        static void DisposeDeviceEffects(Joystick device)
        {
            foreach (var effect in device.CreatedEffects.ToArray())
            {
                if (effect.Status == EffectStatus.Playing)
                    effect.Stop();
                effect.Dispose();
            }
        }

        /// <summary>
        /// The two answers a device gives for an effect it will never take: not implemented, and
        /// refused settings. Neither is a fault of the program, and asking again gets the same answer.
        /// </summary>
        static bool IsUnsupported(SharpDX.SharpDXException ex)
        {
            var code = ex.ResultCode.Code;
            return code == unchecked((int)0x80004001) || code == unchecked((int)0x80070057);
        }

        /// <summary>An effect on the device, or null when the device will not take one of that kind.</summary>
        static Effect CreateEffect(Joystick device, Guid guid, EffectParameters parameters)
        {
            try
            {
                return new Effect(device, guid, parameters);
            }
            catch (SharpDX.SharpDXException ex) when (IsUnsupported(ex))
            {
                return null;
            }
        }

        void SetParamaters(Effect effect, EffectParameters parameters, EffectParameterFlags flags)
        {
            if (parameters == null || effect == null || unsupported.Contains(effect))
                return;
            // Do not restart playing effect.
            flags |= effect.Status == EffectStatus.Playing
                ? EffectParameterFlags.NoRestart : EffectParameterFlags.Start;
            try
            {
                effect.SetParameters(parameters, flags);
            }
            catch (SharpDX.SharpDXException ex) when (IsUnsupported(ex))
            {
                unsupported.Add(effect);
            }
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
           old_OveralStrength != ps.ForceOverall ||
           old_SpringEnable != ps.ForceSpringEnable ||
           old_SpringStrength != ps.ForceSpringStrength ||
           old_WheelRange != ps.WheelRange;
        }


        #endregion


    }
}
