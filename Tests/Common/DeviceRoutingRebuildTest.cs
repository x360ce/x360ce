// @under-test: App.v4/Common/DInput/DeviceRouting.cs, App.v4/Common/DInput/DInputHelper.cs, App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs, App.v4/Common/DInput/DInputHelper.Step3.UpdateXiStates.cs, App.v4/Common/DInput/DInputHelper.Step4.CombineXiStates.cs, App.v4/Issues/ForceFeedbackIssue.cs, App.v4/Common/SettingsManager.cs
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
	/// <summary>The routing rebuilds when a mapping, the stored settings, the device list or the current game changes.</summary>
	/// <remarks>
	/// <c>DeviceRouting.Watch</c> subscribes the routing to the settings lists and the current game, so
	/// <c>DeviceRouting.Current</c> is rebuilt whenever one of them changes and read fresh by whichever
	/// pass runs next. The engine itself reads <c>DeviceRouting.Current</c> once a pass and copies
	/// neither the settings lists nor the devices list to do it.
	/// </remarks>
	[TestClass]
	public class DeviceRoutingRebuildTest
	{
		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("The routing is built again when a mapping or the game changes")]
		public void The_routing_follows_the_settings_and_the_game()
		{
			if (Global.DHelper == null)
				Global.InitDHelperHelper();
			var oldGame = SettingsManager.CurrentGame;
			var game = new UserGame { FileName = "routing-follows.exe", FileProductName = "Routing follows" };
			var row = new UserSetting { InstanceGuid = Guid.NewGuid(), FileName = game.FileName, MapTo = (int)MapTo.Controller2, IsEnabled = true };
			DeviceRouting.Watch();
			try
			{
				SettingsManager.UpdateCurrentGame(game);
				SettingsManager.UserSettings.Items.Add(row);
				CollectionAssert.Contains(DeviceRouting.Current.PadRows[1], row, "A mapping added is not read by the engine.");
				row.IsEnabled = false;
				CollectionAssert.DoesNotContain(DeviceRouting.Current.PadRows[1], row, "A mapping switched off is still read by the engine.");
				row.IsEnabled = true;
				SettingsManager.UpdateCurrentGame(new UserGame { FileName = "another.exe", FileProductName = "Another" });
				CollectionAssert.DoesNotContain(DeviceRouting.Current.Rows, row, "The engine reads the old game's mappings after the game changed.");
			}
			finally
			{
				SettingsManager.UpdateCurrentGame(oldGame);
				SettingsManager.UserSettings.Items.Remove(row);
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("The force source is worked out again when a tab is switched on or off")]
		public void The_routing_follows_a_tab_switch()
		{
			if (Global.DHelper == null)
				Global.InitDHelperHelper();
			var oldGame = SettingsManager.CurrentGame;
			var game = new UserGame
			{
				FileName = "routing-switch.exe",
				FileProductName = "Routing switch",
				EnableMask = (int)(MapToMask.Controller1 | MapToMask.Controller2),
				EmulationType = (int)EmulationType.Virtual,
			};
			var device = Guid.NewGuid();
			var first = Force(true);
			var second = Force(true);
			var rows = new[]
			{
				new UserSetting { InstanceGuid = device, FileName = game.FileName, MapTo = (int)MapTo.Controller1, IsEnabled = true, PadSettingChecksum = first.PadSettingChecksum },
				new UserSetting { InstanceGuid = device, FileName = game.FileName, MapTo = (int)MapTo.Controller2, IsEnabled = true, PadSettingChecksum = second.PadSettingChecksum },
			};
			DeviceRouting.Watch();
			try
			{
				SettingsManager.PadSettings.Items.Add(first);
				SettingsManager.PadSettings.Items.Add(second);
				foreach (var row in rows)
					SettingsManager.UserSettings.Items.Add(row);
				SettingsManager.UserGames.Items.Add(game);
				SettingsManager.UpdateCurrentGame(game);
				DeviceForce force;
				Assert.IsTrue(DeviceRouting.Current.TryGetForce(device, out force), "A mapped device has no force source.");
				CollectionAssert.AreEqual(new[] { 0, 1 }, force.ForcePads, "Both tabs are switched on, so both tabs' force reaches the device.");

				SettingsManager.SetTabEnabled(game, MapTo.Controller1, false);
				DeviceRouting.Current.TryGetForce(device, out force);
				CollectionAssert.AreEqual(new[] { 1 }, force.ForcePads, "A tab switched off stays a force source until something else changes.");
				Assert.AreSame(second, force.PadSetting, "The effects are still made with the settings of the tab switched off.");

				SettingsManager.SetTabEnabled(game, MapTo.Controller1, true);
				DeviceRouting.Current.TryGetForce(device, out force);
				CollectionAssert.AreEqual(new[] { 0, 1 }, force.ForcePads, "A tab switched back on stays out of the force sources.");
			}
			finally
			{
				SettingsManager.UpdateCurrentGame(oldGame);
				SettingsManager.UserGames.Items.Remove(game);
				foreach (var row in rows)
					SettingsManager.UserSettings.Items.Remove(row);
				SettingsManager.PadSettings.Items.Remove(first);
				SettingsManager.PadSettings.Items.Remove(second);
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("The force source is worked out again when the settings a mapping points at are stored")]
		public void The_routing_follows_the_stored_settings()
		{
			if (Global.DHelper == null)
				Global.InitDHelperHelper();
			var oldGame = SettingsManager.CurrentGame;
			var game = new UserGame { FileName = "routing-stored.exe", FileProductName = "Routing stored", EnableMask = (int)MapToMask.Controller2 };
			var row = new UserSetting { InstanceGuid = Guid.NewGuid(), FileName = game.FileName, MapTo = (int)MapTo.Controller2, IsEnabled = true };
			var ps = Force(true);
			DeviceRouting.Watch();
			try
			{
				SettingsManager.UpdateCurrentGame(game);
				SettingsManager.UserSettings.Items.Add(row);
				// The mapping points at its settings first and they are stored after, in the order
				// LoadPadSettingAndCleanup uses, so only the stored settings' change can bring them in.
				row.PadSettingChecksum = ps.PadSettingChecksum;
				SettingsManager.PadSettings.Items.Add(ps);
				DeviceForce force;
				Assert.IsTrue(DeviceRouting.Current.TryGetForce(row.InstanceGuid, out force), "A mapping added is not read by the engine.");
				CollectionAssert.AreEqual(new[] { 1 }, force.ForcePads, "Settings stored after the mapping points at them are not read by the engine.");
				Assert.AreSame(ps, force.PadSetting, "The effects are not made with the stored settings.");
			}
			finally
			{
				SettingsManager.UpdateCurrentGame(oldGame);
				SettingsManager.UserSettings.Items.Remove(row);
				SettingsManager.PadSettings.Items.Remove(ps);
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Where force is passed on is worked out again when a device comes into the list or leaves it")]
		public void The_routing_follows_the_devices_listed()
		{
			if (Global.DHelper == null)
				Global.InitDHelperHelper();
			var oldGame = SettingsManager.CurrentGame;
			var game = new UserGame { FileName = "routing-devices.exe", FileProductName = "Routing devices", EnableMask = (int)MapToMask.Controller3 };
			var device = new UserDevice { InstanceGuid = Guid.NewGuid(), HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\1" };
			var own = PassThrough(0);
			var row = Row(device.InstanceGuid, MapTo.Controller3, own);
			row.FileName = game.FileName;
			DeviceRouting.Watch();
			try
			{
				SettingsManager.PadSettings.Items.Add(own);
				SettingsManager.UserSettings.Items.Add(row);
				SettingsManager.UpdateCurrentGame(game);
				Assert.AreEqual(0, DeviceRouting.Current.PadPassThrough[2].Length, "A device not in the list passes force on.");
				SettingsManager.UserDevices.Items.Add(device);
				Assert.AreEqual(1, DeviceRouting.Current.PadPassThrough[2].Length,
					"A device that came into the list is not passed force on to until some setting changes.");
				SettingsManager.UserDevices.Items.Remove(device);
				Assert.AreEqual(0, DeviceRouting.Current.PadPassThrough[2].Length, "A device taken from the list is still passed force on to.");
			}
			finally
			{
				SettingsManager.UpdateCurrentGame(oldGame);
				SettingsManager.UserDevices.Items.Remove(device);
				SettingsManager.UserSettings.Items.Remove(row);
				SettingsManager.PadSettings.Items.Remove(own);
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("The routing is built again when a device is ticked or unticked on the Devices page")]
		public void The_routing_follows_the_Devices_page_tick()
		{
			if (Global.DHelper == null)
				Global.InitDHelperHelper();
			var oldGame = SettingsManager.CurrentGame;
			var game = new UserGame { FileName = "routing-device-tick.exe", FileProductName = "Routing device tick", EnableMask = (int)MapToMask.Controller1 };
			var device = new UserDevice { InstanceGuid = Guid.NewGuid() };
			var row = Row(device.InstanceGuid, MapTo.Controller1);
			row.FileName = game.FileName;
			DeviceRouting.Watch();
			try
			{
				SettingsManager.UserDevices.Items.Add(device);
				SettingsManager.UserSettings.Items.Add(row);
				SettingsManager.UpdateCurrentGame(game);
				CollectionAssert.Contains(DeviceRouting.Current.MappedDevices, device, "A ticked device is not read.");
				device.IsEnabled = false;
				CollectionAssert.DoesNotContain(DeviceRouting.Current.MappedDevices, device,
					"A device unticked on the Devices page is read until some setting changes.");
				CollectionAssert.DoesNotContain(DeviceRouting.Current.Rows, row, "A device unticked on the Devices page reaches its controller.");
				device.IsEnabled = true;
				CollectionAssert.Contains(DeviceRouting.Current.MappedDevices, device,
					"A device ticked again on the Devices page is not read until some setting changes.");
			}
			finally
			{
				SettingsManager.UpdateCurrentGame(oldGame);
				SettingsManager.UserSettings.Items.Remove(row);
				SettingsManager.UserDevices.Items.Remove(device);
			}
			// The tab's list shows it: the row's Enabled box is greyed, with the reason as its tooltip, from the same tick.
			var pad = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Controls", "PadControl.cs"));
			var format = Ui.Between(pad, "private void MappedDevicesDataGridView_CellFormatting(", "public event EventHandler<EventArgs<UserSetting>> OnSettingChanged;");
			StringAssert.Contains(format, "column == IsEnabledColumn", "A row whose device is unticked on the Devices page looks like any other.");
			StringAssert.Contains(format, "!device.IsEnabled", "The row's Enabled box is greyed by something other than the tick the routing reads.");
			StringAssert.Contains(format, "SwitchedOffOnDevicesPage", "The row's Enabled box does not say why it is greyed.");
			StringAssert.Contains(pad, "SwitchedOffOnDevicesPage = \"Switched off on the Devices page\"", "The tooltip says something else.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("The routing follows an edited mapping, and a reset of the devices list")]
		public void The_routing_follows_an_edited_mapping_and_a_reset()
		{
			if (Global.DHelper == null)
				Global.InitDHelperHelper();
			var oldGame = SettingsManager.CurrentGame;
			var game = new UserGame { FileName = "routing-edit.exe", FileProductName = "Routing edit", EnableMask = (int)MapToMask.Controller1 };
			var device = new UserDevice { InstanceGuid = Guid.NewGuid() };
			var quiet = new UserDevice { InstanceGuid = Guid.NewGuid() };
			var ps = new PadSetting { DPad = "p1", PadSettingChecksum = Guid.NewGuid() };
			var row = Row(device.InstanceGuid, MapTo.Controller1, ps);
			row.FileName = game.FileName;
			var quietRow = Row(quiet.InstanceGuid, MapTo.Controller1, ps);
			quietRow.FileName = game.FileName;
			var stored = new PadSetting { DPad = "p2", PadSettingChecksum = Guid.NewGuid() };
			var late = Row(quiet.InstanceGuid, MapTo.Controller2, stored);
			late.FileName = game.FileName;
			DeviceRouting.Watch();
			try
			{
				SettingsManager.PadSettings.Items.Add(ps);
				SettingsManager.UserDevices.Items.Add(device);
				SettingsManager.UserSettings.Items.Add(row);
				SettingsManager.UserSettings.Items.Add(quietRow);
				SettingsManager.UpdateCurrentGame(game);
				Assert.AreEqual(1, DeviceRouting.Current.RowDPads[0]);
				// The mapping is the first edit after a build. The settings mark their mappings changed before the
				// list tells the routing, because they subscribe to their own changes first; the other way round,
				// this edit would reach the engine with the old mappings, and only the next edit would bring it in.
				var maps = DeviceRouting.Current.RowMaps[0];
				ps.ButtonA = "b4";
				Assert.AreNotSame(maps, DeviceRouting.Current.RowMaps[0], "A mapping changed is converted as it was until something else changes.");
				Assert.IsTrue(DeviceRouting.Current.RowMaps[0].Exists(x => x.IsButton && x.Index == 4),
					"The engine converts with the mappings from before the edit.");
				ps.DPad = "p3";
				Assert.AreEqual(3, DeviceRouting.Current.RowDPads[0], "A D-Pad moved to another POV is read from the old one until something else changes.");
				// Written into the list without a notice, then announced as a reset, as a reload of the list announces it.
				SettingsManager.UserDevices.Items.RaiseListChangedEvents = false;
				SettingsManager.UserDevices.Items.Add(quiet);
				SettingsManager.UserDevices.Items.RaiseListChangedEvents = true;
				SettingsManager.UserDevices.Items.ResetBindings();
				CollectionAssert.Contains(DeviceRouting.Current.MappedDevices, quiet,
					"A reset of the devices list leaves the engine reading the devices it had before.");
				// The same for the settings list and the stored settings. An added item is used, because an edited
				// one notifies even while the list's notices are off.
				SettingsManager.UserSettings.Items.RaiseListChangedEvents = false;
				SettingsManager.UserSettings.Items.Add(late);
				SettingsManager.UserSettings.Items.RaiseListChangedEvents = true;
				SettingsManager.UserSettings.Items.ResetBindings();
				CollectionAssert.Contains(DeviceRouting.Current.Rows, late, "A reset of the settings list leaves the engine converting the rows it had before.");
				SettingsManager.PadSettings.Items.RaiseListChangedEvents = false;
				SettingsManager.PadSettings.Items.Add(stored);
				SettingsManager.PadSettings.Items.RaiseListChangedEvents = true;
				SettingsManager.PadSettings.Items.ResetBindings();
				Assert.AreSame(stored.Maps, DeviceRouting.Current.RowMaps[Array.IndexOf(DeviceRouting.Current.Rows, late)],
					"A reset of the stored settings leaves the engine converting with the ones it had before.");
			}
			finally
			{
				SettingsManager.UserDevices.Items.RaiseListChangedEvents = true;
				SettingsManager.UserSettings.Items.RaiseListChangedEvents = true;
				SettingsManager.PadSettings.Items.RaiseListChangedEvents = true;
				SettingsManager.UpdateCurrentGame(oldGame);
				SettingsManager.UserSettings.Items.Remove(late);
				SettingsManager.PadSettings.Items.Remove(stored);
				SettingsManager.UserSettings.Items.Remove(quietRow);
				SettingsManager.UserSettings.Items.Remove(row);
				SettingsManager.UserDevices.Items.Remove(quiet);
				SettingsManager.UserDevices.Items.Remove(device);
				SettingsManager.PadSettings.Items.Remove(ps);
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("The engine reads the routing once a pass, and neither it nor the Issues tab copies the settings list for it")]
		public void The_engine_reads_the_routing_once_a_pass()
		{
			var dir = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput");
			var main = File.ReadAllText(Path.Combine(dir, "DInputHelper.cs"));
			StringAssert.Contains(main, "var routing = DeviceRouting.Current;", "The engine does not read the routing once for the pass.");
			StringAssert.Contains(main, "UpdateDiStates(manager, game, detector, routing);");
			StringAssert.Contains(main, "UpdateXiStates(routing);");
			StringAssert.Contains(main, "CombineXiStates(routing);");
			foreach (var step in new[] { "DInputHelper.Step2.UpdateDiStates.cs", "DInputHelper.Step3.UpdateXiStates.cs", "DInputHelper.Step4.CombineXiStates.cs" })
				Assert.IsFalse(File.ReadAllText(Path.Combine(dir, step)).Contains("UserSettings.ItemsToArraySynchronized()"),
					step + " copies the settings list on every pass, under the lock the interface holds while it draws.");
			var step2 = File.ReadAllText(Path.Combine(dir, "DInputHelper.Step2.UpdateDiStates.cs"));
			StringAssert.Contains(step2, "mapped = routing.TryGetForce(ud.InstanceGuid, out route);", "Force feedback is looked up some other way than the routing.");
			// A device on no routed row is stopped after the mapped block and before its spring is driven.
			var mappedBlock = step2.IndexOf("if (mapped)");
			var tabStop = step2.IndexOf("ud.FFState.StopDeviceForces(device);", mappedBlock);
			var unroutedStop = step2.IndexOf("ud.FFState.StopDeviceForces(device);", tabStop + 1);
			var spring = step2.IndexOf("ud.FFState.UpdateSpring(");
			Assert.IsTrue(mappedBlock > 0 && tabStop > mappedBlock && unroutedStop > tabStop && spring > unroutedStop,
				"A device whose rows of the game are all switched off goes on playing its last force, and its spring goes on holding the wheel.");
			var step3 = File.ReadAllText(Path.Combine(dir, "DInputHelper.Step3.UpdateXiStates.cs"));
			var reset = step3.IndexOf("setting.XiState = gp;");
			var lookup = step3.IndexOf("var ud = devices[i];");
			Assert.IsTrue(reset > 0 && lookup > reset,
				"A row whose device is gone keeps what it last held, and its controller goes on holding it.");
			StringAssert.Contains(step2, "var userDevices = routing.MappedDevices;", "The engine copies the settings and devices lists to find the devices it reads.");
			// The conversion itself: the D-Pad buffer it reuses is reserved once, above it.
			var convert = step3.Substring(step3.IndexOf("void UpdateXiStates("));
			foreach (var lookupText in new[] { "SettingsManager.GetMappedDevices(", "SettingsManager.GetDevice(", "SettingsManager.GetPadSetting(", "TryParseIniValue(", "new bool[" })
			{
				Assert.IsFalse(step2.Contains(lookupText), "Step 2 looks something up on every pass: " + lookupText);
				Assert.IsFalse(convert.Contains(lookupText), "Step 3 looks something up for every row on every pass: " + lookupText);
			}
			var issue = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Issues", "ForceFeedbackIssue.cs"));
			StringAssert.Contains(issue, "DInput.DeviceRouting.Current.TryGetForce(ud.InstanceGuid, out route)",
				"The Issues tab judges force feedback by other settings than the ones the device plays.");
			var settings = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "SettingsManager.cs"));
			StringAssert.Contains(settings, "DInput.DeviceRouting.Watch();", "The routing does not follow the settings once they are loaded.");
			StringAssert.Contains(settings, "DInput.DeviceRouting.Refresh();", "The routing does not follow a change of game.");
		}
	}
}
