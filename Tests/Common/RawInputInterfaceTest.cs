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
using JocysCom.ClassLibrary.Mcp;

namespace x360ce.Tests
{
	/// <summary>
	/// A controller read through DirectInput and through Raw Input is two rows, usually with the same name. The lists say
	/// which source each row is read through and list and tint the rows by source, the Direct Input tab shows a Raw
	/// Input row's values, the Force Feedback page says when no force feedback reaches it, and a row added to a tab where
	/// its twin is mapped starts with the twin's settings.
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

		/// <summary>The colour the row is painted in, read where no text is drawn.</summary>
		static System.Drawing.Color RowColour(DataGridView grid, object item)
		{
			var cell = grid.GetCellDisplayRectangle(grid.Columns["MySidColumn"].Index, RowOf(grid, item), false);
			using (var picture = new System.Drawing.Bitmap(grid.Width, grid.Height))
			{
				grid.DrawToBitmap(picture, new System.Drawing.Rectangle(0, 0, grid.Width, grid.Height));
				return picture.GetPixel(cell.Left + cell.Width / 2, cell.Top + 1);
			}
		}

		/// <summary>Shows the device list, on the Devices page or as the list Add opens, with the sources mixed in the program's list.</summary>
		static void ShowsBySource(bool mapMode)
		{
			var diPad = DirectInputRow("Pad", PadPath);
			var rawPad = RawInputRow("Pad", PadPath);
			var other = DirectInputRow("Other", null);
			var diWheel = DirectInputRow("Wheel", WheelPath);
			var rawWheel = RawInputRow("Wheel", WheelPath);
			var plugin = new UserDevice { InstanceGuid = Guid.NewGuid(), InstanceName = "Plugin", ProductName = "Plugin",
				InputSourceType = (int)InputSourceType.Plugin, IsOnline = true };
			var devices = new[] { plugin, diPad, rawPad, other, diWheel, rawWheel };
			Ui.OnUiThread(() =>
			{
				SettingsManager.UserDevices.Items.Add(plugin);
				SettingsManager.UserDevices.Items.Add(diPad);
				SettingsManager.UserDevices.Items.Add(rawPad);
				SettingsManager.UserDevices.Items.Add(other);
				SettingsManager.UserDevices.Items.Add(diWheel);
				try
				{
					using (var form = new Form { Width = 1200, Height = 600 })
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
						Assert.AreEqual("Plugin", SourceShown(grid, plugin));
						Assert.AreEqual(RowOf(grid, diPad) + 1, RowOf(grid, other), "The DirectInput rows are not together, in the order they were found.");
						Assert.AreEqual(RowOf(grid, other) + 1, RowOf(grid, diWheel), "The DirectInput rows are not together, in the order they were found.");
						Assert.AreEqual(RowOf(grid, diWheel) + 1, RowOf(grid, rawPad), "The Raw Input rows do not follow the DirectInput ones.");
						Assert.IsTrue(RowOf(grid, plugin) > RowOf(grid, rawPad), "The plugin device is not after the Raw Input rows.");
						// A Raw Input row found later goes with the other Raw Input rows, not to the end.
						SettingsManager.UserDevices.Items.Add(rawWheel);
						Application.DoEvents();
						Assert.AreEqual(RowOf(grid, rawPad) + 1, RowOf(grid, rawWheel), "A Raw Input row found later is not with the other Raw Input rows.");
						Assert.IsTrue(RowOf(grid, plugin) > RowOf(grid, rawWheel), "A Raw Input row found later went after the plugin device.");
						// Each source has its own tint.
						grid.ClearSelection();
						var sources = new[] { diPad, rawPad, plugin };
						foreach (var row in sources)
							Assert.AreEqual(AppHelper.GetInputSourceTint(row).ToArgb(), RowColour(grid, row).ToArgb(),
								"The " + row.InputSource + " row is not painted in its source's tint.");
						Assert.AreEqual(3, sources.Select(x => AppHelper.GetInputSourceTint(x).ToArgb()).Distinct().Count(), "Two sources share a tint.");
						Assert.AreEqual(RowColour(grid, diPad), RowColour(grid, diWheel), "Two DirectInput rows have different tints.");
						// The program's own list keeps the order the devices were found in, which is the order they are saved in.
						var kept = SettingsManager.UserDevices.Items.Where(x => devices.Contains(x)).ToArray();
						CollectionAssert.AreEqual(new[] { plugin, diPad, rawPad, other, diWheel, rawWheel }, kept, "The program's list was reordered.");
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
		[Description("The Devices page names each row's source, lists DirectInput rows, then Raw Input, then plugin devices, and tints each source")]
		public void Devices_page_lists_and_tints_the_rows_by_source()
		{
			ShowsBySource(false);
		}

		[TestMethod, TestCategory("devices"), TestCategory("ui")]
		[Description("The list Add opens names each row's source, lists DirectInput rows, then Raw Input, then plugin devices, and tints each source")]
		public void Add_list_lists_and_tints_the_rows_by_source()
		{
			ShowsBySource(true);
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
					Assert.AreEqual("03", buttons.Rows[0][0], "Buttons.");
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
			Assert.IsTrue(AutoMapHelper.CanGetAutoPreset(TestControllerDevice.NewUserDevice()), "The demo device was refused.");
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
						// Its DirectInput twin takes no force feedback, so the page says none reaches it.
						var effect = (Label)pad.Controls.Find("EffectDescriptionLabel", true).Single();
						var note = PadControl.RawInputForceNote(rawWheel, SettingsManager.UserDevices.ItemsToArraySynchronized());
						Assert.IsNotNull(note);
						Assert.AreEqual(note, effect.Text, "The Force Feedback page does not say no force feedback reaches the Raw Input row.");
						// The wheel's DirectInput twin is on Controller 3: the same wheel, so the page says it is shared.
						Assert.AreEqual("Force Feedback - Shared with Controller 3", pad.Controls.Find("ForceFeedbackGroupBox", true).Single().Text,
							"The page does not count the wheel's twin on another tab.");
						grid.Rows[RowOf(grid, twinRow)].Selected = true;
						Application.DoEvents();
						Assert.AreSame(twinRow, pad.GetSelectedSetting(), "The pad's DirectInput row was not selected.");
						Assert.AreNotEqual(note, effect.Text, "The note stayed for a DirectInput row.");
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
		[Description("The Force Feedback note is for Raw Input rows that no force feedback reaches: with no DirectInput twin that takes it")]
		public void The_force_note_is_for_raw_input_rows_no_force_reaches()
		{
			var raw = RawInputRow("Pad", PadPath);
			var twin = DirectInputRow("Pad", PadPath);
			Assert.IsNotNull(PadControl.RawInputForceNote(raw, new[] { raw, twin }), "No note for a Raw Input row whose twin takes no force feedback.");
			twin.CapFlags = (int)DeviceFlags.ForceFeedback;
			Assert.IsNull(PadControl.RawInputForceNote(raw, new[] { raw, twin }), "The note says no force reaches a Raw Input row whose twin carries it.");
			Assert.IsNotNull(PadControl.RawInputForceNote(raw, new[] { raw }), "No note for a Raw Input row with no twin.");
			Assert.IsNull(PadControl.RawInputForceNote(twin, new[] { raw, twin }), "A note for a DirectInput row.");
			Assert.IsNull(PadControl.RawInputForceNote(new UserDevice { InputSourceType = (int)InputSourceType.DirectInput }, new UserDevice[0]));
			Assert.IsNull(PadControl.RawInputForceNote(null, new UserDevice[0]));
		}

		#endregion
	}
}
