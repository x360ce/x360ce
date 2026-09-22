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
        /// <summary>Whether the spring is wanted, or null before the setting has been read.</summary>
        bool? springEnabled;
        string old_SpringStrength;
        /// <summary>The spring strength in percent, read once when the setting changes rather than parsed every poll.</summary>
        int springStrength;
        /// <summary>The force the device holds now, so a poll that would set the same value sets nothing.</summary>
        int springMagnitude;
        /// <summary>The wheel is at rest inside the dead band, and stays let go of until it is well outside it.</summary>
        /// <summary>The device refused the spring effect. Asked once, or every poll would ask and throw.</summary>
        bool springRefused;

        // Damping that goes with the spring: the device itself resists the wheel's speed, so a
        // constant force does not swing it from one end to the other.

        EffectParameters paramsD;
        Effect effectD;
        int damperCoefficient;
        /// <summary>The damping the device is holding right now, in its units, or nought when it holds none.</summary>
        public int DamperOnDevice { get { return effectD == null ? 0 : damperCoefficient; } }
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
                // The setting is the period at full drive; a real motor slows as the drive falls, and
                // so does the period played, along the measured line. Microseconds for the device.
                rightPeriod = MotorModel.PeriodMs(TryParse(old_RightPeriod), false, Drive(old_RightMotorSpeed)) * 1000;
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
                leftPeriod = MotorModel.PeriodMs(TryParse(old_LeftPeriod), true, Drive(old_LeftMotorSpeed)) * 1000;
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
            // Off means nothing of ours on the wheel, so whatever else centres it is left in charge: the
            // effects come off, and the device is held again so that DirectInput's own autocenter, which
            // can only be set while the device is let go of, follows the setting.
            var wanted = ps.ForceSpringEnable == "1";
            if (springEnabled.HasValue && springEnabled.Value != wanted)
                ud.IsExclusiveMode = null;
            springEnabled = wanted;
            if (springStrength == 0)
                DropSpring();
            // The actuator the spring sits on may have changed, so the effect is made again on the next poll.
            if (motorsChanged)
                DropSpring();
            // The range goes to the wheel once per setting and once per device, because a wheel
            // plugged in again has forgotten it. Not before the device is known by its ids: they
            // come from a read of the machine that finishes after the first polls, and a range
            // sent while they were still nought was sent to nothing and never sent again.
            if (ud.DevVendorId != 0 && (Changed(ref old_WheelRange, ps.WheelRange) || rangeDevice != device))
            {
                rangeDevice = device;
                var degrees = ps.GetWheelRange();
                if (degrees > 0 && LogitechWheel.SupportsRange(ud.DevVendorId, ud.DevProductId))
                    LogitechWheel.SetRange(ud.HidDevicePath, degrees);
            }
            return true;
        }

        /// <summary>How far from the centre the spring reaches full strength, as a share of the travel to one side.</summary>
        /// <remarks>
        /// A spring's force grows with distance; this is where the growth stops. Narrow, so that a
        /// wheel held off centre by the friction in its own gears comes to rest close to the centre:
        /// it rests where the spring's force equals that friction, and the steeper the ramp the
        /// nearer that is. A DirectInput spring effect could do this on the device, but its steepest
        /// slope is full force over the whole travel, which on a geared wheel leaves the rest far
        /// out; the loop runs a thousand times a second, which is steep enough to do it here.
        /// </remarks>
        public const int SpringRampPercent = 2;

        /// <summary>The ramp in axis units, at strengths up to <see cref="SpringStiffnessLimit"/>.</summary>
        public const int SpringRamp = SpringCalibration.Center * SpringRampPercent / 100;

        /// <summary>The strength above which the ramp widens, so the spring gets no stiffer than it is here.</summary>
        /// <remarks>
        /// The force answers the position a few milliseconds late: the poll, the bus, the device.
        /// A spring stiff enough that the wheel crosses the ramp in that time chases its own tail:
        /// pumped into a swing at half strength over the two percent ramp, a G27 stayed in a nine
        /// hertz shake of ten degrees either side for as long as it was watched, damping and all,
        /// while at the strength Auto finds for it, a quarter, it came to rest in two crossings.
        /// Above this the ramp widens in proportion, which keeps the force per degree, and so the
        /// loop, where it was measured to hold.
        /// </remarks>
        public const int SpringStiffnessLimit = 30;

        /// <summary>How far from the centre the spring reaches full strength at a strength, in axis units.</summary>
        public static int SpringRampFor(int strengthPercent)
        {
            return strengthPercent <= SpringStiffnessLimit ? SpringRamp : SpringRamp * strengthPercent / SpringStiffnessLimit;
        }

        /// <summary>The most one step of the ramp may change the force by, in percent of the device's force.</summary>
        /// <remarks>
        /// A wheel held just off the centre sits on a step of the ramp and is knocked across it by
        /// each change of force. Eight fixed steps made that knock an eighth of the strength, which
        /// at full strength was a twelve percent blow every poll and a wheel that chattered under a
        /// finger; a step no larger than this is too small to move it. Each change is still a message
        /// to the device, so the steps are as few as the cap allows.
        /// </remarks>
        public const int SpringStepPercent = 3;

        /// <summary>How many force levels the ramp is cut into at a strength: enough that no step is larger than <see cref="SpringStepPercent"/>.</summary>
        public static int SpringRampSteps(int strengthPercent)
        {
            return Math.Max(1, (strengthPercent + SpringStepPercent - 1) / SpringStepPercent);
        }

        /// <summary>How near the centre the spring asks nothing, as DirectInput counts the axis: a fifth of a percent of the travel.</summary>
        /// <remarks>
        /// Small. The force just outside it is the ramp's lowest step, which is small too, so there
        /// is no edge for the wheel to chatter across; a wide band was where a wheel came to rest
        /// short of the centre.
        /// </remarks>
        public const int SpringDeadBand = SpringCalibration.AxisMax / 500;

        /// <summary>The spring's force for a wheel position, in percent, positive towards the high end of the axis.</summary>
        /// <remarks>
        /// Beyond the ramp the force is the same at every angle, so the weakest setting that brings
        /// the wheel home from one place brings it home from everywhere, and a wheel in a 200 degree
        /// range feels the same as one in 900. Inside the ramp it grows with the distance, in steps,
        /// down to nothing at the centre. It used to switch between nothing and full at the edge of
        /// a band, and a wheel nudged gently was pushed back, let go, nudged and pushed back again,
        /// pulsing under a finger or a small weight; a force that grows has no edge to pulse at.
        /// </remarks>
        public static int SpringForce(int position, int strengthPercent)
        {
            if (strengthPercent <= 0)
                return 0;
            var offset = position - SpringCalibration.Center;
            var distance = Math.Abs(offset);
            if (distance <= SpringDeadBand)
                return 0;
            var percent = strengthPercent;
            var ramp = SpringRampFor(strengthPercent);
            if (distance < ramp)
            {
                // Counted from the edge of the dead band, so the first force outside it is one step.
                var steps = SpringRampSteps(strengthPercent);
                var step = (int)((long)(distance - SpringDeadBand) * steps / (ramp - SpringDeadBand)) + 1;
                percent = strengthPercent * step / steps;
            }
            return offset < 0 ? percent : -percent;
        }

        /// <summary>How much the device resists the wheel's speed while the spring is on, in DirectInput's units.</summary>
        /// <remarks>
        /// A constant force alone swings a wheel from one end to the other: it arrives at the centre
        /// at speed, the force turns round, and it goes back just as far. Resistance in proportion
        /// to speed takes that energy out, and the device computes it itself between polls.
        ///
        /// One value, whatever the strength and wherever the wheel is: a wheel's feel must not
        /// change with the angle unless the game asks it to. Measured on a G27, where a thirty
        /// percent push crossed the wheel in 0.8 s free, 1.2 s at this much, and could not move it
        /// at all at 5000: enough to take a swing out, far from enough to hold the wheel against
        /// its own motor. Scaled with the strength it reached 10000 at full, and the wheel ground
        /// its way home against its own brake.
        /// </remarks>
        public const int DamperCoefficient = 2500;

        /// <summary>The damping to ask of the device while the spring is on at this strength.</summary>
        public static int DamperFor(int strengthPercent)
        {
            return strengthPercent <= 0 ? 0 : DamperCoefficient;
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
                : SpringForce(position, springStrength);
            // The damping follows the setting, or the calibration's own asking: none while its pushes
            // must move the wheel freely, the answer's own damping while it checks that answer.
            UpdateDamper(device, calibration != null ? calibration.Damping : DamperFor(springStrength));
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

        /// <summary>A motor speed as the game means it, 0 to 1. Speeds are carried as shorts, off at the bottom of the range.</summary>
        static double Drive(short speed)
        {
            return (speed - (double)short.MinValue) / (short.MaxValue - (double)short.MinValue);
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
