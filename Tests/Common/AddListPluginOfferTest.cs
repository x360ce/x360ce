// @under-test: App.v4/Controls/UserDevicesUserControl.cs, App.v4/Forms/MapDeviceToControllerForm.cs, App.v4/Plugins/PluginDevices.cs
// @area: plugins   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.DInput;
using x360ce.App.Plugins;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A plugin device exists only once somebody adds it. The list Add opens offers one of each kind a loaded plugin makes
	/// that has no row yet, so a plugin that has loaded can be mapped from the controller tab, and adding it gives it its row.
	/// </summary>
	[TestClass]
	public class AddListPluginOfferTest
	{
		static readonly Guid RemoteProduct = new Guid("884e9137-a970-446b-962f-31ed8c2abb11");

		static UserDevice[] Rows(UserDevicesUserControl panel)
		{
			Application.DoEvents();
			return panel.DevicesDataGridView.Rows.Cast<DataGridViewRow>().Select(x => (UserDevice)x.DataBoundItem).ToArray();
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui")]
		[Description("The list Add opens offers each plugin device not added yet, the Devices page does not, and adding one gives it its row")]
		public void Add_list_offers_each_plugin_device_not_yet_added()
		{
			var remote = TestControllerDevice.Manager.FindDeviceType(RemoteProduct);
			Assert.IsNotNull(remote, "The Remote MCP Controller plugin is not embedded in x360ce.");
			Ui.OnUiThread(() =>
			{
				var kinds = new[] { RemoteProduct, TestControllerDevice.ProductGuid };
				var before = SettingsManager.UserDevices.ItemsToArraySynchronized().Where(x => kinds.Contains(x.ProductGuid)).ToArray();
				foreach (var row in before)
					SettingsManager.UserDevices.Items.Remove(row);
				var oldHelper = Global.DHelper;
				var helper = oldHelper == null ? new DInputHelper() : null;
				if (helper != null)
					Global.DHelper = helper;
				// The Test Controller already has its row; the Remote MCP Controller has none.
				var test = PluginDevices.NewUserDevice(TestControllerDevice.Type);
				SettingsManager.UserDevices.Items.Add(test);
				try
				{
					using (var form = new Form { Width = 1200, Height = 600 })
					using (var page = new UserDevicesUserControl { Dock = DockStyle.Fill })
					{
						form.Controls.Add(page);
						form.Show();
						Assert.AreEqual(0, Rows(page).Count(x => x.ProductGuid == RemoteProduct), "The Devices page lists a device nobody has added.");
					}
					using (var form = new Form { Width = 1200, Height = 600 })
					using (var panel = new UserDevicesUserControl { MapDeviceToControllerMode = true, Dock = DockStyle.Fill })
					{
						form.Controls.Add(panel);
						form.Show();
						var rows = Rows(panel);
						var offered = rows.Where(x => x.ProductGuid == RemoteProduct).ToArray();
						Assert.AreEqual(1, offered.Length, "The list Add opens does not offer the Remote MCP Controller once.");
						Assert.AreEqual("Remote MCP Controller 1", offered[0].InstanceName);
						Assert.IsTrue(offered[0].IsOnline, "The offered device does not read as connected.");
						Assert.IsFalse(SettingsManager.UserDevices.Items.Contains(offered[0]), "Offering a device added it.");
						CollectionAssert.AreEqual(new[] { test }, rows.Where(x => x.ProductGuid == TestControllerDevice.ProductGuid).ToArray(),
							"A kind that already has its row was offered again.");

						panel.AddOffered(new[] { offered[0], test });
						Assert.AreEqual(1, SettingsManager.UserDevices.Items.Count(x => x == offered[0]), "Adding the offered device did not give it its row.");
						Assert.AreEqual(1, SettingsManager.UserDevices.Items.Count(x => x.ProductGuid == TestControllerDevice.ProductGuid),
							"A device that already had its row was added again.");
						Assert.AreEqual(1, Rows(panel).Count(x => x.ProductGuid == RemoteProduct), "The added device is listed twice.");
					}
				}
				finally
				{
					foreach (var row in SettingsManager.UserDevices.ItemsToArraySynchronized().Where(x => kinds.Contains(x.ProductGuid)).ToArray())
						SettingsManager.UserDevices.Items.Remove(row);
					foreach (var row in before)
						SettingsManager.UserDevices.Items.Add(row);
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
