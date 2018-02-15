using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

namespace JocysCom.ClassLibrary
{

	/// <summary>
	/// Hi accuracy and performance timer.
	/// </summary>
	/// <remarks>
	/// Standard C# timers are highly inaccurate and slow. They would add 8ms to every call-back.
	/// Note: Callback will finish before timer schedules next run.
	/// </remarks>
	[DefaultProperty("Interval"), DefaultEvent("Elapsed")]
	public class HiResTimer: IDisposable
	{


		/// <summary>
		/// Initializes a new instance of the HiResTimer.
		/// </summary>
		public HiResTimer() { }
		/// <summary>
		/// Initializes a new instance of the HiResTimer.
		/// </summary>
		public HiResTimer(int interval)
		{
			if (interval <= 0)
				throw new ArgumentException("Invalid value", "interval");
			_Interval = interval;
		}

		/// <summary>
		/// Gets or sets a value indicating whether the Timer raises the Tick event each time the specified
		/// Interval has elapsed, when Enabled is set to true.
		/// </summary>
		[Category("Behavior"), DefaultValue(true)]
		public bool AutoReset
		{
			get { return _AutoReset; }
			set
			{
				if (_AutoReset == value)
					return;
				_AutoReset = value;
				UpdateTimer();
			}
		}
		bool _AutoReset = true;

		/// <summary>
		/// Gets or sets a value indicating whether the Timer is able
		/// to raise events at a defined interval.
		/// </summary>
		[Category("Behavior"), DefaultValue(false)]
		public bool Enabled
		{
			get { return _Enabled; }
			set
			{
				if (_Enabled == value)
					return;
				_Enabled = value;
				UpdateTimer();
			}
		}
		bool _Enabled;


		/// <summary>
		/// Gets or sets the interval on which to raise events.
		/// </summary>
		[Category("Behavior"), DefaultValue(100)]
		public int Interval
		{
			get { return _Interval; }
			set
			{
				if (_Interval == value)
					return;
				if (value <= 0)
					throw new ArgumentException("Invalid value", "interval");
				_Interval = value;
				UpdateTimer();
			}
		}
		int _Interval = 100;


		void UpdateTimer()
		{
			KillTimer();
			if (_Enabled)
				StartTimer(OnElapsed);
		}

		/// <summary>
		/// Occurs when the Interval has elapsed.
		/// </summary>
		[Category("Behavior")]
		public event ElapsedEventHandler Elapsed;

		/// <summary>
		/// Gets or sets the object used to marshal event-handler calls that are issued when
		/// an interval has elapsed.
		/// </summary>
		[Browsable(false), DefaultValue(null)]
		public ISynchronizeInvoke SynchronizingObject { get; set; }

		/// <summary>Starts the timing by setting 'Enabled' to 'true'.</summary>
		public void Start() { Enabled = true; }

		/// <summary>Stops the timing by setting 'Enabled' to 'false'.</summary>
		public void Stop() { Enabled = false; }

		void OnElapsed(uint uTimerID, uint uMsg, UIntPtr dwUser, UIntPtr dw1, UIntPtr dw2)
		{
			try
			{
				var ev = Elapsed;
				if (ev != null)
				{
					ElapsedEventArgs e = null;
					var so = SynchronizingObject;
					if (so != null && so.InvokeRequired)
						so.BeginInvoke(ev, new object[] { this, e });
					else
						ev(this, e);
				}
			}
			catch { }
		}

		#region Test

		Stopwatch stopwatch;

		int currentIndex;
		long[] marks = new long[0];
		double ticksToWait;

		// 1000 Hz - 1 ms
		//  500 Hz = 2 ms
		//  250 Hz = 4 ms
		//  125 Hz = 8 ms
		public void BeginTest()
		{
			currentIndex = 0;
			stopwatch = new Stopwatch();
			stopwatch.Restart();
			// Get hz.
			var hz = 1000 / Interval;
			// Run for two seconds.
			var maxMarks = hz * 2;
			Array.Resize(ref marks, (int)maxMarks);
			// Get amount of ticks to wait inbetween actions.
			ticksToWait = Stopwatch.Frequency * Interval / 1000;
			StartTimer(OnTestElapsed);
		}

		void OnTestElapsed(uint uTimerID, uint uMsg, UIntPtr dwUser, UIntPtr dw1, UIntPtr dw2)
		{
			marks[currentIndex] = stopwatch.ElapsedTicks;
			currentIndex++;
			if (currentIndex >= marks.Length)
			{
				stopwatch.Stop();
				double totalDiff = 0;
				double minDiff = 0;
				double maxDiff = 0;
				for (int i = 0; i < marks.Length - 1; i++)
				{
					// Get waiting time.
					var waited = marks[i + 1] - marks[i];
					// Get difference:
					// positive - waited too long.
					// negatice - waited not enougth.
					var diff = waited - ticksToWait;
					if (diff < minDiff)
						minDiff = diff;
					if (diff > maxDiff)
						maxDiff = diff;
					// Add to totals
					totalDiff += Math.Abs(diff);
				}
				var averageInaccuracyTicks = totalDiff / marks.Length - 1;
				// Ticks per second.
				var ms = (float)averageInaccuracyTicks * 1000f / (float)Stopwatch.Frequency;
				var msPc = (float)ms / (float)Interval;
				var msMin = (float)minDiff * 1000f / (float)Stopwatch.Frequency;
				var msMax = (float)maxDiff * 1000f / (float)Stopwatch.Frequency;
				var totalRuntimeTicks = marks[marks.Length - 1] - marks[0];
				var totalRuntimeMs = (float)totalRuntimeTicks * 1000f / (float)Stopwatch.Frequency;
				var sb = new StringBuilder();
				sb.AppendFormat("Ticks per Second = {0}\r\n", Stopwatch.Frequency);
				sb.AppendFormat("Timer Interval = {0}\r\n", Interval);
				sb.AppendFormat("Hz = {0}\r\n", 1000 / Interval);
				sb.AppendFormat("Mark Samples = {0}\r\n", marks.Length);
				sb.AppendFormat("AverageInaccuracyTicks = {0}\r\n", averageInaccuracyTicks);
				sb.AppendFormat("AverageInaccuracyMs = {0}\r\n", ms);
				sb.AppendFormat("AverageInaccuracyMs % = {0:0.000%}\r\n", msPc);
				sb.AppendFormat("AverageInaccuracyMsMin = {0}\r\n", msMin);
				sb.AppendFormat("AverageInaccuracyMsMax = {0}\r\n", msMax);
				sb.AppendFormat("Total run time ms = {0}\r\n", totalRuntimeMs);
				TestResults = sb.ToString();
				KillTimer();
				var ev = TestFinished;
				if (ev != null)
					ev(this, new EventArgs());
				return;
			}
		}

		public string TestResults;

		[Category("Behavior")]
		public event EventHandler TestFinished;

		#endregion

		#region IDisposable

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				Stop();
				Elapsed = null;
			}
		}

		#endregion


		#region  Hi Resolution timer.

		/// <summary>
		/// Delegate definition for the API callback
		/// </summary>
		internal delegate void HiResTimerCallback(uint uTimerID, uint uMsg, UIntPtr dwUser, UIntPtr dw1, UIntPtr dw2);

		internal class NativeMethods
		{

			//Lib API declarations
			[DllImport("Winmm.dll", CharSet = CharSet.Auto, SetLastError = true)]
			internal static extern uint timeSetEvent(uint uDelay, uint uResolution, HiResTimerCallback lpTimeProc, UIntPtr dwUser, uint fuEvent);

			[DllImport("Winmm.dll", CharSet = CharSet.Auto, SetLastError = true)]
			internal static extern uint timeKillEvent(uint uTimerID);

			[DllImport("Winmm.dll", CharSet = CharSet.Auto, SetLastError = true)]
			internal static extern uint timeGetTime();

			[DllImport("Winmm.dll", CharSet = CharSet.Auto, SetLastError = true)]
			internal static extern uint timeBeginPeriod(uint uPeriod);

			[DllImport("Winmm.dll", CharSet = CharSet.Auto, SetLastError = true)]
			internal static extern uint timeEndPeriod(uint uPeriod);

		}

		/// <summary>
		/// Timer type definitions.
		/// </summary>
		[Flags]
		public enum fuEvent : uint
		{
			TIME_ONESHOT = 0, // Event occurs once, after uDelay milliseconds.
			TIME_PERIODIC = 1,
			TIME_CALLBACK_FUNCTION = 0x0000,  // callback is function
			TIME_CALLBACK_EVENT_SET = 0x0010, // callback is event - use SetEvent
			TIME_CALLBACK_EVENT_PULSE = 0x0020  // callback is event - use PulseEvent
		}

		/// <summary>
		/// The current timer instance ID
		/// </summary>
		uint _TimerId = 0;

		/// <summary>
		/// Stop the current timer instance (if any)
		/// </summary>
		internal void KillTimer()
		{
			lock (this)
			{
				if (_TimerId <= 0)
					return;
				NativeMethods.timeKillEvent(_TimerId);
				_TimerId = 0;
			}
		}

		HiResTimerCallback _callback;

		/// <summary>
		/// Start a timer instance
		/// </summary>
		internal void StartTimer(HiResTimerCallback callback)
		{
			lock (this)
			{
				// Kill timer.
				if (_TimerId > 0)
				{
					NativeMethods.timeKillEvent(_TimerId);
					_TimerId = 0;
				}
				// Must create callback or timer will crash.
				_callback = new HiResTimerCallback(callback);
				//Set the timer type flags
				var f = fuEvent.TIME_CALLBACK_FUNCTION | (AutoReset ? fuEvent.TIME_PERIODIC : fuEvent.TIME_ONESHOT);
				_TimerId = NativeMethods.timeSetEvent((uint)Interval, 0, _callback, UIntPtr.Zero, (uint)f);
				if (_TimerId == 0)
				{
					var ex = new Win32Exception(Marshal.GetLastWin32Error());
					throw new Exception(ex.Message);
				}
			}
		}

		#endregion

	}
}
