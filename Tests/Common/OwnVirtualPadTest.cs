// @under-test: App.v4/Common/DInput/VirtualDriverInstaller.cs, App.v4/ViGEm/Client/ViGEmClient.x360ce.cs
// @area: devices   @layer: integration-db
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using System.Linq;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// Telling this program's own virtual controllers from ones left behind by a run that died.
	/// </summary>
	/// <remarks>
	/// The program judged that by asking the bus what it had connected at that instant. It is wrong
	/// twice over. A controller is ours before the bus reports it connected, and it stays ours while
	/// Windows is still taking it away after we let go. Putting a controller in a chosen XInput place
	/// also connects the places below it and lets them go again, and those brief ones look exactly
	/// like leftovers.
	///
	/// The result a person saw: switch emulation on, and the program immediately offered to remove the
	/// controller it had just created.
	///
	/// These plug real controllers in, so they need the virtual bus installed, and they unplug
	/// whatever they created even when they fail.
	/// </remarks>
	[TestClass]
	public class OwnVirtualPadTest
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
			// Controllers left by something else make every answer below meaningless: what this test
			// creates cannot be told from what was already there. Said as unable to judge rather than
			// as a failure, because the code may be perfectly correct and the machine merely untidy.
			var already = Leftovers();
			if (already.Length > 0)
				Assert.Inconclusive(
					already.Length + " virtual controllers were already left behind before this ran. " +
					"Remove them from the Issues page, then run again. " + string.Join(", ", already));
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

		static string[] Leftovers()
		{
			return VirtualDriverInstaller.GetLeftoverVirtualPads()
				.Select(x => x.DeviceId)
				.ToArray();
		}

		[TestMethod, TestCategory("devices"), TestCategory("requires-elevation")]
		[Description("A controller this program just created is not called a leftover")]
		public void A_controller_this_program_just_created_is_not_called_a_leftover()
		{

			var client = Connected();
			try
			{
				// Third place, so the two below it are connected and let go again on the way. Those are
				// the ones the program used to report as somebody else's.
				Assert.IsTrue(client.PlugIn(3), "The controller could not be plugged in.");
				var whileHeld = Leftovers();
				Assert.AreEqual(0, whileHeld.Length,
					"The program is calling a controller it created itself a leftover, and offering to " +
					"remove the one emulation is running on. " + string.Join(", ", whileHeld));
			}
			finally
			{
				client.UnPlug(3);
			}

			// Straight after letting go, while Windows may still be taking it away. This is the moment
			// switching emulation off used to report a leftover.
			var afterRelease = Leftovers();
			Assert.AreEqual(0, afterRelease.Length,
				"A controller this program let go of a moment ago is being called somebody else's. " +
				string.Join(", ", afterRelease));
		}

		[TestMethod, TestCategory("devices"), TestCategory("requires-elevation")]
		[Description("Switching emulation on and off leaves nothing behind")]
		public void Switching_emulation_on_and_off_leaves_nothing_behind()
		{
			// What a person does when they try a setting: on, off, on, off. Each round used to add
			// controllers the program then blamed on an earlier run.
			var client = Connected();
			try
			{
				for (var round = 0; round < 3; round++)
				{
					client.PlugIn(1);
					client.UnPlug(1);
				}
			}
			finally
			{
				client.UnPlug(1);
			}
			var left = Leftovers();
			Assert.AreEqual(0, left.Length,
				"Turning emulation on and off three times left " + left.Length + " controllers the " +
				"program no longer recognises as its own. " + string.Join(", ", left));
		}

	}
}
