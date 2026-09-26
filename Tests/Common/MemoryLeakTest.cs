// @under-test: Tests/TestInfrastructure/MemoryLeak.cs, App.v4/MainForm.cs, App.v3/MainForm.cs
// @area: memory   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace x360ce.Tests
{
	/// <summary>
	/// Closing a window must release it. A window that is merely hidden while something still
	/// references it keeps its controls, images and event subscriptions alive, and the process
	/// holds hundreds of megabytes for a session that is doing nothing but polling devices.
	/// </summary>
	[TestClass]
	public class MemoryLeakTest
	{

		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

		[TestMethod, TestCategory("memory"), TestCategory("smoke")]
		[Description("The build is optimised, without which every disposal test is meaningless")]
		public void Disposal_tests_run_against_an_optimised_build()
		{
			// This guards the other tests in this class rather than the product. In an
			// unoptimised build locals stay rooted, weak references never die, and a leak
			// test reports success no matter what the product does.
			Assert.IsFalse(MemoryLeak.IsUnoptimised,
				"This assembly was built without optimisation, so disposal results cannot be trusted. " +
				"Set Optimize=true for this configuration.");
		}

		[TestMethod, TestCategory("memory")]
		[Description("The helper reports a released object as released")]
		public void Helper_reports_a_released_object()
		{
			var released = MemoryLeak.CreateUseAndRelease(
				() => new Form(),
				form => form.Dispose(),
				Timeout);
			Assert.IsTrue(released, "A disposed form with no other reference should be collected.");
		}

		[TestMethod, TestCategory("memory")]
		[Description("The helper reports a held object as leaked, so a real leak cannot pass unnoticed")]
		public void Helper_reports_a_held_object()
		{
			// Without this negative case the suite could pass because the helper never fails.
			var rooted = new List<Form>();
			var released = MemoryLeak.CreateUseAndRelease(
				() => new Form(),
				form => { form.Dispose(); rooted.Add(form); },
				TimeSpan.FromSeconds(1));
			Assert.IsFalse(released, "An object still referenced must be reported as not released.");
			Assert.AreEqual(1, rooted.Count);
			rooted.Clear();
		}

		[TestMethod, TestCategory("memory")]
		[Description("An unreleased subscription to a long-lived publisher keeps a control alive")]
		public void Subscription_to_a_long_lived_publisher_keeps_a_control_alive()
		{
			// This is the defect class behind the "Fixing Unloading/Disposing" work: a control
			// subscribes to something that outlives it, and the publisher's invocation list
			// keeps the whole control tree rooted long after the window is gone.
			var publisher = new System.Timers.Timer(100000);
			try
			{
				var released = MemoryLeak.CreateUseAndRelease(
					() =>
					{
						var form = new Form();
						// The closure captures the form, so the publisher now holds it.
						publisher.Elapsed += (s, e) => form.Text = "tick";
						return form;
					},
					form => form.Dispose(),
					TimeSpan.FromSeconds(1));

				Assert.IsFalse(released,
					"Disposing a control does not remove it from a publisher's invocation list. " +
					"If this now passes, the runtime changed and the guidance below needs revisiting.");
			}
			finally
			{
				publisher.Dispose();
			}
		}

		[TestMethod, TestCategory("memory")]
		[Description("Unsubscribing releases the control, which is the fix for the case above")]
		public void Unsubscribing_releases_the_control()
		{
			var publisher = new System.Timers.Timer(100000);
			try
			{
				var released = MemoryLeak.CreateUseAndRelease(
					() =>
					{
						var form = new Form();
						System.Timers.ElapsedEventHandler handler = (s, e) => form.Text = "tick";
						publisher.Elapsed += handler;
						// Detaching is what actually frees the control; disposing alone does not.
						form.Disposed += (s, e) => publisher.Elapsed -= handler;
						return form;
					},
					form => form.Dispose(),
					Timeout);

				Assert.IsTrue(released,
					"The control was still held after unsubscribing on Disposed.");
			}
			finally
			{
				publisher.Dispose();
			}
		}

		[TestMethod, TestCategory("memory"), TestCategory("ui-interactive")]
		[Description("Version 4 does not sit on hundreds of megabytes once it is up")]
		public void V4_memory_stays_within_a_sane_ceiling()
		{
			// A ceiling rather than a tight number: exact usage varies with the machine and the
			// devices attached. Without WPF this settles around 85 MB; it measured 198 MB when the
			// controller picture was still WPF, so this also fails if WPF ever returns.
			const long ceilingMb = 120;

			var exe = Ui.FindApp("App.v4");
			if (exe == null)
				Assert.Inconclusive("App.v4 is not built. Build the solution before running UI tests.");

			Process process = null;
			try
			{
				process = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = System.IO.Path.GetDirectoryName(exe) });
				Ui.WaitForMainWindow(process, TimeSpan.FromSeconds(45));

				// Let startup settle: device enumeration allocates before it reaches a steady state.
				var settled = Ui.WaitFor(() =>
				{
					process.Refresh();
					var before = process.PrivateMemorySize64;
					System.Threading.Thread.Sleep(1500);
					process.Refresh();
					// Steady means the last one and a half seconds moved it less than 5 MB.
					return Math.Abs(process.PrivateMemorySize64 - before) < 5L * 1024 * 1024
						? (object)process.PrivateMemorySize64 : null;
				}, TimeSpan.FromSeconds(60), "memory to settle");

				var privateMb = (long)settled / 1024 / 1024;
				Console.WriteLine($"Private memory once settled: {privateMb} MB");
				Assert.IsTrue(privateMb < ceilingMb,
					$"Private memory settled at {privateMb} MB, above the {ceilingMb} MB ceiling.");
			}
			finally
			{
				Ui.CloseApp(process);
			}
		}

		/// <summary>
		/// The footprint must be steady, not small. App.v4 hosts WPF islands inside Windows
		/// Forms, and initialising WPF in a process costs around 70 MB of unmanaged memory that
		/// is never returned while the process lives. That baseline is not a leak and no amount
		/// of disposal reclaims it, so a test asserting a small number would only ever be
		/// measuring WPF. What a leak does look like is growth per open and close, which this
		/// test measures directly.
		/// </summary>
		[TestMethod, TestCategory("memory"), TestCategory("ui-interactive")]
		[Description("Sending App.v4 to the tray and back repeatedly does not grow the process")]
		public void V4_does_not_grow_across_minimize_restore_cycles()
		{
			// The first cycles legitimately allocate: the tray icon, the hidden owner window and
			// one-time caches all appear the first time the window is minimised. Growth is only
			// meaningful once that has happened, so those cycles set the baseline instead of
			// being measured against it.
			const int warmupCycles = 2;
			const int measuredCycles = 6;
			const long allowedGrowthMb = 20;
			// Menus and tooltips can hold a handful of handles between cycles. A real control
			// leak grows by dozens per cycle, so this tolerance separates them cleanly.
			const int allowedHandleGrowth = 10;

			var exe = Ui.FindApp("App.v4");
			if (exe == null)
				Assert.Inconclusive("App.v4 is not built. Build the solution before running UI tests.");

			Process process = null;
			try
			{
				process = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = System.IO.Path.GetDirectoryName(exe) });
				Ui.WaitForMainWindow(process, TimeSpan.FromSeconds(45));

				for (var i = 0; i < warmupCycles; i++)
					Cycle(process);
				var baseline = MemoryLeak.Measure(process);
				Console.WriteLine("baseline : " + baseline);

				var worst = baseline;
				for (var i = 1; i <= measuredCycles; i++)
				{
					Cycle(process);
					var now = MemoryLeak.Measure(process);
					Console.WriteLine("cycle " + i + "  : " + now);
					if (now.PrivateBytes > worst.PrivateBytes) worst.PrivateBytes = now.PrivateBytes;
					if (now.GdiHandles > worst.GdiHandles) worst.GdiHandles = now.GdiHandles;
					if (now.UserHandles > worst.UserHandles) worst.UserHandles = now.UserHandles;
				}

				var grewMb = (long)Math.Round(worst.PrivateMb - baseline.PrivateMb);
				Assert.IsTrue(grewMb <= allowedGrowthMb,
					"Private memory grew " + grewMb + " MB over " + measuredCycles +
					" minimise and restore cycles, above the " + allowedGrowthMb + " MB allowance.");
				Assert.IsTrue(worst.GdiHandles - baseline.GdiHandles <= allowedHandleGrowth,
					"GDI handles grew from " + baseline.GdiHandles + " to " + worst.GdiHandles +
					", which means controls are being created and not disposed.");
				Assert.IsTrue(worst.UserHandles - baseline.UserHandles <= allowedHandleGrowth,
					"USER handles grew from " + baseline.UserHandles + " to " + worst.UserHandles +
					", which means windows are being created and not destroyed.");
			}
			finally
			{
				Ui.CloseApp(process);
			}
		}

		private static void Cycle(Process process)
		{
			Ui.Minimize(process);
			SettleAfterWindowStateChange(process);
			Ui.Restore(process);
			SettleAfterWindowStateChange(process);
		}

		/// <summary>
		/// Version 3 reads and redraws the controllers ten times a second for as long as its window
		/// is open, so anything one pass leaves behind grows steadily: a minute is six hundred
		/// passes, and a leak of one handle a pass grows by some two hundred every twenty seconds.
		/// </summary>
		/// <remarks>
		/// What is asserted is growth that goes on, not the largest value seen. Something created once
		/// while the program runs, such as a tooltip the first time the pointer rests on the window,
		/// raises the count by a step and stays there. That step is not a leak, yet measured as the
		/// worst value against the first it would fail. So the minute is cut into thirds, and a count
		/// fails only when each third grows past the allowance on the one before.
		/// </remarks>
		[TestMethod, TestCategory("memory"), TestCategory("ui-interactive")]
		[Description("Version 3 does not grow while it reads and redraws the controllers")]
		public void V3_does_not_grow_while_it_runs()
		{
			const int warmupSeconds = 15;
			const int measuredSeconds = 60;
			const long allowedGrowthMb = 20;
			const int allowedHandleGrowth = 10;
			const int allowedKernelHandleGrowth = 50;

			var exe = Ui.FindApp("App.v3");
			if (exe == null)
				Assert.Inconclusive("App.v3 is not built. Build it before running UI tests.");
			var rateText = new System.Text.RegularExpressions.Regex(@"^UI Hz:\s*(\d+)");

			Process process = null;
			try
			{
				process = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = System.IO.Path.GetDirectoryName(exe) });
				var window = Ui.WaitForMainWindow(process, TimeSpan.FromSeconds(60));
				var rate = Ui.WaitFor(() => Ui.FindByName(window, rateText), TimeSpan.FromSeconds(60),
					"the status bar never reported an interface rate");
				// Questions about new controllers are answered No; the passes run once they are.
				Ui.WaitFor(() =>
				{
					Ui.CloseDialogs(process, window);
					return Ui.ReadNumber(rate, rateText) >= 7 ? rate : null;
				}, TimeSpan.FromSeconds(60), "the program to read the controllers");
				// The first passes fill caches and load the library, which is growth that stops.
				Thread.Sleep(warmupSeconds * 1000);
				var samples = new List<MemoryLeak.Usage>();
				for (var second = 5; second <= measuredSeconds; second += 5)
				{
					Thread.Sleep(5000);
					var now = MemoryLeak.Measure(process);
					Console.WriteLine("{0,3} s : {1}", second, now);
					samples.Add(now);
				}
				// Nothing measured means nothing proved: the passes must still have been running.
				Assert.IsTrue(Ui.ReadNumber(rate, rateText) >= 7, "The program stopped reading the controllers while it was measured.");

				AssertNoSustainedGrowth("Private memory, MB", samples.Select(x => x.PrivateMb).ToArray(), allowedGrowthMb,
					"memory taken each pass is not given back");
				AssertNoSustainedGrowth("GDI handles", samples.Select(x => (double)x.GdiHandles).ToArray(), allowedHandleGrowth,
					"something drawn each pass is not released");
				AssertNoSustainedGrowth("USER handles", samples.Select(x => (double)x.UserHandles).ToArray(), allowedHandleGrowth,
					"windows are being created and not destroyed");
				AssertNoSustainedGrowth("Kernel handles", samples.Select(x => (double)x.KernelHandles).ToArray(), allowedKernelHandleGrowth,
					"something each pass opens, such as a device, is not closed");
			}
			finally
			{
				Ui.CloseApp(process);
			}
		}

		/// <summary>Fails when a count grows past the allowance from each third of the samples to the next.</summary>
		private static void AssertNoSustainedGrowth(string what, double[] samples, double allowance, string meaning)
		{
			var third = samples.Length / 3;
			Func<int, double> median = start =>
			{
				var part = samples.Skip(start).Take(third).OrderBy(x => x).ToArray();
				return part[part.Length / 2];
			};
			double first = median(0), middle = median(third), last = median(third * 2);
			Assert.IsFalse(middle - first > allowance && last - middle > allowance, string.Format(
				"{0} kept growing: {1:N1}, then {2:N1}, then {3:N1} across the run, more than {4} each time. That is {5}.",
				what, first, middle, last, allowance, meaning));
		}

		// Minimising and restoring is asynchronous: the state change is posted to the
		// application, which then releases or rebuilds its render surfaces. Sampling before
		// that finishes reads a value mid-transition. The condition waited on is the process
		// footprint holding still, which is what the caller is about to measure. CPU time
		// would not work here: the device polling thread never goes idle.
		private static void SettleAfterWindowStateChange(Process process)
		{
			const long stillMb = 2;
			Ui.WaitFor(() =>
			{
				var before = MemoryLeak.Measure(process).PrivateMb;
				Thread.Sleep(400);
				var after = MemoryLeak.Measure(process).PrivateMb;
				return Math.Abs(after - before) < stillMb ? (object)true : null;
			}, TimeSpan.FromSeconds(15), "the window state change to settle");
		}

	}
}
