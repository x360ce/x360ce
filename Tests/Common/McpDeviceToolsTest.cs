// @under-test: App.v4/Mcp/McpTools.cs
// @area: mcp   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.App;
using x360ce.App.Mcp;
using x360ce.Engine;
using x360ce.Engine.Data;
using x360ce.Engine.Mcp;

namespace x360ce.Tests
{
	/// <summary>The tools a single control cannot stand in for: what is plugged in, and where it goes.</summary>
	[TestClass]
	public class McpDeviceToolsTest
	{
		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("The device list names the test controller, and mapping without a game is refused")]
		public void Device_list_names_the_test_controller()
		{
			McpTools.Register();
			McpCatalog.OnUiThread = a => a();
			var device = TestDeviceHelper.NewUserDevice();
			SettingsManager.UserDevices.Items.Add(device);
			var game = SettingsManager.CurrentGame;
			SettingsManager.CurrentGame = null;
			try
			{
				var rows = ((object[])McpTools.DevicesList()).Cast<Dictionary<string, object>>().ToList();
				var row = rows.FirstOrDefault(x => (string)x["InstanceGuid"] == device.InstanceGuid.ToString());
				Assert.IsNotNull(row, "The test controller is not listed.");
				Assert.AreEqual(device.ProductName, row["Product"]);
				Assert.AreEqual(0, ((int[])row["Controllers"]).Length, "With no game, the device is listed on a controller.");
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpTools.DeviceMap(device.InstanceGuid.ToString(), 2)).Message, "No game");
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpTools.DeviceMap("not-a-guid", 1)).Message, "No device");
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpTools.DeviceMap(device.InstanceGuid.ToString(), 5)).Message, "1 to 4");
			}
			finally
			{
				SettingsManager.CurrentGame = game;
				SettingsManager.UserDevices.Items.Remove(device);
			}
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Unmapping through device_map switches off the tab it leaves empty, as the Remove button does")]
		public void Unmapping_switches_off_the_tab_it_leaves_empty()
		{
			McpTools.Register();
			McpCatalog.OnUiThread = a => a();
			// Configure, so the unmap leaves HID Guardian alone, which would elevate.
			var level = McpCatalog.Level;
			McpCatalog.Level = () => AiAccess.Configure;
			var settings = SettingsManager.UserSettings.ItemsToArraySyncronized();
			var oldGame = SettingsManager.CurrentGame;
			var device = TestDeviceHelper.NewUserDevice();
			SettingsManager.UserDevices.Items.Add(device);
			var game = new UserGame
			{
				FileName = "unmap.exe",
				FileProductName = "Unmap",
				EnableMask = (int)MapToMask.Controller1,
				EmulationType = (int)EmulationType.Virtual,
			};
			try
			{
				SettingsManager.UserSettings.Items.Clear();
				SettingsManager.UserSettings.Items.Add(AppHelper.GetNewSetting(device, game, MapTo.Controller1));
				SettingsManager.CurrentGame = game;

				McpTools.DeviceMap(device.InstanceGuid.ToString(), 0);

				Assert.AreEqual(0, SettingsManager.GetDeviceTabs(game.FileName, device.InstanceGuid).Length, "The device is still mapped.");
				Assert.AreEqual(0, game.EnableMask & (int)MapToMask.Controller1,
					"The tab device_map left empty is still switched on, so the game sees a controller nothing drives.");
				Assert.AreEqual((int)EmulationType.None, game.EmulationType, "The last tab switched off left the game on virtual emulation.");
			}
			finally
			{
				McpCatalog.Level = level;
				SettingsManager.CurrentGame = oldGame;
				SettingsManager.UserDevices.Items.Remove(device);
				SettingsManager.UserSettings.Items.Clear();
				foreach (var setting in settings)
					SettingsManager.UserSettings.Items.Add(setting);
			}
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("device_map moves a device unless told to keep it, 0 takes it off every controller, and devices_list names every controller it is on")]
		public void Device_map_keeps_or_moves_and_the_list_names_every_controller()
		{
			var keep = typeof(McpTools).GetMethod(nameof(McpTools.DeviceMap)).GetParameters().Last();
			Assert.AreEqual("keep", keep.Name, "device_map takes no keep argument.");
			Assert.AreEqual(typeof(bool), keep.ParameterType);
			Assert.AreEqual(false, keep.DefaultValue, "device_map keeps a device where it was unless told to; moving is the default.");

			McpTools.Register();
			McpCatalog.OnUiThread = a => a();
			var level = McpCatalog.Level;
			// Below Administer HID Guardian is left alone, so nothing here asks Windows to elevate.
			McpCatalog.Level = () => AiAccess.Configure;
			var device = TestDeviceHelper.NewUserDevice();
			SettingsManager.UserDevices.Items.Add(device);
			var settings = SettingsManager.UserSettings.ItemsToArraySyncronized();
			var game = SettingsManager.CurrentGame;
			var twoTabs = new UserGame
			{
				FileName = "mcp-two-tabs.exe",
				FileProductName = "Two tabs",
				EnableMask = (int)MapToMask.Controller1,
				EmulationType = (int)EmulationType.Virtual,
			};
			SettingsManager.CurrentGame = twoTabs;
			// Already on Controller 1, so no new mapping is made and no page is filled in.
			var first = AppHelper.GetNewSetting(device, twoTabs, MapTo.Controller1);
			first.PadSettingChecksum = Guid.NewGuid();
			SettingsManager.UserSettings.Items.Add(first);
			try
			{
				var id = device.InstanceGuid.ToString();
				McpTools.DeviceMap(id, 2, true);
				CollectionAssert.AreEqual(new[] { 1, 2 }, Controllers(device), "keep true did not leave the device on both controllers.");
				McpTools.DeviceMap(id, 3);
				CollectionAssert.AreEqual(new[] { 3 }, Controllers(device), "Without keep the device was not moved off the controllers it was on.");
				McpTools.DeviceMap(id, 0);
				Assert.AreEqual(0, Controllers(device).Length, "0 left the device on a controller.");
			}
			finally
			{
				McpCatalog.Level = level;
				SettingsManager.CurrentGame = game;
				SettingsManager.UserSettings.Items.Clear();
				foreach (var setting in settings)
					SettingsManager.UserSettings.Items.Add(setting);
				SettingsManager.UserDevices.Items.Remove(device);
			}
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("After Keep then Move, neither devices_list nor device_map 0 picks the row left on no tab, and Keep reuses that row instead of piling up another one")]
		public void Keep_then_move_reuses_the_row_left_on_no_tab()
		{
			McpTools.Register();
			McpCatalog.OnUiThread = a => a();
			var level = McpCatalog.Level;
			McpCatalog.Level = () => AiAccess.Configure;
			var device = TestDeviceHelper.NewUserDevice();
			SettingsManager.UserDevices.Items.Add(device);
			var settings = SettingsManager.UserSettings.ItemsToArraySyncronized();
			var game = SettingsManager.CurrentGame;
			var keepMove = new UserGame
			{
				FileName = "mcp-keep-move.exe",
				FileProductName = "Keep then move",
				EnableMask = (int)MapToMask.Controller2,
				EmulationType = (int)EmulationType.Virtual,
			};
			SettingsManager.CurrentGame = keepMove;
			// Already on Controller 2, so no new mapping is made and no page is filled in.
			var onController2 = AppHelper.GetNewSetting(device, keepMove, MapTo.Controller2);
			onController2.PadSettingChecksum = Guid.NewGuid();
			SettingsManager.UserSettings.Items.Add(onController2);
			try
			{
				var id = device.InstanceGuid.ToString();

				// 1. Keep it on Controller 1 too: a second row.
				McpTools.DeviceMap(id, 1, true);
				CollectionAssert.AreEqual(new[] { 1, 2 }, Controllers(device));
				Assert.AreEqual(2, RowCount(device, keepMove));

				// 2. Move it to Controller 3: the lowest tab (Controller 1) moves there, and Controller 2's
				// row is left on no tab. devices_list must not report Controller 0 for that row.
				McpTools.DeviceMap(id, 3);
				CollectionAssert.AreEqual(new[] { 3 }, Controllers(device), "devices_list picked the row that is left on no tab.");
				Assert.AreEqual(2, RowCount(device, keepMove));

				// Give the row left on no tab (Controller 2's, now Disabled) a checksum of its own,
				// distinct from Controller 3's, so the assertion below can tell "kept its own" apart
				// from "took the source's".
				var disabledRow = SettingsManager.GetSettings(keepMove.FileName).First(x => x.InstanceGuid == device.InstanceGuid && x.MapTo == (int)MapTo.Disabled);
				disabledRow.PadSettingChecksum = Guid.NewGuid();

				// 3. Keep it on Controller 1 again: the row left on no tab is reused, not a third row,
				// and it takes the source row's (Controller 3's) checksum rather than keeping its own.
				McpTools.DeviceMap(id, 1, true);
				CollectionAssert.AreEqual(new[] { 1, 3 }, Controllers(device));
				Assert.AreEqual(2, RowCount(device, keepMove), "Keep made a new row instead of reusing the one left on no tab.");
				var controller1Row = SettingsManager.GetSettings(keepMove.FileName).First(x => x.InstanceGuid == device.InstanceGuid && x.MapTo == (int)MapTo.Controller1);
				var controller3Row = SettingsManager.GetSettings(keepMove.FileName).First(x => x.InstanceGuid == device.InstanceGuid && x.MapTo == (int)MapTo.Controller3);
				Assert.AreEqual(controller3Row.PadSettingChecksum, controller1Row.PadSettingChecksum, "The reused row kept its own checksum instead of taking the source row's.");

				// 4. 0 takes the device off every controller, not just the row it happens to find first.
				McpTools.DeviceMap(id, 0);
				Assert.AreEqual(0, Controllers(device).Length, "device_map 0 left the device on a controller.");
			}
			finally
			{
				McpCatalog.Level = level;
				SettingsManager.CurrentGame = game;
				SettingsManager.UserSettings.Items.Clear();
				foreach (var setting in settings)
					SettingsManager.UserSettings.Items.Add(setting);
				SettingsManager.UserDevices.Items.Remove(device);
			}
		}

		/// <summary>The controllers devices_list gives for the device.</summary>
		static int[] Controllers(UserDevice device)
		{
			var row = ((object[])McpTools.DevicesList()).Cast<Dictionary<string, object>>()
				.First(x => (string)x["InstanceGuid"] == device.InstanceGuid.ToString());
			return (int[])row["Controllers"];
		}

		/// <summary>How many settings rows the device has for the game, mapped or not.</summary>
		static int RowCount(UserDevice device, UserGame game)
		{
			return SettingsManager.GetSettings(game.FileName).Count(x => x.InstanceGuid == device.InstanceGuid);
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("The semantic tools are catalogued at the levels the spec gives them, and only input_wait leaves the interface thread")]
		public void Semantic_tools_carry_their_levels()
		{
			McpTools.Register();
			var tools = McpCatalog.Tools.ToDictionary(t => t.Name);
			Assert.AreEqual(AiAccess.Read, tools["devices_list"].Level);
			foreach (var name in new[] { "device_map", "input_wait", "preset_apply", "settings_save" })
				Assert.AreEqual(AiAccess.Configure, tools[name].Level, name);
			Assert.IsFalse(tools["input_wait"].OnUiThread, "Waiting on the interface thread would freeze the window.");
			Assert.IsFalse(tools["ui_show"].OnUiThread, "Pointing waits too, so the balloon can be read while the window keeps drawing.");
			Assert.IsTrue(McpCatalog.Tools.Where(t => t.Name != "input_wait" && t.Name != "ui_show" && t.Name != "ui_script").All(t => t.OnUiThread));
		}
	}
}
