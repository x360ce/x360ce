// @under-test: App.v4/Controls/PadControl.cs
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
	/// <summary>
	/// Changing the current game rebuilds the mapped-devices list on every controller tab: the
	/// old game's rows go, the new game's rows come. Four users of 4.21.30.0 reported the program
	/// closing at that moment, from inside the list's own measurement of a cell, and one more
	/// reported the same measurement failing under the mouse. These pin the game change in the
	/// states a real window has: a tab never shown, a tab in view, and a row selected.
	/// </summary>
	[TestClass]
	public class PadGridRefreshTest
	{
		static UserGame NewGame(string name)
		{
			return new UserGame
			{
				FileName = name + ".exe",
				FileProductName = name,
				EnableMask = (int)MapToMask.Controller1,
				AutoMapMask = 0,
				EmulationType = (int)EmulationType.Virtual,
			};
		}

		static UserSetting NewSetting(UserGame game, MapTo controller)
		{
			return new UserSetting
			{
				MapTo = (int)controller,
				FileName = game.FileName,
				FileProductName = game.FileProductName,
				InstanceGuid = Guid.NewGuid(),
				IsEnabled = true,
			};
		}

		/// <summary>Builds a window with the pad on a tab, shown or not, and runs the game changes.</summary>
		static void SwitchGames(bool padTabShown, bool selectRowFirst)
		{
			Ui.OnUiThread(() =>
			{
				// A failure inside the message pump must reach the test, not a dialog that waits
				// for a person: the grid's own error dialog and the framework's unhandled-exception
				// dialog both stop the interface thread for good on a machine nobody is watching.
				Exception pumpError = null;
				System.Threading.ThreadExceptionEventHandler onPumpError = (s, e) =>
				{
					if (pumpError == null)
						pumpError = e.Exception;
				};
				Application.ThreadException += onPumpError;
				var existing = SettingsManager.UserSettings.ItemsToArraySyncronized();
				var oldGame = SettingsManager.CurrentGame;
				var a = NewGame("GameA");
				var b = NewGame("GameB");
				SettingsManager.UserSettings.Items.Clear();
				SettingsManager.CurrentGame = null;
				// The main window shows the suspend count in its status bar; here nobody looks.
				var oldStatus = SettingsManager.Current.NotifySettingsStatus;
				SettingsManager.Current.NotifySettingsStatus = count => { };
				Exception dataError = null;
				try
				{
					using (var form = new Form { Width = 900, Height = 700 })
					using (var tabs = new TabControl { Dock = DockStyle.Fill })
					using (var first = new TabPage("First"))
					using (var second = new TabPage("Second"))
					using (var pad = new PadControl(MapTo.Controller2) { Dock = DockStyle.Fill })
					{
						tabs.TabPages.Add(first);
						tabs.TabPages.Add(second);
						(padTabShown ? first : second).Controls.Add(pad);
						form.Controls.Add(tabs);
						// The same three steps the main window takes for each pad.
						pad.InitPadControl();
						pad.UpdateSettingsMap();
						pad.InitPadData();
						var grid = pad.MappedDevicesDataGridView;
						grid.DataError += (s, e) =>
						{
							if (dataError == null)
								dataError = e.Exception;
							e.ThrowException = false;
						};
						form.Show();
						Application.DoEvents();
						Assert.AreEqual(padTabShown, grid.IsHandleCreated,
							"The list's window state does not match the state under test.");

						// Game A has two mapped devices, game B has one of its own.
						SettingsManager.UserSettings.Items.Add(NewSetting(a, MapTo.Controller2));
						SettingsManager.UserSettings.Items.Add(NewSetting(a, MapTo.Controller2));
						SettingsManager.UserSettings.Items.Add(NewSetting(b, MapTo.Controller2));
						SettingsManager.CurrentGame = a;
						pad.UpdateFromCurrentGame();
						Application.DoEvents();
						// Three here means one device was inserted twice: loading the first row's
						// settings changed the setting, the list reported it, and the rebuild ran again
						// inside the insert it had not finished.
						Assert.AreEqual(2, grid.Rows.Count, "Game A's two devices should be listed once each.");
						if (selectRowFirst && grid.Rows.Count > 0)
						{
							var visibleColumn = grid.Columns.Cast<DataGridViewColumn>().First(c => c.Visible).Index;
							grid.CurrentCell = grid.Rows[1].Cells[visibleColumn];
							grid.Rows[1].Selected = true;
						}

						// The switch the reports describe: rows removed and rows added in one pass.
						SettingsManager.CurrentGame = b;
						pad.UpdateFromCurrentGame();
						Application.DoEvents();
						Assert.AreEqual(1, grid.Rows.Count, "Game B's device did not replace game A's.");

						// And back, so the list grows again after it shrank.
						SettingsManager.CurrentGame = a;
						pad.UpdateFromCurrentGame();
						Application.DoEvents();
						Assert.AreEqual(2, grid.Rows.Count, "Game A's devices did not come back.");
						if (dataError != null)
							throw new AssertFailedException("The list raised a data error: " + dataError, dataError);
						if (pumpError != null)
							throw new AssertFailedException("The interface thread threw while drawing: " + pumpError, pumpError);
					}
				}
				finally
				{
					Application.ThreadException -= onPumpError;
					SettingsManager.Current.NotifySettingsStatus = oldStatus;
					SettingsManager.CurrentGame = oldGame;
					SettingsManager.UserSettings.Items.Clear();
					foreach (var setting in existing)
						SettingsManager.UserSettings.Items.Add(setting);
				}
			});
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Changing the game while the controller tab has never been shown does not throw")]
		public void Game_change_on_a_tab_never_shown_does_not_throw()
		{
			SwitchGames(padTabShown: false, selectRowFirst: false);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Changing the game while the controller tab is in view does not throw")]
		public void Game_change_on_the_tab_in_view_does_not_throw()
		{
			SwitchGames(padTabShown: true, selectRowFirst: false);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Changing the game while a mapped device row is selected does not throw")]
		public void Game_change_with_a_row_selected_does_not_throw()
		{
			SwitchGames(padTabShown: true, selectRowFirst: true);
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A device added to a controller that already has one is selected, not left behind the old one")]
		public void A_device_just_added_is_selected()
		{
			Ui.OnUiThread(() =>
			{
				var existing = SettingsManager.UserSettings.ItemsToArraySyncronized();
				var existingPads = SettingsManager.PadSettings.ItemsToArraySyncronized();
				var oldGame = SettingsManager.CurrentGame;
				var oldStatus = SettingsManager.Current.NotifySettingsStatus;
				SettingsManager.Current.NotifySettingsStatus = count => { };
				var game = NewGame("GameA");
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
						// A controller that already has a device, selected.
						var first = NewSetting(game, MapTo.Controller2);
						SettingsManager.UserSettings.Items.Add(first);
						SettingsManager.CurrentGame = game;
						pad.UpdateFromCurrentGame();
						Application.DoEvents();
						var grid = pad.MappedDevicesDataGridView;
						Assert.AreEqual(1, grid.Rows.Count);
						Assert.AreEqual(first.InstanceGuid, pad.GetSelectedSetting()?.InstanceGuid, "The one device is selected to begin with.");

						// A second device, from the list of devices, as Add does after its dialog.
						var added = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Second pad", InstanceName = "Second pad" };
						pad.MapDevices(game, new[] { added });
						Application.DoEvents();
						Assert.AreEqual(2, grid.Rows.Count, "The device was not added.");
						Assert.AreEqual(added.InstanceGuid, pad.GetSelectedSetting()?.InstanceGuid,
							"The device just added is not selected; the page still shows the one that was.");
						Assert.AreEqual(1, grid.SelectedRows.Count, "Only the device just added is selected.");
					}
				}
				finally
				{
					SettingsManager.Current.NotifySettingsStatus = oldStatus;
					SettingsManager.CurrentGame = oldGame;
					SettingsManager.UserSettings.Items.Clear();
					foreach (var setting in existing)
						SettingsManager.UserSettings.Items.Add(setting);
					foreach (var ps in SettingsManager.PadSettings.ItemsToArraySyncronized().Except(existingPads).ToArray())
						SettingsManager.PadSettings.Items.Remove(ps);
				}
			});
		}
	}
}
