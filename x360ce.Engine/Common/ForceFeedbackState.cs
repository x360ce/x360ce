using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			LeftRestart = true;
			RightRestart = true;
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
				//var actuator = actuators.FirstOrDefault(x => x.Type == ObjectGuid.XAxis);
				var actuator = actuators[0];
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
				//var actuator = actuators.FirstOrDefault(x => x.Type == ObjectGuid.YAxis);
				var actuator = actuators[0];
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
			p.SetAxes(new int[1], new int[1]);
			//p.Axes = new int[1] { offset };
			p.Flags = EffectFlags.Cartesian | EffectFlags.ObjectOffsets;
			//p.Envelope = new Envelope();
			p.StartDelay = 0;
			p.Duration = unchecked((int)INFINITE);
			p.SamplePeriod = 0;
			p.TriggerButton = unchecked((int)DIEB_NOTRIGGER);
			p.TriggerRepeatInterval = 0;
			return p;
		}

		// Left

		public EffectParameters LeftParameters;
		public Effect LeftEffect;
		public bool LeftRestart;
		public PeriodicForce LeftPeriodicForce;
		public ConstantForce LeftConstantForce;

		// Right

		public EffectParameters RightParameters;
		public Effect RightEffect;
		public bool RightRestart;
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

		public bool SetDeviceForces(Joystick device, IntPtr handle, PadSetting ps, Vibration v)
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
				if (GUID_Force == EffectGuid.ConstantForce)
					LeftParameters.Parameters = LeftConstantForce;
				else
					LeftParameters.Parameters = LeftPeriodicForce;
				LeftEffect = new Effect(device, GUID_Force, LeftParameters);
				if (RightParameters != null)
					LeftRestart = true;
				if (RightParameters != null)
				{
					if (GUID_Force == EffectGuid.ConstantForce)
						RightParameters.Parameters = RightConstantForce;
					else
						RightParameters.Parameters = RightPeriodicForce;
					RightEffect = new Effect(device, GUID_Force, RightParameters);
					RightRestart = true;
				}
			}

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
				int leftStrength = ConvertHelper.ConvertRange(0, 100, 0, overalStrength, ps.GetLeftMotorStrength());
				int rightStrength = ConvertHelper.ConvertRange(0, 100, 0, overalStrength, ps.GetRightMotorStrength());

				LeftParameters.Gain = leftStrength;
				//int leftDirection = TryParse(ps.LeftMotorDirection);
				//LeftParameters.Directions = new int[1] { 1 }; // leftDirection;
				if (RightParameters != null)
				{
					RightParameters.Gain = rightStrength;
					//int rightDirection = TryParse(ps.RightMotorDirection);
					//RightParameters.Directions = new int[1] { 1 }; // // rightDirection
				}

				// Convert speed into magnitude/amplitude.
				var leftMagnitudeAdjusted = ConvertHelper.ConvertRange(short.MinValue, short.MaxValue, 0, leftStrength, v.LeftMotorSpeed);
				var rightMagnitudeAdjusted = ConvertHelper.ConvertRange(short.MinValue, short.MaxValue, 0, rightStrength, v.RightMotorSpeed);

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
			}
			device.Unacquire();
			device.SetCooperativeLevel(handle, CooperativeLevel.Background | CooperativeLevel.Exclusive);
			device.Acquire();
			// If start new effect then.
			if (LeftRestart)
			{
				LeftEffect.Start();
			}
			else
			{
				// Modify effect.
				LeftEffect.SetParameters(LeftParameters, EffectParameterFlags.NoRestart);
			}
			if (RightParameters != null)
			{
				// If start new effect then.
				if (RightRestart)
				{
					RightEffect.Start();
				}
				else
				{
					// Modify effect.
					RightEffect.SetParameters(RightParameters, EffectParameterFlags.NoRestart);
				}
			}
			device.Unacquire();
			// If combined then...
			if (RightParameters == null)
			{
				// Restart combined effect if it was stopped.
				LeftRestart = (v.LeftMotorSpeed == 0 && v.RightMotorSpeed == 0);
			}
			else
			{
				// Restart left motor effect next time if it was stopped.
				LeftRestart = (v.LeftMotorSpeed == 0);
				// Restart right motor effect next time if it was stopped.
				RightRestart = (v.RightMotorSpeed == 0);
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
