using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Linq;
using x360ce.Engine;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{
		/// <summary>
		/// Convert DiStates to XInput states.
		/// </summary>
		void UpdateXiStates()
		{
			// Get all mapped devices.
			var settings = SettingsManager.Settings.Items
			   .Where(x => x.MapTo > (int)MapTo.None)
			   .ToArray();
			for (int i = 0; i < settings.Length; i++)
			{
				var setting = settings[i];
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
					var gp = new Gamepad();
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
					setting.XiState = gp;
				}
			}
			var ev = StatesUpdated;
			if (ev != null)
				ev(this, new EventArgs());
		}
	}
}
