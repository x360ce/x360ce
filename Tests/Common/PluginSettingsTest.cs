// @under-test: App.v4/Plugins/PluginSettings.cs, App.v4/Plugins/PluginDevices.cs, App.v4/Plugins/PluginManager.cs, App.v4/Controls/PadTabPages/PluginDeviceUserControl.cs, App.v4/Controls/PadControl.cs
// @area: plugins   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.App.Plugins;
using x360ce.Engine;
using x360ce.Plugins;

namespace x360ce.Tests
{
	/// <summary>
	/// A plugin device's settings: its public properties, shown on a tab of the controller page it is mapped to, kept per
	/// device, and given to the plugin by the engine; and the control a plugin may declare for its device.
	/// </summary>
	[TestClass]
	public class PluginSettingsTest
	{
		static PluginDeviceType Type(Guid product)
		{
			var file = new PluginFile("Tests.dll", false);
			return PluginManager.FindDeviceTypes(typeof(SettingsPluginDevice).Assembly, file).Single(x => x.ProductGuid == product);
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("Each public property of a plugin device becomes a setting of its kind, with its name, help, group, limits and the value a new device starts with")]
		public void Each_public_property_becomes_a_setting_of_its_kind()
		{
			var settings = Type(SettingsPluginDevice.Product).Settings;
			CollectionAssert.AreEqual(new[] { "Address", "Port", "Listen", "Smoothing", "Mode", "Label", "Packets" },
				settings.Select(x => x.Name).ToArray(),
				"The settings are the public properties, in the order written, without the device's own members or a hidden one.");
			var port = settings.Single(x => x.Name == "Port");
			Assert.AreEqual(PluginSettingKind.Number, port.Kind);
			Assert.AreEqual("Port", port.DisplayName);
			Assert.AreEqual("The UDP port to listen on.", port.Description);
			Assert.AreEqual("Network", port.Category);
			Assert.AreEqual(1m, port.Minimum);
			Assert.AreEqual(65535m, port.Maximum);
			Assert.AreEqual(0, port.DecimalPlaces);
			Assert.AreEqual("26760", port.DefaultText);
			var smoothing = settings.Single(x => x.Name == "Smoothing");
			Assert.AreEqual(PluginSettingKind.Number, smoothing.Kind);
			Assert.AreEqual(0m, smoothing.Minimum);
			Assert.AreEqual(1m, smoothing.Maximum);
			Assert.IsTrue(smoothing.DecimalPlaces > 0, "A fraction is shown with its decimals.");
			Assert.AreEqual("0.5", smoothing.DefaultText);
			Assert.AreEqual(PluginSetting.DefaultCategory, smoothing.Category, "A setting with no group goes in the default one.");
			Assert.AreEqual(PluginSettingKind.CheckBox, settings.Single(x => x.Name == "Listen").Kind);
			Assert.AreEqual("True", settings.Single(x => x.Name == "Listen").DefaultText);
			var mode = settings.Single(x => x.Name == "Mode");
			Assert.AreEqual(PluginSettingKind.List, mode.Kind);
			Assert.IsTrue(mode.ListOnly, "An enum's list holds every value it can have.");
			CollectionAssert.AreEqual(new[] { "Fast, less accurate", "Accurate" }, mode.GetChoices().Select(x => x.Text).ToArray());
			CollectionAssert.AreEqual(new[] { "Fast", "Accurate" }, mode.GetChoices().Select(x => x.Value).ToArray());
			var address = settings.Single(x => x.Name == "Address");
			Assert.AreEqual(PluginSettingKind.List, address.Kind);
			Assert.IsFalse(address.ListOnly, "The converter lets other addresses be typed.");
			CollectionAssert.AreEqual(new[] { "All Unassigned", "127.0.0.1" }, address.GetChoices().Select(x => x.Text).ToArray());
			var label = settings.Single(x => x.Name == "Label");
			Assert.AreEqual(PluginSettingKind.Text, label.Kind);
			Assert.AreEqual("Label", label.DisplayName, "A property with no display name is shown by its own name.");
			var packets = settings.Single(x => x.Name == "Packets");
			Assert.AreEqual(PluginSettingKind.Status, packets.Kind, "A property that cannot be set is shown, not edited.");
			Assert.AreEqual(PluginSetting.StatusCategory, packets.Category);
			Assert.AreEqual(0, Type(SamplePluginDevice.Product).Settings.Length, "A device without properties has no settings.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("The engine gives the plugin the values kept for its device before it reads, sets only what changed, and gives the defaults back after a reset")]
		public void The_engine_gives_the_plugin_its_values_before_it_reads()
		{
			var store = new PluginSettings();
			var guid = Guid.NewGuid();
			var type = Type(SettingsPluginDevice.Product);
			var device = new SettingsPluginDevice();
			var reader = new PluginDeviceReader(type, device);
			reader.ApplySettings(store, guid);
			Assert.AreEqual(0, device.SetCount, "A new device was given values it already had.");
			var version = store.Version;
			store.Set(guid, "Port", "9000");
			store.Set(guid, "Mode", "Accurate");
			store.Set(guid, "Smoothing", "0.25");
			store.Set(Guid.NewGuid(), "Port", "1");
			Assert.IsTrue(store.Version > version, "A change does not tell the engine to look again.");
			reader.ApplySettings(store, guid);
			Assert.AreEqual(9000, device.Port);
			Assert.AreEqual(SettingsPluginMode.Accurate, device.Mode);
			Assert.AreEqual(0.25, device.Smoothing);
			Assert.AreEqual(3, device.SetCount, "Values another device keeps, or that did not change, were set.");
			reader.ApplySettings(store, guid);
			Assert.AreEqual(3, device.SetCount, "Nothing changed, and values were set again.");
			store.Reset(guid);
			reader.ApplySettings(store, guid);
			Assert.AreEqual(26760, device.Port, "A reset does not give the plugin its first value back.");
			Assert.AreEqual(SettingsPluginMode.Fast, device.Mode);
			Assert.AreEqual(0.5, device.Smoothing);
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("The values kept for each device are saved, read back as they were, and those of a device no longer listed are dropped")]
		public void Kept_values_survive_a_restart()
		{
			var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "x360ce.PluginSettingsTest." + Guid.NewGuid().ToString("N") + ".xml");
			var kept = Guid.NewGuid();
			var gone = Guid.NewGuid();
			try
			{
				var store = new PluginSettings();
				store.Set(kept, "Address", "192.168.1.5");
				store.Set(kept, "Port", "9000");
				store.Set(gone, "Port", "1");
				store.SaveAs(path, new[] { kept });
				var read = new PluginSettings();
				read.LoadFrom(path);
				Assert.AreEqual("192.168.1.5", read.Get(kept, "Address"));
				Assert.AreEqual("9000", read.Get(kept, "Port"));
				Assert.IsNull(read.Get(gone, "Port"), "A device no longer listed keeps its values.");
			}
			finally
			{
				if (System.IO.File.Exists(path))
					System.IO.File.Delete(path);
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui")]
		[Description("The settings tab shows each setting as a grouped row of a property grid, starts from the kept value, keeps what is changed, refuses what the setting does not take, shows the status, and resets to the first values")]
		public void The_settings_tab_edits_the_values_kept_for_the_device()
		{
			Ui.OnUiThread(() =>
			{
				var store = new PluginSettings();
				var guid = Guid.NewGuid();
				store.Set(guid, "Port", "9000");
				var device = new SettingsPluginDevice();
				using (var form = new Form())
				using (var panel = new PluginDeviceUserControl(guid, Type(SettingsPluginDevice.Product), device, store) { Dock = DockStyle.Fill })
				{
					form.Controls.Add(panel);
					form.Show();
					var grid = (PropertyGrid)Find(panel, "SettingsPropertyGrid");
					var view = grid.SelectedObject;
					var rows = System.ComponentModel.TypeDescriptor.GetProperties(view);
					var port = rows["Port"];
					Assert.AreEqual(9000, port.GetValue(view), "The row does not start from the kept value.");
					Assert.AreEqual("Port", port.DisplayName);
					Assert.AreEqual("The UDP port to listen on.", port.Description);
					Assert.AreEqual("Network", port.Category);
					Assert.IsTrue(port.ShouldSerializeValue(view), "A kept value is not drawn bold.");
					port.SetValue(view, 9001);
					Assert.AreEqual("9001", store.Get(guid, "Port"));
					var outside = Assert.ThrowsExactly<ArgumentException>(() => port.SetValue(view, 0));
					StringAssert.Contains(outside.Message, "1 to 65535", "A number outside its range does not say what it takes.");
					Assert.AreEqual("9001", store.Get(guid, "Port"), "A number outside its range was kept.");
					var listen = rows["Listen"];
					Assert.AreEqual(true, listen.GetValue(view), "The row does not start from the device's first value.");
					Assert.IsFalse(listen.ShouldSerializeValue(view), "A first value is drawn bold.");
					listen.SetValue(view, false);
					Assert.AreEqual("False", store.Get(guid, "Listen"));
					var mode = rows["Mode"];
					Assert.IsTrue(mode.Converter.GetStandardValuesExclusive(), "An enum can be typed into.");
					CollectionAssert.AreEqual(new[] { "Fast, less accurate", "Accurate" },
						mode.Converter.GetStandardValues().Cast<object>().Select(x => mode.Converter.ConvertToString(x)).ToArray());
					mode.SetValue(view, mode.Converter.ConvertFromString("Accurate"));
					Assert.AreEqual("Accurate", store.Get(guid, "Mode"));
					var address = rows["Address"];
					Assert.IsFalse(address.Converter.GetStandardValuesExclusive(), "An address the list does not hold cannot be typed.");
					Assert.AreEqual("All Unassigned", address.Converter.ConvertToString(address.GetValue(view)));
					address.SetValue(view, address.Converter.ConvertFromString("192.168.1.5"));
					Assert.AreEqual("192.168.1.5", store.Get(guid, "Address"));
					rows["Label"].SetValue(view, "Wheel");
					Assert.AreEqual("Wheel", store.Get(guid, "Label"));
					Assert.AreEqual(PluginSetting.DefaultCategory, rows["Smoothing"].Category, "A setting with no group is not in the default one.");
					var packets = rows["Packets"];
					Assert.IsTrue(packets.IsReadOnly, "A status can be typed into.");
					Assert.AreEqual("0", packets.GetValue(view));
					device.Receive();
					panel.RefreshStatus();
					Assert.AreEqual("1", packets.GetValue(view), "The status does not follow the device.");
					Assert.IsNull(rows["SetCount"], "A hidden property is shown.");
					port.ResetValue(view);
					Assert.IsNull(store.Get(guid, "Port"), "The row's Reset leaves its kept value.");
					Assert.AreEqual(26760, port.GetValue(view), "The row's Reset does not give the first value back.");
					((TabControl)Find(panel, "PluginTabControl")).SelectedTab = (TabPage)Find(panel, "PropertiesTabPage");
					Application.DoEvents();
					((Button)Find(panel, "ResetButton")).PerformClick();
					Assert.IsNull(store.Get(guid, "Listen"), "Reset leaves a kept value.");
					Assert.AreEqual(true, listen.GetValue(view), "Reset does not give the first value back.");
					Assert.AreEqual(SettingsPluginMode.Fast, mode.GetValue(view));
				}
			});
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui")]
		[Description("The device's help heads its tab, so what to know before using the device is in view")]
		public void The_device_help_heads_its_tab()
		{
			Ui.OnUiThread(() =>
			{
				var type = Type(SettingsPluginDevice.Product);
				using (var panel = new PluginDeviceUserControl(Guid.NewGuid(), type, new SettingsPluginDevice(), new PluginSettings()))
				{
					var help = Find(panel, "DescriptionLabel");
					Assert.AreEqual(type.Description, help.Text);
					Assert.AreEqual(0, ((TableLayoutPanel)help.Parent).GetRow(help), "The help is not at the top of the tab.");
				}
			});
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui")]
		[Description("The AI door reads each setting on the tab once, by its name and with its value, a status included")]
		public void The_ai_door_reads_each_setting_once()
		{
			Ui.OnUiThread(() =>
			{
				var device = new SettingsPluginDevice();
				device.Receive();
				var store = new PluginSettings();
				var guid = Guid.NewGuid();
				using (var panel = new PluginDeviceUserControl(guid, Type(SettingsPluginDevice.Product), device, store))
				{
					var nodes = new System.Collections.Generic.List<JocysCom.ClassLibrary.Controls.UiTree.UiNode>();
					Flatten(JocysCom.ClassLibrary.Controls.UiTree.UiTreeWalker.Read(panel, false, ""), nodes);
					var port = nodes.Where(x => x.Name == "Port").ToList();
					Assert.AreEqual(1, port.Count, "Port is read " + port.Count + " times.");
					Assert.AreEqual("26760", port[0].Value);
					Assert.AreEqual("Text", port[0].Role);
					Assert.AreEqual("The UDP port to listen on.", port[0].Description);
					var packets = nodes.Where(x => x.Name == "Packets received").ToList();
					Assert.AreEqual(1, packets.Count, "The status is read " + packets.Count + " times.");
					Assert.AreEqual("1", packets[0].Value, "The status is not read with its value.");
					Assert.AreEqual("Value", packets[0].Role);
					Assert.AreEqual("CheckBox", nodes.Single(x => x.Name == "Listen").Role);
					Assert.AreEqual("List", nodes.Single(x => x.Name == "Mode").Role);
					// The door's ui_set: a row is set by its path, as a person types into it, and refused with what it takes.
					var element = JocysCom.ClassLibrary.Controls.UiTree.UiTreeWalker.Find(panel, port[0].Path);
					Assert.IsNotNull(element, "The row's path does not lead back to it: " + port[0].Path);
					Assert.IsNull(JocysCom.ClassLibrary.Controls.UiTree.UiTreeWalker.SetValue(element, "9002"));
					Assert.AreEqual("9002", store.Get(guid, "Port"));
					StringAssert.Contains(JocysCom.ClassLibrary.Controls.UiTree.UiTreeWalker.SetValue(element, "0"), "1 to 65535");
					Assert.AreEqual("9002", store.Get(guid, "Port"), "A number outside its range was kept.");
					var mode = JocysCom.ClassLibrary.Controls.UiTree.UiTreeWalker.Find(panel, nodes.Single(x => x.Name == "Mode").Path);
					Assert.IsNull(JocysCom.ClassLibrary.Controls.UiTree.UiTreeWalker.SetValue(mode, "Accurate"));
					Assert.AreEqual("Accurate", store.Get(guid, "Mode"));
					var status = JocysCom.ClassLibrary.Controls.UiTree.UiTreeWalker.Find(panel, packets[0].Path);
					Assert.IsNotNull(JocysCom.ClassLibrary.Controls.UiTree.UiTreeWalker.SetValue(status, "5"), "A status was set.");
				}
			});
		}

		static void Flatten(JocysCom.ClassLibrary.Controls.UiTree.UiNode node, System.Collections.Generic.List<JocysCom.ClassLibrary.Controls.UiTree.UiNode> list)
		{
			list.Add(node);
			if (node.Items != null)
				foreach (var child in node.Items)
					Flatten(child, list);
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui")]
		[Description("A control the plugin declares for its device is placed on a page of its own, first, before the Properties page, with the device x360ce reads, and waits while there is none")]
		public void A_declared_control_is_placed_on_the_tab()
		{
			var type = Type(SettingsPluginDevice.Product);
			Assert.AreEqual(typeof(SettingsPluginControl), type.ControlType, "The control is found by its constructor.");
			Assert.IsNull(Type(SamplePluginDevice.Product).ControlType, "A control was found for a device that declares none.");
			Ui.OnUiThread(() =>
			{
				var device = new SettingsPluginDevice();
				using (var panel = new PluginDeviceUserControl(Guid.NewGuid(), type, device, new PluginSettings()))
				{
					var control = (SettingsPluginControl)Find(panel, "SettingsPluginControl");
					Assert.AreSame(device, control.Device, "The control was given another device than the one x360ce reads.");
					var pages = ((TabControl)Find(panel, "PluginTabControl")).TabPages.Cast<TabPage>().ToArray();
					CollectionAssert.AreEqual(new[] { "ControlTabPage", "PropertiesTabPage" }, pages.Select(x => x.Name).ToArray(),
						"The plugin's control and the properties are not on pages of their own, the control first.");
					Assert.AreEqual("Properties", pages[1].Text);
					Assert.AreSame(pages[0], control.Parent, "The control is not on its own page.");
					Assert.IsTrue(pages[1].Contains(Find(panel, "SettingsPropertyGrid")), "The grid is not on the Properties page.");
				}
				using (var panel = new PluginDeviceUserControl(Guid.NewGuid(), type, null, new PluginSettings()))
				{
					Assert.AreEqual(0, panel.Controls.Find("SettingsPluginControl", true).Length, "A control was made without a device.");
					StringAssert.Contains(Find(panel, "NoteLabel").Text, "while x360ce reads the device", "The tab does not say why the control is missing.");
				}
			});
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui"), TestCategory("critical")]
		[Description("The controller page has a tab named after a plugin device with settings or a control, last, and none for any other device")]
		public void The_controller_page_has_a_tab_for_a_plugin_device()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var pad = new PadControl(MapTo.Controller1))
				{
					form.Controls.Add(pad);
					var tabs = (TabControl)Find(pad, "PadTabControl");
					var count = tabs.TabPages.Count;
					pad.ShowPluginTab(Guid.NewGuid(), Type(SettingsPluginDevice.Product), new SettingsPluginDevice());
					Assert.AreEqual(count + 1, tabs.TabPages.Count, "No tab was added for the plugin device.");
					var page = tabs.TabPages[tabs.TabPages.Count - 1];
					Assert.AreEqual("PluginTabPage", page.Name, "The plugin's tab is not the last.");
					Assert.AreEqual("Settings Device", page.Text);
					Assert.AreEqual(1, page.Controls.OfType<PluginDeviceUserControl>().Count());
					pad.ShowPluginTab(Guid.NewGuid(), Type(SamplePluginDevice.Product), new SamplePluginDevice());
					Assert.AreEqual(count, tabs.TabPages.Count, "A device with nothing to show has a tab.");
					pad.ShowPluginTab(Guid.Empty, null, null);
					Assert.AreEqual(count, tabs.TabPages.Count, "A page with no plugin device has a plugin tab.");
				}
			});
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui"), TestCategory("critical")]
		[Description("No field or help on the settings tab covers another or runs past the edge, at any zoom and width")]
		public void The_settings_tab_keeps_every_field_clear_at_any_zoom()
		{
			Ui.OnUiThread(() =>
			{
				// The Test Controller's help runs to two paragraphs, which must wrap at every width.
				foreach (var type in new[] { Type(SettingsPluginDevice.Product), TestControllerDevice.Type })
				foreach (var zoom in new[] { 1f, 1.5f, 2f })
				{
					using (var form = new Form { Font = new Font(Control.DefaultFont.FontFamily, Control.DefaultFont.Size * zoom) })
					using (var panel = new PluginDeviceUserControl(Guid.NewGuid(), type, type.NewInstance(), new PluginSettings()))
					{
						panel.Dock = DockStyle.Fill;
						form.Controls.Add(panel);
						form.Show();
						foreach (var width in new[] { 400, 700, 1200 })
						{
							form.ClientSize = new Size((int)(width * zoom), (int)(500 * zoom));
							Application.DoEvents();
							var covered = ControlOverlapTest.Covered(panel).ToList();
							Assert.AreEqual(0, covered.Count, string.Format("{0} at {1} % and {2} pixels wide, covered:{3}{4}",
								type.Name, zoom * 100, width, Environment.NewLine, string.Join(Environment.NewLine, covered)));
							var outside = Descendants(panel).Where(x => x.Visible && x.Parent != null && x.Right > x.Parent.ClientSize.Width)
								.Select(x => x.Name).ToList();
							Assert.AreEqual(0, outside.Count, string.Format("{0} at {1} % and {2} pixels wide, past the edge: {3}",
								type.Name, zoom * 100, width, string.Join(", ", outside)));
						}
					}
				}
			});
		}

		static Control Find(Control root, string name)
		{
			var found = root.Controls.Find(name, true);
			Assert.AreEqual(1, found.Length, name + " was not found once.");
			return found[0];
		}

		static System.Collections.Generic.IEnumerable<Control> Descendants(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				yield return child;
				foreach (var grandChild in Descendants(child))
					yield return grandChild;
			}
		}
	}
}
