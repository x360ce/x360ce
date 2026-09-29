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
			var settings = new[] { new PadSetting(), half, new PadSetting(), null };
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
			var sent = DInputHelper.MergePassedForces(new[] { 0, 0, -1, -1 }, new[] { new PadSetting(), new PadSetting(), null, null },
				1 << 2, new byte[4], new byte[4], new byte[4], new byte[4]);
			Assert.AreEqual(0, sent, "A place was sent a force when none of its pads had anything new.");
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
			var skip = step5.IndexOf("if (!routing.PassesForceThrough)", method);
			var resolve = step5.IndexOf("ResolvePassPlaces(routing, XInputPlaces.Version, XInputPlaces.Current);", method);
			var merge = step5.IndexOf("MergePassedForces(_passPlaces, _passSettings,", method);
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
			var settings = new PadSetting[4];
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
