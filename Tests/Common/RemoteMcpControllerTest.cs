// @under-test: Plugins/RemoteMcpController/RemoteMcpController.cs, App.v4/Plugins/PluginDeviceTools.cs, App.v4/Plugins/PluginMcpDoors.cs, App.v4/Mcp/McpTools.cs, App.v4/Controls/PadTabPages/PluginDeviceUserControl.cs
// @area: plugins   @layer: integration
using JocysCom.ClassLibrary.Mcp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.Mcp;
using x360ce.App.Plugins;
using x360ce.Plugins;

namespace x360ce.Tests
{
	/// <summary>
	/// The Remote MCP Controller, a plugin embedded in x360ce, takes its controls from its State setting, as JSON named and
	/// valued as in formulas. A program or an AI assistant sets it over MCP: through x360ce's door with device_call, or
	/// on the controller's own MCP server, which its tab opens with the AI page's rows and which reaches that device alone.
	/// </summary>
	[TestClass]
	public class RemoteMcpControllerTest
	{
		static readonly Guid Product = new Guid("884e9137-a970-446b-962f-31ed8c2abb11");

		/// <summary>A port of its own, so the program's door the other tests open is not in the way.</summary>
		const int Port = 37371;

		static PluginDeviceType Type()
		{
			var type = TestControllerDevice.Manager.FindDeviceType(Product);
			Assert.IsNotNull(type, "The Remote MCP Controller plugin is not embedded in x360ce.");
			return type;
		}

		static void SetState(IPluginDevice device, string json)
		{
			device.GetType().GetProperty("State").SetValue(device, json);
		}

		static string Get(IPluginDevice device, string name)
		{
			return (string)device.GetType().GetProperty(name).GetValue(device);
		}

		[TestMethod, TestCategory("plugins")]
		[Description("State sets the controls it names, keeps the others, and rest lets go of all first")]
		public void State_sets_the_controls_it_names()
		{
			var device = Type().NewInstance();
			Assert.AreEqual("Remote MCP Controller", device.Name);
			Assert.AreEqual(128, device.ButtonCount);
			var state = new PluginState();
			device.Read(state);
			Assert.AreEqual(PluginState.Middle, state.Axes[0], "A new controller does not rest with its axes in the middle.");
			Assert.AreEqual(PluginState.PovReleased, state.Povs[0]);

			SetState(device, "{\"b1\":1,\"a1\":-0.5,\"s1\":0.25,\"p1\":90}");
			device.Read(state);
			Assert.IsTrue(state.Buttons[0]);
			Assert.AreEqual(16384, state.Axes[0], "Axis 1 at -0.5 is not a quarter of the way from the left.");
			Assert.AreEqual(16384, state.Sliders[0]);
			Assert.AreEqual(9000, state.Povs[0]);
			Assert.AreEqual("", Get(device, "Error"));
			Assert.AreEqual("{\"b1\":1,\"a1\":-0.5,\"s1\":0.25,\"p1\":90}", Get(device, "Now"));

			SetState(device, "{\"b2\":true}");
			device.Read(state);
			Assert.IsTrue(state.Buttons[0] && state.Buttons[1], "A control the State did not name lost its value.");

			SetState(device, "{\"b3\":1,\"rest\":1}");
			device.Read(state);
			Assert.IsFalse(state.Buttons[0] || state.Buttons[1], "rest did not let go of the controls set before.");
			Assert.IsTrue(state.Buttons[2], "rest undid a control set beside it.");
			Assert.AreEqual(PluginState.Middle, state.Axes[0]);

			SetState(device, "{\"q1\":1,\"b200\":1}");
			StringAssert.Contains(Get(device, "Error"), "Not a control: q1");
			StringAssert.Contains(Get(device, "Error"), "No such control: b200");
			SetState(device, "not json");
			StringAssert.Contains(Get(device, "Error"), "Not JSON");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("mcp")]
		[Description("The kind declares an MCP server, whose settings are not the plugin's, and whose tools are read and set_state")]
		public void It_declares_an_MCP_server_and_its_tools()
		{
			var type = Type();
			Assert.IsNotNull(type.McpServer, "The kind declares no MCP server.");
			Assert.IsFalse(type.Settings.Any(x => x.Property.PropertyType == typeof(PluginMcpServer)), "The MCP server is a setting the plugin is given.");
			CollectionAssert.AreEqual(new[] { "read", "set_state" }, PluginDeviceTools.Names(type));
		}

		[TestMethod, TestCategory("plugins"), TestCategory("mcp")]
		[Description("x360ce's ports are 37ACD: AI assistant access is 37101, and a device's server takes its controller and its number there")]
		public void Ports_follow_the_plan()
		{
			Assert.AreEqual(37101, PortPlan.AiAccess);
			Assert.AreEqual(PortPlan.AiAccess, new AiOptions().AiAccessPort, "AI assistant access does not start on the plan's port.");
			Assert.AreEqual(37141, PortPlan.Port(4, 1), "The first device on controller 4 is not on 37141.");
			Assert.AreEqual(37112, PortPlan.Port(1, 2));
			Assert.AreEqual(0, PortPlan.Port(0, 1), "A device on no controller has a port.");
			Assert.AreEqual(0, PortPlan.Port(4, 10), "A tenth device takes the next controller's port.");
			var door = new PluginDeviceDoor(Guid.NewGuid(), Type(), new PluginSettings());
			Ui.OnUiThread(() =>
			{
				var oldGame = SettingsManager.CurrentGame;
				var oldStatus = SettingsManager.Current.NotifySettingsStatus;
				SettingsManager.Current.NotifySettingsStatus = count => { };
				var game = new x360ce.Engine.Data.UserGame { FileName = "PortPlanTest.exe", FileProductName = "PortPlanTest" };
				SettingsManager.CurrentGame = game;
				// The device is the second on controller 4.
				var rows = new[] { Guid.NewGuid(), door.InstanceGuid }.Select(x => new x360ce.Engine.Data.UserSetting
				{
					FileName = game.FileName,
					InstanceGuid = x,
					MapTo = (int)x360ce.Engine.MapTo.Controller4,
				}).ToArray();
				try
				{
					Assert.AreEqual(0, door.Port, "A device on no controller has a port.");
					foreach (var row in rows)
						SettingsManager.UserSettings.Items.Add(row);
					Assert.AreEqual(37142, door.Port, "The second device on controller 4 does not take the plan's port.");
					door.Port = 37199;
					Assert.AreEqual(37199, door.Port, "A port the person typed is not kept.");
				}
				finally
				{
					foreach (var row in rows)
						SettingsManager.UserSettings.Items.Remove(row);
					SettingsManager.CurrentGame = oldGame;
					SettingsManager.Current.NotifySettingsStatus = oldStatus;
				}
			});
		}

		[TestMethod, TestCategory("plugins"), TestCategory("mcp")]
		[Description("The program's door lists a plugin device's tools in devices_list and calls them with device_call: read at Read, set_state only at Configure")]
		public void The_program_door_lists_and_calls_its_tools()
		{
			var type = Type();
			var level = McpCatalog.Level;
			var ud = PluginDevices.NewUserDevice(type);
			var guid = ud.InstanceGuid.ToString();
			Ui.OnUiThread(() => SettingsManager.UserDevices.Items.Add(ud));
			try
			{
				McpCatalog.Level = () => AiAccess.Read;
				var rows = ((object[])McpTools.DevicesList()).Cast<Dictionary<string, object>>().ToArray();
				CollectionAssert.AreEqual(new[] { "read", "set_state" }, (string[])rows.Single(x => (string)x["InstanceGuid"] == guid)["Tools"]);
				Assert.IsTrue(rows.Where(x => (string)x["Source"] != "Plugin").All(x => ((string[])x["Tools"]).Length == 0), "A device no plugin adds lists tools.");
				var read = (Dictionary<string, object>)McpTools.DeviceCall(guid, "read");
				Assert.AreEqual("", read["State"]);
				Assert.IsTrue(read.ContainsKey("Now") && read.ContainsKey("Error"), "read does not return the statuses.");
				var refused = Assert.ThrowsExactly<InvalidOperationException>(() => McpTools.DeviceCall(guid, "set_state", "{\"b1\":1}"));
				StringAssert.Contains(refused.Message, "needs Configure", "set_state was allowed at Read.");
				Assert.IsNull(PluginSettings.Current.Get(ud.InstanceGuid, "State"), "A refused call kept a value.");
				McpCatalog.Level = () => AiAccess.Configure;
				McpTools.DeviceCall(guid, "set_state", "{\"b1\":1}");
				Assert.AreEqual("{\"b1\":1}", PluginSettings.Current.Get(ud.InstanceGuid, "State"));
				Assert.AreEqual("{\"b1\":1}", ((Dictionary<string, object>)McpTools.DeviceCall(guid, "read"))["State"]);
				var none = Assert.ThrowsExactly<InvalidOperationException>(() => McpTools.DeviceCall(guid, "set_now", "1"));
				StringAssert.Contains(none.Message, "read, set_state", "A tool the device lacks does not name the ones it has.");
			}
			finally
			{
				McpCatalog.Level = level;
				PluginSettings.Current.Reset(ud.InstanceGuid);
				Ui.OnUiThread(() => SettingsManager.UserDevices.Items.Remove(ud));
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("mcp"), TestCategory("critical"), Timeout(30000)]
		[Description("A device's own MCP server opens on its own port with its own token and level, and serves that device's tools alone")]
		public void Its_own_server_serves_its_tools_alone()
		{
			var type = Type();
			var guid = Guid.NewGuid();
			var store = new PluginSettings();
			var door = new PluginDeviceDoor(guid, type, store);
			var onUiThread = McpCatalog.OnUiThread;
			McpCatalog.OnUiThread = a => a();
			try
			{
				door.Apply();
				Assert.IsFalse(door.Listener.IsRunning, "A server nobody opened is open.");
				door.Enabled = true;
				door.Apply();
				Assert.IsFalse(door.Listener.IsRunning, "A device on no controller opened a server with no port.");
				StringAssert.Contains(door.Listener.LastError, "on no controller", "A server with no port does not say why it is closed.");
				door.Port = Port;
				door.Apply();
				Assert.IsTrue(door.Listener.IsRunning, door.Listener.LastError);
				Assert.IsTrue(JocysCom.ClassLibrary.Security.TokenHelper.IsSecretToken(door.Token), "Opening the server made no token.");
				Assert.AreEqual(door.Token, store.Get(guid, PluginDeviceDoor.Key + "Token"), "The token is not kept with the device.");
				var refused = Assert.ThrowsExactly<McpHttpException>(() => McpClient.Post(Port, "wrong", Request("tools/list", null)));
				Assert.AreEqual((int)HttpStatusCode.Unauthorized, refused.Status);
				StringAssert.Contains(refused.Body, "MCP server settings", "The refusal does not say where the token is.");
				var list = McpClient.Post(Port, door.Token, Request("tools/list", null));
				StringAssert.Contains(list, "\"set_state\"");
				StringAssert.Contains(list, "\"read\"");
				Assert.IsFalse(list.Contains("\"ui_find\"") || list.Contains("\"devices_list\""), "The device's server serves the program's tools.");
				StringAssert.Contains(McpClient.Post(Port, door.Token, Request("initialize", null)), "x360ce-remote-mcp-controller");
				var set = Request("tools/call", "{\"name\":\"set_state\",\"arguments\":{\"value\":\"{\\\"b1\\\":1}\"}}");
				var atRead = McpClient.Post(Port, door.Token, set);
				StringAssert.Contains(atRead, "needs Configure access; the door is set to Read", "set_state was allowed at Read.");
				Assert.IsNull(store.Get(guid, "State"));
				door.Access = AiAccess.Configure;
				StringAssert.Contains(McpClient.Post(Port, door.Token, set), "State is set", "The level is not read live.");
				Assert.AreEqual("{\"b1\":1}", store.Get(guid, "State"));
				StringAssert.Contains(McpClient.Post(Port, door.Token, Request("tools/call", "{\"name\":\"read\",\"arguments\":{}}")), "b1");
				door.Enabled = false;
				door.Apply();
				Assert.IsFalse(door.Listener.IsRunning, "Closing the server left it open.");
			}
			finally
			{
				door.Listener.Stop();
				McpCatalog.OnUiThread = onUiThread;
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui")]
		[Description("The device's tab shows its MCP server and then its properties on pages of their own, the server with the AI page's rows, which the program's door refuses to touch, and no tabs, skill or snippet of the AI page")]
		public void Its_tab_shows_its_server_with_the_AI_page_rows()
		{
			Ui.OnUiThread(() =>
			{
				var type = Type();
				var guid = Guid.NewGuid();
				using (var form = new Form { Width = 900, Height = 700 })
				using (var panel = new PluginDeviceUserControl(guid, type, type.NewInstance(), new PluginSettings()) { Dock = DockStyle.Fill })
				{
					form.Controls.Add(panel);
					form.Show();
					var tabs = (TabControl)Find(panel, "PluginTabControl");
					CollectionAssert.AreEqual(new[] { "MCP Server", "Properties" }, tabs.TabPages.Cast<TabPage>().Select(x => x.Text).ToArray());
					Assert.IsTrue(tabs.TabPages[1].Contains(Find(panel, "SettingsPropertyGrid")), "The grid is not on the Properties page.");
					var section = (AiAccessUserControl)Find(panel, "McpServerUserControl");
					Assert.AreSame(tabs.TabPages[0], section.Parent, "The server is not on the MCP Server page.");
					Assert.IsTrue(Find(panel, "AiAccessEnabledCheckBox").Visible, "The server's rows are not shown.");
					Assert.IsFalse(Find(panel, "AiTabControl").Visible, "The server shows the AI page's tabs inside its own page.");
					var door = section.Model.Door as PluginDeviceDoor;
					Assert.IsNotNull(door, "The section shows another door than the device's.");
					Assert.AreEqual(guid, door.InstanceGuid);
					foreach (var name in new[] { "AiAccessEnabledCheckBox", "AiAccessComboBox", "AiAccessAddressComboBox", "AiAccessPortTextBox", "AiAccessRegenerateButton" })
					{
						Find(panel, name);
						CollectionAssert.Contains(McpTools.DoorControls, name, name + " is not one the program's door refuses.");
					}
					CollectionAssert.Contains(McpTools.SecretControls, "AiAccessTokenTextBox");
					Assert.AreEqual("", Find(panel, "AiAccessPortTextBox").Text, "A device on no controller shows a port.");
					Assert.AreEqual("", Find(panel, "AiAccessUrlTextBox").Text, "A device on no controller shows a URL.");
					Assert.IsFalse(Find(panel, "AiAccessSnippetTextBox").Visible, "The device's server shows the program's command line.");
					Assert.IsFalse(Find(panel, "AiHeaderPanel").Visible, "The device's server shows the AI page's header.");
					StringAssert.Contains(Find(panel, "AiAccessStatusText").Text, "Nothing can reach Remote MCP Controller");
					door.Port = 37141;
					section.Model.Refresh();
					Assert.AreEqual("37141", Find(panel, "AiAccessPortTextBox").Text);
					var prompt = section.Model.Prompt();
					StringAssert.Contains(prompt, "http://localhost:37141/mcp/");
					StringAssert.Contains(prompt, "read, set_state");
				}
			});
		}

		[TestMethod, TestCategory("plugins")]
		[Description("A plugin inside x360ce is made by x360ce, and a row saved under the kind's old name takes its new one, its number kept")]
		public void Its_rows_take_its_names()
		{
			var type = Type();
			var fresh = PluginDevices.NewUserDevice(type);
			Assert.AreEqual("x360ce", fresh.DevManufacturer, "A plugin inside x360ce is not made by x360ce.");
			Assert.AreEqual("Remote MCP Controller", fresh.ProductName);
			Assert.AreEqual("x360ce", PluginDevices.VendorOf(TestControllerDevice.Type));
			var saved = PluginDevices.NewUserDevice(type);
			saved.ProductName = "Remote Controller";
			saved.InstanceName = "Remote Controller 7";
			saved.HidDescription = "Remote Controller 7";
			saved.DevManufacturer = "RemoteController";
			Ui.OnUiThread(() =>
			{
				SettingsManager.UserDevices.Items.Add(saved);
				try
				{
					PluginDevices.UpdateOnline(TestControllerDevice.Manager);
					Assert.AreEqual("Remote MCP Controller", saved.ProductName, "A row keeps the kind's old product name.");
					Assert.AreEqual("Remote MCP Controller 7", saved.InstanceName, "A row keeps the kind's old name, or lost its number.");
					Assert.AreEqual("Remote MCP Controller 7", saved.HidDescription);
					Assert.AreEqual("x360ce", saved.DevManufacturer, "A row keeps the old vendor.");
					Assert.AreEqual("x360ce", saved.HidManufacturer, "The Devices page shows no vendor for a plugin device.");
				}
				finally
				{
					SettingsManager.UserDevices.Items.Remove(saved);
				}
			});
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui")]
		[Description("A controller tab lists a mapped device by its name now, not the one it had when it was mapped")]
		public void Its_controller_tab_shows_its_name_now()
		{
			var device = PluginDevices.NewUserDevice(Type());
			Ui.OnUiThread(() =>
			{
				var existing = SettingsManager.UserSettings.ItemsToArraySynchronized();
				var oldGame = SettingsManager.CurrentGame;
				var oldStatus = SettingsManager.Current.NotifySettingsStatus;
				SettingsManager.Current.NotifySettingsStatus = count => { };
				var game = new x360ce.Engine.Data.UserGame { FileName = "RenameTest.exe", FileProductName = "RenameTest", EnableMask = (int)x360ce.Engine.MapToMask.Controller1 };
				var setting = AppHelper.GetNewSetting(device, game, x360ce.Engine.MapTo.Controller1);
				setting.ProductName = "Remote Controller";
				SettingsManager.UserDevices.Items.Add(device);
				SettingsManager.UserSettings.Items.Clear();
				SettingsManager.UserSettings.Items.Add(setting);
				try
				{
					using (var form = new Form { Width = 900, Height = 700 })
					using (var pad = new PadControl(x360ce.Engine.MapTo.Controller1) { Dock = DockStyle.Fill })
					{
						form.Controls.Add(pad);
						pad.InitPadControl();
						pad.UpdateSettingsMap();
						pad.InitPadData();
						form.Show();
						SettingsManager.CurrentGame = game;
						pad.UpdateFromCurrentGame();
						Application.DoEvents();
						var grid = pad.MappedDevicesDataGridView;
						Assert.AreEqual(1, grid.Rows.Count, "The mapped device is not listed.");
						Assert.AreEqual("Remote MCP Controller", grid.Rows[0].Cells["ProductNameColumn"].FormattedValue,
							"The controller tab shows the name the device had when it was mapped.");
						Assert.AreEqual("x360ce", grid.Rows[0].Cells["VendorNameColumn"].FormattedValue);
					}
				}
				finally
				{
					SettingsManager.Current.NotifySettingsStatus = oldStatus;
					SettingsManager.CurrentGame = oldGame;
					SettingsManager.UserSettings.Items.Clear();
					foreach (var row in existing)
						SettingsManager.UserSettings.Items.Add(row);
					SettingsManager.UserDevices.Items.Remove(device);
				}
			});
		}

		static string Request(string method, string paramsJson)
		{
			return "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"" + method + "\"" + (paramsJson == null ? "" : ",\"params\":" + paramsJson) + "}";
		}

		static Control Find(Control root, string name)
		{
			var found = root.Controls.Find(name, true);
			Assert.AreEqual(1, found.Length, name + " was not found once.");
			return found[0];
		}
	}
}
