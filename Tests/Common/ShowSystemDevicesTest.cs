// @under-test: App.v4/Controls/UserDevicesUserControl.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A device Windows files as a system device can be chosen for a controller once asked for.
	/// </summary>
	/// <remarks>
	/// Issue #1211: the Logitech G13 appeared on the Devices page but not in the list of devices to map,
	/// which leaves out system devices so that nobody maps a keyboard by accident. Windows files the
	/// G13, and other game devices, there too.
	/// </remarks>
	[TestClass]
	public class ShowSystemDevicesTest
	{
		static int Listed(UserDevicesUserControl panel, params UserDevice[] devices)
		{
			Application.DoEvents();
			return panel.DevicesDataGridView.Rows.Cast<DataGridViewRow>()
				.Count(r => devices.Contains(r.DataBoundItem as UserDevice));
		}

		static ToolStripButton Switch(Control panel)
		{
			var strip = (ToolStrip)panel.Controls.Find("ControllersToolStrip", true).Single();
			return (ToolStripButton)strip.Items["ShowSystemDevicesButton"];
		}

		[TestMethod, TestCategory("devices"), TestCategory("ui")]
		public void A_system_device_can_be_listed_for_mapping()
		{
			var gamepad = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "A gamepad" };
			var g13 = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Logitech G13", ConnectionClass = DEVCLASS.SYSTEM };
			// The lists hand their changes to the interface thread, so they are changed on it.
			Ui.OnUiThread(() =>
			{
				SettingsManager.UserDevices.Items.Add(gamepad);
				SettingsManager.UserDevices.Items.Add(g13);
				try
				{
					using (var form = new Form())
					using (var panel = new UserDevicesUserControl { MapDeviceToControllerMode = true, Dock = DockStyle.Fill })
					{
						form.Controls.Add(panel);
						form.Show();
						var showSystem = Switch(panel);
						Assert.IsTrue(showSystem.Visible, "Choosing a device for a controller offers the switch.");
						// The picker is narrow and its toolbar full; the one button that matters there stays on it.
						form.Width = 300;
						Application.DoEvents();
						Assert.IsFalse(showSystem.IsOnOverflow, "The switch was pushed into the overflow menu, where nobody finds it.");
						Assert.IsFalse(showSystem.Checked, "The switch starts off.");
						Assert.AreEqual(1, Listed(panel, gamepad, g13), "With the switch off only the gamepad is offered.");
						showSystem.PerformClick();
						Assert.AreEqual(2, Listed(panel, gamepad, g13), "With the switch on the G13 is offered too.");
						showSystem.PerformClick();
						Assert.AreEqual(1, Listed(panel, gamepad, g13), "Off again, it is left out again.");
					}
				}
				finally
				{
					SettingsManager.UserDevices.Items.Remove(gamepad);
					SettingsManager.UserDevices.Items.Remove(g13);
				}
			});
		}

		[TestMethod, TestCategory("devices"), TestCategory("ui")]
		public void The_devices_page_lists_everything_and_has_no_switch()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var panel = new UserDevicesUserControl())
				{
					form.Controls.Add(panel);
					form.Show();
					Assert.IsFalse(Switch(panel).Visible, "The Devices page already lists system devices; the switch would do nothing there.");
				}
			});
		}
	}
}
