using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary
{
	public partial class HiResTimer
	{



		Stopwatch stopwatch;


		public HiResTimer()
		{
			stopwatch = new Stopwatch();
			timer = new System.Timers.Timer();
			timer.Interval = 1000;
			//timer.AutoReset = false;
			timer.Elapsed += Timer_Elapsed;
		}

		private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			marks[currentIndex] = stopwatch.ElapsedTicks;
			currentIndex++;
			if (currentIndex >= marks.Length)
			{
				stopwatch.Stop();
				long totalDiff = 0;
				long minDiff = 0;
				long maxDiff = 0;
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
				var msPc = (float)ms / (float)timer.Interval;
				var msMin = (float)minDiff * 1000f / (float)Stopwatch.Frequency;
				var msMax = (float)maxDiff * 1000f / (float)Stopwatch.Frequency;
				var totalRuntimeTicks = marks[marks.Length - 1] - marks[0];
				var totalRuntimeMs = (float)totalRuntimeTicks * 1000f / (float)Stopwatch.Frequency;
				var sb = new StringBuilder();
				sb.AppendFormat("Ticks per Second = {0}\r\n", Stopwatch.Frequency);
				sb.AppendFormat("Timer Interval = {0}\r\n", timer.Interval);
				sb.AppendFormat("Timer Autoreset = {0}\r\n", timer.AutoReset);
				sb.AppendFormat("Hz = {0}\r\n", 1000 / timer.Interval);
				sb.AppendFormat("Mark Samples = {0}\r\n", marks.Length);
				sb.AppendFormat("AverageInaccuracyTicks = {0}\r\n", averageInaccuracyTicks);
				sb.AppendFormat("AverageInaccuracyMs = {0}\r\n", ms);
				sb.AppendFormat("AverageInaccuracyMs % = {0:0.000%}\r\n", msPc);
				sb.AppendFormat("AverageInaccuracyMsMin = {0}\r\n", msMin);
				sb.AppendFormat("AverageInaccuracyMsMax = {0}\r\n", msMax);
				sb.AppendFormat("Total run time ms = {0}\r\n", totalRuntimeMs);
				if (timerId > 0)
				{
					try
					{
						Stop();
					}
					catch (Exception)
					{
						MessageBox.Show("Error");
					}
					// end timer
				}
				else if (timer.AutoReset)
				{
					timer.Stop();
				}
				MessageBox.Show(sb.ToString());
				return;
			}
			if (timerId == 0 && !timer.AutoReset)
				timer.Start();
		}

		int currentIndex;
		long[] marks = new long[0];

		long ticksToWait;
		uint timerId;


		// 1000 Hz - 1 ms
		//  500 Hz = 2 ms
		//  250 Hz = 4 ms
		//  125 Hz = 8 ms
		public void Test(int interval, bool hiResTimer)
		{
			currentIndex = 0;
			stopwatch.Restart();
			// Get hz.
			var hz = 1000 / interval;
			// Run for two seconds.
			var maxMarks = hz * 2;
			Array.Resize(ref marks, maxMarks);
			timer.Interval = interval;
			// Get amount of ticks to wait inbetween actions.
			ticksToWait = Stopwatch.Frequency * interval / 1000;
			if (hiResTimer)
			{
				try
				{
					thisCB = new TimerCallback(OnElapsed);
					Start((uint)interval, true);
				}
				catch (Exception)
				{
					MessageBox.Show("error 1");
				}
			}
			else
			{
				timer.Start();
			}
		}

		System.Timers.Timer timer;

		#region  Hi Resolution timer.

		//Lib API declarations
		[DllImport("Winmm.dll", CharSet = CharSet.Auto)]
		static extern uint timeSetEvent(uint uDelay, uint uResolution, TimerCallback lpTimeProc, UIntPtr dwUser, uint fuEvent);

		[DllImport("Winmm.dll", CharSet = CharSet.Auto)]
		static extern uint timeKillEvent(uint uTimerID);

		[DllImport("Winmm.dll", CharSet = CharSet.Auto)]
		static extern uint timeGetTime();

		[DllImport("Winmm.dll", CharSet = CharSet.Auto)]
		static extern uint timeBeginPeriod(uint uPeriod);

		[DllImport("Winmm.dll", CharSet = CharSet.Auto)]
		static extern uint timeEndPeriod(uint uPeriod);

		//Timer type definitions
		[Flags]
		public enum fuEvent : uint
		{
			TIME_ONESHOT = 0,      //Event occurs once, after uDelay milliseconds. 
			TIME_PERIODIC = 1,
			TIME_CALLBACK_FUNCTION = 0x0000,  /* callback is function */
											  //TIME_CALLBACK_EVENT_SET = 0x0010, /* callback is event - use SetEvent */
											  //TIME_CALLBACK_EVENT_PULSE = 0x0020  /* callback is event - use PulseEvent */
		}

		void OnElapsed(uint uTimerID, uint uMsg, UIntPtr dwUser, UIntPtr dw1, UIntPtr dw2)
		{
			Timer_Elapsed(null, null);
		}

		//Delegate definition for the API callback
		delegate void TimerCallback(uint uTimerID, uint uMsg, UIntPtr dwUser, UIntPtr dw1, UIntPtr dw2);

		//IDisposable code
		private bool disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					Stop();
				}
			}
			disposed = true;
		}

		/// <summary>
		/// The current timer instance ID
		/// </summary>
		uint id = 0;

		/// <summary>
		/// The callback used by the the API
		/// </summary>
		TimerCallback thisCB;

		/// <summary>
		/// The timer elapsed event 
		/// </summary>
		public event EventHandler Timer;
		protected virtual void OnTimer(EventArgs e)
		{
			if (Timer != null)
				Timer(this, e);
		}

		/// <summary>
		/// Stop the current timer instance (if any)
		/// </summary>
		public void Stop()
		{
			lock (this)
			{
				if (id != 0)
				{
					timeKillEvent(id);
					Debug.WriteLine("MMTimer " + id.ToString() + " stopped");
					id = 0;
				}
			}
		}

		/// <summary>
		/// Start a timer instance
		/// </summary>
		/// <param name="ms">Timer interval in milliseconds</param>
		/// <param name="repeat">If true sets a repetitive event, otherwise sets a one-shot</param>
		public void Start(uint ms, bool repeat)
		{
			//Kill any existing timer
			Stop();

			//Set the timer type flags
			fuEvent f = fuEvent.TIME_CALLBACK_FUNCTION | (repeat ? fuEvent.TIME_PERIODIC : fuEvent.TIME_ONESHOT);

			lock (this)
			{
				id = timeSetEvent(ms, 0, thisCB, UIntPtr.Zero, (uint)f);
				if (id == 0)
					throw new Exception("timeSetEvent error");
				Debug.WriteLine("MMTimer " + id.ToString() + " started");
			}
		}

		void CBFunc(uint uTimerID, uint uMsg, UIntPtr dwUser, UIntPtr dw1, UIntPtr dw2)
		{
			//Callback from the MMTimer API that fires the Timer event. Note we are in a different thread here
			OnTimer(new EventArgs());
		}

		#endregion

	}
}
