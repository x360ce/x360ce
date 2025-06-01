#if NETCOREAPP // .NET Core
#elif NETSTANDARD // .NET Standard
#else // .NET Framework
using System;
using System.Diagnostics;

namespace JocysCom.ClassLibrary
{

	/// <summary>
	/// Provides data for the <see cref="HiResDateTime.Reset"/> event, indicating how far the high resolution clock was adjusted due to system clock changes.
	/// </summary>
	public class HiResDateTimeEvetnArgs : EventArgs
	{
		public HiResDateTimeEvetnArgs(TimeSpan adjustedBy)
		{
			AdjustedBy = adjustedBy;
		}
		/// <summary>
		/// TimeSpan representing how far the clock was adjusted, in seconds.
		/// </summary>
		public TimeSpan AdjustedBy { get; set; }
	}

	/// <summary>
	/// Provides monotonically increasing high-resolution timestamps synchronized with system time.
	/// Raises <see cref="Reset"/> when the system clock changes beyond the allowable threshold.
	/// </summary>
	public class HiResDateTime
	{

		/// <summary>High resolution stop timer.</summary>
		Stopwatch _stopWatch;
		/// <summary>Stop watch start time</summary>
		DateTime _startTime;
		/// <summary>Previous time used to </summary>
		long _prevTimeMs;
		/// <summary>Lock which will be used to prevent return of same time values.</summary>
		object valueLock = new object();
		/// <summary>Occurs when system time changes and adjustment in small steps is not possible.</summary>
		public event EventHandler<HiResDateTimeEvetnArgs> Reset;
		/// <summary>
		/// Specifies maximum number of seconds between high resolution time and system time when slow synchronization won't be used.
		/// </summary>
		int slowAdjustmentThreshhold = 10;

		static object _currentLock = new object();
		static HiResDateTime _Current;
		/// <summary>
		/// Gets a singleton instance providing high-resolution timestamp generation.
		/// </summary>
		public static HiResDateTime Current { get { lock (_currentLock) { return _Current = _Current ?? new HiResDateTime(); } } }

		/// <summary>
		/// Gets the current local time based on the high-resolution clock.
		/// </summary>
		public DateTime Now { get { return UtcNow.ToLocalTime(); } }

		/// <summary>
		/// Gets the current UTC time based on a monotonic high-resolution timer resynchronized with <see cref="DateTime.UtcNow"/>.
		/// Ensures unique millisecond values by spinning until the timestamp advances.
		/// </summary>
		public DateTime UtcNow
		{
			get
			{
				lock (valueLock)
				{
					// Get current system time.
					var now = DateTime.UtcNow;
					TimeSpan elapsed;
					// If values are not set yet then...
					if (_stopWatch is null)
					{
						elapsed = new TimeSpan();
						_stopWatch = Stopwatch.StartNew();
						_startTime = now;
						// Subscribe to time changed event.
						Microsoft.Win32.SystemEvents.TimeChanged += new EventHandler(SystemEvents_TimeChanged);
					}
					else
					{
						elapsed = _stopWatch.Elapsed;
					}
					// Make sure that value is unique.
					while (true)
					{
						CheckReset();
						// Get current hi resolution time.
						var hiResTime = _startTime.Add(_stopWatch.Elapsed);
						// Get milliseconds passed
						var msPassed = (hiResTime.Ticks / TimeSpan.TicksPerMillisecond);
						// If returned value is different from previous value then.
						if (msPassed != _prevTimeMs)
						{
							_prevTimeMs = msPassed;
							return hiResTime;
						}
					}
				}
			}
		}

		void CheckReset()
		{
			if (_stopWatch is null) return;
			// Get current system time.
			var now = DateTime.UtcNow;
			// Get current hi resolution time.
			var hiResTime = _startTime.Add(_stopWatch.Elapsed);
			// Get difference between system time and current high resolution time.
			var desync = now.Subtract(hiResTime);
			// Get time difference in seconds.
			var desyncSeconds = Math.Abs(desync.TotalSeconds);
			// If difference is too big then...
			if (desyncSeconds > slowAdjustmentThreshhold)
			{
				// Reset all values.
				_stopWatch.Restart();
				_startTime = now;
				var e2 = new HiResDateTimeEvetnArgs(desync);
				if (Reset != null) Reset(null, e2);
			}
		}

		void SystemEvents_TimeChanged(object sender, EventArgs e)
		{
			// The system time was changed.
			lock (valueLock)
			{
				CheckReset();
			}
		}

	}
}
#endif