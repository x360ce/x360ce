// @under-test: App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs, App.v4/Common/DInput/XInputPlaces.cs, App.v4/Common/AppHelper.cs, App.v4/Common/DInput/DeviceRouting.cs
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
	/// <summary>Two tabs passing their force to one XInput place are merged there, not sent one after the other.</summary>
	/// <remarks>
	/// Where each pad passes its force is worked out from the routing and the current place table, and reused
	/// on every pass where neither has changed. Two pads that pass to the same place are merged at their own
	/// strengths, the same way two tabs of one device are, and the interface works out where force is passed
	/// by its own rule, free to disagree with the engine's.
	/// </remarks>
	[TestClass]
	public class SharedForcePassThroughTest
	{
		[TestMethod, TestCategory("force-feedback"), TestCategory("performance")]
		[Description("Working out where force is passed, whenever the routing or the place table is new, hands nothing to the collector")]
		public void Resolving_the_places_hands_nothing_to_the_collector()
		{
			var device = new UserDevice { InstanceGuid = Guid.NewGuid(), HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\1", DevDeviceId = "USB\\VID_045E&PID_028E\\1" };
			var routing = OwnPlaceRouting(device);
			// Found by the device's second id, so both lookups run on every resolve.
			var places = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { { device.DevDeviceId, 2 } };
			var helper = new DInputHelper();
			helper.ResolvePassPlaces(routing, 0, places);
			PadSetting ps;
			Assert.AreEqual(2, routing.PassThroughPlace(0, places, out ps),
				"The measured resolve does not find the place by the device's second id, so it measures less than the engine does.");
			const int calls = 20000;
			var version = 0;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				// A new version on every call, so every call works the places out.
				for (var i = 0; i < calls; i++)
					helper.ResolvePassPlaces(routing, ++version, places);
			});
			Assert.IsTrue(allocated < calls, calls + " resolves handed the collector " + allocated + " bytes; each runs on the engine thread.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("Two pads passing force to one place are merged there, each at its own strengths")]
		public void Two_pads_passing_to_one_place_are_merged()
		{
			var half = new PadSetting { ForceOverall = "50" };
			var places = new[] { 0, 0, 1, -1 };
			var settings = new[] { Source(new PadSetting()), Source(half), Source(new PadSetting()), null };
			var lastLarge = new byte[] { 100, 255, 30, 0 };
			var lastSmall = new byte[] { 0, 100, 30, 0 };
			var large = new byte[4];
			var small = new byte[4];

			var sent = DInputHelper.MergePassedForces(places, settings, 1 << 0, lastLarge, lastSmall, large, small);

			Assert.AreEqual(1 << 0, sent, "Only the place whose pads have something new is sent to.");
			Assert.AreEqual(128, large[0], "The second pad's force at its own half strength is the stronger large motor, and it was not used.");
			Assert.AreEqual(50, small[0], "The second pad said nothing new this pass and was left out.");
		}

		[TestMethod, TestCategory("force-feedback")]
		[Description("A place none of whose pads has anything new is sent nothing")]
		public void Nothing_new_sends_nothing()
		{
			var sent = DInputHelper.MergePassedForces(new[] { 0, 0, -1, -1 }, new[] { Source(new PadSetting()), Source(new PadSetting()), null, null },
				1 << 2, new byte[4], new byte[4], new byte[4], new byte[4]);
			Assert.AreEqual(0, sent, "A place was sent a force when none of its pads had anything new.");
		}

		/// <summary>A row passing force on with these settings' strengths, for the merge.</summary>
		static PassThroughSource Source(PadSetting ps)
		{
			return new PassThroughSource(ps, 0, null);
		}

		sealed class CountedPlaces : IReadOnlyDictionary<string, int>
		{
			readonly Dictionary<string, int> _places = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			public int Lookups;
			public CountedPlaces(string id, int place) { _places[id] = place; }
			public bool TryGetValue(string key, out int value) { Lookups++; return _places.TryGetValue(key, out value); }
			public bool ContainsKey(string key) { Lookups++; return _places.ContainsKey(key); }
			public int this[string key] { get { Lookups++; return _places[key]; } }
			public IEnumerable<string> Keys { get { return _places.Keys; } }
			public IEnumerable<int> Values { get { return _places.Values; } }
			public int Count { get { return _places.Count; } }
			public IEnumerator<KeyValuePair<string, int>> GetEnumerator() { return _places.GetEnumerator(); }
			IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
		}

		static DeviceRouting OwnPlaceRouting(UserDevice device)
		{
			var own = new PadSetting { ForcePassThrough = "1", ForcePassThroughIndex = "0", PadSettingChecksum = Guid.NewGuid() };
			var game = new UserGame { FileName = "pass-through.exe", FileProductName = "Pass through", EnableMask = (int)MapToMask.Controller1 };
			var row = new UserSetting { InstanceGuid = device.InstanceGuid, FileName = game.FileName, MapTo = (int)MapTo.Controller1, IsEnabled = true, PadSettingChecksum = own.PadSettingChecksum };
			return DeviceRouting.Build(game, new[] { row }, new[] { own }, new[] { device });
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("Where each pad passes its force is worked out when the routing or the place table changes, and reused on every other pass")]
		public void Pass_through_places_are_worked_out_once_per_change()
		{
			var device = new UserDevice { InstanceGuid = Guid.NewGuid(), HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\1" };
			var routing = OwnPlaceRouting(device);
			var places = new CountedPlaces(device.HidDeviceId, 2);
			var helper = new DInputHelper();

			helper.ResolvePassPlaces(routing, 1, places);
			Assert.AreEqual(1, places.Lookups, "The places were not worked out for a routing the engine had not seen.");
			for (var i = 0; i < 1000; i++)
				helper.ResolvePassPlaces(routing, 1, places);
			Assert.AreEqual(1, places.Lookups, "The places were worked out again on passes where neither the routing nor the place table changed.");
			helper.ResolvePassPlaces(routing, 2, places);
			Assert.AreEqual(2, places.Lookups, "A new place table was not read, so a controller that moved goes on being passed its force where it was.");
			helper.ResolvePassPlaces(OwnPlaceRouting(device), 2, places);
			Assert.AreEqual(3, places.Lookups, "A new routing was not read, so a tab that now passes elsewhere goes on passing to the old place.");

			// The engine's pass: skipped when no tab passes its force on, and otherwise asking with the table's version read
			// before the table, so a table published between the two reads is worked out again on the next pass.
			var step5 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			var method = step5.IndexOf("public void PassForcesThrough(DeviceRouting routing, int arrived)");
			var skip = step5.IndexOf("if (!routing.PassesForceThrough &&", method);
			var resolve = step5.IndexOf("ResolvePassPlaces(routing, XInputPlaces.Version, XInputPlaces.Current);", method);
			var merge = step5.IndexOf("MergePassedForces(_passPlaces, _passSources,", method);
			Assert.IsTrue(method > 0 && skip > method && resolve > skip && merge > resolve,
				"The pass does not skip pass-through when no tab uses it, or does not work the places out only on a change before merging.");
			Assert.IsFalse(step5.Contains("GetForcePassThroughPlace"), "The engine asks the settings lists where to pass force, under the lock the interface takes.");
			var app = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "AppHelper.cs"));
			StringAssert.Contains(app, "DeviceRouting.Current.PassThroughPlace(",
				"The interface works out where force is passed by a rule of its own, free to disagree with the engine's.");
			Assert.IsFalse(app.Contains("ForcePassThroughIndex"), "A second copy of the pass-through rule is left in the interface.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A tab switched off passes on the stop that unplugging its controller sends")]
		public void A_tab_switched_off_passes_on_the_stop()
		{
			// Controller 1's tab is switched off, and its settings pass force on to place 3.
			var named = new PadSetting { ForcePassThrough = "1", ForcePassThroughIndex = "3", PadSettingChecksum = Guid.NewGuid() };
			var game = new UserGame { FileName = "pass-stop.exe", FileProductName = "Pass stop", EnableMask = (int)MapToMask.Controller2 };
			var row = new UserSetting { InstanceGuid = Guid.NewGuid(), FileName = game.FileName, MapTo = (int)MapTo.Controller1, IsEnabled = true, PadSettingChecksum = named.PadSettingChecksum };
			var routing = DeviceRouting.Build(game, new[] { row }, new[] { named });
			var places = new int[4];
			var settings = new PassThroughSource[4];
			for (var pad = 0; pad < 4; pad++)
				places[pad] = routing.PassThroughPlace(pad, new Dictionary<string, int>(), out settings[pad]);
			// What DisableFeeding leaves when the tab's controller is unplugged: its last force 0, and the pad counted as new.
			var lastLarge = new byte[4];
			var lastSmall = new byte[4];
			var large = new byte[] { 255, 255, 255, 255 };
			var small = new byte[] { 255, 255, 255, 255 };

			var sent = DInputHelper.MergePassedForces(places, settings, 1 << 0, lastLarge, lastSmall, large, small);

			Assert.AreEqual(1 << 2, sent, "A tab switched off leaves the controller it passed force to buzzing.");
			Assert.AreEqual(0, large[2], "A tab switched off leaves the controller it passed force to buzzing.");
			Assert.AreEqual(0, small[2], "A tab switched off leaves the controller it passed force to buzzing.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A row unticked while its tab's force is passed on sends that place a stop once, and nothing after")]
		public void An_unticked_row_sends_a_stop_once()
		{
			// Passed to a place no controller holds, so nothing is felt.
			var place = FreePlace();
			var ps = new PadSetting { ForcePassThrough = "1", ForcePassThroughIndex = (place + 1).ToString(), PadSettingChecksum = Guid.NewGuid() };
			var game = new UserGame { FileName = "pass-untick.exe", FileProductName = "Pass untick", EnableMask = (int)MapToMask.Controller1 };
			var row = new UserSetting { InstanceGuid = Guid.NewGuid(), FileName = game.FileName, MapTo = (int)MapTo.Controller1, IsEnabled = true, PadSettingChecksum = ps.PadSettingChecksum };
			var ticked = DeviceRouting.Build(game, new[] { row }, new[] { ps });
			row.IsEnabled = false;
			var unticked = DeviceRouting.Build(game, new[] { row }, new[] { ps });
			var helper = new DInputHelper();
			var last = LastPassedForce(helper);
			var lastLarge = (byte[])typeof(DInputHelper).GetField("_lastLargeMotor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(helper);
			// Controller 1's game asks for a rumble, and it is passed on.
			lastLarge[0] = 200;
			helper.PassForcesThrough(ticked, 1 << 0);
			Assert.AreEqual(200 << 8, last[place], "The game's force was not passed on.");

			helper.PassForcesThrough(unticked, 0);

			Assert.AreEqual(0, last[place], "The controller an unticked row passed force to goes on vibrating.");
			// Once: marked as running again, the place is sent nothing more while the routing stays.
			last[place] = 1;
			helper.PassForcesThrough(unticked, 0);
			Assert.AreEqual(1, last[place], "A place no row passes to is sent a stop on every pass.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A stop XInput was too busy to take is sent on the next pass, with nothing new from the game")]
		public void A_refused_stop_is_sent_on_the_next_pass()
		{
			var routing = PassingToPlaceOne();
			var helper = new DInputHelper();
			var last = LastPassedForce(helper);
			// The input thread leaves loading the library to the places reader.
			Assert.IsNotNull(DInputHelper.OccupiedPlaces(), "XInput does not answer, so the library is not loaded before the test.");
			// The controller's motors are running, and the game has just asked for them to stop. Only a stop is ever sent.
			var running = (100 << 8) | 50;
			last[0] = running;
			long took = -1;
			// A read of the places holds XInput, so the stop is refused.
			Assert.IsTrue(HeldLock.Finishes(() =>
			{
				var watch = System.Diagnostics.Stopwatch.StartNew();
				helper.PassForcesThrough(routing, 1 << 0);
				took = watch.ElapsedMilliseconds;
			}, 1000, XInputPlacesNotAnsweringTest.LoadLock()), "The pass waits for XInput.");
			Assert.IsTrue(took < 50, "The pass waited " + took + " ms for XInput.");
			Assert.AreEqual(running, last[0], "A stop XInput was too busy to take is recorded as sent.");
			// XInput free again, and nothing new from the game.
			helper.PassForcesThrough(routing, 0);
			Assert.AreEqual(0, last[0], "A stop XInput was too busy to take is not sent on the next pass, so the controller goes on buzzing.");
		}

		/// <summary>A place Windows' XInput reports no controller in, so a force sent there reaches nothing.</summary>
		internal static int FreePlace()
		{
			var taken = DInputHelper.OccupiedPlaces();
			if (taken == null)
				Assert.Inconclusive("XInput did not answer, so no free place is known.");
			var free = Array.IndexOf(taken, false);
			if (free < 0)
				Assert.Inconclusive("All four XInput places are taken, so there is no place to send a force that reaches nothing.");
			return free;
		}

		static bool PassForceTo(DInputHelper helper, int place, byte large, byte small)
		{
			var method = typeof(DInputHelper).GetMethod("PassForceTo", BindingFlags.NonPublic | BindingFlags.Instance);
			return (bool)method.Invoke(helper, new object[] { place, large, small, TimeSpan.FromSeconds(1) });
		}

		internal static int[] LastPassedForce(DInputHelper helper)
		{
			return (int[])typeof(DInputHelper).GetField("_lastPassedForce", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(helper);
		}

		/// <summary>Controller 1 passing its force to place 1, with the strengths left unset, as a loaded setting at full strength leaves them.</summary>
		/// <param name="forceOverall">The stored overall strength, or null to leave it unset (full).</param>
		internal static DeviceRouting PassingToPlaceOne(string forceOverall = null)
		{
			var ps = new PadSetting { ForcePassThrough = "1", ForcePassThroughIndex = "1", PadSettingChecksum = Guid.NewGuid() };
			if (forceOverall != null)
				ps.ForceOverall = forceOverall;
			var game = new UserGame { FileName = "pass-retry.exe", FileProductName = "Pass retry", EnableMask = (int)MapToMask.Controller1 };
			var row = new UserSetting { InstanceGuid = Guid.NewGuid(), FileName = game.FileName, MapTo = (int)MapTo.Controller1, IsEnabled = true, PadSettingChecksum = ps.PadSettingChecksum };
			return DeviceRouting.Build(game, new[] { row }, new[] { ps });
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A force XInput was too busy to take is sent again as it was worked out: a retry makes nothing and takes no lock the interface takes")]
		public void A_retried_force_is_not_worked_out_again()
		{
			var routing = PassingToPlaceOne();
			var helper = new DInputHelper();
			var last = LastPassedForce(helper);
			Assert.IsNotNull(DInputHelper.OccupiedPlaces(), "XInput does not answer, so the library is not loaded before the test.");
			var running = (100 << 8) | 50;
			last[0] = running;
			// Held the way a read of the places holds it, saying so.
			var enter = HeldLock.Enter(typeof(SystemXInput), "EnterLoadLock");
			var exit = HeldLock.Exit(typeof(SystemXInput), "ExitLoadLock");
			Assert.IsTrue(HeldLock.Finishes(() => helper.PassForcesThrough(routing, 1 << 0), 5000, enter, exit), "The pass waits for the load lock.");
			Assert.AreEqual(running, last[0], "The stop was sent while a read held the load lock.");
			// The interface reads a setting's default under this lock; the strengths left unset are read that way.
			var defaults = typeof(JocysCom.ClassLibrary.Runtime.Attributes).GetField("DefaultValuesLock", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
			const int passes = 20000;
			long bytes = -1;
			Assert.IsTrue(HeldLock.Finishes(() =>
			{
				bytes = Allocations.FewestBytes(5, () =>
				{
					for (var i = 0; i < passes; i++)
						helper.PassForcesThrough(routing, 0);
				});
			}, 10000, enter, exit, defaults), "A retry waits for the lock the interface reads a setting's default under.");
			Assert.IsTrue(bytes < passes, passes + " retries handed the collector " + bytes + " bytes.");
			Assert.AreEqual(running, last[0], "The stop was sent while a read held the load lock.");
			// Free: the stop is sent as it was worked out.
			helper.PassForcesThrough(routing, 0);
			Assert.AreEqual(0, last[0], "The stop was not sent once the load lock was free.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A routing rebuild that changes a pad's stored strengths sends the force already passed through again, at the new strengths, with no new force from the game")]
		public void A_strength_change_from_a_rebuild_is_sent_with_no_new_game_force()
		{
			var full = PassingToPlaceOne();
			var helper = new DInputHelper();
			var last = LastPassedForce(helper);
			var lastLarge = (byte[])typeof(DInputHelper).GetField("_lastLargeMotor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(helper);
			lastLarge[0] = 200;
			Assert.IsNotNull(DInputHelper.OccupiedPlaces(), "XInput does not answer, so the library is not loaded before the test.");
			// The game's force is sent at full strength, the setting left unset.
			helper.PassForcesThrough(full, 1 << 0);
			Assert.AreEqual((200 << 8) | 0, last[0], "The full-strength force was not sent.");
			// The routing is rebuilt with the strength halved. The game asks for nothing new.
			var half = PassingToPlaceOne("50");
			helper.PassForcesThrough(half, 0);
			Assert.AreEqual((100 << 8) | 0, last[0], "A strength change from a rebuild was not sent without a new force from the game.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A controller passed a force of any strength is stopped when Repair, a removal or a reorder lets go of the controllers")]
		public void Any_force_passed_on_is_stopped_when_the_controllers_are_let_go_of()
		{
			var place = FreePlace();
			var helper = new DInputHelper();
			var last = LastPassedForce(helper);
			// Either side of the large motor's top bit, and full strength.
			foreach (var force in new[] { new byte[] { 127, 0 }, new byte[] { 128, 0 }, new byte[] { 200, 100 }, new byte[] { 255, 255 } })
			{
				Assert.IsTrue(PassForceTo(helper, place, force[0], force[1]), "A force was not sent while XInput was free.");
				Assert.AreNotEqual(0, last[place], "A force of " + force[0] + " and " + force[1] + " was recorded as a stop.");
				// No bus client, so letting go unplugs nothing.
				var client = ViGEmClient.Current;
				ViGEmClient.Current = null;
				try
				{
					Assert.IsTrue(helper.ReleaseForDeviceRemoval(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)), "The controllers were not let go of.");
				}
				finally
				{
					ViGEmClient.Current = client;
				}
				Assert.AreEqual(0, last[place], "A controller passed a large motor of " + force[0] + " and a small motor of " + force[1] +
					" is left vibrating when the controllers are let go of.");
			}
			// Repair, removing leftover controllers and putting controllers in order each let go of the controllers this way first.
			var dir = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput");
			var installer = File.ReadAllText(Path.Combine(dir, "VirtualDriverInstaller.cs"));
			StringAssert.Contains(Ui.Between(installer, "public static void RepairViGEmBusElevated()", "Program.RunElevated("),
				"helper.ReleaseForDeviceRemoval(", "Repair does not let go of the controllers first.");
			StringAssert.Contains(Ui.Between(installer, "public static string RemoveLeftoverPadsElevated(", "succeeded = false;"),
				"helper.ReleaseForDeviceRemoval(", "Removing leftover controllers does not let go of the controllers first.");
			StringAssert.Contains(Ui.Between(File.ReadAllText(Path.Combine(dir, "DInputHelper.cs")), "public bool StopForReorder(", "SettingsManager.UserDevices.SyncRoot"),
				"ReleaseForDeviceRemoval(", "Putting controllers in order does not let go of the controllers first.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("The first force passed on at full strength is sent, not taken as already sent")]
		public void The_first_full_force_is_sent()
		{
			var place = FreePlace();
			var helper = new DInputHelper();
			var last = LastPassedForce(helper);
			Assert.AreEqual(-1, last[place], "A new helper has a force recorded as sent.");
			Assert.IsTrue(PassForceTo(helper, place, 255, 255), "A force was not sent while XInput was free.");
			Assert.AreNotEqual(-1, last[place], "The first full-strength force was taken as already sent, so it never went out.");
			helper.StopPassedForces();
			Assert.AreEqual(0, last[place], "A full-strength force is not stopped.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("performance")]
		[Description("Passing force through and taking what the games asked for hand nothing to the collector")]
		public void Passing_through_hands_nothing_to_the_collector()
		{
			if (Global.DHelper == null)
				Global.InitDHelperHelper();
			var oldGame = SettingsManager.CurrentGame;
			var oldClient = ViGEmClient.Current;
			var game = new UserGame { FileName = "pass-alloc.exe", FileProductName = "Pass alloc", EnableMask = (int)MapToMask.Controller2 };
			var named = new PadSetting { ForcePassThrough = "1", ForcePassThroughIndex = "3", PadSettingChecksum = Guid.NewGuid() };
			var row = new UserSetting { InstanceGuid = Guid.NewGuid(), FileName = game.FileName, MapTo = (int)MapTo.Controller2, IsEnabled = true, PadSettingChecksum = named.PadSettingChecksum };
			// A bus client with nothing native behind it, so the copy is measured on the path a game's rumble takes. It is
			// never finalised, because there is nothing to let go of.
			var client = (ViGEmClient)FormatterServices.GetUninitializedObject(typeof(ViGEmClient));
			GC.SuppressFinalize(client);
			client.Feedbacks = new Xbox360FeedbackReceivedEventArgs[4];
			var rumble = Rumble(100, 50);
			DeviceRouting.Watch();
			try
			{
				SettingsManager.PadSettings.Items.Add(named);
				SettingsManager.UserSettings.Items.Add(row);
				SettingsManager.UpdateCurrentGame(game);
				var routing = DeviceRouting.Current;
				Assert.IsTrue(routing.PassesForceThrough, "The tab passing its force on was not in the routing, so the pass-through would not be measured.");
				var helper = new DInputHelper();
				ViGEmClient.Current = client;
				client.Feedbacks[1] = rumble;
				var taken = helper.CopyAndClearFeedbacks();
				Assert.AreSame(rumble, taken[1], "What the game asked for was not taken.");
				Assert.IsNull(client.Feedbacks[1], "What the game asked for is taken again on the next pass.");
				// Nothing new arrives, so nothing is sent to a real controller from a test. Once before counting: the places
				// are worked out when the routing is new.
				helper.PassForcesThrough(routing, 0);
				const int calls = 20000;
				var withClient = Allocations.FewestBytes(5, () =>
				{
					for (var i = 0; i < calls; i++)
					{
						client.Feedbacks[1] = rumble;
						helper.CopyAndClearFeedbacks();
						helper.PassForcesThrough(routing, 0);
					}
				});
				ViGEmClient.Current = null;
				var withoutClient = Allocations.FewestBytes(5, () =>
				{
					for (var i = 0; i < calls; i++)
					{
						helper.CopyAndClearFeedbacks();
						helper.PassForcesThrough(routing, 0);
					}
				});
				Console.WriteLine("Passes: " + calls + " each; bytes handed to the collector: " + withClient + " with a bus client, " + withoutClient + " without.");
				Assert.IsTrue(withClient < calls,
					calls + " passes with a bus client handed the collector " + withClient + " bytes; this runs on every pass.");
				Assert.IsTrue(withoutClient < calls,
					calls + " passes without a bus client handed the collector " + withoutClient + " bytes; this runs on every pass.");
				Assert.AreSame(helper.CopyAndClearFeedbacks(), helper.CopyAndClearFeedbacks(), "A new array is made for what the games asked for.");
			}
			finally
			{
				ViGEmClient.Current = oldClient;
				SettingsManager.UpdateCurrentGame(oldGame);
				SettingsManager.UserSettings.Items.Remove(row);
				SettingsManager.PadSettings.Items.Remove(named);
			}
		}
	}
}
