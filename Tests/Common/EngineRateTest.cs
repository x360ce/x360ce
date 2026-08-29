// @under-test: App.v4/Common/DInput/DInputHelper.cs, Engine/JocysCom/ComponentModel/BindingListInvoked.cs
// @area: engine   @layer: ui-wpf
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Automation;

namespace x360ce.Tests
{
	/// <summary>
	/// What the running program actually achieves, read where a person reads it: the rate in
	/// the status bar.
	/// </summary>
	/// <remarks>
	/// The engine is meant to run at up to a thousand cycles a second, on its own thread, beside
	/// the interface rather than behind it. It has been dropped to single digits several times,
	/// always by a crash fix that made the engine wait for the interface - a shared lock, or a
	/// call marshalled to the interface thread.
	///
	/// Every one of those shipped past the whole suite. The tests below it measure a synthetic
	/// loop against a synthetic interface, and a fixed loop over a fixed two seconds does not
	/// reproduce a stall that only happens when a real interface is drawing real controls. The
	/// number in the status bar does, so it is read here.
	///
	/// Two things are checked, because they fail differently. The typical rate catches a steady
	/// loss. The worst sample catches the stall: when the engine takes a lock the interface
	/// holds, the rate does not settle at a lower number, it drops to one for as long as the
	/// interface is busy and recovers between. A test that reads the rate once, or that averages,
	/// sees a healthy number and misses it. So the rate is sampled for a stretch and the worst
	/// sample has to be good too.
	///
	/// Reading every device on the machine also stops the engine, for about a second, and that
	/// is meant to happen - it runs on the engine thread by design. The device counter beside
	/// the rate says when one ran, so a sample taken across a refresh is dropped rather than
	/// counted as a stall. Without that this measures Windows and fails at random.
	///
	/// Controllers left behind by runs that ended badly make Windows announce device changes
	/// over and over, and each announcement costs a refresh. A machine carrying a pile of them
	/// cannot measure anything, and the number it produces says more about the machine than the
	/// program, so that is checked before measuring rather than reported as a stall.
	/// </remarks>
	[TestClass]
	public class EngineRateTest
	{
		/// <summary>Cycles a second the engine must reach when nothing is asking anything of it.</summary>
		/// <remarks>
		/// The program asks for 1000. This is set far below, so the test reports a collapse
		/// rather than the ordinary difference between one machine and another.
		/// </remarks>
		const int TypicalFloor = 500;

		/// <summary>Cycles a second no single moment may fall below.</summary>
		/// <remarks>
		/// A stall shows up here and nowhere else: waiting on the interface measured a typical
		/// rate around 440 with samples of 1 in between, against about 990 with nothing waiting.
		/// </remarks>
		const int WorstFloor = 200;

		const int Samples = 16;
		const int SampleMs = 500;

		[TestMethod, TestCategory("engine"), TestCategory("ui-interactive")]
		[Description("The running program keeps its engine rate up, and never stalls")]
		public void Engine_rate_holds_up_while_the_program_runs()
		{
			var leftovers = x360ce.App.DInput.VirtualDriverInstaller.GetLeftoverVirtualPads();
			Assert.AreEqual(0, leftovers.Length, string.Format(
				"{0} virtual controllers left behind by earlier runs are still registered. Windows "
				+ "announces a device change for each of them, and every announcement makes the "
				+ "program read every device on the machine again, which stops the engine for about "
				+ "a second. Nothing measured here would describe the program. Remove them from the "
				+ "Issues tab, which needs Administrator, and run this again.", leftovers.Length));
			var exe = Ui.FindApp("App.v4");
			var app = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = System.IO.Path.GetDirectoryName(exe) });
			try
			{
				var window = Ui.WaitForMainWindow(app, TimeSpan.FromSeconds(60));
				var label = Ui.WaitFor(() => FindLabel(window, RateText), TimeSpan.FromSeconds(60),
					"the status bar never reported an engine rate");

				// Starting up reads every device on the machine, which is slow and is meant to
				// be. Measuring through it would report a stall that is not one.
				Ui.WaitFor(() => Read(label, RateText) >= TypicalFloor ? label : null, TimeSpan.FromSeconds(60),
					"the engine never reached " + TypicalFloor + " cycles a second after starting");

				var devices = Ui.WaitFor(() => FindLabel(window, DeviceText), TimeSpan.FromSeconds(10),
					"the status bar never reported a device count");
				var rates = new List<int>();
				var skipped = 0;
				for (var i = 0; i < Samples; i++)
				{
					var before = Read(devices, DeviceText);
					Thread.Sleep(SampleMs);
					var rate = Read(label, RateText);
					if (Read(devices, DeviceText) != before)
					{
						skipped++;
						continue;
					}
					if (rate >= 0)
						rates.Add(rate);
				}
				Assert.IsTrue(rates.Count >= Samples / 3, string.Format(
					"Only {0} of {1} samples avoided a device refresh, so the engine was never "
					+ "measured. Something is asking for every device on the machine over and over.",
					rates.Count, Samples));

				var ordered = rates.OrderBy(x => x).ToList();
				var typical = ordered[ordered.Count / 2];
				var worst = ordered[0];
				Console.WriteLine("engine rate: typical {0} Hz, worst {1} Hz, {2} sample(s) dropped "
					+ "for crossing a device refresh, all: {3}", typical, worst, skipped,
					string.Join(", ", rates.Select(x => x.ToString()).ToArray()));

				Assert.IsTrue(typical >= TypicalFloor, string.Format(
					"The engine ran at {0} cycles a second, below the floor of {1}. Something on "
					+ "the engine path is costing it every cycle. Samples: {2}",
					typical, TypicalFloor, string.Join(", ", rates.Select(x => x.ToString()).ToArray())));
				Assert.IsTrue(worst >= WorstFloor, string.Format(
					"The engine dropped to {0} cycles a second at least once, below the floor of "
					+ "{1}, while typically running at {2}. It is stalling, which is what waiting "
					+ "on the interface thread looks like: fine most of the time and stopped while "
					+ "the interface is busy. Samples: {3}",
					worst, WorstFloor, typical,
					string.Join(", ", rates.Select(x => x.ToString()).ToArray())));
			}
			finally
			{
				Ui.CloseApp(app);
			}
		}

		static readonly Regex RateText = new Regex(@"^HW Hz:\s*(\d+)");
		static readonly Regex DeviceText = new Regex(@"^D:\s*(\d+)");

		/// <summary>
		/// Finds the label once. Walking the whole window on every sample is itself an expensive
		/// call into the interface thread, which lowers the very number being measured.
		/// </summary>
		static AutomationElement FindLabel(AutomationElement window, Regex says)
		{
			var all = window.FindAll(TreeScope.Descendants, Condition.TrueCondition);
			foreach (AutomationElement element in all)
			{
				if (says.IsMatch(element.Current.Name ?? ""))
					return element;
			}
			return null;
		}

		/// <summary>The number the label is showing, or -1 when it cannot be read just now.</summary>
		static int Read(AutomationElement label, Regex says)
		{
			try
			{
				var match = says.Match(label.Current.Name ?? "");
				return match.Success ? int.Parse(match.Groups[1].Value) : -1;
			}
			catch (ElementNotAvailableException)
			{
				return -1;
			}
		}
	}
}
