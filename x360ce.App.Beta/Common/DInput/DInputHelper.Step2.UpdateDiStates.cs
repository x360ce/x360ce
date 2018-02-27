using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		void UpdateDiStates()
		{
			// Get all mapped user instances.
			var instanceGuids = SettingsManager.Settings.Items
				.Where(x => x.MapTo > (int)MapTo.None)
				.Select(x => x.InstanceGuid).ToArray();
			// Get all connected devices.
			var userDevices = SettingsManager.UserDevices.Items
				.Where(x => instanceGuids.Contains(x.InstanceGuid) && x.IsOnline)
				.ToArray();

			// Aquire copy of feedbacks for processing.
			var feedbacks = CopyAndClearFeedbacks();

			for (int i = 0; i < userDevices.Count(); i++)
			{
				// Update direct input form and return actions (pressed buttons/dpads, turned axis/sliders).
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
							device.Acquire();
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

							var force = feedbacks[userIndex - 1];
							// Get setting related to user device.
							var setting = SettingsManager.Settings.Items
								.FirstOrDefault(x => x.MapTo == userIndex && x.InstanceGuid == ud.InstanceGuid);
							if (setting != null)
							{

								// Get pad setting attached to device.
								var ps = SettingsManager.GetPadSetting(setting.PadSettingChecksum);
								if (ps != null && ps.ForceEnable == "1")
								{
									var v = new Vibration();
									v.LeftMotorSpeed = force.LargeMotor;
									v.RightMotorSpeed = force.SmallMotor;
									SetDeviceForces(ud, ps, v);
								}
							}
						}
						catch (Exception ex)
						{
							var error = ex;
						}
					}
					// If this is test device then...
					else if (TestDeviceHelper.ProductGuid.Equals(ud.ProductGuid))
					{
						state = TestDeviceHelper.GetCurrentState(ud);
						// Fill device objects.
						if (ud.DeviceObjects == null)
							ud.DeviceObjects = TestDeviceHelper.GetDeviceObjects();
						ud.DeviceEffects = new DeviceEffectItem[0];
					}
				}
				ud.JoState = state ?? new JoystickState();
				ud.DiState = new CustomDiState(ud.JoState);
			}
		}

		#region Force Feedback

		const int DI_FFNOMINALMAX = 10000;
		const uint INFINITE = 0xFFFFFFFF;
		const uint DIEB_NOTRIGGER = 0xFFFFFFFF;


		uint m_OveralStrength;
		byte m_Type;
		// Left Motor.
		uint m_LeftPeriod;
		uint m_LeftStrength;
		int m_LeftDirection;
		// Right Motor.
		uint m_RightPeriod;
		uint m_RightStrength;
		int m_RightDirection;
		bool m_SwapMotors;

		bool SetDeviceForces(UserDevice ud, PadSetting ps, Vibration v)
		{
			var actuatorCount = 0;
			if (ud.FFState == null)
			{
				ud.FFState = new Engine.ForceFeedbackState();
				// Find actuators.
				var actuators = ud.DeviceObjects.Where(x => x.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator)).ToArray();
				actuatorCount = actuators.Length;
			}
			// Return if no force feedback actuators found.
			if (actuatorCount == 0)
				return false;

			var leftSpeed = v.LeftMotorSpeed;
			var rightSpeed = v.RightMotorSpeed;

			var leftPeriod = int.Parse(ps.LeftMotorDirection) * 1000;
			var rightPeriod = int.Parse(ps.RightMotorPeriod) * 1000;
			var leftStrength = int.Parse(ps.LeftMotorStrength);
			var rightStrength = int.Parse(ps.RightMotorStrength);
			var leftDirection = int.Parse(ps.LeftMotorDirection);
			var rightDirection = int.Parse(ps.RightMotorDirection);

			var overalStrength = int.Parse(ps.ForceOverall);


			var forceType = (ForceFeedBackType)int.Parse(ps.ForceType);

			// Combine strengths into magnitude.
			var leftMagnitude = MulDiv(leftSpeed, DI_FFNOMINALMAX, ushort.MaxValue);
			var rightMagnitude = MulDiv(rightSpeed, DI_FFNOMINALMAX, ushort.MaxValue);

			var leftMagnitudeAdjusted = MulDiv(leftMagnitude, leftStrength, DI_FFNOMINALMAX);
			var rightMagnitudeAdjusted = MulDiv(rightMagnitude, rightStrength, DI_FFNOMINALMAX);

			// Parameters for created effect.
			var diEffectX = new EffectParameters();
			// Parameters for created effect.
			var diEffectY = new EffectParameters();

			// Right-handed Cartesian direction:
			// x: -1 = left,     1 = right,   0 - no direction
			// y: -1 = backward, 1 = forward, 0 - no direction
			// z: -1 = down,     1 = up,      0 - no direction

			var lZeroX = m_LeftDirection;
			var lZeroY = m_RightDirection;
			var DIJOFS_X = ud.DeviceObjects.FirstOrDefault(x => x.Type == ObjectGuid.XAxis).Offset;
			var DIJOFS_Y = ud.DeviceObjects.FirstOrDefault(x => x.Type == ObjectGuid.YAxis).Offset;
			// Left motor.
			diEffectX.Flags = EffectFlags.Cartesian | EffectFlags.ObjectOffsets;
			diEffectX.Envelope = new Envelope();
			diEffectX.StartDelay = 0;
			diEffectX.Duration = unchecked((int)INFINITE);
			diEffectX.SamplePeriod = 0;
			diEffectX.Gain = overalStrength;
			diEffectX.TriggerButton = unchecked((int)DIEB_NOTRIGGER);
			diEffectX.TriggerRepeatInterval = 0;
			diEffectY.Axes = new int[1] { DIJOFS_X };
			diEffectY.Directions = new int[1] { lZeroX };
			if (actuatorCount > 1)
			{
				// Right motor.
				diEffectY.Axes = new int[1] { DIJOFS_Y };
				diEffectY.Directions = new int[1] { lZeroY };
				diEffectY.Flags = EffectFlags.Cartesian | EffectFlags.ObjectOffsets;
				diEffectY.Envelope = new Envelope();
				diEffectY.StartDelay = 0;
				diEffectY.Duration = unchecked((int)INFINITE);
				diEffectY.SamplePeriod = 0;
				diEffectY.Gain = overalStrength;
				diEffectY.TriggerButton = unchecked((int)DIEB_NOTRIGGER);
				diEffectY.TriggerRepeatInterval = 0;
			}
			Guid GUID_Force;
			// If device have only one force fedback actuator (probably wheel).
			if (actuatorCount == 1)
			{
				// Forces must be combined.
				var combinedMagnitudeAdjusted = Math.Max(leftMagnitudeAdjusted, rightMagnitudeAdjusted);
				var combinedPeriod = 0;
				// If at least one speed is specified then...
				if (leftMagnitudeAdjusted > 0 || rightMagnitudeAdjusted > 0)
				{
					combinedPeriod = 1000 *
						((leftPeriod * leftMagnitudeAdjusted) + (rightPeriod * rightMagnitudeAdjusted))
						/ (leftMagnitudeAdjusted + rightMagnitudeAdjusted);
				}
				leftMagnitudeAdjusted = combinedMagnitudeAdjusted;
				leftPeriod = combinedPeriod;
			}

			// 1 - Periodic 'Sine Wave'.
			// 2 - Periodic 'Sawtooth Down Wave'.
			if (forceType == ForceFeedBackType.PeriodicSine || forceType == ForceFeedBackType.PeriodicSawtooth)
			{
				GUID_Force = forceType == ForceFeedBackType.PeriodicSine
					? EffectGuid.Sine
					: EffectGuid.SawtoothDown;
				// Left motor.
				ud.FFState.LeftPeriodicForce.Magnitude = leftMagnitudeAdjusted;
				ud.FFState.LeftPeriodicForce.Period = leftPeriod;
				diEffectX.Parameters = ud.FFState.LeftPeriodicForce;
				if (actuatorCount > 1)
				{
					// Right motor.
					ud.FFState.RightPeriodicForce.Magnitude = rightMagnitudeAdjusted;
					ud.FFState.RightPeriodicForce.Period = rightPeriod;
					diEffectY.Parameters = ud.FFState.RightPeriodicForce;
				}
			}
			// Constant.
			else
			{
				GUID_Force = EffectGuid.ConstantForce;
				// Left motor.
				ud.FFState.LeftConstantForce.Magnitude = leftMagnitudeAdjusted;
				diEffectX.Parameters = ud.FFState.LeftConstantForce;
				if (actuatorCount > 1)
				{
					// Right motor.
					ud.FFState.RightConstantForce.Magnitude = rightMagnitudeAdjusted;
					diEffectY.Parameters = ud.FFState.RightConstantForce;
				}
			}
			//PrintLog("Type %d Axes %d OMag %d LSpeed %d RSpeed %d LMag %d RMag %d LPeriod %d RPeriod", forceType, m_Axes, m_OveralStrength, leftSpeed, rightSpeed, leftMagnitudeAdjusted, rightMagnitudeAdjusted, leftPeriod, rightPeriod);
			// If no effect exists then...
			if (ud.FFState.LeftEffect == null)
			{
				ud.FFState.LeftRestart = true;
				// Left motor.
				try
				{
					ud.FFState.LeftEffect = new Effect(ud.Device, GUID_Force, diEffectX);
				}
				catch (Exception ex)
				{
					LastException = ex;
					return false;
				}
			}
			// If no effect exists then...
			if (actuatorCount > 1 && ud.FFState.RightEffect == null)
			{
				ud.FFState.RightRestart = true;
				// Right motor.
				try
				{
					ud.FFState.RightEffect = new Effect(ud.Device, GUID_Force, diEffectY);
				}
				catch (Exception ex)
				{
					LastException = ex;
					return false;
				}
			}
			// If start new effect then.
			if (ud.FFState.LeftRestart)
			{
				// Note: stop previous effect first.
				ud.FFState.LeftEffect.Start();
			}
			else
			{
				// Modify effect.
				ud.FFState.LeftEffect.SetParameters(diEffectX);
			}
			if (actuatorCount > 1)
			{
				// If start new effect then.
				if (ud.FFState.RightRestart)
				{
					// Note: stop previous effect first.
					ud.FFState.RightEffect.Start();
				}
				else
				{
					// Modify effect.
					ud.FFState.RightEffect.SetParameters(diEffectY);
				}
				// Restart left motorr effect next time if it was stopped.
				ud.FFState.LeftRestart = (leftSpeed == 0);
				// Restart right motor effect next time if it was stopped.
				ud.FFState.RightRestart = (rightSpeed == 0);
			}
			else
			{
				// Restart combined effect if it was stopped.
				ud.FFState.LeftRestart = (leftMagnitudeAdjusted == 0);
			}
			return true;
		}

		public static int MulDiv(int number, int numerator, int denominator)
		{
			return (int)(((long)number * numerator) / denominator);
		}

		#endregion

	}
}
