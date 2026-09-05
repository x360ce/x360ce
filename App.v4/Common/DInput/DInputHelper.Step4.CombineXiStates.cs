using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		public State[] CombinedXiStates;
		public bool[] CombinedXiConencted;
		public int PacketNumber;

		private readonly List<Gamepad>[] _slotStates = new List<Gamepad>[4]
		{
			new List<Gamepad>(4),
			new List<Gamepad>(4),
			new List<Gamepad>(4),
			new List<Gamepad>(4)
		};

		/// <summary>Gathers every mapped device onto the controller it is mapped to.</summary>
		/// <remarks>
		/// This is where a person's controller becomes controller one, two, three or four. Sending it
		/// to the wrong one, or to more than one, is invisible from inside the program and shows up only
		/// as a game answering a control nobody touched.
		/// </remarks>
		public void CombineXiStates()
		{
			for (int i = 0; i < 4; i++)
				_slotStates[i].Clear();

			var allSettings = SettingsManager.GetUserSettingsSnapshot();
			for (int i = 0; i < allSettings.Length; i++)
			{
				var s = allSettings[i];
				if (s != null && s.MapTo >= 1 && s.MapTo <= 4)
				{
					_slotStates[s.MapTo - 1].Add(s.XiState);
				}
			}

			for (int m = 0; m < 4; m++)
			{
				var slotList = _slotStates[m];
				var count = slotList.Count;
				var gp = new Gamepad();

				if (count == 1)
				{
					gp = slotList[0];
				}
				else if (count > 1)
				{
					var s0 = slotList[0];
					gp.Buttons = s0.Buttons;
					byte maxLT = s0.LeftTrigger;
					byte maxRT = s0.RightTrigger;
					short minLX = s0.LeftThumbX, maxLX = s0.LeftThumbX;
					short minLY = s0.LeftThumbY, maxLY = s0.LeftThumbY;
					short minRX = s0.RightThumbX, maxRX = s0.RightThumbX;
					short minRY = s0.RightThumbY, maxRY = s0.RightThumbY;

					for (int i = 1; i < count; i++)
					{
						var s = slotList[i];
						gp.Buttons |= s.Buttons;
						if (s.LeftTrigger > maxLT) maxLT = s.LeftTrigger;
						if (s.RightTrigger > maxRT) maxRT = s.RightTrigger;
						if (s.LeftThumbX < minLX) minLX = s.LeftThumbX;
						if (s.LeftThumbX > maxLX) maxLX = s.LeftThumbX;
						if (s.LeftThumbY < minLY) minLY = s.LeftThumbY;
						if (s.LeftThumbY > maxLY) maxLY = s.LeftThumbY;
						if (s.RightThumbX < minRX) minRX = s.RightThumbX;
						if (s.RightThumbX > maxRX) maxRX = s.RightThumbX;
						if (s.RightThumbY < minRY) minRY = s.RightThumbY;
						if (s.RightThumbY > maxRY) maxRY = s.RightThumbY;
					}

					gp.LeftTrigger = maxLT;
					gp.RightTrigger = maxRT;
					gp.LeftThumbX = CombineAxis(minLX, maxLX);
					gp.LeftThumbY = CombineAxis(minLY, maxLY);
					gp.RightThumbX = CombineAxis(minRX, maxRX);
					gp.RightThumbY = CombineAxis(minRY, maxRY);
				}

				var combinedState = new State();
				if (PacketNumber == int.MaxValue)
					PacketNumber = 0;
				PacketNumber++;
				combinedState.PacketNumber = PacketNumber;
				combinedState.Gamepad = gp;
				CombinedXiStates[m] = combinedState;
				CombinedXiConencted[m] = count > 0;
			}
		}

		short CombineAxis(short min, short max)
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

		short CombineAxis(IEnumerable<short> values)
		{
			var min = values.Min();
			var max = values.Max();
			return CombineAxis(min, max);
		}

	}
}
