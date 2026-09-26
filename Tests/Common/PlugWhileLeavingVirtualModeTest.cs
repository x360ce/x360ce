// @under-test: App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs, App.v4/Common/DInput/DInputHelper.cs, App.v4/ViGEm/Client/ViGEmClient.x360ce.cs
// @area: devices   @layer: integration-db
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using System;
using System.Linq;
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
	/// This plugs a real controller in, so it needs the virtual bus installed, and every test here
	/// takes away what it made: a controller left on the bus holds an XInput place for every test after.
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

		/// <summary>Fails when controllers that were not on the bus before are still there, once Windows has had a moment to take them away.</summary>
		static void AssertNoNewControllers(System.Collections.Generic.HashSet<string> before, string when)
		{
			var deadline = DateTime.UtcNow.AddSeconds(15);
			var left = XInputPlaces.VirtualHardwareNow();
			left.ExceptWith(before);
			while (left.Count > 0 && DateTime.UtcNow < deadline)
			{
				Thread.Sleep(250);
				left = XInputPlaces.VirtualHardwareNow();
				left.ExceptWith(before);
			}
			Assert.AreEqual(0, left.Count, when + ", " + string.Join(", ", left) + " stayed on the bus.");
		}

		[TestMethod, TestCategory("devices")]
		public void A_plug_in_flight_when_the_client_is_let_go_answers_instead_of_throwing()
		{
			// A plug takes seconds on most machines and about 150 ms here, so no one moment is the
			// race. The client is let go at every point along the plug instead, ten milliseconds apart.
			var before = XInputPlaces.VirtualHardwareNow();
			var helper = new DInputHelper();
			var answers = new System.Collections.Generic.List<string>();
			for (var letGoAfterMs = 0; letGoAfterMs <= 120; letGoAfterMs += 10)
			{
				var client = Connected();
				var plugging = Task.Run(() => helper.EnableFeeding(1));
				Thread.Sleep(letGoAfterMs);
				// The shared reference is let go of, as the input thread does, while the bus handle is kept
				// until the plug has finished. A handle freed under a plug leaves that controller on the bus
				// after the program ends, holding a place for every test after this one.
				lock (ViGEmClient.ClientLock)
					ViGEmClient.Current = null;
				Assert.IsTrue(plugging.Wait(TimeSpan.FromSeconds(30)), "The plug never finished.");
				Assert.IsFalse(plugging.IsFaulted, string.Format("Let go after {0} ms, the plug threw: {1}",
					letGoAfterMs, plugging.Exception == null ? "" : plugging.Exception.GetBaseException().Message));
				answers.Add(letGoAfterMs + "ms=" + plugging.Result);
				for (uint i = 1; i <= 4; i++)
					client.UnPlug(i);
				client.Dispose();
				After();
				AssertNoNewControllers(before, "Let go after " + letGoAfterMs + " ms");
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
			// A controller is kept only in its own place, so the pad tried is one whose place is free.
			var free = Enumerable.Range(0, 4).FirstOrDefault(i => !SystemXInput.IsConnected(i));
			if (SystemXInput.IsConnected(free))
				Assert.Inconclusive("All four XInput places are taken, so no controller can be made.");
			var pad = (uint)free + 1;
			var made = 0;
			for (var closeAfterMs = 0; closeAfterMs <= 120; closeAfterMs += 20)
			{
				Connected();
				var helper = new DInputHelper();
				var plugging = helper.BeginPlug(pad);
				Thread.Sleep(closeAfterMs);
				helper.Dispose();
				Assert.IsTrue(plugging.IsCompleted, "Closing did not wait for the plug.");
				Assert.IsNull(ViGEmClient.Current, "Closing did not let go of the bus client.");
				if (!plugging.IsFaulted && plugging.Result == VirtualError.None)
					made++;
				AssertNoNewControllers(before, "Closed " + closeAfterMs + " ms into a plug");
			}
			// Nothing was tested if no controller was ever made. Windows gives a controller the place it
			// remembers for it, and one put anywhere but its own place is taken away again, so on a
			// machine that remembers another place for it none is kept - which is not a failure here.
			Console.WriteLine("Controllers made and taken away: " + made + " of 7, for pad " + pad + ".");
			if (made == 0)
				Assert.Inconclusive("Windows put every controller for pad " + pad + " in another place, so none was kept to leave behind.");
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
