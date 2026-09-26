// @under-test: App.v4/Common/DInput/DInputHelper.Step6.RetrieveXiStates.cs, App.v4/Common/DInput/DInputHelper.XInputLibrarry.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// The timed wait around an XInput call must leave nothing behind.
	/// </summary>
	/// <remarks>
	/// The display read asks XInput for four states up to sixty times a second, each on a worker
	/// with a one-second limit. Done with a delegate's BeginInvoke and no EndInvoke, every call left
	/// its wait handle open, and after a long enough session Windows had no handles left to give:
	/// "Insufficient system resources exist to complete the requested service", reported from an
	/// IoT LTSC machine that had been running for days.
	/// </remarks>
	[TestClass]
	public class XInputReadHandleTest
	{
		const int Passes = 10000;

		static int HandleCount()
		{
			using (var process = Process.GetCurrentProcess())
				return process.HandleCount;
		}

		/// <summary>Handles open after running <paramref name="wait"/> <see cref="Passes"/> times, less those open before.</summary>
		static int HandlesGrownBy(Action wait)
		{
			// A short run first warms the pool and the timer queue so their handles are not
			// counted against the wait itself.
			for (var i = 0; i < 100; i++)
				wait();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			var before = HandleCount();
			for (var i = 0; i < Passes; i++)
				wait();
			return HandleCount() - before;
		}

		[TestMethod, TestCategory("critical")]
		public void Ten_thousand_reads_leave_the_handle_count_where_it_was()
		{
			var grown = HandlesGrownBy(() => DInputHelper.RanWithin(() => { }, 1000));
			// The old shape, measured beside the new one so the number this test guards against is
			// on record rather than remembered.
			var leaked = HandlesGrownBy(() =>
			{
				Action action = () => { };
				var result = action.BeginInvoke(null, null);
				result.AsyncWaitHandle.WaitOne(1000);
			});
			Console.WriteLine("after {0} passes: task wait grew the handle count by {1}; BeginInvoke without EndInvoke by {2}", Passes, grown, leaked);
			Assert.IsTrue(grown < 100, string.Format("{0} handles more after {1} reads: the wait is leaking again.", grown, Passes));
		}

		[TestMethod]
		public void A_call_that_does_not_come_back_is_given_up_on_after_its_time()
		{
			var watch = Stopwatch.StartNew();
			var released = new ManualResetEventSlim();
			var answered = DInputHelper.RanWithin(() => released.Wait(), 200);
			released.Set();
			Assert.IsFalse(answered);
			Assert.IsTrue(watch.ElapsedMilliseconds < 2000, "The wait did not give up in time: " + watch.ElapsedMilliseconds + " ms");
		}

		[TestMethod]
		public void A_call_that_comes_back_in_time_answers_true_with_its_result()
		{
			var value = 0;
			Assert.IsTrue(DInputHelper.RanWithin(() => value = 7, 1000));
			Assert.AreEqual(7, value);
		}
	}
}
