using System;
using System.Diagnostics;

namespace x360ce.Engine
{
	/// <summary>Counts how often something happens and gives the rate once a second.</summary>
	/// <remarks>
	/// The rate is the count divided by the time it was counted over, not the bare count. A loop
	/// that stops for three seconds and then runs once has counted one event; given as "1 a
	/// second" that reads as slow rather than stopped. Divided by the three seconds it reads 0,
	/// which is what happened.
	///
	/// A new rate is only given when an event arrives. While nothing arrives the last rate stays,
	/// so a reader that must tell a stopped loop from a running one has to watch it change.
	/// </remarks>
	public class RateCounter
	{
		long _windowStart;
		long _count;

		/// <summary>Events a second, measured over the last second or more that was counted.</summary>
		public int Rate { get; private set; }

		/// <summary>Counts one event, happening now.</summary>
		/// <returns>True when a second has passed since the last rate, so <see cref="Rate"/> holds a new one.</returns>
		public bool Tick()
		{
			return Tick(Stopwatch.GetTimestamp());
		}

		/// <summary>Counts one event, happening at the given <see cref="Stopwatch"/> timestamp.</summary>
		/// <returns>True when a second has passed since the last rate, so <see cref="Rate"/> holds a new one.</returns>
		public bool Tick(long now)
		{
			// The first event only starts the clock: counted, a loop running ten times a second
			// read eleven for its first second.
			if (_windowStart == 0)
			{
				_windowStart = now;
				return false;
			}
			_count++;
			var elapsed = now - _windowStart;
			if (elapsed <= Stopwatch.Frequency)
				return false;
			Rate = (int)Math.Round(_count * (double)Stopwatch.Frequency / elapsed);
			_count = 0;
			_windowStart = now;
			return true;
		}

		/// <summary>Starts counting again, as though nothing had been counted.</summary>
		public void Reset()
		{
			_windowStart = 0;
			_count = 0;
		}
	}
}
