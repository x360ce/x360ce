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
				//// If device is not set or test device then...
				//var device = ud.Device;
				//if (device == null)
				//	// Continue loop.
				//	continue;
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

				success = SettingsConverter.TryParseTextValue(padSetting.DPad, out type, out index);
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
				// MAP:
				// --------------------------------------------------------

				// Get all mapped items.
				var maps = padSetting.Maps;

				foreach (var map in maps)
				{
					// If not mapped then continue.
					if (map.Index == 0)
						continue;

					// --------------------------------------------------------
					// MAP Source: Button
					// --------------------------------------------------------
					if (map.IsButton)
					{
						// If mapped index is in range then...
						if (map.Index < diState.Buttons.Length)
						{
							var pressed = diState.Buttons[map.Index - 1];
							if (pressed)
							{
								if (map.Target == TargetType.Button)
									gp.Buttons |= map.ButtonFlag;
								else if (map.Target == TargetType.LeftTrigger)
									gp.LeftTrigger = byte.MaxValue;
								else if (map.Target == TargetType.RightTrigger)
									gp.RightTrigger = byte.MaxValue;
								else if (map.Target == TargetType.LeftThumbX)
									gp.LeftThumbX = map.IsInverted ? short.MinValue : short.MaxValue;
								else if (map.Target == TargetType.LeftThumbY)
									gp.LeftThumbY = map.IsInverted ? short.MinValue : short.MaxValue;
								else if (map.Target == TargetType.RightThumbX)
									gp.RightThumbX = map.IsInverted ? short.MinValue : short.MaxValue;
								else if (map.Target == TargetType.RightThumbY)
									gp.RightThumbY = map.IsInverted ? short.MinValue : short.MaxValue;
							}
						}
					}
					// --------------------------------------------------------
					// MAP Source: D-PAD button converted from POV
					// --------------------------------------------------------
					else if (map.Type == SettingType.DPadButton)
					{
						// If mapped index is in range then...
						if (map.Index < dPadButtons.Length)
						{
							var pressed = dPadButtons[map.Index - 1];
							if (pressed)
							{
								if (map.Target == TargetType.Button)
									gp.Buttons |= map.ButtonFlag;
								else if (map.Target == TargetType.LeftTrigger)
									gp.LeftTrigger = byte.MaxValue;
								else if (map.Target == TargetType.RightTrigger)
									gp.RightTrigger = byte.MaxValue;
								else if (map.Target == TargetType.LeftThumbX)
									gp.LeftThumbX = map.IsInverted ? short.MinValue : short.MaxValue;
								else if (map.Target == TargetType.LeftThumbY)
									gp.LeftThumbY = map.IsInverted ? short.MinValue : short.MaxValue;
								else if (map.Target == TargetType.RightThumbX)
									gp.RightThumbX = map.IsInverted ? short.MinValue : short.MaxValue;
								else if (map.Target == TargetType.RightThumbY)
									gp.RightThumbY = map.IsInverted ? short.MinValue : short.MaxValue;
							}
						}
					}
					// --------------------------------------------------------
					// MAP Source: Axis or Slider
					// --------------------------------------------------------
					else if (map.IsAxis || map.IsSlider)
					{
						// Get source value.
						int[] values = map.IsAxis
							? diState.Axis
							: diState.Sliders;

						// If index is out of range then...
						if (map.Index > values.Length)
							continue;

						// Get value.
						var v = (ushort)values[map.Index - 1];

						// If value is inverted (I) then...
						if (map.IsInverted && !map.IsHalf)
							v = (ushort)(ushort.MaxValue - v);
						// If half value (H) then...
						else if (!map.IsInverted && map.IsHalf && (v + short.MinValue) > 0)
							v = (ushort)((v + short.MinValue) * 2 + 1);
						// If inverted half value (IH) then...
						else if (!map.IsInverted && map.IsHalf && v <= short.MaxValue)
							v = (ushort)((short.MaxValue - v) * 2 + 1);

						var deadZone = map.DeadZone;

						// If full range then double deadzone.
						if (!map.IsHalf)
							deadZone = map.DeadZone * 2;

						// If target is button.
						if (map.Target == TargetType.Button)
						{
							// If axis reached beyond dead zone then...
							if (v > deadZone)
							{
								gp.Buttons |= map.ButtonFlag;
							}
						}
						// If target is Trigger.
						else if (TargetType.Triggers.HasFlag(map.Target))
						{
							// Scale ushort (0-65535) to byte (0-255).
							var value = (byte)ConvertRange(ushort.MinValue, ushort.MaxValue, byte.MinValue, byte.MaxValue, v);
							//value = (byte)DeadZone(value, 0, byte.MaxValue, map.DeadZone, byte.MaxValue);
							if (map.Target == TargetType.LeftTrigger)
								gp.LeftTrigger = value;
							if (map.Target == TargetType.RightTrigger)
								gp.RightTrigger = value;
						}
						// If target is Thumb.
						else if (TargetType.Thumbs.HasFlag(map.Target))
						{
							// Scale ushort (0-65535) to short (-32768 - 32767).
							var value = (short)ConvertRange(ushort.MinValue, ushort.MaxValue, short.MinValue, short.MaxValue, v);
							if (map.Target == TargetType.LeftThumbX)
								gp.LeftThumbX = value;
							if (map.Target == TargetType.LeftThumbY)
								gp.LeftThumbY = value;
							if (map.Target == TargetType.RightThumbX)
								gp.RightThumbX = value;
							if (map.Target == TargetType.RightThumbY)
								gp.RightThumbY = value;
						}
					}
				}
				setting.XiState = gp;

				//        [  32768 steps | 32768 steps ]
				// ushort [      0 32767 | 32768 65535 ] DInput
				//  short [ -32768    -1 |     0 32767 ] XInput

				//        [  128 steps | 128 steps ]
				//  byte  [    0   127 | 128   255 ]
				// sbyte  [ -128    -1 |   0   127 ]

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

		int DeadZone(int val, int min, int max, int lowerDZ, int upperDZ)
		{
			if (val < lowerDZ) return min;
			if (val > upperDZ) return max;
			return val;
		}
	}
}
