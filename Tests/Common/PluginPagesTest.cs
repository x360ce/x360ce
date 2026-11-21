// @under-test: App.v4/Plugins/PluginPages.cs, App.v4/Plugins/PluginManager.cs, App.v4/MainControl.cs, App.v4/Plugins/PluginSettings.cs, PluginApi/IPluginPage.cs, PluginApi/IPluginHost.cs
// @area: plugins   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.DInput;
using x360ce.App.Issues;
using x360ce.App.Plugins;
using x360ce.Engine;
using x360ce.Engine.Data;
using x360ce.Plugins;

namespace x360ce.Tests
{
	/// <summary>
	/// A plugin can add a page to the main window: x360ce makes it once with a host, places it after the Devices tab, and
	/// leaves out a page that throws, saying why. The host adds and maps the plugin's device as the person does, and keeps
	/// the plugin's values with the other settings.
	/// </summary>
	[TestClass]
	public class PluginPagesTest
	{
		/// <summary>The test library loaded as a plugin from a folder of its own, as a third-party plugin is.</summary>
		static void WithSamplePlugin(Action<PluginManager> test)
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.PluginPagesTest." + Guid.NewGuid().ToString("N"));
			var sub = Path.Combine(folder, "SamplePlugin");
			Directory.CreateDirectory(sub);
			try
			{
				File.Copy(typeof(SamplePluginPage).Assembly.Location, Path.Combine(sub, "SamplePlugin.dll"));
				using (var manager = new PluginManager(folder, null, () => true, null))
				{
					manager.Scan();
					test(manager);
				}
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("A public control that implements IPluginPage is a page; each is made once with a host, and one that throws is left out and reported on the Plugins page and the Issues tab")]
		public void Pages_are_made_once_and_a_throwing_page_is_reported()
		{
			var types = PluginManager.FindPageTypes(typeof(SamplePluginPage).Assembly);
			CollectionAssert.AreEquivalent(new[] { typeof(SamplePluginPage), typeof(ThrowingPluginPage) }, types,
				"The pages found are not the controls that implement IPluginPage.");
			WithSamplePlugin(manager => Ui.OnUiThread(() =>
			{
				var file = manager.Files.Single();
				Assert.AreEqual(PluginFileState.Loaded, file.State);
				CollectionAssert.AreEquivalent(new[] { "SamplePluginPage", "ThrowingPluginPage" }, file.PageTypes.Select(x => x.Name).ToArray());
				var tried = new HashSet<Type>();
				var pages = PluginPages.Make(manager, new PluginSettings(), tried);
				try
				{
					Assert.AreEqual(1, pages.Length, "The page that throws was not left out, or the other was not made.");
					Assert.AreEqual("SamplePluginPage", pages[0].GetType().Name);
					Assert.AreEqual("Sample Page", ((IPluginPage)pages[0]).PageName);
					Assert.IsInstanceOfType(pages[0].GetType().GetProperty("Host").GetValue(pages[0]), typeof(PluginHost), "The page was not given a host.");
					StringAssert.Contains(file.PageFault, "ThrowingPluginPage");
					StringAssert.Contains(file.PageFault, "No page today.");
					Assert.IsTrue(PluginIssue.Problems(manager).Any(x => x.Contains("No page today.")), "The Issues tab does not say why the page is missing.");
					StringAssert.Contains(OptionsPluginsUserControl.GetRows(manager).Single().Reason, "No page today.", "The Plugins page does not say why the page is missing.");
					Assert.AreEqual(0, PluginPages.Make(manager, new PluginSettings(), tried).Length, "A page was made twice.");
				}
				finally
				{
					foreach (var page in pages)
						page.Dispose();
				}
			}));
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui")]
		[Description("A plugin's page is on a tab named after it right after the Devices tab, once, and stays after it as the tabs before it are hidden and shown")]
		public void A_page_is_placed_after_the_devices_tab()
		{
			WithSamplePlugin(manager => Ui.OnUiThread(() =>
			{
				var oldManager = PluginManager.Current;
				var oldStore = PluginSettings.Current;
				PluginManager.Current = manager;
				PluginSettings.Current = new PluginSettings();
				try
				{
					using (var form = new Form { Width = 1100, Height = 850 })
					using (var main = SpacingGridTest.MainWindowPages())
					{
						var tabs = main.MainTabControl.TabPages;
						// As the options leave the tabs at first, and the window builds them: before any of their windows are made.
						main.ShowProgramsTab(false);
						main.ShowSettingsTab(false);
						var count = tabs.Count;
						main.AddPluginPages();
						main.AddPluginPages();
						Assert.AreEqual(count + 1, tabs.Count, "The page was not added as one tab.");
						var tab = tabs.Cast<TabPage>().Where(x => x.Text == "Sample Page").ToArray();
						Assert.AreEqual(1, tab.Length, "The page is not on one tab of its own.");
						Assert.AreEqual("SamplePageTabPage", tab[0].Name, "The tab is not named after the page, as the program's tabs are.");
						Assert.AreEqual(tabs.IndexOf(main.DevicesTabPage) + 1, tabs.IndexOf(tab[0]), "The page is not right after the Devices tab.");
						Assert.AreEqual(main.IssuesTabPage, tabs[tabs.Count - 1], "The tabs after the page are not in their order.");
						Assert.AreEqual(DockStyle.Fill, tab[0].Controls.Cast<Control>().Single().Dock);
						// On the window, as the options hide and show the tabs before it.
						main.Dock = DockStyle.Fill;
						form.Controls.Add(main);
						form.Show();
						main.ShowSettingsTab(true);
						Assert.AreEqual(tabs.IndexOf(main.DevicesTabPage) + 1, tabs.IndexOf(tab[0]), "Settings shown moved the page off Devices.");
						main.ShowDevicesTab(false);
						Assert.AreEqual(tabs.IndexOf(main.SettingsTabPage) + 1, tabs.IndexOf(tab[0]), "With Devices hidden the page does not follow the tab before it.");
						main.ShowDevicesTab(true);
						Assert.AreEqual(tabs.IndexOf(main.DevicesTabPage) + 1, tabs.IndexOf(tab[0]), "Devices shown again did not go before the page.");
					}
				}
				finally
				{
					PluginManager.Current = oldManager;
					PluginSettings.Current = oldStore;
				}
			}));
		}

		[TestMethod, TestCategory("plugins"), TestCategory("mapping"), TestCategory("critical")]
		[Description("The host adds a device of a kind once, maps the first to a controller as Add does, says why it cannot, and keeps the plugin's values with the settings")]
		public void The_host_adds_and_maps_a_device_and_keeps_values()
		{
			Ui.OnUiThread(() =>
			{
				var product = TestControllerDevice.ProductGuid;
				var store = new PluginSettings();
				var host = new PluginHost(TestControllerDevice.Type.File, TestControllerDevice.Manager, store);
				var before = SettingsManager.UserDevices.ItemsToArraySynchronized().Where(x => x.ProductGuid == product).ToArray();
				foreach (var row in before)
					SettingsManager.UserDevices.Items.Remove(row);
				var settings = SettingsManager.UserSettings.ItemsToArraySynchronized();
				var game = SettingsManager.CurrentGame;
				var hidHide = SettingsManager.Options.HidHideConfigureAutomatically;
				SettingsManager.Options.HidHideConfigureAutomatically = false;
				var oldHelper = Global.DHelper;
				var helper = oldHelper == null ? new DInputHelper() : null;
				if (helper != null)
					Global.DHelper = helper;
				try
				{
					host.AddDevice(product);
					host.AddDevice(product);
					var added = SettingsManager.UserDevices.ItemsToArraySynchronized().Where(x => x.ProductGuid == product).ToArray();
					Assert.AreEqual(1, added.Length, "AddDevice did not add the device once.");
					Assert.AreEqual("Test Controller 1", added[0].InstanceName);
					Assert.ThrowsExactly<InvalidOperationException>(() => host.AddDevice(Guid.NewGuid()));

					SettingsManager.CurrentGame = null;
					StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => host.MapDevice(product, 2)).Message, "No game");
					var mapped = new UserGame
					{
						FileName = "plugin-page.exe",
						FileProductName = "Plugin page",
						EnableMask = (int)MapToMask.Controller1,
						EmulationType = (int)EmulationType.Virtual,
					};
					SettingsManager.CurrentGame = mapped;
					Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => host.MapDevice(product, 5));
					host.MapDevice(product, 2);
					var rows = SettingsManager.GetSettings(mapped.FileName).Where(x => x.InstanceGuid == added[0].InstanceGuid).ToArray();
					Assert.AreEqual(1, rows.Length, "MapDevice did not map the device once.");
					Assert.AreEqual((int)MapTo.Controller2, rows[0].MapTo, "MapDevice did not map the device to Controller 2.");

					Assert.IsNull(host.GetValue("Reconnect"));
					host.SetValue("Reconnect", "True");
					Assert.AreEqual("True", host.GetValue("Reconnect"));
					Assert.AreEqual("True", store.GetPluginValue(TestControllerDevice.Type.File.Name, "Reconnect"), "The value is not kept under the plugin's name.");
					var path = Path.Combine(Path.GetTempPath(), "x360ce.PluginPagesTest." + Guid.NewGuid().ToString("N") + ".xml");
					try
					{
						store.SaveAs(path, new Guid[0]);
						var read = new PluginSettings();
						read.LoadFrom(path);
						Assert.AreEqual("True", read.GetPluginValue(TestControllerDevice.Type.File.Name, "Reconnect"), "Saving with no device listed dropped the plugin's value.");
					}
					finally
					{
						File.Delete(path);
					}
					host.SetValue("Reconnect", null);
					Assert.IsNull(host.GetValue("Reconnect"), "Null did not forget the value.");
				}
				finally
				{
					foreach (var row in SettingsManager.UserDevices.ItemsToArraySynchronized().Where(x => x.ProductGuid == product).ToArray())
						SettingsManager.UserDevices.Items.Remove(row);
					foreach (var row in before)
						SettingsManager.UserDevices.Items.Add(row);
					SettingsManager.UserSettings.Items.Clear();
					foreach (var setting in settings)
						SettingsManager.UserSettings.Items.Add(setting);
					SettingsManager.CurrentGame = game;
					SettingsManager.Options.HidHideConfigureAutomatically = hidHide;
					if (helper != null)
					{
						Global.DHelper = oldHelper;
						helper.Dispose();
					}
				}
			});
		}
	}
}
