// @under-test: App.v4/Common/SettingsManager.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using x360ce.App;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>A controller's saved settings come back when it is put on its tab again: the databases never overwrite them.</summary>
	/// <remarks>
	/// Removing a controller from a tab, or deleting it from the Devices page, keeps its row for the game, and the row keeps
	/// its settings by checksum. Putting the controller back reuses that row, so the automatic preset from x360ce's or SDL's
	/// controller database is made only for a controller the game has no row for.
	/// </remarks>
	[TestClass]
	public class SavedSettingsComeBackTest
	{
		UserSetting[] _settings;
		PadSetting[] _pads;

		[TestInitialize]
		public void Before()
		{
			_settings = SettingsManager.UserSettings.ItemsToArraySynchronized();
			_pads = SettingsManager.PadSettings.ItemsToArraySynchronized();
			SettingsManager.UserSettings.Items.Clear();
		}

		[TestCleanup]
		public void After()
		{
			SettingsManager.UserSettings.Items.Clear();
			foreach (var setting in _settings)
				SettingsManager.UserSettings.Items.Add(setting);
			SettingsManager.PadSettings.Items.Clear();
			foreach (var ps in _pads)
				SettingsManager.PadSettings.Items.Add(ps);
		}

		static UserSetting[] RowsOf(UserGame game, UserDevice device)
		{
			return SettingsManager.GetSettings(game.FileName).Where(x => x.InstanceGuid == device.InstanceGuid).ToArray();
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A controller removed from its tab and added again gets its saved settings back, not the database's")]
		public void Removed_and_added_again_keeps_its_settings()
		{
			var game = DeviceOnTwoTabsTest.NewGame();
			var wheel = DeviceOnTwoTabsTest.NewDevice("Wheel");
			var saved = DeviceOnTwoTabsTest.Map(game, wheel, MapTo.Controller1);
			var checksum = saved.PadSettingChecksum;

			SettingsManager.UnMapGamePadDevices(game, saved, false);
			SettingsManager.MapGamePadDevices(game, MapTo.Controller1, new[] { wheel }, false, false);

			var rows = RowsOf(game, wheel);
			Assert.AreEqual(1, rows.Length, "Adding the controller again made a second row.");
			Assert.AreSame(saved, rows[0], "The saved row was not reused.");
			Assert.AreEqual(checksum, rows[0].PadSettingChecksum, "The saved settings were replaced.");
			Assert.AreEqual((int)MapTo.Controller1, rows[0].MapTo, "The controller is not on its tab again.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A controller deleted from the Devices page and found again gets its saved settings back, not the database's")]
		public void Deleted_and_found_again_keeps_its_settings()
		{
			var game = DeviceOnTwoTabsTest.NewGame();
			var wheel = DeviceOnTwoTabsTest.NewDevice("Wheel");
			var saved = DeviceOnTwoTabsTest.Map(game, wheel, MapTo.Controller1);
			var checksum = saved.PadSettingChecksum;
			SettingsManager.UnMapGamePadDevices(game, saved, false);

			// Plugged in again after Delete on the Devices page: the same instance, read as a new device.
			var found = new UserDevice { InstanceGuid = wheel.InstanceGuid, ProductName = wheel.ProductName, InstanceName = wheel.InstanceName };
			SettingsManager.MapGamePadDevices(game, MapTo.Controller1, new[] { found }, false, false);

			var rows = RowsOf(game, found);
			Assert.AreEqual(1, rows.Length, "Finding the controller again made a second row.");
			Assert.AreEqual(checksum, rows[0].PadSettingChecksum, "The saved settings were replaced.");
			Assert.AreEqual((int)MapTo.Controller1, rows[0].MapTo, "The controller is not on its tab again.");
			Assert.IsTrue(rows[0].IsEnabled, "The row came back switched off.");
		}
	}
}
