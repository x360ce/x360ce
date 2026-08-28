// @under-test: App.v4/ViGEm/Client/ViGEmClient.x360ce.cs, App.v4/ViGEm/Client/ViGEmTarget.cs
// @area: devices   @layer: integration-db
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using System.Diagnostics;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// The light on a controller tab says whether a virtual controller exists. It has to be right.
	/// </summary>
	/// <remarks>
	/// It used to answer from a flag the program set when it plugged one in. A flag cannot know the
	/// controller was taken away afterwards - removed in Device Manager, or swept up with the
	/// leftovers - so it went on saying yes. A person then saw a green light for a controller that did
	/// not exist, and the program never made a new one, because the loop that would have made it read
	/// the same flag and believed there already was one.
	///
	/// This needs the virtual bus installed, and unplugs whatever it creates even when it fails.
	/// </remarks>
	[TestClass]
	public class VirtualPadStatusTest
	{


		/// <summary>Every place empty before and after, whatever ran before this.</summary>
		/// <remarks>
		/// The bus is one object shared by the whole test run, so a controller one test leaves plugged
		/// in is a controller the next test did not ask for. That showed up as a test which passed on
		/// its own and failed in company, which is the least useful kind of failure.
		/// </remarks>
		static void UnplugEverything()
		{
			var client = ViGEmClient.Current;
			if (client == null || client.Targets == null)
				return;
			for (uint i = 1; i <= 4; i++)
				client.UnPlug(i);
		}

		[TestInitialize]
		public void Before()
		{
			UnplugEverything();
			var already = VirtualDriverInstaller.GetLeftoverVirtualPads();
			if (already.Length > 0)
				Assert.Inconclusive(already.Length +
					" virtual controllers were already left behind before this ran. Remove them from " +
					"the Issues page, then run again.");
		}

		[TestCleanup]
		public void After() => UnplugEverything();

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

		[TestMethod, TestCategory("devices"), TestCategory("requires-elevation")]
		[Description("The light goes out when the controller goes, however it went")]
		public void The_light_goes_out_when_the_controller_goes()
		{
			var client = Connected();
			try
			{
				Assert.IsFalse(client.IsControllerConnected(1),
					"A controller is reported before one has been made.");
				Assert.IsTrue(client.PlugIn(1), "The controller could not be plugged in.");
				Assert.IsTrue(client.IsControllerConnected(1),
					"No controller is reported straight after one was made.");

				// Taken away behind the program's back, which is what happens when somebody removes it in
				// Device Manager or sweeps it up with the leftovers. Nothing tells the program.
				client.Targets[0].Disconnect();

				Assert.IsFalse(client.IsControllerConnected(1),
					"The controller is gone and the program still reports it. That is the green light " +
					"on a controller which does not exist, and the reason no new one is ever made: the " +
					"loop asks this same question and is told there is nothing to do.");
			}
			finally
			{
				client.UnPlug(1);
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("requires-elevation")]
		[Description("A controller taken away is made again on the next attempt")]
		public void A_controller_taken_away_is_made_again()
		{
			// The other half of the same fault. Answering honestly is only useful if the program then
			// acts on the answer, and what it does with it is plug a new one in.
			var client = Connected();
			try
			{
				client.PlugIn(1);
				client.Targets[0].Disconnect();
				Assert.IsTrue(client.PlugIn(1),
					"A controller taken away could not be replaced.");
				Assert.IsTrue(client.IsControllerConnected(1),
					"The replacement is not reported, so the person is left with no controller and no " +
					"sign of why.");
			}
			finally
			{
				client.UnPlug(1);
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("requires-elevation")]
		[Description("Asking whether a controller is there is cheap enough for the update loop")]
		public void Asking_whether_a_controller_is_there_is_cheap()
		{
			// The update loop asks this for all four places on every pass, up to a thousand passes a
			// second, so an answer that costs a request to the driver would be paid for in polling rate.
			// The budget is far above a field read and far below anything that leaves the process.
			const int Calls = 200000;
			const int BudgetMs = 300;
			var client = Connected();
			try
			{
				client.PlugIn(1);
				client.IsControllerConnected(1);
				var watch = Stopwatch.StartNew();
				for (var i = 0; i < Calls; i++)
					client.IsControllerConnected(1);
				watch.Stop();
				System.Console.WriteLine(Calls + " questions in " + watch.ElapsedMilliseconds + " ms");
				Assert.IsTrue(watch.ElapsedMilliseconds < BudgetMs,
					Calls + " questions took " + watch.ElapsedMilliseconds + " ms, over the " +
					BudgetMs + " ms budget. The update loop asks this four times a pass, so this comes " +
					"straight off the polling rate.");
			}
			finally
			{
				client.UnPlug(1);
			}
		}

	}
}
