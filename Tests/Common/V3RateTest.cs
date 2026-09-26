// @under-test: App.v3/MainForm.cs, Engine/Common/RateCounter.cs
// @area: engine   @layer: ui-wpf
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace x360ce.Tests
{
	/// <summary>
	/// Version 3 keeps reading and redrawing the controllers, read where a person reads it: the
	/// rate in its status bar.
	/// </summary>
	/// <remarks>
	/// Version 3 reads the controllers and redraws them in one pass on the window's own thread,
	/// ten times a second at most. It has no engine to measure: games read the controllers
	/// through the emulator library on their own. A pass that takes long does not slow the program down, it
	/// freezes the window: nothing can be clicked and nothing moves until it ends. So the rate is
	/// sampled for a stretch, and the worst sample has to be good as well as the typical one.
	///
	/// Unlike the engine test for version 4, samples taken across a device refresh are not
	/// dropped. Version 4 reads the devices on its engine thread, where a pause costs a little
	/// rate; version 3 reads them on the window's thread, where the same pause is the freeze
	/// this test exists to catch.
	///
	/// The first start asks about each controller it has no settings for. Those questions are
	/// answered No, as a person could, before the measuring starts; one asked while measuring
	/// stops the pass and fails the test, which is right, because it froze the window.
	/// </remarks>
	[TestClass]
	public class V3RateTest
	{
		/// <summary>Passes a second the program must reach when nothing is asking anything of it.</summary>
		/// <remarks>It waits 100 ms between passes, so 10 is the most it can reach.</remarks>
		const int TypicalFloor = 7;

		/// <summary>Passes a second no single moment may fall below.</summary>
		/// <remarks>Below 3, one pass held the window for most of a second.</remarks>
		const int WorstFloor = 3;

		const int Samples = 16;
		const int SampleMs = 500;

		[TestMethod, TestCategory("engine"), TestCategory("ui-interactive")]
		[Description("Version 3 keeps reading and redrawing the controllers, and its window never freezes")]
		public void V3_keeps_its_rate_up_and_never_freezes()
		{
			var exe = Ui.FindApp("App.v3");
			if (exe == null)
				Assert.Inconclusive("App.v3 is not built. Build it before running UI tests.");
			var app = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = System.IO.Path.GetDirectoryName(exe) });
			try
			{
				var window = Ui.WaitForMainWindow(app, TimeSpan.FromSeconds(60));
				var rate = Ui.WaitFor(() => Ui.FindByName(window, RateText), TimeSpan.FromSeconds(60),
					"the status bar never reported an interface rate");

				var last = -1;
				try
				{
					Ui.WaitFor(() =>
					{
						Ui.CloseDialogs(app, window);
						last = Ui.ReadNumber(rate, RateText);
						return last >= TypicalFloor ? rate : null;
					}, TimeSpan.FromSeconds(60), "the rate to settle");
				}
				catch (TimeoutException)
				{
					Assert.Fail(string.Format(
						"Version 3 never read the controllers {0} times a second in the minute after "
						+ "starting; the last rate it showed was {1}. Its window thread is held by "
						+ "something that takes seconds on every pass, such as reading every device on "
						+ "the machine each time Windows announces a device change.",
						TypicalFloor, last));
				}

				var readings = new List<int>();
				long longestRead = 0;
				for (var i = 0; i < Samples; i++)
				{
					Thread.Sleep(SampleMs);
					var watch = Stopwatch.StartNew();
					readings.Add(Ui.ReadNumber(rate, RateText));
					longestRead = Math.Max(longestRead, watch.ElapsedMilliseconds);
				}
				Console.WriteLine("longest read of the status bar: {0} ms", longestRead);
				AssertRate("interface rate", readings);
			}
			finally
			{
				Ui.CloseApp(app);
			}
		}

		static void AssertRate(string what, List<int> readings)
		{
			var rates = readings.Where(x => x >= 0).OrderBy(x => x).ToList();
			var all = string.Join(", ", readings.Select(x => x.ToString()).ToArray());
			Assert.IsTrue(rates.Count >= Samples / 2, string.Format(
				"The {0} could be read only {1} times out of {2}. Samples: {3}", what, rates.Count, Samples, all));
			var typical = rates[rates.Count / 2];
			var worst = rates[0];
			Console.WriteLine("{0}: typical {1} Hz, worst {2} Hz, all: {3}", what, typical, worst, all);
			Assert.IsTrue(typical >= TypicalFloor, string.Format(
				"The {0} was typically {1} a second, below the floor of {2}. Every pass is costing "
				+ "the window thread more than it should. Samples: {3}", what, typical, TypicalFloor, all));
			Assert.IsTrue(worst >= WorstFloor, string.Format(
				"The {0} dropped to {1} a second at least once, below the floor of {2}, while typically "
				+ "{3}. A pass held the window thread for most of a second: the window froze. Samples: {4}",
				what, worst, WorstFloor, typical, all));
		}

		static readonly Regex RateText = new Regex(@"^UI Hz:\s*(\d+)");
	}
}
