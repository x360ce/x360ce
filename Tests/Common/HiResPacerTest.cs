// @under-test: Engine/JocysCom/Common/HiResTimer.cs, App.v4/Common/DInput/DInputHelper.cs
// @area: engine   @layer: unit
using JocysCom.ClassLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;

namespace x360ce.Tests
{
	/// <summary>
	/// The device thread must run a thousand passes a second when asked for a thousand.
	/// </summary>
	/// <remarks>
	/// It was woken by a multimedia timer on another thread, through an event. Windows expires a timer
	/// on its clock tick, and a one millisecond timer on a one millisecond clock now and then fell just
	/// past a tick, waited for the next, and then fired twice in a row to catch up. The device thread
	/// could take only one of the two, so 945 to 975 passes a second came out of a thousand ticks, with
	/// each pass taking under a tenth of a millisecond. The thread now waits on its own timer, armed
	/// for each exact due time.
	/// </remarks>
	[TestClass]
	public class HiResPacerTest
	{
		/// <summary>Measured 999.9 here. The multimedia timer and an event measured 945 to 975 on the same machine.</summary>
		const int MinimumPassesPerSecond = 990;

		/// <summary>Passes a second over <paramref name="seconds"/>, on a thread of its own.</summary>
		static double PassesPerSecond(int seconds)
		{
			var passes = 0;
			var thread = new Thread(() =>
			{
				using (var wake = new ManualResetEvent(false))
				using (var pacer = new HiResPacer(wake))
				{
					var end = Stopwatch.GetTimestamp() + Stopwatch.Frequency * seconds;
					while (Stopwatch.GetTimestamp() < end)
					{
						pacer.Wait(1);
						passes++;
					}
				}
			});
			thread.Start();
			thread.Join();
			return (double)passes / seconds;
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("A loop paced at one millisecond runs a thousand times a second")]
		public void A_loop_paced_at_one_millisecond_runs_a_thousand_times_a_second()
		{
			PassesPerSecond(1);
			var rate = PassesPerSecond(3);
			Console.WriteLine("{0:0.0} passes a second", rate);
			Assert.IsTrue(rate >= MinimumPassesPerSecond, string.Format(
				"{0:0.0} passes a second, under {1}. Waits are being lost again.", rate, MinimumPassesPerSecond));
			Assert.IsTrue(rate <= 1010, string.Format(
				"{0:0.0} passes a second: the pacer is running faster than it was asked to.", rate));
		}

		[TestMethod, TestCategory("engine")]
		[Description("Setting the wake handle ends a wait at once")]
		public void Setting_the_wake_handle_ends_a_wait_at_once()
		{
			using (var wake = new ManualResetEvent(true))
			using (var pacer = new HiResPacer(wake))
			{
				var watch = Stopwatch.StartNew();
				pacer.Wait(1000);
				Assert.IsTrue(watch.ElapsedMilliseconds < 200,
					"A wait of a second ran " + watch.ElapsedMilliseconds + " ms with the wake handle set: stopping the device thread would wait for it.");
			}
		}

		[TestMethod, TestCategory("engine")]
		[Description("A stall is not followed by a burst of passes to catch up")]
		public void A_stall_is_not_followed_by_a_burst_of_passes()
		{
			using (var wake = new ManualResetEvent(false))
			using (var pacer = new HiResPacer(wake))
			{
				for (var i = 0; i < 10; i++)
					pacer.Wait(1);
				// A pass that takes fifty intervals, as one reading a device list once did.
				Thread.Sleep(50);
				var passes = 0;
				var watch = Stopwatch.StartNew();
				while (watch.ElapsedMilliseconds < 10)
				{
					pacer.Wait(1);
					passes++;
				}
				Console.WriteLine("{0} passes in the 10 ms after a 50 ms stall", passes);
				Assert.IsTrue(passes <= 15, passes + " passes in the 10 ms after a stall: the missed passes are being run back to back.");
			}
		}
	}
}
