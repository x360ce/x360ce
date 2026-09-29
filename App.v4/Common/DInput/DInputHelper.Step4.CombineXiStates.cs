using SharpDX.XInput;
using System;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		public State[] CombinedXiStates;
		public bool[] CombinedXiConencted;
		public int PacketNumber;

		/// <summary>Gathers every mapped device onto the controller it is mapped to.</summary>
		/// <remarks>
		/// This is where a person's controller becomes controller one, two, three or four. Sending it
		/// to the wrong one, or to more than one, is invisible from inside the program and shows up only
		/// as a game answering a control nobody touched.
		///
		/// Only the rows converted this pass are gathered: the current game's, on a tab and switched on. Any
		/// other row still carries the state it had when it was last converted, and a button held then would
		/// stay held in the game.
		///
		/// The rows are read where they are, with no copy: this runs on every pass, and a copy would be
		/// handed to the collector each time.
		/// </remarks>
		/// <param name="routing">The rows each controller reads, for this pass.</param>
		public void CombineXiStates(DeviceRouting routing)
		{
			for (int m = 0; m < 4; m++)
			{
				// The rows converted this pass for this controller.
				var rows = routing.PadRows[m];
				var gp = new Gamepad();
				if (rows.Length > 0)
				{
					// The lowest and the highest value of each thumb axis, which decide how the thumbs combine.
					var low = rows[0].XiState;
					var high = low;
					for (int r = 0; r < rows.Length; r++)
					{
						var state = rows[r].XiState;
						// Combine buttons.
						gp.Buttons |= state.Buttons;
						// Apply maximum on triggers.
						gp.LeftTrigger = Math.Max(gp.LeftTrigger, state.LeftTrigger);
						gp.RightTrigger = Math.Max(gp.RightTrigger, state.RightTrigger);
						low.LeftThumbX = Math.Min(low.LeftThumbX, state.LeftThumbX);
						high.LeftThumbX = Math.Max(high.LeftThumbX, state.LeftThumbX);
						low.LeftThumbY = Math.Min(low.LeftThumbY, state.LeftThumbY);
						high.LeftThumbY = Math.Max(high.LeftThumbY, state.LeftThumbY);
						low.RightThumbX = Math.Min(low.RightThumbX, state.RightThumbX);
						high.RightThumbX = Math.Max(high.RightThumbX, state.RightThumbX);
						low.RightThumbY = Math.Min(low.RightThumbY, state.RightThumbY);
						high.RightThumbY = Math.Max(high.RightThumbY, state.RightThumbY);
					}
					// Apply difference to thumbs:
					// 1) Players, pushing thumbs to opposite sides, will cancel each other.
					// 2) Player have full range of the thumb axis if thumb of the other player sits idle in the middle.
					gp.LeftThumbX = CombineAxis(low.LeftThumbX, high.LeftThumbX);
					gp.LeftThumbY = CombineAxis(low.LeftThumbY, high.LeftThumbY);
					gp.RightThumbX = CombineAxis(low.RightThumbX, high.RightThumbX);
					gp.RightThumbY = CombineAxis(low.RightThumbY, high.RightThumbY);
				}
				var combinedState = new State();
				if (PacketNumber == int.MaxValue)
					PacketNumber = 0;
				PacketNumber++;
				combinedState.PacketNumber = PacketNumber;
				combinedState.Gamepad = gp;
				CombinedXiStates[m] = combinedState;
				CombinedXiConencted[m] = rows.Length > 0;
			}
		}

		/// <summary>One thumb axis of several devices, from the lowest and the highest value among them.</summary>
		static short CombineAxis(short min, short max)
		{
			// If both positive then return maximum.
			if (min > 0 && max > 0)
				return Math.Max(min, max);
			// If both negative then return minimum.
			if (min < 0 && max < 0)
				return Math.Min(min, max);
			// If on opposite sides then cancel each other.
			return (short)(min + max);
		}

	}
}
