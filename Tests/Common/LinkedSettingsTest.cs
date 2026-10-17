// @under-test: App.v4/Common/CloudClient.cs, App.v4/Controls/OptionsInternetUserControl.cs, App.v4/Controls/OptionsUserControl.cs, App.v4/Controls/PadControl.cs, App.v4/Controls/AxisMapUserControl.cs, App.v4/Controls/GameDetailsUserControl.cs
// @area: options   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A setting that does nothing in the present state is greyed out, never hidden, and says why; a setting that needs
	/// another one turns it on where that costs nothing.
	/// </summary>
	[TestClass]
	public class LinkedSettingsTest
	{
		static T Find<T>(Control root, string name) where T : Control
		{
			return (T)root.Controls.Find(name, true).Single();
		}

		static T Field<T>(object owner, string name)
		{
			return (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner);
		}

		[TestMethod, TestCategory("options"), TestCategory("critical")]
		[Description("Nothing goes to the cloud while Internet features are off, and the reason names the box to tick")]
		public void Internet_features_switch_the_whole_cloud_off()
		{
			var o = SettingsManager.Options;
			var features = o.InternetFeatures;
			var save = o.InternetAutoSave;
			try
			{
				o.InternetFeatures = true;
				o.InternetAutoSave = true;
				Assert.IsNull(CloudClient.OffReason, "The cloud is refused with both boxes ticked.");
				o.InternetAutoSave = false;
				StringAssert.Contains(CloudClient.OffReason, "Save Settings to Cloud");
				o.InternetFeatures = false;
				o.InternetAutoSave = true;
				StringAssert.Contains(CloudClient.OffReason, "Enable Internet Features",
					"Saving to the cloud went on with Internet features off.");
			}
			finally
			{
				o.InternetFeatures = features;
				o.InternetAutoSave = save;
			}
		}

		[TestMethod, TestCategory("options")]
		[Description("The cloud boxes are greyed out while Internet features are off")]
		public void Cloud_boxes_follow_internet_features()
		{
			Ui.OnUiThread(() =>
			{
				// Not bound to the options, so ticking a box here writes no setting.
				using (var panel = new OptionsInternetUserControl())
				{
					var internet = Find<CheckBox>(panel, "InternetFeaturesCheckBox");
					internet.Checked = true;
					Assert.IsTrue(Find<CheckBox>(panel, "InternetAutoSaveCheckBox").Enabled);
					Assert.IsTrue(Find<CheckBox>(panel, "InternetAutoLoadCheckBox").Enabled);
					internet.Checked = false;
					Assert.IsFalse(Find<CheckBox>(panel, "InternetAutoSaveCheckBox").Enabled, "Save stays live with Internet features off.");
					Assert.IsFalse(Find<CheckBox>(panel, "InternetAutoLoadCheckBox").Enabled, "Load stays live with Internet features off.");
				}
			});
		}

		[TestMethod, TestCategory("options")]
		[Description("The start-up window state and the hotkey's note are greyed out until their box is ticked, and recording keys ticks the hotkey's box")]
		public void Options_follow_the_box_they_belong_to()
		{
			Ui.OnUiThread(() =>
			{
				// Not bound to the options, so ticking a box here writes no setting and no Run key.
				using (var page = new OptionsUserControl())
				{
					page.CreateControl();
					var start = Find<CheckBox>(page, "StartWithWindowsCheckBox");
					var state = Find<ComboBox>(page, "StartWithWindowsStateComboBox");
					start.Checked = false;
					Assert.IsFalse(state.Enabled, "The start-up window state is live while the program does not start with Windows.");
					start.Checked = true;
					Assert.IsTrue(state.Enabled);
					var hotkey = Find<CheckBox>(page, "EmulationHotkeyCheckBox");
					var note = Find<CheckBox>(page, "EmulationOverlayCheckBox");
					hotkey.Checked = false;
					Assert.IsFalse(note.Enabled, "The hotkey's note is live while the hotkey is off.");
					var keys = Find<TextBox>(page, "EmulationHotkeyTextBox");
					typeof(OptionsUserControl).GetMethod("EmulationHotkeyTextBox_KeyDown", BindingFlags.NonPublic | BindingFlags.Instance)
						.Invoke(page, new object[] { keys, new KeyEventArgs(Keys.Control | Keys.Alt | Keys.X) });
					Assert.IsTrue(hotkey.Checked, "Recording keys left the hotkey's box unticked.");
					Assert.IsTrue(note.Enabled);
				}
			});
		}

		[TestMethod, TestCategory("options")]
		[Description("Start with Windows shows Windows' own setting for this copy, Task Manager's switch included, and writes every change back there")]
		public void Start_with_Windows_is_the_Windows_setting()
		{
			const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
			var runField = typeof(OptionsUserControl).GetField("RunKeyPath", flags);
			var approvedField = typeof(OptionsUserControl).GetField("StartupApprovedKeyPath", flags);
			var oldRun = runField.GetValue(null);
			var oldApproved = approvedField.GetValue(null);
			const string root = "Software\\x360ce.Tests.WindowsStart";
			runField.SetValue(null, root + "\\Run");
			approvedField.SetValue(null, root + "\\StartupApproved");
			try
			{
				Ui.OnUiThread(() =>
				{
					var name = Application.ProductName;
					using (var run = Registry.CurrentUser.CreateSubKey(root + "\\Run"))
					using (var approved = Registry.CurrentUser.CreateSubKey(root + "\\StartupApproved"))
					using (var page = new OptionsUserControl())
					{
						page.CreateControl();
						var start = Find<CheckBox>(page, "StartWithWindowsCheckBox");
						var state = Find<ComboBox>(page, "StartWithWindowsStateComboBox");
						var load = typeof(OptionsUserControl).GetMethod("LoadWindowsStart", BindingFlags.NonPublic | BindingFlags.Instance);
						run.SetValue(name, "\"C:\\Other copy\\x360ce.exe\" /WindowState=Normal");
						page.InitWindowsStart();
						var hint = Field<ToolTip>(page, "StartWithWindowsHint");
						Assert.IsFalse(start.Checked, "The box is ticked while Windows starts another copy.");
						StringAssert.Contains(hint.GetToolTip(start), "C:\\Other copy\\x360ce.exe");

						run.SetValue(name, "\"" + Application.ExecutablePath + "\" /WindowState=Maximized");
						load.Invoke(page, null);
						Assert.IsTrue(start.Checked, "The box is unticked while Windows starts this copy.");
						Assert.AreEqual("Maximized", state.SelectedItem);
						Assert.AreEqual("", hint.GetToolTip(start));

						approved.SetValue(name, new byte[] { 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary);
						load.Invoke(page, null);
						Assert.IsFalse(start.Checked, "The box is ticked while Task Manager has it switched off.");
						StringAssert.Contains(hint.GetToolTip(start), "Task Manager");

						start.Checked = true;
						Assert.AreEqual("\"" + Application.ExecutablePath + "\" /WindowState=Maximized", run.GetValue(name));
						Assert.IsNull(approved.GetValue(name), "Ticking left Task Manager's switch off.");
						state.SelectedItem = "Normal";
						Assert.AreEqual("\"" + Application.ExecutablePath + "\" /WindowState=Normal", run.GetValue(name));
						start.Checked = false;
						Assert.IsNull(run.GetValue(name), "Unticking left the program in Windows' start-up list.");
					}
				});
			}
			finally
			{
				Registry.CurrentUser.DeleteSubKeyTree(root, false);
				runField.SetValue(null, oldRun);
				approvedField.SetValue(null, oldApproved);
			}
		}

		[TestMethod, TestCategory("options")]
		[Description("The update's signature and version checks are on by default, and the person's choice is kept")]
		public void Update_checks_keep_the_choice()
		{
			var fresh = new Options();
			Assert.IsTrue(fresh.CheckUpdateSignature, "A new install skips the signature check.");
			Assert.IsTrue(fresh.CheckUpdateVersion, "A new install skips the version check.");
			var serializer = new System.Xml.Serialization.XmlSerializer(typeof(Options));
			var writer = new System.IO.StringWriter();
			serializer.Serialize(writer, new Options { CheckUpdateSignature = false, CheckUpdateVersion = false });
			var read = (Options)serializer.Deserialize(new System.IO.StringReader(writer.ToString()));
			Assert.IsFalse(read.CheckUpdateSignature, "The signature check came back on after a restart.");
			Assert.IsFalse(read.CheckUpdateVersion, "The version check came back on after a restart.");
			var o = SettingsManager.Options;
			var signature = o.CheckUpdateSignature;
			var version = o.CheckUpdateVersion;
			try
			{
				o.CheckUpdateSignature = false;
				o.CheckUpdateVersion = true;
				Ui.OnUiThread(() =>
				{
					using (var page = new OptionsUpdateUserControl())
					{
						page.UpdateSettingsMap();
						var box = Find<CheckBox>(page, "CheckDigitalSignatureCheckBox");
						Assert.IsFalse(box.Checked, "The box does not show the kept choice.");
						Assert.IsTrue(Find<CheckBox>(page, "CheckVersionCheckBox").Checked);
						box.Checked = true;
						Assert.IsTrue(o.CheckUpdateSignature, "Ticking the box was not kept.");
					}
				});
			}
			finally
			{
				o.CheckUpdateSignature = signature;
				o.CheckUpdateVersion = version;
			}
		}

		[TestMethod, TestCategory("options")]
		[Description("The dead zones and the sensitivity are greyed out with the reason in the header, and the chart stays")]
		public void Axis_tuning_is_greyed_out_with_its_reason()
		{
			Ui.OnUiThread(() =>
			{
				using (var chart = new AxisMapUserControl { HeaderText = "Left Trigger" })
				{
					var header = Find<GroupBox>(chart, "MainGroupBox");
					chart.SetTuningEnabled(false);
					Assert.IsFalse(Find<TrackBar>(chart, "DeadZoneTrackBar").Enabled, "The dead zone is live for a row that is no axis.");
					Assert.IsFalse(Find<NumericUpDown>(chart, "SensitivityNumericUpDown").Enabled);
					Assert.IsTrue(Find<PictureBox>(chart, "MainPictureBox").Enabled, "The chart went grey with the settings.");
					StringAssert.StartsWith(header.Text, "Left Trigger (");
					StringAssert.Contains(header.Text, "axis or slider");
					Assert.AreEqual("Left Trigger", chart.HeaderText);
					chart.SetTuningEnabled(true);
					Assert.IsTrue(Find<TrackBar>(chart, "DeadZoneTrackBar").Enabled);
					Assert.AreEqual("Left Trigger", header.Text);
				}
			});
		}

		[TestMethod, TestCategory("options")]
		[Description("Auto Map is greyed out and unticked on the Games tab, whatever the game kept")]
		public void Games_tab_auto_map_is_greyed_out()
		{
			Ui.OnUiThread(() =>
			{
				using (var details = new GameDetailsUserControl())
				{
					details.CurrentItem = new UserGame { FileName = "auto-map.exe", AutoMapMask = (int)MapToMask.Controller1 };
					var group = Find<GroupBox>(details, "AutoMapMaskGroupBox");
					Assert.IsFalse(group.Enabled, "Auto Map can still be ticked.");
					Assert.IsFalse(group.Controls.OfType<CheckBox>().Any(x => x.Checked), "Auto Map shows a mask nothing acts on.");
				}
			});
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A game that kept Auto Map leaves the controller's devices mapped; Auto Map is greyed out with its reason; the motor periods follow the effect type; x360ce 3's settings, the Advanced page and Axis to D-Pad, are shown greyed out under every emulation")]
		public void Controller_page_greys_out_what_does_nothing()
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
					FileName = "linked-settings.exe",
					FileProductName = "Linked settings",
					EnableMask = (int)MapToMask.Controller2,
					AutoMapMask = (int)MapToMask.Controller2,
					EmulationType = (int)EmulationType.Virtual,
				};
				var device = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Pad", InstanceName = "Pad" };
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
						var setting = AppHelper.GetNewSetting(device, game, MapTo.Controller2);
						SettingsManager.UserSettings.Items.Add(setting);
						SettingsManager.CurrentGame = game;
						pad.UpdateFromCurrentGame();
						Assert.AreEqual((int)MapTo.Controller2, setting.MapTo, "Auto Map took the device off the controller.");
						var autoMap = Field<ToolStripButton>(pad, "AutoMapButton");
						Assert.IsFalse(autoMap.Enabled, "Auto Map can still be pressed.");
						StringAssert.Contains(autoMap.ToolTipText, "Add");
						Assert.IsTrue(Field<ToolStripButton>(pad, "AddMapButton").Enabled, "Add is greyed out by a kept Auto Map.");

						var type = Find<ComboBox>(pad, "ForceTypeComboBox");
						var period = Find<TrackBar>(pad, "LeftMotorPeriodTrackBar");
						type.SelectedItem = ForceEffectType.Constant;
						Assert.IsFalse(period.Enabled, "The motor period is live under Constant, which has no period.");
						Assert.IsFalse(Find<ComboBox>(pad, "MotorPeriodPresetComboBox").Enabled);
						type.SelectedItem = ForceEffectType.PeriodicSine2;
						Assert.IsTrue(period.Enabled, "The motor period is greyed out under Sine.");

						var tabs = Find<TabControl>(pad, "PadTabControl");
						var advanced = tabs.TabPages.Cast<TabPage>().SingleOrDefault(x => x.Name == "AdvancedTabPage");
						Assert.IsNotNull(advanced, "The Advanced page is hidden for a virtual controller.");
						Assert.IsFalse(Find<Panel>(pad, "AdvancedOptionsPanel").Enabled, "x360ce 3's settings can be changed here, where nothing reads them.");
						StringAssert.Contains(Find<GroupBox>(pad, "DeviceGroupBox").Text, "x360ce 3");
						Find<CheckBox>(pad, "PassThroughCheckBox").Checked = true;
						Assert.IsTrue(Find<CheckBox>(pad, "ForcePassThroughCheckBox").Enabled,
							"x360ce 3's full pass-through greys out this version's force pass-through.");
						Find<CheckBox>(pad, "PassThroughCheckBox").Checked = false;

						var dpad = Find<GroupBox>(pad, "AxisToDPadGroupBox");
						Assert.IsFalse(dpad.Enabled, "Axis to D-Pad is live for a virtual controller, which does not read it.");
						StringAssert.Contains(dpad.Text, "x360ce 3");
						game.EmulationType = (int)EmulationType.Library;
						pad.UpdateFromCurrentGame();
						Assert.IsFalse(dpad.Enabled, "Axis to D-Pad is live under Library emulation, though this version writes no x360ce.ini.");
						Assert.IsTrue(tabs.TabPages.Contains(advanced), "The Advanced page went away.");
					}
				}
				finally
				{
					SettingsManager.Current.NotifySettingsStatus = oldStatus;
					SettingsManager.CurrentGame = oldGame;
					SettingsManager.UserSettings.Items.Clear();
					foreach (var item in settings)
						SettingsManager.UserSettings.Items.Add(item);
					SettingsManager.PadSettings.Items.Clear();
					foreach (var ps in pads)
						SettingsManager.PadSettings.Items.Add(ps);
				}
			});
		}
	}
}
