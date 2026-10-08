using SharpDX.XInput;
using x360ce.Engine;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{
		/// <summary>The D-Pad buttons worked out from a row's POVs: four for each of the four POVs a state holds.</summary>
		/// <remarks>Reserved here and cleared for each row, rather than made for every row on every pass.</remarks>
		readonly bool[] _dPadButtons = new bool[4 * 4];

		/// <summary>
		/// Convert DiStates to XInput states.
		/// </summary>
		/// <param name="routing">The rows to convert this pass: the current game's, on a tab and switched on.</param>
		void UpdateXiStates(DeviceRouting routing)
		{
			// The same rows the controllers combine, so no row is combined that was not converted.
			var settings = routing.Rows;
			// Each row's device, mappings and D-Pad, found when the routing was built. Found here, the devices list
			// and the stored settings list would be copied for every row on every pass.
			var devices = routing.RowDevices;
			var rowMaps = routing.RowMaps;
			var dPads = routing.RowDPads;
			var dPadButtons = _dPadButtons;
			for (int i = 0; i < settings.Length; i++)
			{
				var setting = settings[i];
				// Create GamePad to map to.
				var gp = new Gamepad();
				// Assing state with default values. Set before anything can end the row's turn, so a row
				// whose device is gone reaches its controller as nothing rather than as what it last held.
				setting.XiState = gp;
				// Buttons an axis or a slider held on the previous pass, which decide where each lets go.
				// Cleared here, so a device that goes offline or loses its settings lets go of them all.
				var wasAxisButtons = setting.AxisButtons;
				setting.AxisButtons = GamepadButtonFlags.None;
				var axisButtons = GamepadButtonFlags.None;
				var ud = devices[i];
				// If device was not found then continue.
				if (ud == null)
					continue;
				// If device Direct Input state failed then...
				if (ud.JoState == null)
				{
					// A Raw Input device has no DirectInput state. It is converted unless the hub has lost it.
					if (ud.InputSourceType != (int)InputSourceType.RawInput || ud.RawInputMissing)
						continue;
				}
				// If device is offline then continue.
				if (!ud.IsOnline)
					continue;
				//// If device is not set or test device then...
				//var device = ud.Device;
				//if (device == null)
				//	// Continue loop.
				//	continue;
				// All mapped items, as the routing holds them: none when the row's settings are not stored.
				var maps = rowMaps[i];
				// If setting was not found then continue.
				if (maps == null)
					continue;
				var diState = ud.SourceState;
				// If custom directInput state is not available then continue.
				if (diState == null)
					continue;
				int index;

				// Contains
				//var gamepadUpdates = new List<KeyValue<GamepadKeyCode, int?>>();

				// --------------------------------------------------------
				// Convert DInput POV Hat value to D-PAD buttons.
				// --------------------------------------------------------

				// Four buttons for each POV, sixteen in all, cleared for this row.
				System.Array.Clear(dPadButtons, 0, dPadButtons.Length);
				// Loop trough D-Pad button states.
				for (int p = 0; p < diState.Povs.Length; ++p)
				{
					// Get degree value from the POV.
					int povdeg = diState.Povs[p];
					// If POV is pressed into one of the directions then up, right, down and left, a diagonal pressing two.
					if (povdeg >= 0)
					{
						for (var d = 0; d < 4; d++)
							dPadButtons[p * 4 + d] = ConvertHelper.IsPovDirectionPressed(povdeg, d);
					}
				}

				// --------------------------------------------------------
				// MAP: D-PAD
				// --------------------------------------------------------

				// The POV the D-Pad is mapped to, counted from one, read from the settings when the routing was built.
				index = dPads[i];
				// If POV index is mapped to the D-PAD. The index counts from one, and a POV number above 4 is rejected.
				if (index > 0 && index <= diState.Povs.Length)
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

				foreach (var map in maps)
				{
					// A row driven by a formula names no single control, so it is worked out here and
					// the rest of this loop, which is entirely about one control, is skipped.
					if (map.Expression != null)
					{
						ApplyExpression(map, diState, ref gp);
						continue;
					}
					// If not mapped then continue.
					if (map.Index == 0)
						continue;

					// --------------------------------------------------------
					// MAP Source: Button
					// --------------------------------------------------------
					if (map.IsButton)
					{
						// An inverted button (IButton) counts as pressed while it is released. The
						// inversion is applied here, once, so every destination below receives a plain press.
						if (ConvertHelper.IsButtonPressed(diState.Buttons, map.Index, map.IsInverted))
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
								gp.LeftThumbX = map.AxisValue.HasValue ? map.AxisValue.Value : short.MaxValue;
							else if (map.Target == TargetType.LeftThumbY)
								gp.LeftThumbY = map.AxisValue.HasValue ? map.AxisValue.Value : short.MaxValue;
							else if (map.Target == TargetType.RightThumbX)
								gp.RightThumbX = map.AxisValue.HasValue ? map.AxisValue.Value : short.MaxValue;
							else if (map.Target == TargetType.RightThumbY)
								gp.RightThumbY = map.AxisValue.HasValue ? map.AxisValue.Value : short.MaxValue;
						}
					}
					// --------------------------------------------------------
					// MAP Source: D-PAD button converted from POV.
					// --------------------------------------------------------
					else if (map.Type == MapType.POV)
					{
						// --------------------------------------------------------
						// Target: POV.
						// --------------------------------------------------------
						if (map.Target == TargetType.Button)
						{
							//gp.Buttons |= map.ButtonFlag;
						}
					}
					// --------------------------------------------------------
					// MAP Source: D-PAD button converted from POV.
					// --------------------------------------------------------
					else if (map.Type == MapType.DPOVButton)
					{
						// If mapped index is in range then... It counts from one, so the last is the length.
						if (map.Index <= dPadButtons.Length)
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
									gp.LeftThumbX = map.AxisValue.HasValue
										? map.IsInverted ? (short)0 : map.AxisValue.Value
										: map.IsInverted ? short.MinValue : short.MaxValue;
								else if (map.Target == TargetType.LeftThumbY)
									gp.LeftThumbY = map.AxisValue.HasValue
										? map.IsInverted ? (short)0 : map.AxisValue.Value
										: map.IsInverted ? short.MinValue : short.MaxValue;
								else if (map.Target == TargetType.RightThumbX)
									gp.RightThumbX = map.AxisValue.HasValue
										? map.IsInverted ? (short)0 : map.AxisValue.Value
										: map.IsInverted ? short.MinValue : short.MaxValue;
								else if (map.Target == TargetType.RightThumbY)
									gp.RightThumbY = map.AxisValue.HasValue
										? map.IsInverted ? (short)0 : map.AxisValue.Value
										: map.IsInverted ? short.MinValue : short.MaxValue;
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
						// A control that reports movement rests in the middle. A trigger or a button reads only the half past
						// it, as with H, so at rest it is released; a stick reads it whole and rests centred.
						var relative = ((map.IsAxis ? ud.DiRelativeAxisMask : ud.DiRelativeSliderMask) & (1 << (map.Index - 1))) != 0;
						var half = map.IsHalf || relative;

						// Destination range.
						//var min = short.MinValue; // -32768;
						//var max = short.MaxValue; //  32767;

						/// If half value (H) then...
						//else if (!map.IsInverted && map.IsHalf)
						//{
						//	// If value is in [32768;65535] range then...
						//	v = (v > max)
						//		// Convert [32768;65535] range to [0;65535] range.
						//		? (ushort)ConvertHelper.ConvertRange(max + 1, ushort.MaxValue, ushort.MinValue, ushort.MaxValue, v)
						//		: (ushort)0;
						//}
						// If inverted half value (IH) then...
						//else if (map.IsInverted && map.IsHalf)
						//{
						//	// If value is in [0;32767] range then...
						//	v = (v <= max)
						//		// Convert [32767;0] range to [0;65535] range.
						//		? (ushort)ConvertHelper.ConvertRange(max, 0, ushort.MinValue, ushort.MaxValue, v)
						//		: (ushort)0;
						//}



						// --------------------------------------------------------
						// Target: Button.
						// --------------------------------------------------------
						if (map.Target == TargetType.Button)
						{
							// Pressed past the press point, released only a little below it, so a reading
							// resting on the press point does not press and release the button every pass.
							var wasPressed = (wasAxisButtons & map.ButtonFlag) != GamepadButtonFlags.None;
							if (ConvertHelper.IsAxisButtonPressed(v, map.IsInverted, half, map.DeadZone, wasPressed))
							{
								gp.Buttons |= map.ButtonFlag;
								axisButtons |= map.ButtonFlag;
							}
						}
						// --------------------------------------------------------
						// Target: Trigger.
						// --------------------------------------------------------
						else if (map.Target == TargetType.LeftTrigger || map.Target == TargetType.RightTrigger)
						{
							var triggerValue = (byte)ConvertHelper.GetThumbValue(v, map.DeadZone, map.AntiDeadZone, map.Linear, map.IsInverted, half, false);
							if (map.Target == TargetType.LeftTrigger)
								gp.LeftTrigger = triggerValue;
							if (map.Target == TargetType.RightTrigger)
								gp.RightTrigger = triggerValue;
						}
						// --------------------------------------------------------
						// Target: Thumb.
						// --------------------------------------------------------
						else if (map.Target != TargetType.None)
						{
							var thumbValue = (short)ConvertHelper.GetThumbValue(v, map.DeadZone, map.AntiDeadZone, map.Linear, map.IsInverted, map.IsHalf);
							// Set negative center value (-1) to 0.
							if (thumbValue == -1)
								thumbValue = 0;
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
				setting.AxisButtons = axisButtons;

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
		}

		/// <summary>
		/// Buffer for a formula's source values, reused rather than allocated on every poll.
		/// </summary>
		/// <remarks>
		/// This runs for every mapped row of every controller, up to a thousand times a second. A
		/// fresh array each time would hand the collector a steady stream of rubbish to clear up, and
		/// a collection at the wrong moment is a stutter a player feels.
		/// </remarks>
		readonly float[] _expressionValues = new float[MapExpression.MaxReferences];

		/// <summary>
		/// Works out a formula against the controller as it is now, and puts the answer where the row
		/// points.
		/// </summary>
		/// <remarks>
		/// A formula answers in plain units: -1 to 1 for something that rests in the middle, 0 to 1
		/// for something that rests at one end. Those are turned into what the destination expects
		/// here, so the same formula can drive a stick, a trigger or a button and mean the same thing
		/// in each.
		///
		/// A row driven by a formula ignores its dead zone, anti dead zone and sensitivity. Those
		/// three shape a value on the way through, and so does the formula; applying both would give
		/// a result neither of them describes. Switching a row over writes the settings out as part
		/// of the formula, so nothing is lost by them no longer being read.
		/// </remarks>
		void ApplyExpression(Map map, SourceState diState, ref Gamepad gp)
		{
			// A stick is read from the middle, a trigger and a button from one end, exactly as the
			// ordinary mapping path already does through GetThumbValue's own thumb flag.
			var isThumb = map.Target == TargetType.LeftThumbX || map.Target == TargetType.LeftThumbY
				|| map.Target == TargetType.RightThumbX || map.Target == TargetType.RightThumbY;
			float value;
			if (!MapExpressionUnits.TryEvaluate(map.Expression, diState, isThumb, _expressionValues, out value))
				return;
			switch (map.Target)
			{
				case TargetType.LeftTrigger:
					gp.LeftTrigger = MapExpressionUnits.ToTrigger(value);
					break;
				case TargetType.RightTrigger:
					gp.RightTrigger = MapExpressionUnits.ToTrigger(value);
					break;
				case TargetType.LeftThumbX:
					gp.LeftThumbX = MapExpressionUnits.ToThumb(value);
					break;
				case TargetType.LeftThumbY:
					gp.LeftThumbY = MapExpressionUnits.ToThumb(value);
					break;
				case TargetType.RightThumbX:
					gp.RightThumbX = MapExpressionUnits.ToThumb(value);
					break;
				case TargetType.RightThumbY:
					gp.RightThumbY = MapExpressionUnits.ToThumb(value);
					break;
				case TargetType.Button:
					if (MapExpressionUnits.IsPressed(value))
						gp.Buttons |= map.ButtonFlag;
					break;
			}
		}

	}
}
