using System;
using System.Diagnostics;

namespace JocysCom.ClassLibrary
{

	public class HiResDateTimeEvetnArgs : EventArgs
	{
		public HiResDateTimeEvetnArgs(TimeSpan adjustedBy)
		{
			AdjustedBy = adjustedBy;
		}
		/// <summary>
		/// Time in seconds.
		/// </summary>
		public TimeSpan AdjustedBy { get; set; }
	}

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
		public static HiResDateTime Current { get { lock (_currentLock) { return _Current = _Current ?? new HiResDateTime(); } } }

		public DateTime Now { get { return UtcNow.ToLocalTime(); } }

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
					if (_stopWatch == null)
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
			if (_stopWatch == null) return;
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
