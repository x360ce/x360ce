// @under-test: App.v4/Mcp/McpTools.cs
// @area: mcp   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.App;
using x360ce.App.Mcp;

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
			McpCatalog.Load(typeof(McpTools));
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
				Assert.AreEqual(0, row["Controller"]);
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
		[Description("The semantic tools are catalogued at the levels the spec gives them, and only input_wait leaves the interface thread")]
		public void Semantic_tools_carry_their_levels()
		{
			McpCatalog.Load(typeof(McpTools));
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
