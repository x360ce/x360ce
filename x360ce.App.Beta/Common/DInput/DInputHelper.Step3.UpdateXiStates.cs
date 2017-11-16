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
					// --------------------------------------------------------
					// Target: Button (DPad).
					// --------------------------------------------------------
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
								// --------------------------------------------------------
								// Target: Button.
								// --------------------------------------------------------
								if (map.Target == TargetType.Button)
									gp.Buttons |= map.ButtonFlag;
								// --------------------------------------------------------
								// Target: Trigger.
								// --------------------------------------------------------
								else if (map.Target == TargetType.LeftTrigger)
									gp.LeftTrigger = byte.MaxValue;
								else if (map.Target == TargetType.RightTrigger)
									gp.RightTrigger = byte.MaxValue;
								// --------------------------------------------------------
								// Target: Thumb.
								// --------------------------------------------------------
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
					// MAP Source: D-PAD button converted from POV.
					// --------------------------------------------------------
					else if (map.Type == SettingType.DPadButton)
					{
						// If mapped index is in range then...
						if (map.Index < dPadButtons.Length)
						{
							var pressed = dPadButtons[map.Index - 1];
							if (pressed)
							{
								// --------------------------------------------------------
								// Target: Button.
								// --------------------------------------------------------
								if (map.Target == TargetType.Button)
									gp.Buttons |= map.ButtonFlag;
								// --------------------------------------------------------
								// Target: Trigger.
								// --------------------------------------------------------
								else if (map.Target == TargetType.LeftTrigger)
									gp.LeftTrigger = byte.MaxValue;
								else if (map.Target == TargetType.RightTrigger)
									gp.RightTrigger = byte.MaxValue;
								// --------------------------------------------------------
								// Target: Thumb.
								// --------------------------------------------------------
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
					// MAP Source: Axis or Slider.
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

						// Destination range.
						var min = short.MinValue; // -32768;
						var max = short.MaxValue; //  32767;

						// If value is inverted (I) then...
						if (map.IsInverted && !map.IsHalf)
						{
							// Convert [0;65535] range to [65535;0] range.
							v = (ushort)(ushort.MaxValue - v);
						}
						// If half value (H) then...
						else if (!map.IsInverted && map.IsHalf)
						{
							// If value is in [32768;65535] range then...
							v = (v > max)
								// Convert [32768;65535] range to [0;65535] range.
								? (ushort)ConvertRange(max + 1, ushort.MaxValue, ushort.MinValue, ushort.MaxValue, v)
								: (ushort)0;
						}
						// If inverted half value (IH) then...
						else if (map.IsInverted && map.IsHalf)
						{
							// If value is in [0;32767] range then...
							v = (v <= max)
								// Convert [32767;0] range to [0;65535] range.
								? (ushort)ConvertRange(max, 0, ushort.MinValue, ushort.MaxValue, v)
								: (ushort)0;
						}

						//var deadZone = map.DeadZone;
						// If full range then double deadzone.
						//if (!map.IsHalf)
						//	deadZone = map.DeadZone * 2;

						// --------------------------------------------------------
						// Target: Button.
						// --------------------------------------------------------
						if (map.Target == TargetType.Button)
						{
							// If axis reached beyond dead zone then...
							if (v > map.DeadZone)
							{
								gp.Buttons |= map.ButtonFlag;
							}
						}
						// --------------------------------------------------------
						// Target: Trigger.
						// --------------------------------------------------------
						else if (TargetType.Triggers.HasFlag(map.Target))
						{
							// Convert range from ushort (0-65535) to byte (0-255).
							var value = (byte)ConvertRange(ushort.MinValue, ushort.MaxValue, byte.MinValue, byte.MaxValue, v);
							// Apply dead zone range (0 to 255).
							value = (byte)DeadZone(value, 0, byte.MaxValue, map.DeadZone, byte.MaxValue);
							if (map.Target == TargetType.LeftTrigger)
								gp.LeftTrigger = value;
							if (map.Target == TargetType.RightTrigger)
								gp.RightTrigger = value;
						}
						// --------------------------------------------------------
						// Target: Thumb.
						// --------------------------------------------------------
						else if (TargetType.Thumbs.HasFlag(map.Target))
						{

							// Convert DInput range (ushort[0;65535]) to XInput range (ushort[-32768;32767]).
							var xInput = ConvertRange(ushort.MinValue, ushort.MaxValue, min, max, v);

							// If axis should be inverted, convert [-32768;32767] -> [32767;-32768]
							//if (map.IsInverted)
							//	xInput = -1 - xInput;

							// NEGATIVE START:
							// The following sections expect xInput values in range [0;32767]
							// So, convert to positive: [-32768;-1] -> [32767;0]
							bool negative = xInput < 0;
							if (negative)
								xInput = -1 - xInput;

							// If deadzone value is set then...
							if (map.DeadZone > 0)
							{
								// if value is inside deadzone then...
								if (map.DeadZone <= xInput)
								{
									// Reset to minimum value.
									xInput = 0;
								}
								else
								{
									// DeadZone is applied on source axis. Destination thumb will have full range.
									// Convert to new range: [deadZone;32767] => [0;32767];
									xInput = ConvertRange(map.DeadZone, max, 0, max, xInput);
								}
							}

							// If anti-deadzone value is set then...
							if (map.AntiDeadZone > 0)
							{
								if (xInput > 0)
								{
									// AntiDeadzone is applied to destination thumb. Source will have full range.
									// Convert to new range: [0;32767] => [antiDeadZone;32767];
									xInput = ConvertRange(0, max, map.AntiDeadZone, max, xInput);
								}
							}

							// If linear value is set then...
							if (map.Linear != 0 && xInput > 0)
							{
								// [antiDeadZone;32767] => [0;32767];
								float xInputF = ConvertRange(map.AntiDeadZone, max, 0, max, xInput);
								float linearF = (float)map.Linear / 100f;
								xInputF = ConvertToFloat((short)xInputF);
								float x = -xInputF;
								if (linearF < 0f) x = 1f + x;
								float sv = (float)Math.Sqrt(1f - x * x);
								if (linearF < 0f) sv = 1f - sv;
								xInputF = xInputF + (2f - sv - xInputF - 1f) * Math.Abs(linearF);
								xInput = ConvertToShort(xInputF);
								// [0;32767] => [antiDeadZone;32767];
								xInput = ConvertRange(0, max, map.AntiDeadZone, max, xInput);
							}

							// NEGATIVE END:
							// If originally negative, convert back: [32767;0] -> [-32768;-1]
							if (negative)
								xInput = -1 - xInput;

							// Make sure values are in range.
							var thumbValue = (short)Clamp(xInput, min, max);

							if (map.Target == TargetType.LeftThumbX)
								gp.LeftThumbX = thumbValue;
							if (map.Target == TargetType.LeftThumbY)
								gp.LeftThumbY = thumbValue;
							if (map.Target == TargetType.RightThumbX)
								gp.RightThumbX = thumbValue;
							if (map.Target == TargetType.RightThumbY)
								gp.RightThumbY = thumbValue;
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

		int Clamp(int val, int min, int max)
		{
			if (val < min) return min;
			if (val > max) return max;
			return val;
		}
	}
}
