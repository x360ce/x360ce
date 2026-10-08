using SharpDX.DirectInput;
using System;

namespace x360ce.Engine
{
	/// <summary>
	/// What DirectInput kept for a device since the read before: which buttons were pressed and which let go, where each
	/// hat went, and how far each axis and slider went either way.
	/// </summary>
	/// <remarks>
	/// Read after the device's state, so a change the state has not caught yet is here, and <see cref="ShowIn"/> shows
	/// for one pass a change the state, reading as it did the pass before, missed. Made once a device; filled and read by
	/// the input thread only, and doing so makes nothing.
	/// </remarks>
	public sealed class DirectInputChanges
	{
		/// <summary>The buttons pressed, and let go of, by button.</summary>
		public readonly bool[] Presses = new bool[128];
		public readonly bool[] Releases = new bool[128];

		/// <summary>Each hat's latest kept value, whether it moved at all, and the latest kept value before that differs from it.</summary>
		public readonly int[] PovLast = new int[4];
		public readonly bool[] PovMoved = new bool[4];
		public readonly int[] PovOther = new int[4];
		public readonly bool[] PovHasOther = new bool[4];

		/// <summary>The lowest and highest kept value of X, Y, Z, X Rotation, Y Rotation, Z Rotation and the two sliders, in the order of their offsets in DirectInput's joystick state.</summary>
		public readonly int[] Low = new int[8];
		public readonly int[] High = new int[8];
		public readonly bool[] Moved = new bool[8];

		/// <summary>True when the last read kept anything. A device kept nothing on most passes, which then cost no more than this.</summary>
		public bool Any { get; private set; }

		/// <summary>Forgets every change, as for a device that kept none.</summary>
		public void Clear()
		{
			if (!Any)
				return;
			Array.Clear(Presses, 0, Presses.Length);
			Array.Clear(Releases, 0, Releases.Length);
			Array.Clear(PovMoved, 0, PovMoved.Length);
			Array.Clear(PovHasOther, 0, PovHasOther.Length);
			Array.Clear(Moved, 0, Moved.Length);
			Any = false;
		}

		/// <summary>Takes in the DIDEVICEOBJECTDATA entries DirectInput gave, in the order the changes happened.</summary>
		/// <param name="data">The entries: each starts with the control's offset in DirectInput's joystick state, then its value.</param>
		/// <param name="entrySize">The size of one entry, which depends on the process's pointer size.</param>
		public void Load(byte[] data, int count, int entrySize)
		{
			Clear();
			if (count <= 0)
				return;
			Any = true;
			for (var i = 0; i < count; i++)
			{
				var offset = BitConverter.ToInt32(data, i * entrySize);
				var value = BitConverter.ToInt32(data, i * entrySize + 4);
				var button = offset - (int)JoystickOffset.Buttons0;
				if (button >= 0 && button < Presses.Length)
				{
					if ((value & 0x80) != 0)
						Presses[button] = true;
					else
						Releases[button] = true;
				}
				else if (offset >= (int)JoystickOffset.PointOfViewControllers0 && offset < (int)JoystickOffset.Buttons0)
				{
					var hat = (offset - (int)JoystickOffset.PointOfViewControllers0) / 4;
					if (!PovMoved[hat])
					{
						PovMoved[hat] = true;
						PovLast[hat] = value;
					}
					else if (value != PovLast[hat])
					{
						PovOther[hat] = PovLast[hat];
						PovHasOther[hat] = true;
						PovLast[hat] = value;
					}
				}
				else if (offset >= 0 && offset < (int)JoystickOffset.PointOfViewControllers0)
				{
					var slot = offset / 4;
					if (!Moved[slot])
					{
						Moved[slot] = true;
						Low[slot] = value;
						High[slot] = value;
					}
					else if (value < Low[slot])
						Low[slot] = value;
					else if (value > High[slot])
						High[slot] = value;
				}
			}
		}

		/// <summary>Shows for this pass a kept change that the state, reading as it did the pass before, missed: a button pressed and let go, a hat tapped, a stick flicked and back.</summary>
		/// <param name="axes">False for a device whose axes report movement rather than position, whose kept values are steps, not places.</param>
		/// <returns>How many controls the state alone missed.</returns>
		public int ShowIn(SourceState previous, SourceState state, bool axes)
		{
			if (!Any)
				return 0;
			var shown = 0;
			for (var b = 0; b < state.Buttons.Length; b++)
				if (state.Buttons[b] == previous.Buttons[b] && (state.Buttons[b] ? Releases[b] : Presses[b]))
				{
					state.Buttons[b] = !state.Buttons[b];
					shown++;
				}
			for (var h = 0; h < state.Povs.Length; h++)
			{
				if (!PovMoved[h] || state.Povs[h] != previous.Povs[h])
					continue;
				if (PovLast[h] != state.Povs[h])
				{
					state.Povs[h] = PovLast[h];
					shown++;
				}
				else if (PovHasOther[h])
				{
					state.Povs[h] = PovOther[h];
					shown++;
				}
			}
			if (!axes)
				return shown;
			for (var slot = 0; slot < Moved.Length; slot++)
			{
				if (!Moved[slot])
					continue;
				var values = slot < 6 ? state.Axis : state.Sliders;
				var i = slot < 6 ? slot : slot - 6;
				var from = slot < 6 ? previous.Axis[i] : previous.Sliders[i];
				var far = Math.Abs(Low[slot] - from) >= Math.Abs(High[slot] - from) ? Low[slot] : High[slot];
				if (Math.Abs(far - from) >= Math.Abs(values[i] - from) + SourceState.TurnShown)
				{
					values[i] = far;
					shown++;
				}
			}
			return shown;
		}
	}
}
