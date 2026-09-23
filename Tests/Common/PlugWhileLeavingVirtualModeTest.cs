// @under-test: App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs, App.v4/Common/DInput/DInputHelper.cs, App.v4/ViGEm/Client/ViGEmClient.x360ce.cs
// @area: devices   @layer: integration-db
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using System;
using System.Threading;
using System.Threading.Tasks;
using x360ce.App;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// A controller being plugged in on a worker while the bus client is let go of on the input thread.
	/// </summary>
	/// <remarks>
	/// Plugging in takes seconds: it waits for Windows to give the controller a place and reads the
	/// device tree twice. The game leaving virtual mode, or the program closing, disposes the shared
	/// client meanwhile. The worker read the shared client again for each step, found null, and died
	/// with a NullReferenceException - eight of the eighteen crash reports for 4.22.21.0, two of them
	/// arriving later still as the unobserved fault of that task.
	///
	/// This plugs a real controller in, so it needs the virtual bus installed.
	/// </remarks>
	[TestClass]
	public class PlugWhileLeavingVirtualModeTest
	{
		static ViGEmClient Connected()
		{
			if (!ViGEmClient.isVBusExists(true))
				Assert.Inconclusive("The virtual bus is not installed on this machine.");
			var client = ViGEmClient.Current;
			if (client.Targets == null)
			{
				client.Targets = new Xbox360Controller[4];
				for (var i = 0; i < 4; i++)
					client.Targets[i] = new Xbox360Controller(client);
			}
			return client;
		}

		[TestCleanup]
		public void After()
		{
			var client = ViGEmClient.Current;
			if (client != null && client.Targets != null)
				for (uint i = 1; i <= 4; i++)
					client.UnPlug(i);
			ViGEmClient.DisposeCurrent();
		}

		[TestMethod]
		public void A_plug_in_flight_when_the_client_is_let_go_answers_instead_of_throwing()
		{
			// A plug takes seconds on most machines and about 150 ms here, so no one moment is the
			// race. The client is let go at every point along the plug instead, ten milliseconds apart.
			var helper = new DInputHelper();
			var answers = new System.Collections.Generic.List<string>();
			for (var letGoAfterMs = 0; letGoAfterMs <= 120; letGoAfterMs += 10)
			{
				Connected();
				var plugging = Task.Run(() => helper.EnableFeeding(1));
				Thread.Sleep(letGoAfterMs);
				ViGEmClient.DisposeCurrent();
				Assert.IsTrue(plugging.Wait(TimeSpan.FromSeconds(30)), "The plug never finished.");
				Assert.IsFalse(plugging.IsFaulted, string.Format("Let go after {0} ms, the plug threw: {1}",
					letGoAfterMs, plugging.Exception == null ? "" : plugging.Exception.GetBaseException().Message));
				answers.Add(letGoAfterMs + "ms=" + plugging.Result);
				After();
			}
			Console.WriteLine(string.Join(" ", answers));
		}

		[TestMethod, TestCategory("devices")]
		[Description("A controller being plugged in as the program closes is taken away with the rest")]
		public void A_plug_in_flight_when_the_program_closes_leaves_no_controller_behind()
		{
			// The program closed a moment after it began plugging a controller in. The plug connected
			// it after the others had been taken away, and the bus kept it after the program ended:
			// a controller nobody owned, holding a place until Windows restarted. Controllers already
			// on the bus before the test are not its business, so only new ones are counted.
			var before = XInputPlaces.VirtualHardwareNow();
			var made = 0;
			for (var closeAfterMs = 0; closeAfterMs <= 120; closeAfterMs += 20)
			{
				Connected();
				var helper = new DInputHelper();
				var plugging = helper.BeginPlug(1);
				Thread.Sleep(closeAfterMs);
				helper.Dispose();
				Assert.IsTrue(plugging.IsCompleted, "Closing did not wait for the plug.");
				Assert.IsNull(ViGEmClient.Current, "Closing did not let go of the bus client.");
				if (!plugging.IsFaulted && plugging.Result == VirtualError.None)
					made++;
				// Windows takes a moment to remove a controller after it is let go of.
				var deadline = DateTime.UtcNow.AddSeconds(15);
				var left = XInputPlaces.VirtualHardwareNow();
				left.ExceptWith(before);
				while (left.Count > 0 && DateTime.UtcNow < deadline)
				{
					Thread.Sleep(250);
					left = XInputPlaces.VirtualHardwareNow();
					left.ExceptWith(before);
				}
				Assert.AreEqual(0, left.Count, string.Format("Closed {0} ms into a plug, the program left {1} behind.",
					closeAfterMs, string.Join(", ", left)));
			}
			// Nothing was tested if no controller was ever made.
			Console.WriteLine("Controllers made and taken away: " + made + " of 7.");
			Assert.IsTrue(made > 0, "No plug made a controller, so nothing was left behind to find.");
		}

		[TestMethod]
		public void A_disposed_client_refuses_a_plug_without_touching_the_bus()
		{
			var client = Connected();
			ViGEmClient.DisposeCurrent();
			Assert.IsTrue(client.IsDisposed);
			Assert.IsFalse(client.PlugIn(1));
			Assert.IsFalse(client.UnPlug(1));
		}
	}
}
