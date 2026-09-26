// @under-test: Engine/Common/RateCounter.cs
// @area: engine   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The rate both programs show in the status bar says how often the controllers were read,
	/// including when they were not read at all.
	/// </summary>
	[TestClass]
	public class RateCounterTest
	{
		static readonly long Second = Stopwatch.Frequency;

		[TestMethod, TestCategory("critical")]
		[Description("A loop running ten times a second reads 10 from its first second")]
		public void A_steady_loop_reads_as_its_rate()
		{
			var counter = new RateCounter();
			var given = false;
			for (long i = 0; i <= 11; i++)
				given = counter.Tick(Second + i * Second / 10);
			Assert.IsTrue(given, "No rate was given after more than a second.");
			Assert.AreEqual(10, counter.Rate);
		}

		[TestMethod, TestCategory("critical")]
		[Description("A loop that ran twice in four seconds reads 0, where a bare count would read 2")]
		public void A_loop_that_stopped_reads_as_stopped_not_as_slow()
		{
			var counter = new RateCounter();
			counter.Tick(Second);
			counter.Tick(Second + Second / 10);
			Assert.IsTrue(counter.Tick(Second * 5 + Second / 10), "No rate was given after four seconds.");
			Assert.AreEqual(0, counter.Rate, "A window that froze for four seconds would read as merely slow.");
		}

		[TestMethod]
		[Description("After a reset nothing counted before it lowers or raises the next rate")]
		public void A_reset_forgets_what_was_counted()
		{
			var counter = new RateCounter();
			for (long i = 0; i < 5; i++)
				counter.Tick(Second + i * Second / 10);
			counter.Reset();
			var start = Second * 100;
			for (long i = 0; i <= 11; i++)
				counter.Tick(start + i * Second / 10);
			Assert.AreEqual(10, counter.Rate);
		}
	}
}
