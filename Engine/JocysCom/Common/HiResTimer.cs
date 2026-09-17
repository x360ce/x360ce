#nullable disable

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
	public class HiResTimer : IDisposable
	{

		/// <summary>
		/// Initializes a new instance of the HiResTimer.
		/// </summary>
		public HiResTimer(int interval = 0, string name = null)
		{
			Name = name;
			//if (interval <= 0)
			//	throw new ArgumentException("Invalid value", nameof(interval));
			_Interval = interval;
		}

		public string Name { get; set; }

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
					throw new ArgumentException("Invalid value", nameof(Interval));
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
		public TaskScheduler SynchronizingObject { get; set; }

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
					if (so != null)
						new Task(() => { ev(this, e); }).RunSynchronously(so);
					else
						ev(this, e);
				}
			}
			catch (Exception ex)
			{
				LastException = ex;
			}
		}

		public Exception LastException;

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
			// Get Hz.
			var hz = 1000 / Interval;
			// Run for two seconds.
			var maxMarks = hz * 2;
			Array.Resize(ref marks, maxMarks);
			// Get amount of ticks to wait in-between actions.
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
					// negative - waited not enough.
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
				var ms = (float)averageInaccuracyTicks * 1000f / Stopwatch.Frequency;
				var msPc = ms / Interval;
				var msMin = (float)minDiff * 1000f / Stopwatch.Frequency;
				var msMax = (float)maxDiff * 1000f / Stopwatch.Frequency;
				var totalRuntimeTicks = marks[marks.Length - 1] - marks[0];
				var totalRuntimeMs = totalRuntimeTicks * 1000f / Stopwatch.Frequency;
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

			[StructLayout(LayoutKind.Sequential)]
			internal struct PROCESS_POWER_THROTTLING_STATE
			{
				public uint Version;
				public uint ControlMask;
				public uint StateMask;
			}

			internal const uint PROCESS_POWER_THROTTLING_CURRENT_VERSION = 1;
			internal const uint PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 0x1;
			internal const uint PROCESS_POWER_THROTTLING_IGNORE_TIMER_RESOLUTION = 0x4;
			internal const int ProcessPowerThrottling = 4;

			[DllImport("kernel32.dll", SetLastError = true)]
			internal static extern bool SetProcessInformation(IntPtr hProcess, int ProcessInformationClass,
				ref PROCESS_POWER_THROTTLING_STATE ProcessInformation, uint ProcessInformationSize);

			[DllImport("kernel32.dll", SetLastError = true)]
			internal static extern bool GetProcessInformation(IntPtr hProcess, int ProcessInformationClass,
				ref PROCESS_POWER_THROTTLING_STATE ProcessInformation, uint ProcessInformationSize);

			[DllImport("kernel32.dll")]
			internal static extern IntPtr GetCurrentProcess();

			[DllImport("ntdll.dll")]
			internal static extern int NtQueryTimerResolution(out uint minimum, out uint maximum, out uint current);
		}

		static bool _FullResolutionAsked;

		/// <summary>
		/// Asks Windows to honour a one millisecond timer for this process whatever its window is
		/// doing.
		/// </summary>
		/// <remarks>
		/// Windows 11 puts a process it considers to be in the background into its efficiency mode:
		/// its threads are woken late and its timer resolution requests are ignored, so a thread
		/// that asks to run every millisecond runs every eight. That is exactly where this program
		/// sits while a game is played. Measured: the timer ticked a thousand times a second and
		/// the device thread, woken by it, managed 125 passes, and a wheel fed at that rate swings
		/// from side to side. The request is made once per process: the period is asked for
		/// outright, and both parts of the throttling are switched off for this process, which a
		/// bit set in the control mask and clear in the state mask means. Windows without the
		/// call carries on as before.
		/// </remarks>
		static void AskForFullResolution()
		{
			if (_FullResolutionAsked)
				return;
			_FullResolutionAsked = true;
			try { NativeMethods.timeBeginPeriod(1); }
			catch (Exception) { }
			try
			{
				var state = new NativeMethods.PROCESS_POWER_THROTTLING_STATE
				{
					Version = NativeMethods.PROCESS_POWER_THROTTLING_CURRENT_VERSION,
					ControlMask = NativeMethods.PROCESS_POWER_THROTTLING_EXECUTION_SPEED
						| NativeMethods.PROCESS_POWER_THROTTLING_IGNORE_TIMER_RESOLUTION,
					StateMask = 0,
				};
				NativeMethods.SetProcessInformation(NativeMethods.GetCurrentProcess(), NativeMethods.ProcessPowerThrottling,
					ref state, (uint)Marshal.SizeOf(typeof(NativeMethods.PROCESS_POWER_THROTTLING_STATE)));
			}
			catch (Exception) { }
		}

		/// <summary>The process's power throttling masks as Windows reports them, "control/state", or "?" where it cannot say.</summary>
		public static string PowerThrottlingState
		{
			get
			{
				try
				{
					var state = new NativeMethods.PROCESS_POWER_THROTTLING_STATE { Version = NativeMethods.PROCESS_POWER_THROTTLING_CURRENT_VERSION };
					var ok = NativeMethods.GetProcessInformation(NativeMethods.GetCurrentProcess(), NativeMethods.ProcessPowerThrottling,
						ref state, (uint)Marshal.SizeOf(typeof(NativeMethods.PROCESS_POWER_THROTTLING_STATE)));
					return ok ? state.ControlMask.ToString("X") + "/" + state.StateMask.ToString("X") : "?";
				}
				catch (Exception) { return "?"; }
			}
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

		/// <summary>Whether the multimedia timer is the one ticking, rather than the ordinary fallback.</summary>
		public bool UsesMultimediaTimer
		{
			get { return MultimediaTimerAvailable && _TimerId != 0; }
		}

		/// <summary>The timer resolution the process currently gets from Windows, in milliseconds.</summary>
		public static double CurrentResolutionMs
		{
			get
			{
				try
				{
					uint min, max, current;
					NativeMethods.NtQueryTimerResolution(out min, out max, out current);
					return current / 10000.0;
				}
				catch (Exception) { return -1; }
			}
		}

		/// <summary>
		/// Whether the multimedia timer can be used. Cleared for the whole process the first time
		/// Windows says its library is not there.
		/// </summary>
		/// <remarks>
		/// A Windows without Winmm.dll exists: a stripped installation reported it, and the program
		/// stopped with an error before it had polled anything. The ordinary timer below is coarser,
		/// at about fifteen milliseconds, but a slower poll beats no poll at all.
		/// </remarks>
		public static bool MultimediaTimerAvailable = true;

		/// <summary>The timer used when the multimedia one is not available.</summary>
		System.Threading.Timer _fallback;

		/// <summary>
		/// Stop the current timer instance (if any)
		/// </summary>
		internal void KillTimer()
		{
			lock (this)
			{
				if (_fallback != null)
				{
					_fallback.Dispose();
					_fallback = null;
				}
				if (_TimerId <= 0)
					return;
				var unused = NativeMethods.timeKillEvent(_TimerId);
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
				KillTimer();
				// Must create callback or timer will crash.
				_callback = new HiResTimerCallback(callback);
				if (MultimediaTimerAvailable)
				{
					//Set the timer type flags
					var f = fuEvent.TIME_CALLBACK_FUNCTION | (AutoReset ? fuEvent.TIME_PERIODIC : fuEvent.TIME_ONESHOT);
					try
					{
						AskForFullResolution();
						_TimerId = NativeMethods.timeSetEvent((uint)Interval, 0, _callback, UIntPtr.Zero, (uint)f);
					}
					catch (DllNotFoundException)
					{
						MultimediaTimerAvailable = false;
					}
					catch (EntryPointNotFoundException)
					{
						MultimediaTimerAvailable = false;
					}
					if (MultimediaTimerAvailable && _TimerId == 0)
					{
						var ex = new Win32Exception(Marshal.GetLastWin32Error());
						throw new Exception(ex.Message);
					}
				}
				if (!MultimediaTimerAvailable)
					_fallback = new System.Threading.Timer(FallbackElapsed, null, Interval, System.Threading.Timeout.Infinite);
			}
		}

		/// <summary>One tick of the ordinary timer, armed again only once the callback has finished.</summary>
		/// <remarks>
		/// Armed one tick at a time rather than periodically, so that a callback which takes longer
		/// than the interval is never entered twice at once.
		/// </remarks>
		void FallbackElapsed(object state)
		{
			var callback = _callback;
			if (callback != null)
				callback(0, 0, UIntPtr.Zero, UIntPtr.Zero, UIntPtr.Zero);
			lock (this)
			{
				if (_fallback != null && AutoReset)
					_fallback.Change(Interval, System.Threading.Timeout.Infinite);
			}
		}

		#endregion

	}
}
