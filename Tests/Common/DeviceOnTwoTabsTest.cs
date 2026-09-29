// @under-test: App.v4/Common/SettingsManager.cs, App.v4/Controls/PadControl.cs
// @area: mapping   @layer: unit
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
	/// <summary>A device added to a second controller tab of the same game: moved there, or kept on both.</summary>
	/// <remarks>
	/// Adding a device that is on another tab asks first. Moving it takes it off that tab, and a tab left
	/// with nothing on it is switched off, so the game is not offered a controller nothing drives. Keeping
	/// it on both is a second mapping row. Settings are stored by checksum, so the second row starts as a
	/// copy of the first, force feedback included, and the two change apart from then on.
	/// </remarks>
	[TestClass]
	public class DeviceOnTwoTabsTest
	{
		UserSetting[] _settings;
		PadSetting[] _pads;
		UserGame _game;

		[TestInitialize]
		public void Before()
		{
			_settings = SettingsManager.UserSettings.ItemsToArraySyncronized();
			_pads = SettingsManager.PadSettings.ItemsToArraySyncronized();
			_game = SettingsManager.CurrentGame;
			SettingsManager.UserSettings.Items.Clear();
		}

		[TestCleanup]
		public void After()
		{
			SettingsManager.CurrentGame = _game;
			SettingsManager.UserSettings.Items.Clear();
			foreach (var setting in _settings)
				SettingsManager.UserSettings.Items.Add(setting);
			SettingsManager.PadSettings.Items.Clear();
			foreach (var ps in _pads)
				SettingsManager.PadSettings.Items.Add(ps);
		}

		/// <summary>A game with Controller 1 switched on, as it is once a device has been added there.</summary>
		static UserGame NewGame()
		{
			return new UserGame
			{
				FileName = "two-tabs.exe",
				FileProductName = "Two tabs",
				EnableMask = (int)MapToMask.Controller1,
				EmulationType = (int)EmulationType.Virtual,
			};
		}

		static UserDevice NewDevice(string name)
		{
			return new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = name, InstanceName = name };
		}

		/// <summary>Puts the device on a tab with stored settings that play force feedback.</summary>
		static UserSetting Map(UserGame game, UserDevice device, MapTo controller)
		{
			var ps = new PadSetting { ForceEnable = "1", ForceOverall = "80" };
			ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
			SettingsManager.PadSettings.Items.Add(ps);
			var setting = AppHelper.GetNewSetting(device, game, controller);
			setting.PadSettingChecksum = ps.PadSettingChecksum;
			SettingsManager.UserSettings.Items.Add(setting);
			return setting;
		}

		static UserSetting[] RowsOf(UserGame game, UserDevice device)
		{
			return SettingsManager.GetSettings(game.FileName)
				.Where(x => x.InstanceGuid == device.InstanceGuid)
				.OrderBy(x => x.MapTo)
				.ToArray();
		}

		static bool IsOn(UserGame game, MapTo controller)
		{
			return (game.EnableMask & (int)AppHelper.GetMapFlag(controller)) != 0;
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Keep there too adds a second row with the same settings, force feedback included, and leaves the first")]
		public void Keep_on_both_adds_a_row_with_the_same_settings()
		{
			var game = NewGame();
			var wheel = NewDevice("Wheel");
			var first = Map(game, wheel, MapTo.Controller1);

			SettingsManager.MapGamePadDevices(game, MapTo.Controller2, new[] { wheel }, false, true);

			var rows = RowsOf(game, wheel);
			CollectionAssert.AreEqual(new[] { 1, 2 }, rows.Select(x => x.MapTo).ToArray(), "The device is not on both tabs.");
			Assert.AreSame(first, rows[0], "The first tab's row was replaced.");
			Assert.AreEqual(first.PadSettingChecksum, rows[1].PadSettingChecksum,
				"The second tab does not start with the first tab's settings, force feedback included.");
			Assert.IsTrue(rows[1].IsEnabled, "The row kept on the second tab is switched off.");
			Assert.IsTrue(IsOn(game, MapTo.Controller1), "Keeping the device switched the first tab off.");
			Assert.IsTrue(IsOn(game, MapTo.Controller2), "The tab that got its first device is not switched on.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Move takes the device off the other tab and switches that tab off when nothing is left on it")]
		public void Move_switches_off_the_tab_it_leaves_empty()
		{
			var game = NewGame();
			var wheel = NewDevice("Wheel");
			var first = Map(game, wheel, MapTo.Controller1);

			SettingsManager.MapGamePadDevices(game, MapTo.Controller2, new[] { wheel }, false, false);

			var rows = RowsOf(game, wheel);
			Assert.AreEqual(1, rows.Length, "Move left a second row behind.");
			Assert.AreSame(first, rows[0], "The row did not move with its settings.");
			Assert.AreEqual((int)MapTo.Controller2, first.MapTo, "The device did not move.");
			Assert.IsFalse(IsOn(game, MapTo.Controller1),
				"The tab the device left is still switched on with nothing on it, so the game sees a controller nothing drives.");
			Assert.IsTrue(IsOn(game, MapTo.Controller2), "The tab the device moved to is not switched on.");
			Assert.AreEqual((int)EmulationType.Virtual, game.EmulationType, "A tab is still on, so the game stays on virtual emulation.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A tab that keeps another device stays switched on when one moves away")]
		public void Move_leaves_a_tab_on_that_still_has_a_device()
		{
			var game = NewGame();
			var wheel = NewDevice("Wheel");
			var pedals = NewDevice("Pedals");
			Map(game, wheel, MapTo.Controller1);
			Map(game, pedals, MapTo.Controller1);

			SettingsManager.MapGamePadDevices(game, MapTo.Controller2, new[] { wheel }, false, false);

			Assert.IsTrue(IsOn(game, MapTo.Controller1), "The pedals' tab was switched off while they are still on it.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A device on two tabs moved to a third leaves both, and both empty tabs are switched off")]
		public void Move_from_two_tabs_leaves_both()
		{
			var game = NewGame();
			game.EnableMask = (int)(MapToMask.Controller1 | MapToMask.Controller2);
			var wheel = NewDevice("Wheel");
			Map(game, wheel, MapTo.Controller1);
			Map(game, wheel, MapTo.Controller2);

			SettingsManager.MapGamePadDevices(game, MapTo.Controller3, new[] { wheel }, false, false);

			CollectionAssert.AreEqual(new[] { MapTo.Controller3 }, SettingsManager.GetDeviceTabs(game.FileName, wheel.InstanceGuid),
				"The device stayed on a tab it was moved away from.");
			Assert.IsFalse(IsOn(game, MapTo.Controller1), "An emptied tab is still switched on.");
			Assert.IsFalse(IsOn(game, MapTo.Controller2), "An emptied tab is still switched on.");
			Assert.IsTrue(IsOn(game, MapTo.Controller3), "The tab the device moved to is not switched on.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Adding a device to a tab it is already on asks nothing and leaves the other tab alone")]
		public void Adding_a_device_again_leaves_the_other_tab_alone()
		{
			var game = NewGame();
			game.EnableMask = (int)(MapToMask.Controller1 | MapToMask.Controller2);
			var wheel = NewDevice("Wheel");
			Map(game, wheel, MapTo.Controller1);
			Map(game, wheel, MapTo.Controller2);

			Assert.IsNull(PadControl.SharedDeviceQuestion(game, MapTo.Controller2, new[] { wheel }),
				"Adding a device to a tab it is already on asks a question.");
			SettingsManager.MapGamePadDevices(game, MapTo.Controller2, new[] { wheel }, false, false);

			CollectionAssert.AreEqual(new[] { MapTo.Controller1, MapTo.Controller2 },
				SettingsManager.GetDeviceTabs(game.FileName, wheel.InstanceGuid),
				"Adding the device again took it off the other tab.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The question names each device already on another tab and that tab, and no other device")]
		public void The_question_names_the_device_and_its_tab()
		{
			var game = NewGame();
			var wheel = NewDevice("Wheel");
			var pad = NewDevice("Pad");
			Map(game, wheel, MapTo.Controller1);

			var text = PadControl.SharedDeviceQuestion(game, MapTo.Controller2, new[] { wheel, pad });

			StringAssert.Contains(text, "Wheel is already on Controller 1.");
			Assert.IsFalse(text.Contains("Pad is"), "A device on no other tab is named in the question.");
			Assert.IsNull(PadControl.SharedDeviceQuestion(game, MapTo.Controller2, new[] { pad }),
				"A device on no other tab is asked about.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A device on a tab of another game, the default x360ce.exe entry included, is neither asked about nor moved")]
		public void A_device_on_another_game_is_not_asked_about_or_moved()
		{
			var game = NewGame();
			var other = NewGame();
			other.FileName = "other.exe";
			var own = NewGame();
			own.FileName = "x360ce.exe";
			var wheel = NewDevice("Wheel");
			var pedals = NewDevice("Pedals");
			var shifter = NewDevice("Shifter");
			var otherRow = Map(other, wheel, MapTo.Controller1);
			var ownRow = Map(own, wheel, MapTo.Controller1);
			// On this game too, so the move takes a row that exists.
			Map(game, wheel, MapTo.Controller1);
			Map(other, pedals, MapTo.Controller1);
			Map(own, pedals, MapTo.Controller1);
			// A row saved with the file name in capitals.
			Map(game, shifter, MapTo.Controller1).FileName = "TWO-TABS.EXE";

			SettingsManager.MapGamePadDevices(game, MapTo.Controller2, new[] { wheel }, false, false);

			Assert.AreEqual((int)MapTo.Controller1, otherRow.MapTo, "Moving the device in one game moved it in another.");
			Assert.AreEqual((int)MapTo.Controller1, ownRow.MapTo, "Moving the device in a game moved it in the default x360ce.exe entry.");
			Assert.AreEqual((int)MapToMask.Controller1, other.EnableMask, "Moving the device in one game switched a tab of another.");
			Assert.AreEqual((int)MapToMask.Controller1, own.EnableMask, "Moving the device in a game switched a tab of the default x360ce.exe entry.");
			Assert.IsNull(PadControl.SharedDeviceQuestion(game, MapTo.Controller2, new[] { pedals }),
				"A device mapped only in other games is asked about.");
			StringAssert.Contains(PadControl.SharedDeviceQuestion(game, MapTo.Controller2, new[] { shifter }),
				"Shifter is already on Controller 1.", "A row whose file name differs only in case is not the same game.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The first tab switched on puts the game on virtual emulation, and the last one switched off takes it off")]
		public void The_last_tab_switched_off_takes_the_game_off_emulation()
		{
			var game = NewGame();
			game.EnableMask = 0;
			game.EmulationType = (int)EmulationType.None;

			SettingsManager.SetTabEnabled(game, MapTo.Controller1, true);
			Assert.AreEqual((int)EmulationType.Virtual, game.EmulationType, "The first tab switched on left the game off emulation.");
			SettingsManager.SetTabEnabled(game, MapTo.Controller2, true);
			SettingsManager.SetTabEnabled(game, MapTo.Controller1, false);
			Assert.AreEqual((int)EmulationType.Virtual, game.EmulationType, "A tab is still on, so the game stays on virtual emulation.");
			SettingsManager.SetTabEnabled(game, MapTo.Controller2, false);
			Assert.AreEqual(0, game.EnableMask);
			Assert.AreEqual((int)EmulationType.None, game.EmulationType, "The last tab switched off left the game on virtual emulation.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Remove switches off a tab left with no device, and the last one takes the game off emulation")]
		public void Remove_switches_off_the_tab_it_leaves_empty()
		{
			var game = NewGame();
			game.EnableMask = (int)(MapToMask.Controller1 | MapToMask.Controller2);
			var wheel = Map(game, NewDevice("Wheel"), MapTo.Controller1);
			var pedals = Map(game, NewDevice("Pedals"), MapTo.Controller2);
			var shifter = Map(game, NewDevice("Shifter"), MapTo.Controller2);

			SettingsManager.UnMapGamePadDevices(game, wheel, false);
			Assert.IsFalse(IsOn(game, MapTo.Controller1), "The tab Remove left empty is still switched on.");
			Assert.AreEqual((int)EmulationType.Virtual, game.EmulationType, "A tab is still on, so the game stays on virtual emulation.");

			SettingsManager.UnMapGamePadDevices(game, pedals, false);
			Assert.IsTrue(IsOn(game, MapTo.Controller2), "The shifter's tab was switched off while it is still on it.");

			SettingsManager.UnMapGamePadDevices(game, shifter, false);
			Assert.IsFalse(IsOn(game, MapTo.Controller2), "The tab Remove left empty is still switched on.");
			Assert.AreEqual((int)EmulationType.None, game.EmulationType, "The last tab switched off left the game on virtual emulation.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Mapping no devices switches no tab on")]
		public void Mapping_no_devices_switches_nothing_on()
		{
			var game = NewGame();

			SettingsManager.MapGamePadDevices(game, MapTo.Controller2, new UserDevice[0], false, false);

			Assert.IsFalse(IsOn(game, MapTo.Controller2), "A tab with no device on it was switched on.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Add asks about a device on another tab: Cancel changes nothing, Keep there too puts it here as well")]
		public void Add_asks_before_it_takes_a_device_off_another_tab()
		{
			Ui.OnUiThread(() =>
			{
				var oldAsk = PadControl.AskMoveOrKeep;
				var oldStatus = SettingsManager.Current.NotifySettingsStatus;
				SettingsManager.Current.NotifySettingsStatus = count => { };
				var game = NewGame();
				var wheel = NewDevice("Wheel");
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
						var first = Map(game, wheel, MapTo.Controller1);
						SettingsManager.CurrentGame = game;
						pad.UpdateFromCurrentGame();
						Application.DoEvents();

						string asked = null;
						PadControl.AskMoveOrKeep = text => { asked = text; return DialogResult.Cancel; };
						pad.MapDevices(game, new[] { wheel });
						Application.DoEvents();
						Assert.IsNotNull(asked, "A device on another tab was added without asking.");
						StringAssert.Contains(asked, "Wheel is already on Controller 1.");
						Assert.AreEqual(1, RowsOf(game, wheel).Length, "Cancel changed the mapping.");
						Assert.AreEqual((int)MapTo.Controller1, first.MapTo, "Cancel moved the device.");

						PadControl.AskMoveOrKeep = text => DialogResult.No;
						pad.MapDevices(game, new[] { wheel });
						Application.DoEvents();
						CollectionAssert.AreEqual(new[] { 1, 2 }, RowsOf(game, wheel).Select(x => x.MapTo).ToArray(),
							"Keep there too did not leave the device on both tabs.");
						Assert.AreEqual(1, pad.MappedDevicesDataGridView.Rows.Count, "The tab does not list the device kept on it.");
					}
				}
				finally
				{
					PadControl.AskMoveOrKeep = oldAsk;
					SettingsManager.Current.NotifySettingsStatus = oldStatus;
				}
			});
		}
	}
}
