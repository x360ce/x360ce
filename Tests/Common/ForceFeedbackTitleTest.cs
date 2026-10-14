// @under-test: App.v4/Controls/PadControl.cs
// @area: force-feedback   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>The Force Feedback page names the other tabs the selected device is on, and which of them shapes its force.</summary>
	/// <remarks>
	/// Each tab's Enable switch decides whether that tab's game rumbles the device, so a device on two tabs
	/// is felt from every tab switched on. Without a word on the page, the switch on the other tab is easy
	/// to forget. The lowest enabled tab's settings shape the force, so the others' are greyed out.
	/// </remarks>
	[TestClass]
	public class ForceFeedbackTitleTest
	{
		[TestMethod, TestCategory("force-feedback")]
		[Description("The Force Source box names the tab whose settings shape the force, and the tabs whose force the device plays")]
		public void The_force_source_names_the_tabs()
		{
			var shared = new DeviceForce(new PadSetting(), 1, new[] { 1, 2 });
			StringAssert.Contains(PadControl.ForceSourceNote(1, shared), "these settings shape its force");
			StringAssert.Contains(PadControl.ForceSourceNote(2, shared), "Controller 2 is the lowest enabled controller tab",
				"Another tab's page does not name the tab whose settings shape the force.");
			StringAssert.Contains(PadControl.ForceSourceNote(2, shared), "the strongest force of Controllers 2 and 3");
			StringAssert.Contains(PadControl.ForceSourceNote(0, new DeviceForce(new PadSetting(), 0, new[] { 0, 1, 3 })), "Controllers 1, 2 and 4");
			StringAssert.Contains(PadControl.ForceSourceNote(1, new DeviceForce(new PadSetting(), 0, new[] { 1 })), "It plays Controller 2's force.");
			var springOnly = new DeviceForce(new PadSetting { ForceSpringEnable = "1" }, 0, new int[0]);
			StringAssert.Contains(PadControl.ForceSourceNote(0, springOnly), "Only the centering spring holds it.");
			StringAssert.Contains(PadControl.ForceSourceNote(0, null), "No force reaches this device");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("Another tab's page greys out what shapes the force and names the tab that shapes it; Enable and Pass Through stay live; the box shows only for a device on several tabs")]
		public void Another_tabs_page_greys_out_what_shapes_the_force()
		{
			Ui.OnUiThread(() =>
			{
				var settings = SettingsManager.UserSettings.ItemsToArraySynchronized();
				var pads = SettingsManager.PadSettings.ItemsToArraySynchronized();
				var oldGame = SettingsManager.CurrentGame;
				var oldStatus = SettingsManager.Current.NotifySettingsStatus;
				SettingsManager.Current.NotifySettingsStatus = count => { };
				var game = new UserGame
				{
					FileName = "ff-source.exe",
					FileProductName = "FF source",
					EnableMask = (int)(MapToMask.Controller2 | MapToMask.Controller3),
					EmulationType = (int)EmulationType.Virtual,
				};
				var wheel = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Wheel", InstanceName = "Wheel", IsEnabled = true };
				var ps = new PadSetting { ForceEnable = "1" };
				ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
				SettingsManager.UserSettings.Items.Clear();
				SettingsManager.UserDevices.Items.Add(wheel);
				SettingsManager.PadSettings.Items.Add(ps);
				try
				{
					using (var form = new Form { Width = 900, Height = 700 })
					using (var pad = new PadControl(MapTo.Controller3) { Dock = DockStyle.Fill })
					{
						form.Controls.Add(pad);
						pad.InitPadControl();
						pad.UpdateSettingsMap();
						pad.InitPadData();
						form.Show();
						var rows = new[] { MapTo.Controller2, MapTo.Controller3 }.Select(tab =>
						{
							var row = AppHelper.GetNewSetting(wheel, game, tab);
							row.PadSettingChecksum = ps.PadSettingChecksum;
							SettingsManager.UserSettings.Items.Add(row);
							return row;
						}).ToArray();
						SettingsManager.CurrentGame = game;
						DeviceRouting.Refresh();
						pad.UpdateFromCurrentGame();
						var tabs = (TabControl)pad.Controls.Find("PadTabControl", true).Single();
						tabs.SelectedTab = (TabPage)pad.Controls.Find("ForceFeedbackTabPage", true).Single();
						Application.DoEvents();
						var box = pad.Controls.Find("ForceSourceGroupBox", true).Single();
						Assert.IsTrue(box.Visible, "The Force Source box is hidden while the device is on another tab too.");
						var source = pad.Controls.Find("ForceSourceLabel", true).Single();
						StringAssert.Contains(source.Text, "Controller 2 is the lowest enabled controller tab", "The page does not name the tab that shapes the force.");
						Assert.IsFalse(pad.Controls.Find("ForceTypeComboBox", true).Single().Enabled, "Controller 3's effect type is live while Controller 2's shapes the force.");
						Assert.IsFalse(pad.Controls.Find("LeftMotorStrengthTrackBar", true).Single().Enabled);
						Assert.IsFalse(pad.Controls.Find("ForceSpringEnableCheckBox", true).Single().Enabled);
						Assert.IsTrue(pad.Controls.Find("ForceEnableCheckBox", true).Single().Enabled, "Enable, which says whether this tab's game is felt, is greyed out.");
						Assert.IsTrue(pad.Controls.Find("ForcePassThroughCheckBox", true).Single().Enabled, "Pass Through, which belongs to this tab, is greyed out.");
						Assert.IsTrue(pad.Controls.Find("LeftMotorTestTrackBar", true).Single().Enabled, "The Test slider, which belongs to this tab, is greyed out.");

						// Controller 2 switched off: Controller 3 is now the lowest enabled tab.
						game.EnableMask = (int)MapToMask.Controller3;
						DeviceRouting.Refresh();
						pad.UpdateFromCurrentGame();
						Application.DoEvents();
						StringAssert.Contains(source.Text, "these settings shape its force");
						Assert.IsTrue(pad.Controls.Find("ForceTypeComboBox", true).Single().Enabled, "The effect type stays greyed out on the tab that now shapes the force.");

						// Taken off Controller 2: on one tab only, so there is nothing to explain.
						rows[0].MapTo = (int)MapTo.Disabled;
						DeviceRouting.Refresh();
						pad.UpdateFromCurrentGame();
						Application.DoEvents();
						Assert.IsFalse(box.Visible, "The Force Source box shows for a device on one tab only.");
					}
				}
				finally
				{
					SettingsManager.Current.NotifySettingsStatus = oldStatus;
					SettingsManager.CurrentGame = oldGame;
					SettingsManager.UserDevices.Items.Remove(wheel);
					SettingsManager.UserSettings.Items.Clear();
					foreach (var setting in settings)
						SettingsManager.UserSettings.Items.Add(setting);
					SettingsManager.PadSettings.Items.Clear();
					foreach (var item in pads)
						SettingsManager.PadSettings.Items.Add(item);
					DeviceRouting.Refresh();
				}
			});
		}

		[TestMethod, TestCategory("force-feedback")]
		[Description("The title names the other tabs, and says nothing more for a device on one tab")]
		public void The_title_names_the_other_tabs()
		{
			Assert.AreEqual("Force Feedback", PadControl.ForceFeedbackTitle(new MapTo[0]));
			Assert.AreEqual("Force Feedback - Shared with Controller 3", PadControl.ForceFeedbackTitle(new[] { MapTo.Controller3 }));
			Assert.AreEqual("Force Feedback - Shared with Controllers 1, 4", PadControl.ForceFeedbackTitle(new[] { MapTo.Controller1, MapTo.Controller4 }));
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("The page names the other tab the selected device is on, and stops when the device leaves it")]
		public void The_page_names_the_other_tab()
		{
			Ui.OnUiThread(() =>
			{
				var settings = SettingsManager.UserSettings.ItemsToArraySynchronized();
				var pads = SettingsManager.PadSettings.ItemsToArraySynchronized();
				var oldGame = SettingsManager.CurrentGame;
				var oldStatus = SettingsManager.Current.NotifySettingsStatus;
				SettingsManager.Current.NotifySettingsStatus = count => { };
				var game = new UserGame
				{
					FileName = "ff-title.exe",
					FileProductName = "FF title",
					EnableMask = (int)(MapToMask.Controller2 | MapToMask.Controller3),
					EmulationType = (int)EmulationType.Virtual,
				};
				var wheel = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Wheel", InstanceName = "Wheel" };
				SettingsManager.UserSettings.Items.Clear();
				try
				{
					using (var form = new Form { Width = 900, Height = 700 })
					using (var pad = new PadControl(MapTo.Controller2) { Dock = DockStyle.Fill })
					{
						form.Controls.Add(pad);
						pad.InitPadControl();
						pad.UpdateSettingsMap();
						pad.InitPadData();
						form.Show();
						SettingsManager.UserSettings.Items.Add(AppHelper.GetNewSetting(wheel, game, MapTo.Controller2));
						var third = AppHelper.GetNewSetting(wheel, game, MapTo.Controller3);
						SettingsManager.UserSettings.Items.Add(third);
						SettingsManager.CurrentGame = game;
						pad.UpdateFromCurrentGame();
						Application.DoEvents();
						var box = pad.Controls.Find("ForceFeedbackGroupBox", true).Single();
						Assert.AreEqual("Force Feedback - Shared with Controller 3", box.Text,
							"The page does not say the device is on another tab too.");

						third.MapTo = (int)MapTo.Disabled;
						Application.DoEvents();
						Assert.AreEqual("Force Feedback", box.Text, "The page still names a tab the device has left.");
					}
				}
				finally
				{
					SettingsManager.Current.NotifySettingsStatus = oldStatus;
					SettingsManager.CurrentGame = oldGame;
					SettingsManager.UserSettings.Items.Clear();
					foreach (var setting in settings)
						SettingsManager.UserSettings.Items.Add(setting);
					SettingsManager.PadSettings.Items.Clear();
					foreach (var ps in pads)
						SettingsManager.PadSettings.Items.Add(ps);
				}
			});
		}
	}
}
