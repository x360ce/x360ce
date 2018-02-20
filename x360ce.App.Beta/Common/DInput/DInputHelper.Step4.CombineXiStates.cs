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

		public State[] CombinedXiStates;
		public bool[] CombinedXiConencted;
		public int PacketNumber;

		void CombineXiStates()
		{
			for (int m = 0; m < 4; m++)
			{
				// Get all mapped devices.
				var states = SettingsManager.Settings.Items
				   .Where(x => x.MapTo == m + 1)
				   .Select(x => x.XiState)
				   .ToArray();
				var gp = new Gamepad();
				if (states.Length > 0)
				{
					// Combine buttons.
					foreach (var state in states)
					{
						gp.Buttons |= state.Buttons;
					}
					// Apply maximun on triggers.
					gp.LeftTrigger = states.Max(x => x.LeftTrigger);
					gp.RightTrigger = states.Max(x => x.RightTrigger);
					// Apply differenceto thumbs:
					// 1) Players, pushing thumbs to oposite sides, will cancel each other.
					// 2) Player have full range of the thumb axis if thumb of the other player sits idle in the middle.
					gp.LeftThumbX = CombineAxis(states.Select(x => x.LeftThumbX));
					gp.LeftThumbY = CombineAxis(states.Select(x => x.LeftThumbY));
					gp.RightThumbX = CombineAxis(states.Select(x => x.RightThumbX));
					gp.RightThumbY = CombineAxis(states.Select(x => x.RightThumbY));
				}
				var combinedState = new State();
				if (PacketNumber == int.MaxValue)
					PacketNumber = 0;
				PacketNumber++;
				combinedState.PacketNumber = PacketNumber;
				combinedState.Gamepad = gp;
				CombinedXiStates[m] = combinedState;
				CombinedXiConencted[m] = states.Length > 0;
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
