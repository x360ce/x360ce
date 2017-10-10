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

		public Gamepad[] CombinedXInputStates;

		void CombineXiStates()
		{
			for (int m = 0; m < 4; m++)
			{
				// Get all mapped devices.
				var states = SettingsManager.Settings.Items
				   .Where(x => x.MapTo == m + 1)
				   .Select(x => x.XiState)
				   .ToArray();
				var result = new Gamepad();
				// Combine buttons.
				foreach (var state in states)
				{
					result.Buttons |= state.Buttons;
				}
				// Apply maximun on triggers.
				result.LeftTrigger = states.Max(x => x.LeftTrigger);
				result.RightTrigger = states.Max(x => x.RightTrigger);
				// Apply differenceto thumbs:
				// 1) Players, pushing thumbs to oposite sides, will cancel each other.
				// 2) Player have full range of the thumb axis if thumb of the other player sits idle in the middle.
				result.LeftThumbX = CombineAxis(states.Select(x => x.LeftThumbX));
				result.LeftThumbY = CombineAxis(states.Select(x => x.LeftThumbY));
				result.RightThumbX = CombineAxis(states.Select(x => x.RightThumbX));
				result.RightThumbY = CombineAxis(states.Select(x => x.RightThumbY));
				CombinedXInputStates[m] = result;
			}
		}

		short CombineAxis(IEnumerable<short> values)
		{
			var min = values.Min();
			var max = values.Max();
			// If both positive then return maximum.
			if (min > 0 && max > 0)
				return Math.Max(min, max);
			// If both negative then return minimum.
			if (min < 0 && max < 0)
				return Math.Min(min, max);
			// If on oposite sides then cancel each other.
			return (short)(min + max);
		}

	}
}
