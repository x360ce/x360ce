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

			// Acquire copy of feedbacks for processing.
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
								ud.DeviceEffects = AppHelper.GetDeviceEffects(device, deviceForm.Handle);
							// Get PAD index this device is mapped to.
							var userIndex = SettingsManager.Settings.Items
								.Where(x => x.MapTo > (int)MapTo.None)
								.Where(x => x.InstanceGuid == ud.InstanceGuid)
								.Select(x => x.MapTo).First();

							var force = feedbacks[userIndex - 1];
							if (force != null)
							{
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
										v.LeftMotorSpeed = (short)ConvertHelper.ConvertRange(byte.MinValue, byte.MaxValue, short.MinValue, short.MaxValue, force.LargeMotor);
										v.RightMotorSpeed = (short)ConvertHelper.ConvertRange(byte.MinValue, byte.MaxValue, short.MinValue, short.MaxValue, force.SmallMotor);
										if (ud.FFState == null)
										{
											ud.FFState = new Engine.ForceFeedbackState(ud);
										}
										ud.FFState.SetDeviceForces(ud.Device, deviceForm.Handle, ps, v);
									}
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

	}
}
