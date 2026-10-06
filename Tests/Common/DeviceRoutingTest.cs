// @under-test: App.v4/Common/DInput/DeviceRouting.cs, App.v4/Common/DInput/DInputHelper.cs
// @area: engine   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.IO;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;
using static x360ce.Tests.DeviceRoutingFixtures;

namespace x360ce.Tests
{
	/// <summary>Which tabs each device reaches in the current game, and where its force feedback comes from.</summary>
	/// <remarks>
	/// The engine reads this once a pass and builds none of it: the interface works it out when a mapping,
	/// the settings a mapping points at, or the game changes. Only the current game's rows that are on a
	/// tab and switched on count, so another game's settings never drive a device, and a device on two
	/// tabs is routed to both.
	/// </remarks>
	[TestClass]
	public class DeviceRoutingTest
	{
		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Only the current game's rows that are on a tab and switched on are routed")]
		public void Only_the_current_games_switched_on_rows_are_routed()
		{
			var device = Guid.NewGuid();
			var live = Row(device, MapTo.Controller1);
			var off = Row(device, MapTo.Controller2);
			off.IsEnabled = false;
			var otherGame = Row(device, MapTo.Controller3);
			otherGame.FileName = "other.exe";
			var unmapped = Row(device, MapTo.None);
			var removed = Row(device, MapTo.Disabled);
			var otherCase = Row(Guid.NewGuid(), MapTo.Controller4);
			otherCase.FileName = "ROUTED.EXE";

			var routing = DeviceRouting.Build(Game, new[] { live, off, otherGame, unmapped, removed, otherCase }, new PadSetting[0]);

			CollectionAssert.AreEqual(new[] { live, otherCase }, routing.Rows);
			CollectionAssert.AreEqual(new[] { live }, routing.PadRows[0]);
			Assert.AreEqual(0, routing.PadRows[1].Length, "A row switched off in its tab's list reaches its controller.");
			Assert.AreEqual(0, routing.PadRows[2].Length, "Another game's row reaches the current game's controller.");
			CollectionAssert.AreEqual(new[] { otherCase }, routing.PadRows[3],
				"The game's file name is not compared ignoring case, as the settings list compares it.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A device on two tabs is routed to both, and its force comes from each tab whose switch is on")]
		public void A_device_on_two_tabs_routes_to_both()
		{
			var device = Guid.NewGuid();
			var first = Force(true);
			var third = Force(true);

			var routing = DeviceRouting.Build(Game,
				new[] { Row(device, MapTo.Controller3, third), Row(device, MapTo.Controller1, first) },
				new[] { first, third });

			Assert.AreEqual(1, routing.PadRows[0].Length, "The device does not reach Controller 1.");
			Assert.AreEqual(1, routing.PadRows[2].Length, "The device does not reach Controller 3.");
			DeviceForce force;
			Assert.IsTrue(routing.TryGetForce(device, out force), "A mapped device has no force source.");
			CollectionAssert.AreEqual(new[] { 0, 2 }, force.ForcePads, "Both switches are on, so both tabs' force reaches the device.");
			Assert.AreSame(first, force.PadSetting, "The effects are not made with the lowest switched-on tab's settings.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Force comes from the tab whose switch is on, and from none when every switch is off")]
		public void Force_comes_from_the_tab_whose_switch_is_on()
		{
			var device = Guid.NewGuid();
			var first = Force(false);
			var second = Force(true);
			DeviceForce force;
			DeviceRouting.Build(Game, new[] { Row(device, MapTo.Controller1, first), Row(device, MapTo.Controller2, second) }, new[] { first, second })
				.TryGetForce(device, out force);
			CollectionAssert.AreEqual(new[] { 1 }, force.ForcePads);
			Assert.AreSame(second, force.PadSetting, "The device plays the settings of a tab whose force feedback is off.");

			var alsoOff = Force(false);
			DeviceRouting.Build(Game, new[] { Row(device, MapTo.Controller2, alsoOff), Row(device, MapTo.Controller1, first) }, new[] { first, alsoOff })
				.TryGetForce(device, out force);
			Assert.AreEqual(0, force.ForcePads.Length, "Force is taken from a tab whose switch is off.");
			Assert.AreSame(first, force.PadSetting, "With every switch off, the lowest tab's settings no longer decide the wheel's own centering.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A device mapped with force feedback in another game takes nothing from it in this one")]
		public void Another_games_row_gives_no_force()
		{
			var device = Guid.NewGuid();
			var ps = Force(true);
			var elsewhere = Row(device, MapTo.Controller1, ps);
			elsewhere.FileName = "other.exe";
			DeviceForce force;
			Assert.IsFalse(DeviceRouting.Build(Game, new[] { elsewhere }, new[] { ps }).TryGetForce(device, out force),
				"The device takes its force from a game that is not the current one.");
			Assert.IsFalse(DeviceRouting.Build(null, new[] { Row(device, MapTo.Controller1, ps) }, new[] { ps }).TryGetForce(device, out force),
				"With no game, a device is routed somewhere.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A tab switched off gives no force while a switched-on tab of the device has force feedback on")]
		public void A_tab_switched_off_is_not_a_force_source()
		{
			var device = Guid.NewGuid();
			var first = Force(true);
			var second = Force(true);
			var game = new UserGame { FileName = Game.FileName, FileProductName = Game.FileProductName, EnableMask = (int)MapToMask.Controller2 };

			var routing = DeviceRouting.Build(game,
				new[] { Row(device, MapTo.Controller1, first), Row(device, MapTo.Controller2, second) },
				new[] { first, second });

			DeviceForce force;
			Assert.IsTrue(routing.TryGetForce(device, out force), "A mapped device has no force source.");
			CollectionAssert.AreEqual(new[] { 1 }, force.ForcePads,
				"A tab switched off has no virtual controller for a game to speak through, and is still a force source.");
			Assert.AreSame(second, force.PadSetting, "The effects are made with the settings of a tab that is switched off.");
			Assert.AreEqual(1, routing.PadRows[0].Length, "A tab switched off no longer shows its device on its own page.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A switched-on tab with force feedback off is not overruled by a switched-off tab with it on")]
		public void A_switched_on_tab_with_force_off_is_not_overruled()
		{
			var device = Guid.NewGuid();
			var off = Force(false);
			var on = Force(true);
			var game = new UserGame { FileName = Game.FileName, FileProductName = Game.FileProductName, EnableMask = (int)MapToMask.Controller1 };

			var routing = DeviceRouting.Build(game,
				new[] { Row(device, MapTo.Controller1, off), Row(device, MapTo.Controller2, on) },
				new[] { off, on });

			DeviceForce force;
			Assert.IsTrue(routing.TryGetForce(device, out force), "A device on a switched-on tab has no settings to decide its own centering.");
			Assert.AreEqual(0, force.ForcePads.Length, "A tab switched off drives force the switched-on tab has turned off.");
			Assert.AreSame(off, force.PadSetting, "The effects are made with the settings of a tab that is switched off.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A device whose every tab with force feedback on is switched off has no force source")]
		public void Tabs_switched_off_give_no_force()
		{
			var device = Guid.NewGuid();
			var first = Force(true);
			var second = Force(true);
			var game = new UserGame { FileName = Game.FileName, FileProductName = Game.FileProductName, EnableMask = (int)MapToMask.Controller3 };

			var routing = DeviceRouting.Build(game,
				new[] { Row(device, MapTo.Controller1, first), Row(device, MapTo.Controller2, second) },
				new[] { first, second });

			DeviceForce force;
			Assert.IsFalse(routing.TryGetForce(device, out force),
				"A device on tabs that are all switched off still has a force source, so what it last played goes on.");
			Assert.AreEqual(1, routing.PadRows[0].Length, "A tab switched off no longer shows its device on its own page.");
			Assert.AreEqual(1, routing.PadRows[1].Length, "A tab switched off no longer shows its device on its own page.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("The engine's lookup hands nothing to the collector")]
		public void The_lookup_hands_nothing_to_the_collector()
		{
			var device = Guid.NewGuid();
			var missing = Guid.NewGuid();
			var ps = Force(true);
			var routing = DeviceRouting.Build(Game, new[] { Row(device, MapTo.Controller1, ps) }, new[] { ps });
			const int calls = 20000;
			DeviceForce force;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < calls; i++)
				{
					routing.TryGetForce(device, out force);
					routing.TryGetForce(missing, out force);
				}
			});
			Assert.IsTrue(allocated < calls,
				"Looking up " + calls + " devices handed the collector " + allocated + " bytes; it runs for every device that can vibrate on every pass.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("The routing carries each routed row's device, settings, mappings and D-Pad, and every device the game's ticked rows map")]
		public void The_routing_carries_what_the_engine_looks_up()
		{
			var first = new UserDevice { InstanceGuid = Guid.NewGuid() };
			var unticked = new UserDevice { InstanceGuid = Guid.NewGuid() };
			var otherGame = new UserDevice { InstanceGuid = Guid.NewGuid() };
			var ps = new PadSetting { DPad = "p2", ButtonA = "b1", PadSettingChecksum = Guid.NewGuid() };
			var buttonDPad = new PadSetting { DPad = "b3", PadSettingChecksum = Guid.NewGuid() };
			var live = Row(first.InstanceGuid, MapTo.Controller1, ps);
			var switchedOff = Row(unticked.InstanceGuid, MapTo.Controller2, ps);
			switchedOff.IsEnabled = false;
			var elsewhere = Row(otherGame.InstanceGuid, MapTo.Controller1, ps);
			elsewhere.FileName = "other.exe";
			var noDevice = Row(Guid.NewGuid(), MapTo.Controller3, buttonDPad);
			var noSettings = Row(first.InstanceGuid, MapTo.Controller4);

			var routing = DeviceRouting.Build(Game, new[] { live, switchedOff, elsewhere, noDevice, noSettings },
				new[] { ps, buttonDPad }, new[] { otherGame, unticked, first });

			CollectionAssert.AreEqual(new[] { first }, routing.MappedDevices,
				"The engine reads other devices than the game's ticked rows map, or not in the order of the devices list.");
			CollectionAssert.AreEqual(new[] { live, noDevice, noSettings }, routing.Rows);
			CollectionAssert.AreEqual(new[] { first, null, first }, routing.RowDevices, "A row reads another device than its own.");
			Assert.AreSame(ps.Maps, routing.RowMaps[0], "A row converts with other mappings than its settings hold.");
			Assert.AreSame(buttonDPad.Maps, routing.RowMaps[1], "A row converts with other mappings than the settings it points at.");
			Assert.IsNull(routing.RowMaps[2], "A row whose settings are not stored has mappings.");
			CollectionAssert.AreEqual(new[] { 2, 0, 0 }, routing.RowDPads,
				"The D-Pad is read from another POV than the settings name, or a button is read as a POV.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A device whose rows of the game are all unticked is not read, not forced and passed no force, beside a ticked device on the same tab")]
		public void An_unticked_device_takes_part_in_nothing()
		{
			var unticked = new UserDevice { InstanceGuid = Guid.NewGuid(), HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\1" };
			var ticked = new UserDevice { InstanceGuid = Guid.NewGuid(), HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\2" };
			// Both pass the tab's force on to the place their own device holds, and the unticked row comes first.
			var own = PassThrough(0);
			own.ForceEnable = "1";
			var off = Row(unticked.InstanceGuid, MapTo.Controller1, own);
			off.IsEnabled = false;
			var on = Row(ticked.InstanceGuid, MapTo.Controller1, own);

			var routing = DeviceRouting.Build(Game, new[] { off, on }, new[] { own }, new[] { unticked, ticked });

			CollectionAssert.AreEqual(new[] { ticked }, routing.MappedDevices, "An unticked device is read, and held, on every pass.");
			Assert.AreEqual(1, routing.PadPassThrough[0].Length, "An unticked row is asked where the tab's force is passed on to.");
			Assert.AreSame(ticked, routing.PadPassThrough[0][0].Device, "The tab's force is passed on to an unticked device.");
			var places = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { { unticked.HidDeviceId, 1 }, { ticked.HidDeviceId, 3 } };
			PadSetting answered;
			Assert.AreEqual(3, routing.PassThroughPlace(0, places, out answered), "The game's rumble is passed on to the unticked device's place.");
			DeviceForce force;
			Assert.IsFalse(routing.TryGetForce(unticked.InstanceGuid, out force), "An unticked device is sent force feedback.");
			Assert.IsTrue(routing.TryGetForce(ticked.InstanceGuid, out force), "The ticked device beside it is sent no force feedback.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A device unticked on the Devices page takes part in nothing, as an unticked row does, beside a ticked device on the same tab")]
		public void A_device_unticked_on_the_Devices_page_takes_part_in_nothing()
		{
			var disabled = new UserDevice { InstanceGuid = Guid.NewGuid(), HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\1", IsEnabled = false };
			var enabled = new UserDevice { InstanceGuid = Guid.NewGuid(), HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\2" };
			var own = PassThrough(0);
			own.ForceEnable = "1";
			// Both rows are ticked; the first device is switched off on the Devices page.
			var off = Row(disabled.InstanceGuid, MapTo.Controller1, own);
			var on = Row(enabled.InstanceGuid, MapTo.Controller1, own);

			var routing = DeviceRouting.Build(Game, new[] { off, on }, new[] { own }, new[] { disabled, enabled });

			CollectionAssert.AreEqual(new[] { enabled }, routing.MappedDevices, "A device switched off on the Devices page is read, and held, on every pass.");
			CollectionAssert.AreEqual(new[] { on }, routing.Rows, "A device switched off on the Devices page reaches its controller.");
			Assert.AreEqual(1, routing.PadPassThrough[0].Length, "A device switched off on the Devices page is asked where the tab's force is passed on to.");
			Assert.AreSame(enabled, routing.PadPassThrough[0][0].Device, "The tab's force is passed on to a device switched off on the Devices page.");
			DeviceForce force;
			Assert.IsFalse(routing.TryGetForce(disabled.InstanceGuid, out force), "A device switched off on the Devices page is sent force feedback.");
			Assert.IsTrue(routing.TryGetForce(enabled.InstanceGuid, out force), "The device beside it is sent no force feedback.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("Converting a row, and passing over a mapped device that is not connected, hand nothing to the collector")]
		public void The_lookups_hand_nothing_to_the_collector()
		{
			var ud = new UserDevice { InstanceGuid = Guid.NewGuid(), JoState = new JoystickState(), SourceState = new SourceState(new JoystickState()) };
			ud.IsOnline = true;
			var offline = new UserDevice { InstanceGuid = Guid.NewGuid() };
			var ps = new PadSetting { ButtonA = "b1", LeftThumbAxisX = "a1", LeftTrigger = "x5", DPad = "p1", PadSettingChecksum = Guid.NewGuid() };
			var converting = DeviceRouting.Build(Game, new[] { Row(ud.InstanceGuid, MapTo.Controller1, ps) }, new[] { ps }, new[] { ud });
			var passingOver = DeviceRouting.Build(Game, new[] { Row(offline.InstanceGuid, MapTo.Controller2, ps) }, new[] { ps }, new[] { offline });
			var helper = new DInputHelper();
			var xi = EngineSteps.UpdateXiStates(helper);
			var di = EngineSteps.UpdateDiStates(helper);
			xi(converting);
			di(null, Game, null, passingOver);
			const int passes = 20000;
			var converted = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < passes; i++)
					xi(converting);
			});
			var passedOver = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < passes; i++)
					di(null, Game, null, passingOver);
			});
			Console.WriteLine("Bytes per pass: converting a row " + converted / (double)passes + ", passing over a device " + passedOver / (double)passes);
			Assert.IsTrue(converted < passes,
				passes + " conversions of one row handed the collector " + converted + " bytes; a row's device, mappings and D-Pad come from the routing, so converting it makes nothing.");
			Assert.IsTrue(passedOver < passes,
				passes + " passes over a mapped device handed the collector " + passedOver + " bytes; the devices a pass reads come from the routing, so passing over them copies nothing.");
		}
	}
}
