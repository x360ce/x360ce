using System;
using System.ComponentModel;
using System.Diagnostics;
using x360ce.Plugins;

namespace x360ce.Plugins.Samples
{
	/// <summary>A pretend controller that moves by itself, for trying x360ce without hardware.</summary>
	/// <remarks>
	/// The smallest useful plugin, and the template for a new one: one class, one file. It is embedded in x360ce.exe,
	/// so it is always there; built on its own it loads from the plugins folder like any other.
	/// </remarks>
	[Description("A pretend controller that moves by itself, so a mapping can be tried without hardware. "
		+ "In every six seconds it moves for four and rests for two. The even axes (X, Z and Y Rotation) swing like a sine "
		+ "from the middle to each end and back, reversing every other cycle; the odd axes (Y and X Rotation) run from one end "
		+ "to the other and back; the hat turns once round and back. One button is held at a time, a new one each second.")]
	public class TestController : IPluginDevice
	{
		/// <summary>The product of the Test Device this controller replaces, so its saved mappings keep working.</summary>
		static readonly Guid Product = new Guid("a10ef011-41ab-4460-8c45-c1ad2dbf7ede");

		const int Busy = 4000;
		const int Idle = 2000;
		const int Half = Busy / 2;

		readonly Stopwatch _watch = Stopwatch.StartNew();

		public string Name { get { return "Test Controller"; } }
		public Guid ProductGuid { get { return Product; } }
		public int AxisCount { get { return 5; } }
		public int SliderCount { get { return 0; } }
		public int PovCount { get { return 1; } }
		public int ButtonCount { get { return 10; } }

		public void Read(PluginState state)
		{
			var elapsed = _watch.ElapsedMilliseconds;
			var seconds = (int)(elapsed / 1000 % int.MaxValue);
			var ms = (int)(elapsed % int.MaxValue);
			// One button at a time, a new one each second.
			for (var i = 0; i < ButtonCount; i++)
				state.Buttons[i] = seconds % ButtonCount == i;
			// Moving for four seconds [0-3999], resting for two [4000-5999].
			var time = ms % (Busy + Idle);
			var invert = ms % ((Busy + Idle) * 2) > Busy + Idle;
			// The hat turns once round and back, starting from up.
			var degree = PluginState.PovReleased;
			if (time < Busy)
			{
				var value = (time + Half) % Busy;
				degree = value < Half
					? Range(0, Half - 1, 0, 35999, value)
					: Range(Half, Busy - 1, 35999, 0, value);
			}
			state.Povs[0] = degree;
			for (var i = 0; i < AxisCount; i++)
			{
				var isEven = i % 2 == 0;
				// At rest an even axis is in the middle and an odd one at its start.
				var position = isEven ? PluginState.Middle + 1 : 0;
				if (time < Busy)
				{
					if (isEven)
					{
						var sine = Math.Sin(time * 2 * Math.PI / Busy);
						if (invert)
							sine = -sine;
						position = (short)RangeF(-1f, 1f, short.MinValue, short.MaxValue, (float)sine) - short.MinValue;
					}
					else
					{
						position = time < Half
							? Range(0, Half - 1, 0, PluginState.Max, time)
							: Range(Half, Busy - 1, PluginState.Max, 0, time);
					}
				}
				state.Axes[i] = position;
			}
		}

		/// <summary>Maps a value from one range onto another, held inside the new range, and rounded.</summary>
		static int Range(int oldMin, int oldMax, int newMin, int newMax, int value)
		{
			return (int)Math.Round(RangeF(oldMin, oldMax, newMin, newMax, value), 0);
		}

		static float RangeF(float oldMin, float oldMax, float newMin, float newMax, float value)
		{
			var newValue = newMin + (value - oldMin) * ((newMax - newMin) / (oldMax - oldMin));
			var low = Math.Min(newMin, newMax);
			var high = Math.Max(newMin, newMax);
			return newValue < low ? low : newValue > high ? high : newValue;
		}
	}
}
