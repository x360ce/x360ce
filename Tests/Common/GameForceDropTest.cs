// @under-test: App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs, App.v4/Common/DInput/DInputHelper.cs
// @area: force-feedback   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using System;
using System.IO;
using System.Reflection;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.Engine;
using static x360ce.Tests.BusRefusalFixtures;
using static x360ce.Tests.ForceFeedbackFixtures;
using static x360ce.Tests.MisplacedPlugTest;
using static x360ce.Tests.SharedForceTest;
using static x360ce.Tests.StandInXInputFixtures;

namespace x360ce.Tests
{
	/// <summary>What a game asked for goes when its virtual controller goes, by every way one goes, and stays while it stays.</summary>
	/// <remarks>
	/// A game says what it wants when that changes. Once its controller is gone, its last force and a rumble not yet taken
	/// would go on driving the device and any place the force is passed on to: a motor left running, worse than a rumble
	/// left off. Taking a controller away needs the native bus, so the ways that need it are read from the source.
	/// </remarks>
	[TestClass]
	public class GameForceDropTest
	{
		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A force asked for just before Repair, a removal or a reorder lets go of the controllers is not taken once they are picked back up; one asked for after is")]
		public void A_rumble_from_before_letting_go_is_not_taken_after()
		{
			// A bus client with nothing native behind it and a stand-in controller in each slot, so letting go of them unplugs
			// nothing on the machine.
			var client = RefusedRemovalTest.Client();
			for (var i = 0; i < client.Targets.Length; i++)
				client.Targets[i] = RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE);
			var helper = new DInputHelper();
			var received = typeof(DInputHelper).GetMethod("Controller_FeedbackReceived", BindingFlags.NonPublic | BindingFlags.Instance);
			var oldClient = ViGEmClient.Current;
			ViGEmClient.Current = client;
			try
			{
				// Controllers 1 and 2's games rumble after the last pass took what they asked for, and then the controllers are
				// let go of. The Test sliders ask for a force on Controllers 3 and 4 while they are away.
				received.Invoke(helper, new object[] { client.Targets[0], Rumble(200, 100) });
				received.Invoke(helper, new object[] { client.Targets[1], Rumble(200, 100) });
				RefusedRemovalTest.Faults(() => Assert.IsTrue(helper.ReleaseForDeviceRemoval(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)),
					"The controllers were not let go of."));
				helper.SetVibration(MapTo.Controller3, 200, 100, 0);
				helper.SetVibration(MapTo.Controller4, 200, 100, 0);
				helper.ResumeAfterDeviceRemoval();
				var taken = helper.CopyAndClearFeedbacks();
				for (var i = 0; i < taken.Length; i++)
					Assert.IsNull(taken[i], "A force asked for on Controller " + (i + 1) + " before the controllers were picked back up comes back after.");
				// Asked for once they are back: taken, and taken once.
				var after = Rumble(90, 30);
				received.Invoke(helper, new object[] { client.Targets[1], after });
				Assert.AreSame(after, helper.CopyAndClearFeedbacks()[1], "A rumble asked for after the controllers were picked back up is lost.");
				Assert.IsNull(helper.CopyAndClearFeedbacks()[1], "A rumble asked for after the controllers were picked back up is taken twice.");
			}
			finally
			{
				ViGEmClient.Current = oldClient;
			}
			// Dropped with the rest of what each game asked for, before the input thread goes on.
			var step5 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			var resume = Body(step5, "public void ResumeAfterDeviceRemoval()");
			var cleared = resume.IndexOf("DropGameForce(pad);");
			Assert.IsTrue(cleared > 0 && cleared < resume.IndexOf("Suspended = false;"),
				"A force asked for before the controllers were let go of is taken by the first pass after they are picked back up.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("Every way a virtual controller goes drops what its game asked for, in one step: the rumble not yet taken and its last force")]
		public void Every_way_a_controller_goes_drops_its_games_force()
		{
			var flags = BindingFlags.NonPublic | BindingFlags.Instance;
			var drop = typeof(DInputHelper).GetMethod("DropGameForce", flags);
			Assert.IsNotNull(drop, "Nothing drops what a pad's game asked for in one step when its controller is taken away.");
			var helper = new DInputHelper();
			var lastLarge = (byte[])typeof(DInputHelper).GetField("_lastLargeMotor", flags).GetValue(helper);
			var lastSmall = (byte[])typeof(DInputHelper).GetField("_lastSmallMotor", flags).GetValue(helper);
			var dropped = typeof(DInputHelper).GetField("_forcesDropped", flags);
			var client = RefusedRemovalTest.Client();
			var oldClient = ViGEmClient.Current;
			ViGEmClient.Current = client;
			try
			{
				// Controllers 1 and 3's games rumbled, and each has a rumble not yet taken. Controller 3's controller goes.
				lastLarge[0] = lastLarge[2] = 200;
				lastSmall[0] = lastSmall[2] = 100;
				var other = Rumble(90, 30);
				client.Feedbacks[0] = other;
				client.Feedbacks[2] = Rumble(90, 30);
				drop.Invoke(helper, new object[] { 3u });
				Assert.IsNull(client.Feedbacks[2], "A rumble asked for before Controller 3's controller went is taken by the next pass.");
				Assert.AreEqual(0, lastLarge[2], "Controller 3's game goes on driving the force after its controller went.");
				Assert.AreEqual(0, lastSmall[2], "Controller 3's game goes on driving the force after its controller went.");
				Assert.AreEqual(1 << 2, (int)dropped.GetValue(helper), "The devices and places Controller 3's game drove are not told.");
				Assert.AreSame(other, client.Feedbacks[0], "Another pad's rumble was dropped.");
				Assert.AreEqual(200, lastLarge[0], "Another pad's force was dropped.");
				// With no bus client there is no slot to empty, and the force goes all the same.
				ViGEmClient.Current = null;
				drop.Invoke(helper, new object[] { 1u });
				Assert.AreEqual(0, lastLarge[0], "With no bus client, a game's force stays after its controller went.");
				Assert.AreEqual((1 << 2) | (1 << 0), (int)dropped.GetValue(helper), "With no bus client, the devices a game drove are not told.");
			}
			finally
			{
				ViGEmClient.Current = oldClient;
			}
			// One step, in one place. Taking a controller away needs the native bus, so the calls are read from the source.
			var step5 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			StringAssert.Contains(Body(step5, "void DropGameForce(uint userIndex)"), "Interlocked.Exchange(ref client.Feedbacks[userIndex - 1], null)",
				"The rumble not yet taken is emptied some other way than the engine takes it, so one written in between is lost or kept.");
			Assert.AreEqual(2, Ui.Count(step5, "Interlocked.Exchange(ref client.Feedbacks["), "A slot is emptied somewhere other than the take and the drop.");
			Assert.AreEqual(1, Ui.Count(step5, "_lastLargeMotor[userIndex - 1] = 0;"), "A game's force is dropped in more than one place.");
			Assert.AreEqual(1, Ui.Count(step5, "_forcesDropped |="), "A pad is counted as taken away in more than one place.");
			Assert.IsFalse(step5.Contains("Array.Clear(_lastLargeMotor") || step5.Contains("_forcesDropped = 0xF"), "A second copy drops every game's force.");
			// Letting go of one: once the bus has let it go, and only then.
			var disable = Body(step5, "public VirtualError DisableFeeding(uint userIndex)");
			var unplugged = disable.IndexOf("if (success)");
			Assert.IsTrue(unplugged > 0, "The unplug that worked is no longer where this test looks for it.");
			var block = disable.Substring(unplugged, disable.IndexOf("\n\t\t\t}", unplugged) - unplugged);
			StringAssert.Contains(block, "DropGameForce(userIndex);",
				"A rumble asked for just before a pad's controller was taken away is taken by the next pass, after its last force was cleared.");
			// A report the bus refused: the controller is let go of and made again, and nothing of its game plays on meanwhile.
			var refused = Ui.Between(step5, "if (!client.UnPlug(i))", "\n\t\t\t\t\t}");
			var retry = refused.IndexOf("_NextPlugAttempt[i - 1]");
			var otherwise = refused.IndexOf("else");
			Assert.IsTrue(retry > 0 && otherwise > retry && refused.IndexOf("DropGameForce(i);") > otherwise,
				"A controller let go of after a refused report leaves its game's force playing while it is made again.");
			// Leaving virtual emulation: every controller goes with the client, let go of by name or not.
			var leave = Ui.Between(step5, "if (!isVirtual)", "virtualModeActive = false;");
			var disposed = leave.IndexOf("ViGEmClient.DisposeCurrent();");
			Assert.IsTrue(disposed > 0 && leave.IndexOf("DropGameForce(i);") > disposed,
				"A game's force goes on after leaving virtual emulation took its controller away.");
			// One put in the wrong place is taken away on the plug worker, and dropped by the input thread as it takes the plug in.
			StringAssert.Contains(Body(step5, "public VirtualError PlugOutcome("), "DropGameForce(userIndex);",
				"A game's force goes on after its controller was taken away from another tab's place.");
			// Picking the controllers back up after Repair, Remove Leftover Pads or Auto-Order.
			StringAssert.Contains(Body(step5, "public void ResumeAfterDeviceRemoval()"), "DropGameForce(pad);",
				"A game's force comes back after the controllers are picked back up.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("Leaving virtual emulation drops every game's force, whether its controller was let go of or not")]
		public void Leaving_virtual_emulation_drops_every_games_force()
		{
			var flags = BindingFlags.NonPublic | BindingFlags.Instance;
			var helper = new DInputHelper();
			var lastLarge = (byte[])typeof(DInputHelper).GetField("_lastLargeMotor", flags).GetValue(helper);
			var lastSmall = (byte[])typeof(DInputHelper).GetField("_lastSmallMotor", flags).GetValue(helper);
			var active = typeof(DInputHelper).GetField("virtualModeActive", flags);
			// In virtual emulation, with forces from Controllers 2 and 4 whose controllers there is no bus to let go of.
			active.SetValue(helper, true);
			lastLarge[1] = lastLarge[3] = 200;
			lastSmall[1] = lastSmall[3] = 100;
			// No bus client, and a refused connection still recent, so nothing asks the bus.
			var errorField = typeof(ViGEmClient).GetField("_LastConnectError", BindingFlags.NonPublic | BindingFlags.Static);
			var tickField = typeof(ViGEmClient).GetField("_LastConnectTick", BindingFlags.NonPublic | BindingFlags.Static);
			var oldClient = ViGEmClient.Current;
			var oldError = errorField.GetValue(null);
			var oldTick = tickField.GetValue(null);
			var o = SettingsManager.Options;
			var testEnabled = o.TestEnabled;
			try
			{
				ViGEmClient.Current = null;
				errorField.SetValue(null, VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND);
				tickField.SetValue(null, Environment.TickCount);
				o.TestEnabled = false;
				// No game any more, so nothing uses virtual emulation.
				typeof(DInputHelper).GetMethod("UpdateVirtualDevices", flags).Invoke(helper, new object[] { null });
			}
			finally
			{
				o.TestEnabled = testEnabled;
				errorField.SetValue(null, oldError);
				tickField.SetValue(null, oldTick);
				ViGEmClient.Current = oldClient;
			}
			Assert.IsFalse((bool)active.GetValue(helper), "Virtual emulation was not left, so the test shows nothing.");
			CollectionAssert.AreEqual(new byte[4], lastLarge, "A game's force goes on after leaving virtual emulation took its controller away.");
			CollectionAssert.AreEqual(new byte[4], lastSmall, "A game's force goes on after leaving virtual emulation took its controller away.");
			Assert.AreEqual(0xF, (int)typeof(DInputHelper).GetField("_forcesDropped", flags).GetValue(helper),
				"The devices and places the games drove are not told that their controllers went.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A controller taken away from another tab's place takes what its game asked for there with it, dropped as the plug is taken in; a plug held back takes nothing")]
		public void A_controller_taken_from_the_wrong_place_takes_its_games_force_with_it()
		{
			var helper = new DInputHelper();
			var client = RefusedRemovalTest.Client();
			var pad = RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE);
			var lastLarge = Field<byte[]>(helper, "_lastLargeMotor");
			var lastSmall = Field<byte[]>(helper, "_lastSmallMotor");
			var dropped = typeof(DInputHelper).GetField("_forcesDropped", BindingFlags.NonPublic | BindingFlags.Instance);
			var rumbles = new[] { ForceFeedbackFixtures.Rumble(90, 30), ForceFeedbackFixtures.Rumble(60, 20) };
			var results = new VirtualError[2];
			var slots = new object[2];
			var large = new byte[2];
			var small = new byte[2];
			var told = new int[2];
			RefusedRemovalTest.Faults(() => Logged(() => WithStandInXInput(() => OnStandInBus(client, pad, () =>
			{
				// Windows puts the controller in another tab's place, and it is taken away again. Then the places are as they
				// were, so the next plug is held back and makes nothing.
				for (var attempt = 0; attempt < 2; attempt++)
				{
					var plugging = helper.BeginPlug(1);
					Assert.IsTrue(plugging.Wait(20000), "The plug did not finish.");
					// Controller 1's game rumbled while the plug ran: its force, and a rumble not yet taken.
					lastLarge[0] = 200;
					lastSmall[0] = 100;
					client.Feedbacks[0] = rumbles[attempt];
					dropped.SetValue(helper, 0);
					// Taken in as the input thread takes in every finished plug.
					results[attempt] = helper.PlugOutcome(1, plugging);
					slots[attempt] = client.Feedbacks[0];
					large[attempt] = lastLarge[0];
					small[attempt] = lastSmall[0];
					told[attempt] = (int)dropped.GetValue(helper);
				}
			}))));
			Assert.AreEqual(VirtualError.PlaceWrong, results[0], "Windows put the stand-in controller in XInput 3.");
			Assert.IsFalse(pad.Attached, "The controller put in another tab's place was not taken away.");
			Assert.IsNull(slots[0], "A rumble asked for while the controller sat in another tab's place is taken after it went.");
			Assert.AreEqual(0, large[0], "Controller 1's game goes on driving the force after its controller was taken away again.");
			Assert.AreEqual(0, small[0], "Controller 1's game goes on driving the force after its controller was taken away again.");
			Assert.AreEqual(1, told[0], "The devices and places Controller 1's game drove are not told that its controller went.");
			// Held back: nothing was made or taken away, so nothing of the game's is dropped.
			Assert.AreEqual(1, pad.ConnectsAsked, "The second plug was not held back with the places as they were.");
			Assert.IsTrue(Field<bool[]>(helper, "_heldBack")[0], "The second plug was not held back.");
			Assert.AreSame(rumbles[1], slots[1], "A plug held back dropped a rumble, though no controller went.");
			Assert.AreEqual(200, large[1], "A plug held back dropped the pad's force, though no controller went.");
			Assert.AreEqual(100, small[1], "A plug held back dropped the pad's force, though no controller went.");
			Assert.AreEqual(0, told[1], "A plug held back told the devices and places that a controller went.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A controller the bus will not let go of from another tab's place is still there, so its game's force stays")]
		public void A_controller_the_bus_keeps_in_the_wrong_place_keeps_its_games_force()
		{
			var helper = new DInputHelper();
			var client = RefusedRemovalTest.Client();
			var pad = RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_REMOVAL_FAILED);
			var lastLarge = Field<byte[]>(helper, "_lastLargeMotor");
			var lastSmall = Field<byte[]>(helper, "_lastSmallMotor");
			var rumble = Rumble(90, 30);
			var result = VirtualError.None;
			object slot = null;
			RefusedRemovalTest.Faults(() => Logged(() => WithStandInXInput(() => OnStandInBus(client, pad, () =>
			{
				var plugging = helper.BeginPlug(1);
				Assert.IsTrue(plugging.Wait(20000), "The plug did not finish.");
				// Controller 1's game rumbled the controller in another tab's place, which the bus then would not let go of.
				lastLarge[0] = 200;
				lastSmall[0] = 100;
				client.Feedbacks[0] = rumble;
				result = helper.PlugOutcome(1, plugging);
				slot = client.Feedbacks[0];
			}))));
			Assert.AreEqual(VirtualError.PlaceWrong, result, "Windows put the stand-in controller in XInput 3.");
			Assert.IsTrue(pad.Attached, "The bus let go of a controller it keeps.");
			Assert.IsFalse(Field<bool[]>(helper, "_takenAgain")[0], "A controller the bus kept is taken for gone.");
			Assert.AreSame(rumble, slot, "A controller the bus kept lost its game's rumble, though it is still there.");
			Assert.AreEqual(200, lastLarge[0], "A controller the bus kept lost its game's force, though it is still there.");
			Assert.AreEqual(100, lastSmall[0], "A controller the bus kept lost its game's force, though it is still there.");
			Assert.AreEqual(0, Field<int>(helper, "_forcesDropped"), "The devices were told a controller went that is still there.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A new controller put in the slot of one the bus would not let go of drops that one's game force as the plug is taken in, whatever the plug came to")]
		public void A_controller_replaced_while_the_bus_keeps_it_drops_its_games_force()
		{
			// The game's rumbles to the kept controller, its stop among them, no longer reach the engine.
			var helper = new DInputHelper();
			var client = RefusedRemovalTest.Client();
			var lastLarge = Field<byte[]>(helper, "_lastLargeMotor");
			var lastSmall = Field<byte[]>(helper, "_lastSmallMotor");
			lastLarge[0] = 200;
			lastSmall[0] = 100;
			client.Feedbacks[0] = Rumble(90, 30);
			// What the worker leaves: the flag, and its answer that the kept controller still holds the place.
			Field<bool[]>(helper, "_takenAgain")[0] = true;
			var oldClient = ViGEmClient.Current;
			ViGEmClient.Current = client;
			try
			{
				Assert.AreEqual(VirtualError.RemovalRefused, helper.PlugOutcome(1, System.Threading.Tasks.Task.FromResult(VirtualError.RemovalRefused)),
					"The plug's answer was not handed back.");
			}
			finally
			{
				ViGEmClient.Current = oldClient;
			}
			Assert.IsNull(client.Feedbacks[0], "A rumble the replaced controller's game asked for is taken after it was replaced.");
			Assert.AreEqual(0, lastLarge[0], "The replaced controller's game goes on driving the force.");
			Assert.AreEqual(0, lastSmall[0], "The replaced controller's game goes on driving the force.");
			Assert.AreEqual(1, Field<int>(helper, "_forcesDropped"), "The devices and places the replaced controller's game drove are not told.");
			// Replacing needs a controller the bus reads as attached, which only the native bus says, so the worker is read: the
			// flag is set as the kept controller is replaced, and nothing later in the plug clears it.
			var step5 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			var enable = Ui.Between(step5, "public VirtualError EnableFeeding(", "public VirtualError DisableFeeding(");
			var replaced = enable.IndexOf("client.Replace(userIndex, NewTarget(client));");
			var flagged = enable.IndexOf("_takenAgain[own] = true;", replaced + 1);
			Assert.IsTrue(replaced > 0 && flagged > replaced && flagged < enable.IndexOf("}", replaced),
				"A controller put in the slot of one the bus kept leaves the kept one's game force playing.");
			Assert.AreEqual(1, Ui.Count(enable, "_takenAgain[own] ="),
				"A later step of the plug clears the flag, so a replaced controller's game force plays on.");
		}
	}
}
