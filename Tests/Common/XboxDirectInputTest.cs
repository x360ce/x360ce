// @under-test: App.v4/Common/DInput/XInputPlaces.cs, App.v4/Controls/PadControl.cs, App.v4/Issues/XboxDirectInputIssue.cs, Engine/Data/UserDevice.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.Issues;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// Windows feeds an Xbox controller's DirectInput row only to the program in front. Mapped that way, the controller
	/// works on x360ce's pages and does nothing in the game, which a 4.26.38.0 report showed with an Xbox One pad
	/// combined with a wheel (issue #1652). Its Raw Input row keeps working, so adding the DirectInput row offers the Raw
	/// Input row instead, and one already mapped is named on the Issues tab with a fix that moves it.
	/// </summary>
	[TestClass]
	public class XboxDirectInputTest
	{
		const string XboxPath = @"\\?\HID#VID_045E&PID_02FF&IG_00#7&2a5e1b9f&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";

		UserSetting[] _settings;
		PadSetting[] _pads;
		UserDevice[] _devices;
		UserGame[] _games;
		Func<string, string, DialogResult> _ask;

		[TestInitialize]
		public void Before()
		{
			_settings = SettingsManager.UserSettings.ItemsToArraySynchronized();
			_pads = SettingsManager.PadSettings.ItemsToArraySynchronized();
			_devices = SettingsManager.UserDevices.ItemsToArraySynchronized();
			_games = SettingsManager.UserGames.ItemsToArraySynchronized();
			_ask = PadControl.AskRawInput;
			SettingsManager.UserSettings.Items.Clear();
			SettingsManager.UserDevices.Items.Clear();
			SettingsManager.UserGames.Items.Clear();
		}

		[TestCleanup]
		public void After()
		{
			PadControl.AskRawInput = _ask;
			Restore(SettingsManager.UserSettings.Items, _settings);
			Restore(SettingsManager.PadSettings.Items, _pads);
			Restore(SettingsManager.UserDevices.Items, _devices);
			Restore(SettingsManager.UserGames.Items, _games);
		}

		static void Restore<T>(System.Collections.Generic.IList<T> items, T[] saved)
		{
			items.Clear();
			foreach (var item in saved)
				items.Add(item);
		}

		static UserDevice Device(InputSourceType source, string path, string name)
		{
			var ud = new UserDevice
			{
				InstanceGuid = Guid.NewGuid(),
				ProductName = name,
				InstanceName = name,
				InputSourceType = (int)source,
				HidDevicePath = path,
			};
			SettingsManager.UserDevices.Items.Add(ud);
			return ud;
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Only an Xbox controller read through DirectInput is fed to the program in front alone, and its Raw Input row is found by its path in any letter case")]
		public void Only_an_Xbox_DirectInput_row_reads_only_in_front()
		{
			var direct = Device(InputSourceType.DirectInput, XboxPath, "Controller (Xbox One For Windows)");
			var raw = Device(InputSourceType.RawInput, XboxPath.ToLowerInvariant(), "Controller (Xbox One For Windows)");
			var wheel = Device(InputSourceType.DirectInput, @"\\?\HID#VID_046D&PID_C29B#7&1&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}", "G27");
			var devices = SettingsManager.UserDevices.ItemsToArraySynchronized();
			Assert.IsTrue(App.DInput.XInputPlaces.ReadsOnlyInFront(direct), "An Xbox controller's DirectInput row.");
			Assert.IsFalse(App.DInput.XInputPlaces.ReadsOnlyInFront(raw), "Its Raw Input row.");
			Assert.IsFalse(App.DInput.XInputPlaces.ReadsOnlyInFront(wheel), "A wheel.");
			Assert.AreSame(raw, direct.RawInputTwin(devices), "The Raw Input row was not found.");
			Assert.IsNull(wheel.RawInputTwin(devices), "A device with no Raw Input row has none.");
			Assert.IsNull(raw.RawInputTwin(devices), "A Raw Input row is its own.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Adding an Xbox controller's DirectInput row asks; Use Raw Input adds the Raw Input row, Add DirectInput the row picked, Cancel nothing; a wheel is added without a question")]
		public void Adding_an_Xbox_DirectInput_row_offers_Raw_Input()
		{
			var direct = Device(InputSourceType.DirectInput, XboxPath, "Controller (Xbox One For Windows)");
			var raw = Device(InputSourceType.RawInput, XboxPath.ToLowerInvariant(), "Controller (Xbox One For Windows)");
			var wheel = Device(InputSourceType.DirectInput, @"\\?\HID#VID_046D&PID_C29B#7&1&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}", "G27");
			string heading = null;
			var answer = DialogResult.Yes;
			PadControl.AskRawInput = (h, text) => { heading = h; return answer; };
			CollectionAssert.AreEqual(new[] { raw, wheel }, PadControl.PreferRawInput(new[] { direct, wheel }), "Use Raw Input did not swap the row.");
			StringAssert.Contains(heading, "Controller (Xbox One For Windows)", "The question does not name the controller.");
			answer = DialogResult.No;
			CollectionAssert.AreEqual(new[] { direct, wheel }, PadControl.PreferRawInput(new[] { direct, wheel }), "Add DirectInput changed the rows.");
			answer = DialogResult.Cancel;
			Assert.IsNull(PadControl.PreferRawInput(new[] { direct, wheel }), "Cancel still adds.");
			heading = null;
			CollectionAssert.AreEqual(new[] { wheel }, PadControl.PreferRawInput(new[] { wheel }));
			Assert.IsNull(heading, "A wheel was asked about.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A mapped Xbox DirectInput row is named on the Issues tab, and Use Raw Input moves the mapping to the Raw Input row with the same settings")]
		public void The_fix_moves_the_mapping_to_Raw_Input_with_its_settings()
		{
			var game = new UserGame { FileName = "galactic.exe", FileProductName = "Galactic Racer", EnableMask = (int)MapToMask.Controller1, EmulationType = (int)EmulationType.Virtual };
			SettingsManager.UserGames.Items.Add(game);
			var direct = Device(InputSourceType.DirectInput, XboxPath, "Controller (Xbox One For Windows)");
			var raw = Device(InputSourceType.RawInput, XboxPath.ToLowerInvariant(), "Controller (Xbox One For Windows)");
			var ps = new PadSetting { ForceEnable = "1", ForceOverall = "70" };
			ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
			SettingsManager.PadSettings.Items.Add(ps);
			var setting = AppHelper.GetNewSetting(direct, game, MapTo.Controller1);
			setting.PadSettingChecksum = ps.PadSettingChecksum;
			SettingsManager.UserSettings.Items.Add(setting);

			var moves = XboxDirectInputIssue.Moves();
			Assert.AreEqual(1, moves.Length, "The mapped DirectInput row is not named.");
			XboxDirectInputIssue.Apply(moves, false);

			var rows = SettingsManager.GetSettings(game.FileName, MapTo.Controller1);
			Assert.AreEqual(1, rows.Count, "Controller 1 should hold one row: the Raw Input one.");
			Assert.AreEqual(raw.InstanceGuid, rows[0].InstanceGuid, "The Raw Input row is not on Controller 1.");
			Assert.AreEqual(ps.PadSettingChecksum, rows[0].PadSettingChecksum, "The Raw Input row did not keep the settings.");
			Assert.AreEqual(0, XboxDirectInputIssue.Moves().Length, "The issue is still named after the fix.");
		}
	}
}
