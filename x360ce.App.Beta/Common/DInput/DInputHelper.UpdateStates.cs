using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Linq;
using x360ce.Engine;

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
			for (int i = 0; i < userDevices.Count(); i++)
			{
				var ud = userDevices[i];
				JoystickState state = null;
				// Update direct input form and return actions (pressed buttons/dpads, turned axis/sliders).
				var isOnline = ud != null && ud.IsOnline;
				var device = ud.Device;
				if (isOnline && device != null)
				{
					try
					{
						device.Acquire();
						state = device.GetCurrentState();
					}
					catch (Exception ex)
					{
						var error = ex;
					}
				}
				ud.JoState = state ?? new JoystickState();
				ud.DiState = new CustomDiState(state);
			}
		}

		public Gamepad[] XInputStates;

		/// <summary>
		/// Convert DiStates to XInput states.
		/// </summary>
		void UpdateXiState()
		{
			var gamepads = new Gamepad[4];
			// Loop through XInput devices.
			for (int g = 0; g < gamepads.Length; g++)
			{
				var gp = new Gamepad();
				// Get mapped settings.
				var settings = SettingsManager.Settings.Items.Where(x => x.MapTo == g + 1).ToArray();
				for (int s = 0; s < settings.Length; s++)
				{
					var setting = settings[s];
					var ud = SettingsManager.GetDevice(setting.InstanceGuid);
					// If device is offline then continue.
					if (!ud.IsOnline)
						continue;
					// If device is not set then...
					var device = ud.Device;
					if (device == null)
						// Continue loop.
						continue;
					var padSetting = SettingsManager.GetPadSetting(setting.PadSettingChecksum);
					// If setting was not found then continue.
					if (padSetting == null)
						continue;
					var diState = ud.DiState;
					// If custom directInput state is not available then continue.
					if (diState == null)
						continue;
					int index;
					SettingType type;
					// Test Button A
					var value = padSetting.ButtonA;
					var buttonFlag = GamepadButtonFlags.A;
					// Try to get mapped value.
					var success = SettingsConverter.TryParseIndexAndType(value, out index, out type);
					// If values has valid map then...
					if (success)
					{
						switch (type)
						{
							case SettingType.None:
								break;
							case SettingType.Button:
								// If button is pressed then...
								if (diState.Buttons[index])
								{
									gp.Buttons |= buttonFlag;
								}
								break;
							case SettingType.Axis:
								break;
							case SettingType.IAxis:
								break;
							case SettingType.HAxis:
								break;
							case SettingType.IHAxis:
								break;
							case SettingType.Slider:
								break;
							case SettingType.ISlider:
								break;
							case SettingType.HSlider:
								break;
							case SettingType.IHSlider:
								break;
							case SettingType.DPad:
								break;
							case SettingType.DPadButton:
								break;
							default:
								break;
						}

					}
				}
				gamepads[g] = gp;
			}
			var ev = StatesUpdated;
			if (ev != null)
				ev(this, new EventArgs());
		}

	}
}
