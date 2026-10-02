// @under-test: App.v4/Controls/PadControl.cs
// @area: force-feedback   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>The Force Feedback page names the other tabs the selected device is on.</summary>
	/// <remarks>
	/// Each tab's Enable switch decides whether that tab's game rumbles the device, so a device on two tabs
	/// is felt from every tab switched on. Without a word on the page, the switch on the other tab is easy
	/// to forget.
	/// </remarks>
	[TestClass]
	public class ForceFeedbackTitleTest
	{
		[TestMethod, TestCategory("force-feedback")]
		[Description("The title names the other tabs, and says nothing more for a device on one tab")]
		public void The_title_names_the_other_tabs()
		{
			Assert.AreEqual("Force Feedback", PadControl.ForceFeedbackTitle(new MapTo[0]));
			Assert.AreEqual("Force Feedback - Also on Controller 3", PadControl.ForceFeedbackTitle(new[] { MapTo.Controller3 }));
			Assert.AreEqual("Force Feedback - Also on Controllers 1, 4", PadControl.ForceFeedbackTitle(new[] { MapTo.Controller1, MapTo.Controller4 }));
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("The page names the other tab the selected device is on, and stops when the device leaves it")]
		public void The_page_names_the_other_tab()
		{
			Ui.OnUiThread(() =>
			{
				var settings = SettingsManager.UserSettings.ItemsToArraySyncronized();
				var pads = SettingsManager.PadSettings.ItemsToArraySyncronized();
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
						Assert.AreEqual("Force Feedback - Also on Controller 3", box.Text,
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
