using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
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
				// If device was not found then continue.
				if (ud == null)
					continue;
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
				// Create Gamepad to map to.
				var gp = new Gamepad();
				bool success;
				int index;
				SettingType type;

				// --------------------------------------------------------
				// Convert DInput POV Hat value to D-PAD buttons.
				// --------------------------------------------------------

				// Create arrray to store 4 buttons for each POV.
				var dPadButtons = new bool[4 * diState.Povs.Length];
				// Loop trough D-Pad button states.
				for (int d = 0; d < diState.Povs.Length; ++d)
				{
					// Get degree value from the POV.
					int povdeg = diState.Povs[d];
					// If POV is pressed into one of the directions then...
					if (povdeg >= 0)
					{
						// Split PoV degrees into 8 groups by
						// converting PoV degree from 0 to 36000 to number from 0 to 7.
						// This will allow to have more flexible degree values mapped to buttons.
						var y = ((2250 + povdeg) / 4500) % 8;
						// XINPUT_GAMEPAD_DPAD_UP
						dPadButtons[d * 4 + 0] = (y >= 0 && y <= 1) || y == 7;
						// XINPUT_GAMEPAD_DPAD_RIGHT
						dPadButtons[d * 4 + 1] = (y >= 1 && y <= 3);
						// XINPUT_GAMEPAD_DPAD_DOWN
						dPadButtons[d * 4 + 2] = (y >= 3 && y <= 5);
						// XINPUT_GAMEPAD_DPAD_LEFT
						dPadButtons[d * 4 + 3] = (y >= 5 && y <= 7);
					}
				}

				// --------------------------------------------------------
				// MAP: D-PAD
				// --------------------------------------------------------

				success = SettingsConverter.TryParseIndexAndType(padSetting.DPad, out index, out type);
				// If POV index is mapped to the D-PAD
				if (success && index > 0 && type == SettingType.DPad)
				{
					var dPadIndex = index - 1;
					if (dPadButtons[dPadIndex * 4 + 0])
						gp.Buttons |= GamepadButtonFlags.DPadUp;
					if (dPadButtons[dPadIndex * 4 + 1])
						gp.Buttons |= GamepadButtonFlags.DPadRight;
					if (dPadButtons[dPadIndex * 4 + 2])
						gp.Buttons |= GamepadButtonFlags.DPadDown;
					if (dPadButtons[dPadIndex * 4 + 3])
						gp.Buttons |= GamepadButtonFlags.DPadLeft;
				}

				// --------------------------------------------------------
				// MAP: Buttons
				// --------------------------------------------------------

				var buttonMaps = padSetting.ButtonMaps;

				foreach (var map in buttonMaps)
				{
					// If not mapped then continue.
					if (map.Index == 0)
						continue;

					// If source is simple button then...
					if (map.Type == SettingType.Button)
					{
						// If mapped index is in range then...
						if (map.Index < diState.Buttons.Length)
						{
							// If button is pressed then Enable button on XInput state.
							if (diState.Buttons[map.Index - 1])
								gp.Buttons |= map.Flag;
						}
						continue;
					}

					// If source is D-PAD button converted from POV.
					if (map.Type == SettingType.DPadButton)
					{
						// If mapped index is in range then...
						if (map.Index < dPadButtons.Length)
						{
							// If button is pressed then Enable button on XInput state.
							if (dPadButtons[map.Index - 1])
								gp.Buttons |= map.Flag;
						}
						continue;
					}

					// If source is not axis and not slider then continue.
					if (!map.IsAxis && !map.IsSlider)
						continue;

					int[] values = map.IsAxis
						? diState.Axis
						: diState.Sliders;

					// If index is out of range then...
					if (map.Index > values.Length)
						continue;

					// Get value.
					var v = values[map.Index - 1];

					// Short bounds.
					int min = -32768;
					int max = 32767;

					// Axis to Button DeadZone.
					int deadZone = map.DeadZone;
					int diValue;
					if (map.IsHalf)
					{
						diValue = map.IsInverted ? -1 - v : v;
					}
					else
					{
						diValue = map.IsInverted ? max - v : v - min;
						deadZone = deadZone * 2;
					}
					if (diValue > deadZone)
					{
						gp.Buttons |= map.Flag;
					}
					setting.XiState = gp;
				}

				// --------------------------------------------------------
				// MAP: Triggers
				// --------------------------------------------------------

				//        [  32768 steps | 32768 steps ]
				// DInput [      0 32767 | 32768 65535 ] 
				// XInput [ -32768    -1 |     0 32767 ]

				// From Button:    OFF   |     ON
				// From Axis  :      0 - D - 65535 (D - DeadZone)
				//   To Button:    OFF   |     ON

				// From  IAxis:  65535   -      0    x = uint.Max - x;
				// From  HAxis:  32768   -  65535    map only if x >  32768; x = (x - 32768) * 2 + 1;
				// From IHAxis:  32767   -      0    map only if x <= 32767; x = (32767 - x) * 2 + 1;
				// From   Axis:      0   -  65535    x = x;
				//   To Triger:      0   -    255    scale: 255
				//   To   Axis: -32768   -  32767    shift: -32768
			}
			var ev = StatesUpdated;
			if (ev != null)
				ev(this, new EventArgs());
		}
	}
}
