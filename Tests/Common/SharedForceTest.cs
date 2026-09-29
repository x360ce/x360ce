// @under-test: App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs, App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs, App.v4/Common/DInput/DeviceRouting.cs
// @area: force-feedback   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;
using static x360ce.Tests.ForceFeedbackFixtures;

namespace x360ce.Tests
{
	/// <summary>Force feedback for a device on two tabs: the strongest of each motor, kept while a tab says nothing new.</summary>
	/// <remarks>
	/// Each tab's Force feedback enabled switch says whether its game's rumble reaches the device. With it on
	/// for more than one tab, the device plays the strongest of each motor, and a tab whose game said nothing
	/// new this pass counts with what it asked for last: games say what they want when it changes, so a tab
	/// left out for saying nothing would silence the other as soon as it spoke.
	/// </remarks>
	[TestClass]
	public class SharedForceTest
	{
		/// <summary>The text of one method, from its signature to its closing brace.</summary>
		static string Body(string source, string signature)
		{
			var start = source.IndexOf(signature);
			Assert.IsTrue(start >= 0, "Not found: " + signature);
			return source.Substring(start, source.IndexOf("\n\t\t}", start) - start);
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("What a game or the Test sliders ask for is taken once, in one step, with no lock")]
		public void A_force_is_taken_and_cleared_in_one_step()
		{
			var oldClient = ViGEmClient.Current;
			var client = (ViGEmClient)FormatterServices.GetUninitializedObject(typeof(ViGEmClient));
			GC.SuppressFinalize(client);
			client.Feedbacks = new Xbox360FeedbackReceivedEventArgs[4];
			var helper = new DInputHelper();
			try
			{
				ViGEmClient.Current = client;
				helper.SetVibration(MapTo.Controller3, 200, 100, 0);
				var taken = helper.CopyAndClearFeedbacks();
				Assert.AreEqual(200, taken[2].LargeMotor, "The Test sliders' force does not reach the engine.");
				Assert.IsNull(client.Feedbacks[2], "A force is taken again on the next pass.");
				// The bus's thread: a rumble from the controller in place 2 lands in slot 1 and is taken once.
				var pad = (Xbox360Controller)FormatterServices.GetUninitializedObject(typeof(Xbox360Controller));
				GC.SuppressFinalize(pad);
				client.Targets = new Xbox360Controller[4];
				client.Targets[1] = pad;
				var received = typeof(DInputHelper).GetMethod("Controller_FeedbackReceived", BindingFlags.NonPublic | BindingFlags.Instance);
				var rumble = Rumble(90, 30);
				received.Invoke(helper, new object[] { pad, rumble });
				Assert.AreSame(rumble, helper.CopyAndClearFeedbacks()[1], "A game's rumble does not reach the engine.");
				Assert.IsNull(helper.CopyAndClearFeedbacks()[1], "A game's rumble is taken twice.");
				ViGEmClient.Current = null;
				helper.SetVibration(MapTo.Controller3, 200, 100, 0);
				Assert.IsNull(helper.CopyAndClearFeedbacks()[2], "A force appeared with no bus client to ask for it.");
				received.Invoke(helper, new object[] { pad, rumble });
			}
			finally
			{
				ViGEmClient.Current = oldClient;
			}
			var step5 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			StringAssert.Contains(Body(step5, "public Xbox360FeedbackReceivedEventArgs[] CopyAndClearFeedbacks()"),
				"Interlocked.Exchange(ref client.Feedbacks[i], null)",
				"A force written between the take and the clear is cleared unread, and the controller keeps the one before it.");
			Assert.IsFalse(step5.Contains("FeedbackLock"), "The engine takes a lock on every pass for what its writers store in one step.");
			Assert.AreEqual(1, Ui.Count(Body(step5, "public void SetVibration("), "ViGEmClient.Current"),
				"The Test sliders read the bus client twice, and the engine can let it go between the two reads.");
			Assert.AreEqual(1, Ui.Count(Body(step5, "private void Controller_FeedbackReceived("), "ViGEmClient.Current"),
				"A game's rumble reads the bus client more than once, and the engine can let it go in between.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("Each pad keeps the last force its game asked for, and says which pads asked this pass")]
		public void Each_pad_keeps_its_last_force()
		{
			var large = new byte[4];
			var small = new byte[4];
			Assert.AreEqual((1 << 0) | (1 << 2), DInputHelper.KeepForces(new[] { Rumble(200, 10), null, Rumble(40, 90), null }, large, small));
			Assert.AreEqual(1 << 1, DInputHelper.KeepForces(new[] { null, Rumble(5, 6), null, null }, large, small));
			CollectionAssert.AreEqual(new byte[] { 200, 5, 40, 0 }, large, "A pad with nothing new lost what it asked for last.");
			CollectionAssert.AreEqual(new byte[] { 10, 6, 90, 0 }, small, "A pad with nothing new lost what it asked for last.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A device on one tab plays that tab's force; on two, the strongest of each motor")]
		public void Two_tabs_merge_to_the_strongest_of_each_motor()
		{
			var large = new byte[] { 200, 0, 40, 0 };
			var small = new byte[] { 10, 0, 90, 0 };
			byte l, s;
			Assert.IsTrue(DInputHelper.MergeForces(new[] { 0 }, 1 << 0, large, small, out l, out s));
			Assert.AreEqual(200, l, "One tab: not its own force.");
			Assert.AreEqual(10, s, "One tab: not its own force.");
			Assert.IsTrue(DInputHelper.MergeForces(new[] { 0, 2 }, 1 << 2, large, small, out l, out s),
				"The tab whose game spoke is one the force comes from, so the device has to be told.");
			Assert.AreEqual(200, l, "The first tab said nothing new and stopped being felt.");
			Assert.AreEqual(90, s, "The strongest small motor is not the one played.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A tab whose switch is off is not felt, however hard its game rumbles")]
		public void A_tab_not_chosen_is_not_felt()
		{
			var large = new byte[] { 0, 255, 0, 0 };
			var small = new byte[] { 0, 255, 0, 0 };
			byte l, s;
			Assert.IsFalse(DInputHelper.MergeForces(new[] { 0 }, 1 << 1, large, small, out l, out s),
				"A game on a tab the force does not come from made the device be told again.");
			Assert.AreEqual(0, l, "A tab whose force feedback is off drove the motors.");
			Assert.AreEqual(0, s, "A tab whose force feedback is off drove the motors.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("performance")]
		[Description("Keeping and merging forces hands nothing to the collector")]
		public void Merging_hands_nothing_to_the_collector()
		{
			var full = new PadSetting { ForceOverall = "100", LeftMotorStrength = "100", RightMotorStrength = "100" };
			var feedbacks = new Xbox360FeedbackReceivedEventArgs[4];
			var pads = new[] { 0, 2 };
			var places = new[] { 0, 0, -1, -1 };
			var settings = new[] { full, full, null, null };
			var lastLarge = new byte[4];
			var lastSmall = new byte[4];
			var large = new byte[4];
			var small = new byte[4];
			const int calls = 20000;
			byte l, s;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < calls; i++)
				{
					var arrived = DInputHelper.KeepForces(feedbacks, lastLarge, lastSmall) | 1;
					DInputHelper.MergeForces(pads, arrived, lastLarge, lastSmall, out l, out s);
					DInputHelper.MergePassedForces(places, settings, arrived, lastLarge, lastSmall, large, small);
				}
			});
			Assert.IsTrue(allocated < calls,
				"Merging " + calls + " times handed the collector " + allocated + " bytes; it runs on every pass.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("The engine keeps each pad's force, merges a device's tabs and each place, and forgets a pad taken away")]
		public void The_engine_merges_through_the_rule()
		{
			var dir = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput");
			var step2 = File.ReadAllText(Path.Combine(dir, "DInputHelper.Step2.UpdateDiStates.cs"));
			StringAssert.Contains(step2, "var arrived = KeepForces(feedbacks, _lastLargeMotor, _lastSmallMotor) | _forcesDropped;",
				"The engine does not keep what each pad's game asked for last.");
			StringAssert.Contains(step2, "PassForcesThrough(routing, arrived);");
			StringAssert.Contains(step2, "MergeForces(route.ForcePads, arrived, _lastLargeMotor, _lastSmallMotor, out large, out small)",
				"A device takes its force from one tab only.");
			Assert.IsFalse(step2.Contains("feedbacks[route.ForcePads[0]]"), "A device takes its force from one tab only.");
			// Kept first, then passed on, then merged for each device, all from the same bits.
			var keep = step2.IndexOf("var arrived = KeepForces(");
			var pass = step2.IndexOf("PassForcesThrough(routing, arrived);");
			var merge = step2.IndexOf("MergeForces(route.ForcePads, arrived,");
			Assert.IsTrue(keep > 0 && pass > keep && merge > pass,
				"The force is passed on or merged before what each pad's game asked for is kept.");
			var step5 = File.ReadAllText(Path.Combine(dir, "DInputHelper.Step5.VirtualDevices.cs"));
			StringAssert.Contains(step5, "MergePassedForces(_passPlaces, _passSettings, arrived, _lastLargeMotor, _lastSmallMotor, _passLarge, _passSmall)",
				"Two pads passing to one place are still sent one after the other.");
			var disable = step5.Substring(step5.IndexOf("public VirtualError DisableFeeding(uint userIndex)"));
			StringAssert.Contains(disable, "_forcesDropped |= 1 << (int)(userIndex - 1);",
				"A pad taken away goes on counting towards a device's force with what its game asked for last.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A tab taken from a device's force, by its tab switch or its Force feedback enabled switch, stops being felt at once")]
		public void A_tab_taken_away_stops_being_felt()
		{
			// Controller 2's game rumbled hard. Then Controller 2 stopped being a force source, so the new
			// routing leaves the device's force to Controller 1 alone, whose game has asked for nothing.
			var large = new byte[] { 0, 255, 0, 0 };
			var small = new byte[] { 0, 255, 0, 0 };
			byte l, s;
			// A new routing counts every pad as new once, so every device is told the force of the tabs it has now.
			Assert.IsTrue(DInputHelper.MergeForces(new[] { 0 }, 0xF, large, small, out l, out s),
				"A device whose force sources changed was not told.");
			Assert.AreEqual(0, l, "The tab taken away is still felt.");
			Assert.AreEqual(0, s, "The tab taken away is still felt.");
			// Nothing else tells the device: the tab taken away is no longer one of its force sources, so what
			// its game or its removal says next reaches the device no more.
			var step2 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step2.UpdateDiStates.cs"));
			var told = step2.IndexOf("if (routing != _forceRouting)");
			var pass = step2.IndexOf("PassForcesThrough(routing, arrived);");
			var merge = step2.IndexOf("MergeForces(route.ForcePads, arrived,");
			Assert.IsTrue(told > 0 && pass > told && merge > pass,
				"A new routing does not tell the devices and the places passed to, so a tab taken from one goes on being felt until a game speaks.");
			StringAssert.Contains(step2, "arrived |= 0xF;", "A new routing does not count every pad as new.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A device resting across a routing change is told its force once when it reads again")]
		public void A_device_resting_across_a_new_routing_is_told_when_it_reads_again()
		{
			// The device failed twice and rests through the pass that saw the new routing, so that pass never reached it.
			const int now = 1000;
			var ud = new UserDevice();
			DInputHelper.CountDeviceReadFailure(ud, now, true);
			DInputHelper.CountDeviceReadFailure(ud, now + 1, true);
			Assert.IsTrue(DInputHelper.IsDeviceReadResting(ud, now + 2), "A device that failed twice is read in the pass that sees the new routing.");
			Assert.IsFalse(DInputHelper.IsDeviceReadResting(ud, now + 1 + DInputHelper.DeviceReadRetryMs), "The device never reads again.");
			// A failed read lets go of the hold, so the device is held again when it reads, and being held again tells it.
			// A driver that keeps effects running through that would otherwise go on playing the tab taken away.
			var step2 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step2.UpdateDiStates.cs"));
			var failed = step2.IndexOf("if (CountDeviceReadFailure(ud, Environment.TickCount, benign))");
			var letGo = step2.IndexOf("ud.IsExclusiveMode = null;", failed);
			Assert.IsTrue(failed > 0 && letGo > failed, "A failed read keeps its hold, so the device is not held again when it comes back.");
			var hold = step2.IndexOf("ud.IsExclusiveMode = true;");
			var held = step2.IndexOf("held = true;", hold);
			Assert.IsTrue(hold > 0 && held > hold && held < step2.IndexOf("ud.IsExclusiveMode = false;"),
				"A device held again is not noted as held again.");
			// Once: the note belongs to one device's poll, and the next poll finds the device already held.
			var poll = step2.IndexOf("for (int i = 0; i < userDevices.Length; i++)");
			var declared = step2.IndexOf("var held = false;");
			Assert.IsTrue(poll > 0 && declared > poll && declared < hold, "Being held again is noted for longer than one poll of one device.");
			StringAssert.Contains(step2, "if (forceArrived || held || ud.FFState.Changed(ps))",
				"A device held again is not told its force, so it plays what it played before it was let go of.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A device on a switched-on tab whose settings are not stored is stopped, not left playing")]
		public void A_device_with_no_stored_settings_is_stopped()
		{
			var device = Guid.NewGuid();
			var game = new UserGame { FileName = "shared-force.exe", EnableMask = (int)MapToMask.Controller1 };
			var row = new UserSetting { InstanceGuid = device, FileName = game.FileName, MapTo = (int)MapTo.Controller1, IsEnabled = true, PadSettingChecksum = Guid.NewGuid() };
			DeviceForce force;
			Assert.IsTrue(DeviceRouting.Build(game, new[] { row }, new PadSetting[0]).TryGetForce(device, out force),
				"A device on a switched-on tab is not routed.");
			Assert.IsNull(force.PadSetting, "Settings that are not stored were found.");
			Assert.AreEqual(0, force.ForcePads.Length, "Force comes from a tab with no settings.");
			// Such a device is mapped with no settings and no force source: it has to reach the stop.
			var step2 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step2.UpdateDiStates.cs"));
			var mapped = step2.IndexOf("if (mapped)");
			var forcing = step2.IndexOf("if (route.ForcePads.Length > 0)", mapped);
			var stop = step2.IndexOf("ud.FFState.StopDeviceForces(device);", mapped);
			Assert.IsTrue(mapped > 0 && forcing > mapped && stop > forcing, "The force block and its stop are not where the test expects them.");
			Assert.IsFalse(step2.Substring(mapped, stop - mapped).Contains("if (ps != null)"),
				"A mapped device whose settings are not stored never reaches the stop, and goes on playing its last effect.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("Whether a device is forced is read from the routing's force sources, not from settings that can change after it was built")]
		public void Forcing_is_read_from_the_force_sources()
		{
			var step2 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step2.UpdateDiStates.cs"));
			Assert.IsFalse(step2.Contains("ps.ForceEnable == \"1\""),
				"Forcing is judged by the settings, which can differ from the force sources the routing was built with.");
			StringAssert.Contains(step2, "forcingFromHere = mapped && route.ForcePads.Length > 0;");
			StringAssert.Contains(step2, "if (route.ForcePads.Length > 0)");
		}
	}
}
