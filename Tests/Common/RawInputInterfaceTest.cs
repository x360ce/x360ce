// @under-test: App.v4/Controls/UserDevicesUserControl.cs, App.v4/Controls/PadControl.cs, App.v4/Controls/PadTabPages/DirectInputControl.cs, App.v4/Common/SettingsManager.cs, App.v4/Common/AutoMapHelper.cs, App.v4/Mcp/McpTools.cs, Engine/Data/UserDevice.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.Mcp;
using x360ce.Engine;
using x360ce.Engine.Data;
using x360ce.Engine.Mcp;

namespace x360ce.Tests
{
	/// <summary>
	/// A controller read through DirectInput and through Raw Input is two rows, usually with the same name. The lists say
	/// which source each row is read through and keep the two rows next to each other, the Direct Input tab shows a Raw
	/// Input row's values, the Force Feedback page says it sends no force feedback, and a row added to a tab where its
	/// twin is mapped starts with the twin's settings.
	/// </summary>
	[TestClass]
	public class RawInputInterfaceTest
	{
		const string PadPath = @"\\?\HID#VID_046D&PID_C219#7&1A2B3C4D&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";
		const string WheelPath = @"\\?\HID#VID_046D&PID_C29B#7&2B3C4D5E&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";

		/// <summary>A DirectInput row, with its interface path in lower case, as DirectInput reports it, or empty for a device with none.</summary>
		static UserDevice DirectInputRow(string name, string path)
		{
			return new UserDevice
			{
				InstanceGuid = Guid.NewGuid(),
				InstanceName = name,
				ProductName = name,
				HidDevicePath = (path ?? "").ToLowerInvariant(),
				CapType = (int)DeviceType.Gamepad,
				IsOnline = true,
			};
		}

		/// <summary>A Raw Input row, with its interface path as Raw Input reports it, or empty for a device with none.</summary>
		static UserDevice RawInputRow(string name, string path)
		{
			return new UserDevice
			{
				InstanceGuid = Guid.NewGuid(),
				InstanceName = name,
				ProductName = name,
				HidDevicePath = path ?? "",
				InputSourceType = (int)InputSourceType.RawInput,
				CapType = (int)DeviceType.Gamepad,
				IsOnline = true,
			};
		}

		static int RowOf(DataGridView grid, object item)
		{
			var row = grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(x => x.DataBoundItem == item);
			return row == null ? -1 : row.Index;
		}

		static string SourceShown(DataGridView grid, object item)
		{
			return (string)grid.Rows[RowOf(grid, item)].Cells["SourceColumn"].FormattedValue;
		}

		#region Lists

		[TestMethod, TestCategory("devices")]
		[Description("A device's twin is the same controller read through the other source, found by its interface path in any letter case")]
		public void Twins_are_found_by_path_and_source()
		{
			var diPad = DirectInputRow("Pad", PadPath);
			var rawPad = RawInputRow("Pad", PadPath);
			Assert.IsTrue(rawPad.IsTwinOf(diPad), "The pad read through Raw Input is not taken for its DirectInput twin.");
			Assert.IsTrue(diPad.IsTwinOf(rawPad), "Twins are not twins both ways.");
			Assert.IsFalse(rawPad.IsTwinOf(RawInputRow("Pad", PadPath)), "Two rows of one source were taken for twins.");
			Assert.IsFalse(rawPad.IsTwinOf(DirectInputRow("Wheel", WheelPath)), "A device of another path was taken for a twin.");
			Assert.IsFalse(DirectInputRow("Keyboard", null).IsTwinOf(RawInputRow("Keyboard", null)), "Devices with no path were taken for twins.");
			Assert.AreEqual(InputSourceType.DirectInput, new UserDevice { InputSourceType = 1 }.InputSource, "1 is DirectInput.");
			Assert.AreEqual(InputSourceType.DirectInput, diPad.InputSource, "0 is DirectInput.");
			Assert.AreEqual("DirectInput", AppHelper.GetInputSourceName(diPad));
			Assert.AreEqual("Raw Input", AppHelper.GetInputSourceName(rawPad));
		}

		[TestMethod, TestCategory("devices")]
		[Description("The lists put each device's twin beside it, DirectInput first, and keep the order otherwise")]
		public void Twins_are_listed_together()
		{
			var diPad = DirectInputRow("Pad", PadPath);
			var keyboard = DirectInputRow("Keyboard", null);
			var rawWheel = RawInputRow("Wheel", WheelPath);
			var rawPad = RawInputRow("Pad", PadPath);
			var diWheel = DirectInputRow("Wheel", WheelPath);
			var ordered = UserDevicesUserControl.TwinsTogether(new[] { diPad, keyboard, rawWheel, rawPad, diWheel });
			CollectionAssert.AreEqual(new[] { diPad, rawPad, keyboard, diWheel, rawWheel }, ordered,
				"Expected: " + string.Join(", ", ordered.Select(x => x.ProductName + " " + x.InputSource)));
		}

		/// <summary>Shows the device list, on the Devices page or as the list Add opens, with the twins apart in the program's list.</summary>
		static void ShowsTwinsTogether(bool mapMode)
		{
			var diPad = DirectInputRow("Pad", PadPath);
			var other = DirectInputRow("Other", null);
			var rawPad = RawInputRow("Pad", PadPath);
			var diWheel = DirectInputRow("Wheel", WheelPath);
			var rawWheel = RawInputRow("Wheel", WheelPath);
			var devices = new[] { diPad, other, rawPad, diWheel, rawWheel };
			Ui.OnUiThread(() =>
			{
				SettingsManager.UserDevices.Items.Add(diPad);
				SettingsManager.UserDevices.Items.Add(other);
				SettingsManager.UserDevices.Items.Add(rawPad);
				SettingsManager.UserDevices.Items.Add(diWheel);
				try
				{
					using (var form = new Form())
					using (var panel = new UserDevicesUserControl { MapDeviceToControllerMode = mapMode, Dock = DockStyle.Fill })
					{
						form.Controls.Add(panel);
						form.Show();
						Application.DoEvents();
						var grid = panel.DevicesDataGridView;
						Assert.AreEqual(grid.Columns["MySidColumn"].DisplayIndex + 1, grid.Columns["SourceColumn"].DisplayIndex,
							"The Source column is not beside the Instance ID column.");
						Assert.AreEqual("DirectInput", SourceShown(grid, diPad));
						Assert.AreEqual("Raw Input", SourceShown(grid, rawPad));
						Assert.AreEqual(RowOf(grid, diPad) + 1, RowOf(grid, rawPad), "The pad's two rows are not next to each other.");
						Assert.AreEqual(RowOf(grid, rawPad) + 1, RowOf(grid, other), "The device between them in the program's list did not move down.");
						// A Raw Input row found later goes beside its twin, not to the end.
						SettingsManager.UserDevices.Items.Add(rawWheel);
						Application.DoEvents();
						Assert.AreEqual(RowOf(grid, diWheel) + 1, RowOf(grid, rawWheel), "A Raw Input row found later is not beside its twin.");
						Assert.AreEqual("Raw Input", SourceShown(grid, rawWheel));
						// The program's own list keeps the order the devices were found in, which is the order they are saved in.
						var kept = SettingsManager.UserDevices.Items.Where(x => devices.Contains(x)).ToArray();
						CollectionAssert.AreEqual(new[] { diPad, other, rawPad, diWheel, rawWheel }, kept, "The program's list was reordered.");
					}
				}
				finally
				{
					foreach (var device in devices)
						SettingsManager.UserDevices.Items.Remove(device);
				}
			});
		}

		[TestMethod, TestCategory("devices"), TestCategory("ui")]
		[Description("The Devices page names each row's source and lists a controller's two rows together")]
		public void Devices_page_shows_the_source_and_twins_together()
		{
			ShowsTwinsTogether(false);
		}

		[TestMethod, TestCategory("devices"), TestCategory("ui")]
		[Description("The list Add opens names each row's source and lists a controller's two rows together")]
		public void Add_list_shows_the_source_and_twins_together()
		{
			ShowsTwinsTogether(true);
		}

		[TestMethod, TestCategory("mcp"), TestCategory("devices")]
		[Description("devices_list names each device's source")]
		public void Device_list_names_the_source()
		{
			McpTools.Register();
			McpCatalog.OnUiThread = a => a();
			var diPad = DirectInputRow("Pad", PadPath);
			var rawPad = RawInputRow("Pad", PadPath);
			SettingsManager.UserDevices.Items.Add(diPad);
			SettingsManager.UserDevices.Items.Add(rawPad);
			try
			{
				var rows = ((object[])McpTools.DevicesList()).Cast<Dictionary<string, object>>().ToList();
				Func<UserDevice, Dictionary<string, object>> row = d => rows.Single(x => (string)x["InstanceGuid"] == d.InstanceGuid.ToString());
				Assert.AreEqual("DirectInput", row(diPad)["Source"]);
				Assert.AreEqual("RawInput", row(rawPad)["Source"]);
				Assert.AreEqual(diPad.ProductName, row(rawPad)["Product"], "The other fields are no longer given.");
			}
			finally
			{
				SettingsManager.UserDevices.Items.Remove(diPad);
				SettingsManager.UserDevices.Items.Remove(rawPad);
			}
		}

		#endregion

		#region Direct Input tab and automatic preset

		[TestMethod, TestCategory("devices")]
		[Description("The Direct Input tab is titled by the row's source, and a Raw Input row with a state is not called stateless")]
		public void Direct_input_tab_is_titled_by_the_source()
		{
			var raw = RawInputRow("Pad", PadPath);
			var id = raw.InstanceId;
			Assert.AreEqual("Raw Input - " + id + " - Online", PadControl.DirectInputTabTitle(raw), "Online, not read yet.");
			raw.SourceState = new SourceState();
			Assert.AreEqual("Raw Input - " + id, PadControl.DirectInputTabTitle(raw), "Online with a state.");
			raw.IsOnline = false;
			Assert.AreEqual("Raw Input - " + id + " - Offline", PadControl.DirectInputTabTitle(raw));
			var di = DirectInputRow("Pad", PadPath);
			Assert.AreEqual("Direct Input - " + di.InstanceId + " - Online", PadControl.DirectInputTabTitle(di), "A DirectInput row with no DirectInput device.");
			Assert.AreEqual("Direct Input - Offline", PadControl.DirectInputTabTitle(null), "No device selected.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("ui")]
		[Description("The Direct Input tab shows a Raw Input row's values, which it has no DirectInput state for")]
		public void Direct_input_tab_shows_a_raw_input_row()
		{
			Ui.OnUiThread(() =>
			{
				using (var tab = new DirectInputUserControl())
				{
					tab.CreateControl();
					var raw = RawInputRow("Pad", PadPath);
					raw.SourceState = new SourceState();
					raw.SourceState.Axis[0] = 1234;
					raw.SourceState.Sliders[0] = 55;
					raw.SourceState.Povs[0] = 9000;
					raw.SourceState.Buttons[2] = true;
					Assert.IsNull(raw.JoState);
					tab.UpdateFrom(raw);
					var axis = Table(tab, "DiAxisTable");
					var sliders = Table(tab, "DiSlidersTable");
					var povs = Table(tab, "DiPovsTable");
					var buttons = Table(tab, "DiButtonsTable");
					Assert.AreEqual(1234, axis.Rows[0][1], "Axis X.");
					Assert.AreEqual(55, sliders.Rows[0][1], "Slider 0.");
					Assert.AreEqual(9000, povs.Rows[0][1], "POV 0.");
					Assert.AreEqual("02", buttons.Rows[0][0], "Buttons.");
					// A DirectInput row whose read failed has no DirectInput state, and shows none; its last engine state is not a reading.
					var di = DirectInputRow("Wheel", WheelPath);
					di.SourceState = raw.SourceState;
					tab.UpdateFrom(di);
					Assert.AreEqual(0, axis.Rows[0][1], "A DirectInput row with no DirectInput state showed its last engine state.");
				}
			});
		}

		static DataTable Table(DirectInputUserControl tab, string name)
		{
			return (DataTable)typeof(DirectInputUserControl).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tab);
		}

		[TestMethod, TestCategory("devices")]
		[Description("Auto Preset works for an online Raw Input row, which has no DirectInput device, and waits for an offline one")]
		public void Auto_preset_works_for_a_raw_input_row()
		{
			var raw = RawInputRow("Pad", PadPath);
			raw.DeviceObjects = RawInputFixtures.RumblePad2.Objects;
			Assert.IsTrue(AutoMapHelper.CanGetAutoPreset(raw), "An online Raw Input row was taken for offline.");
			Assert.IsFalse(string.IsNullOrEmpty(AutoMapHelper.GetAutoPreset(raw).ButtonA), "The automatic preset mapped nothing.");
			raw.IsOnline = false;
			Assert.IsFalse(AutoMapHelper.CanGetAutoPreset(raw), "An offline Raw Input row was offered a preset.");
			var di = DirectInputRow("Pad", PadPath);
			di.DeviceObjects = RawInputFixtures.RumblePad2.Objects;
			Assert.IsFalse(AutoMapHelper.CanGetAutoPreset(di), "A DirectInput row with no DirectInput device was offered a preset.");
			Assert.IsTrue(AutoMapHelper.CanGetAutoPreset(TestDeviceHelper.NewUserDevice()), "The demo device was refused.");
		}

		#endregion

		#region Mapping and force feedback

		[TestMethod, TestCategory("mapping"), TestCategory("devices"), TestCategory("critical")]
		[Description("A row added to a tab where its twin is mapped takes the twin's settings; one whose twin is not on the tab gets the automatic preset; the tab's list names each source and the Force Feedback page notes the Raw Input row")]
		public void A_twin_takes_the_twins_settings()
		{
			Ui.OnUiThread(() =>
			{
				var settings = SettingsManager.UserSettings.ItemsToArraySynchronized();
				var pads = SettingsManager.PadSettings.ItemsToArraySynchronized();
				var oldGame = SettingsManager.CurrentGame;
				var oldStatus = SettingsManager.Current.NotifySettingsStatus;
				SettingsManager.Current.NotifySettingsStatus = count => { };
				var diPad = DirectInputRow("Pad", PadPath);
				var rawPad = RawInputRow("Pad", PadPath);
				var diWheel = DirectInputRow("Wheel", WheelPath);
				var rawWheel = RawInputRow("Wheel", WheelPath);
				var devices = new[] { diPad, rawPad, diWheel, rawWheel };
				foreach (var device in devices)
				{
					device.DeviceObjects = RawInputFixtures.RumblePad2.Objects;
					SettingsManager.UserDevices.Items.Add(device);
				}
				var game = new UserGame
				{
					FileName = "twins.exe",
					FileProductName = "Twins",
					EnableMask = (int)MapToMask.Controller2,
					EmulationType = (int)EmulationType.Virtual,
				};
				try
				{
					SettingsManager.UserSettings.Items.Clear();
					using (var form = new Form { Width = 900, Height = 700 })
					using (var pad = new PadControl(MapTo.Controller2) { Dock = DockStyle.Fill })
					{
						form.Controls.Add(pad);
						pad.InitPadControl();
						pad.UpdateSettingsMap();
						pad.InitPadData();
						form.Show();
						// The pad's DirectInput row is on the tab with settings of its own, unlike its automatic preset.
						var ps = new PadSetting { ButtonA = "4", ForceEnable = "1" };
						ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
						SettingsManager.PadSettings.Items.Add(ps);
						var twinRow = AppHelper.GetNewSetting(diPad, game, MapTo.Controller2);
						twinRow.PadSettingChecksum = ps.PadSettingChecksum;
						SettingsManager.UserSettings.Items.Add(twinRow);
						// The wheel's DirectInput row is on another tab.
						var otherTab = AppHelper.GetNewSetting(diWheel, game, MapTo.Controller3);
						otherTab.PadSettingChecksum = ps.PadSettingChecksum;
						SettingsManager.UserSettings.Items.Add(otherTab);
						SettingsManager.CurrentGame = game;
						pad.UpdateFromCurrentGame();
						Application.DoEvents();

						pad.MapDevices(game, new[] { rawPad });
						var rawRow = SettingsManager.GetSettings(game.FileName).Single(x => x.InstanceGuid == rawPad.InstanceGuid);
						Assert.AreEqual((int)MapTo.Controller2, rawRow.MapTo);
						Assert.AreEqual(ps.PadSettingChecksum, rawRow.PadSettingChecksum, "The Raw Input row did not take its twin's settings.");
						Assert.AreEqual((int)MapTo.Controller2, twinRow.MapTo, "The twin was taken off the tab.");
						Assert.IsTrue(twinRow.IsEnabled, "The twin was unticked.");

						pad.MapDevices(game, new[] { rawWheel });
						var wheelRow = SettingsManager.GetSettings(game.FileName).Single(x => x.InstanceGuid == rawWheel.InstanceGuid);
						Assert.AreEqual(AutoMapHelper.GetAutoPreset(rawWheel).PadSettingChecksum, wheelRow.PadSettingChecksum,
							"A row whose twin is on another tab did not get the automatic preset.");
						Assert.AreEqual((int)MapTo.Controller3, otherTab.MapTo, "The wheel's twin on another tab moved.");
						Application.DoEvents();

						var grid = pad.MappedDevicesDataGridView;
						Assert.AreEqual(3, grid.Rows.Count, "The tab lists both rows of the pad and the wheel.");
						Assert.AreEqual(grid.Columns["InstanceIdColumn"].DisplayIndex + 1, grid.Columns["SourceColumn"].DisplayIndex,
							"The Source column is not beside the Instance ID column.");
						Assert.AreEqual("DirectInput", SourceShown(grid, twinRow));
						Assert.AreEqual("Raw Input", SourceShown(grid, rawRow));

						// The wheel's Raw Input row, just added, is the one selected.
						var effect = (Label)pad.Controls.Find("EffectDescriptionLabel", true).Single();
						Assert.AreEqual(PadControl.RawInputForceNote(rawWheel), effect.Text, "The Force Feedback page does not say a Raw Input row sends no force feedback.");
						grid.Rows[RowOf(grid, twinRow)].Selected = true;
						Application.DoEvents();
						Assert.AreSame(twinRow, pad.GetSelectedSetting(), "The pad's DirectInput row was not selected.");
						Assert.AreNotEqual(PadControl.RawInputForceNote(rawWheel), effect.Text, "The note stayed for a DirectInput row.");
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
					foreach (var item in pads)
						SettingsManager.PadSettings.Items.Add(item);
					foreach (var device in devices)
						SettingsManager.UserDevices.Items.Remove(device);
				}
			});
		}

		[TestMethod, TestCategory("devices")]
		[Description("The Force Feedback note is for Raw Input rows only")]
		public void The_force_note_is_for_raw_input_rows_only()
		{
			Assert.IsNotNull(PadControl.RawInputForceNote(RawInputRow("Pad", PadPath)));
			Assert.IsNull(PadControl.RawInputForceNote(DirectInputRow("Pad", PadPath)));
			Assert.IsNull(PadControl.RawInputForceNote(new UserDevice { InputSourceType = (int)InputSourceType.DirectInput }));
			Assert.IsNull(PadControl.RawInputForceNote(null));
		}

		#endregion
	}
}
