// @under-test: Tests/TestInfrastructure/MemoryLeak.cs, App.v4/MainForm.cs
// @area: memory   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			// devices attached. This catches a leak that doubles or triples the footprint, which
			// is what users notice, without failing on ordinary variation.
			const long ceilingMb = 400;

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

	}
}
