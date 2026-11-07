using System.Collections.Generic;
using x360ce.App.Controls;
using static JocysCom.ClassLibrary.Controls.UiTree.UiText;

namespace x360ce.App.UiTree
{
	public static partial class UiCatalog
	{
		/// <summary>The pages that are mostly a list: games, and the settings database.</summary>
		static void AddLists(Dictionary<string, Text> d)
		{
			// Game Settings.
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.GamesTabControl)] = new Text("Game lists",
				"Your games, the program's defaults for well-known games, and this computer's identity.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.GamesDataGridView)] = new Text("My games",
				"Games the emulator is set up for. Select one to see how it is hooked.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.GamesToolStrip)] = new Text("Game list actions",
				"Scans for games, adds, starts, saves and deletes them, and chooses which ones the list shows.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.ProgramsToolStrip)] = new Text("Default game settings actions",
				"Downloads, imports, exports and deletes the default game settings.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.ScanGamesButton)] = new Text("Scan",
				"Searches the scan locations on the Options page for games and adds the ones found.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.AddGameButton)] = new Text("Add...",
				"Adds a game by choosing its program file.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.StartGameButton)] = new Text("Start",
				"Starts the selected game.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.OpenGameButton)] = new Text("Open...",
				"Opens the selected game's folder.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.SaveGamesButton)] = new Text("Save",
				"Writes the game list to disk now.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.DeleteGamesButton)] = new Text("Delete",
				"Removes the selected games from the list.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.ShowGamesDropDownButton)] = new Text("Show",
				"Which games the list shows: all, enabled only, or disabled only.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.ProgramsDataGridView)] = new Text("Default game settings",
				"How the emulator is set up for well-known games when they are added.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.RefreshProgramsButton)] = new Text("Refresh",
				"Downloads the default game settings from the online database.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.ImportProgramsButton)] = new Text("Import...",
				"Reads default game settings from a file.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.ExportProgramsButton)] = new Text("Export...",
				"Writes the default game settings to a file.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.DeleteProgramsButton)] = new Text("Delete",
				"Removes the selected default game settings.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.IncludeEnabledCheckBox)] = new Text("Include Enabled",
				"Which default game settings Refresh downloads: enabled ones, disabled ones, or both.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.MinimumInstanceCountNumericUpDown)] = new Text("Minimum users",
				"Refresh downloads only games set up by at least this many people.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.DiskIdTextBox)] = new Text("Disk ID",
				"Serial number of this computer's system disk, which identifies it to the online database.");
			d[nameof(GameSettingsUserControl) + "." + nameof(GameSettingsUserControl.HashedDiskIdTextBox)] = new Text("Hashed disk ID",
				"The disk ID as the online database receives it, hashed so the serial number itself is not sent.");

			// How one game is hooked, shown for the selected game and for its defaults.
			d[nameof(GameSettingDetailsUserControl)] = new Text("Game details",
				"Which libraries the emulator puts in the game's folder, and how it hooks the game.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.XInputMaskTextBox)] = new Text("XInput files",
				"The XInput libraries ticked below, as one number.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.HookMaskTextBox)] = new Text("Hook modes",
				"The hook modes ticked below, as one number.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.DInputMaskTextBox)] = new Text("DirectInput files",
				"The DirectInput libraries ticked below, as one number.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.DInputFileTextBox)] = new Text("DirectInput file name",
				"File name stored with this game for its DirectInput library.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.ProcessorArchitectureComboBox)] = new Text("Architecture",
				"Whether the game is a 32-bit or a 64-bit program, which decides the libraries it needs.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.TimeoutNumericUpDown)] = new Text("Timeout",
				"Timeout stored with this game in the game database.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.HookModeFakeVidNumericUpDown)] = new Text("Fake vendor ID",
				"Vendor number shown to the game in place of the device's own, when PIDVID is ticked.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.HookModeFakePidNumericUpDown)] = new Text("Fake product ID",
				"Product number shown to the game in place of the device's own, when PIDVID is ticked.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.HookModeFakeVidTextBox)] = Live("Fake vendor ID in hex", "The fake vendor ID as hexadecimal.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.HookModeFakePidTextBox)] = Live("Fake product ID in hex", "The fake product ID as hexadecimal.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.SynchronizeSettingsButton)] = new Text("Apply/Synchronize Settings",
				"Copies the ticked libraries and the settings into the game's folder, after listing what differs.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.ResetToDefaultButton)] = new Text("Reset to Default",
				"Sets this game back to the default settings for it, after asking.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.HelpButton)] = new Text("Get Help...",
				"Searches the web for help with this game.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.toolStrip1)] = new Text("Game actions",
				"Resets this game's settings, and finds help for it on the web.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.NGEmuSearchButton)] = new Text("Search on NGemu...",
				"Searches the NGemu forum for this game.");
			d[nameof(GameSettingDetailsUserControl) + "." + nameof(GameSettingDetailsUserControl.NGEmuThreadButton)] = new Text("Open NGemu...",
				"Opens the emulator's thread on the NGemu forum.");
			Engine.UiTree.UiGameFlags.Add(d, "GameSettingDetailsUserControl");

			// Controller Settings: settings saved online.
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.SettingsListTabControl)] = new Text("Settings lists",
				"Your saved settings, the most popular settings for your controllers, and defaults for well-known controllers.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.toolStrip1)] = new Text("My device settings actions",
				"Reads, loads and deletes your saved settings, and chooses the controller they load into.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.toolStrip2)] = new Text("Most popular settings actions",
				"Reads the most popular settings again, and loads the selected one.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.toolStrip3)] = new Text("Controller defaults actions",
				"Reads the controller defaults again, and loads the selected one.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.MyDevicesDataGridView)] = new Text("My device settings",
				"Settings saved online for your controllers.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.ControllerComboBox)] = new Text("Controller",
				"Which controller's settings Save sends.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.GameComboBox)] = new Text("Game",
				"Which game the saved settings are for.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.CommentTextBox)] = new Text("Comment",
				"A note saved with the settings.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.CommentSelectedTextBox)] = Live("Selected comment",
				"The note saved with the selected settings.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.MySettingsSaveButton)] = new Text("Save",
				"Saves the chosen controller's settings online, for the chosen game.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.MySettingsLoadButton)] = new Text("Load",
				"Loads the selected settings into their controller.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.MySettingsDeleteButton)] = new Text("Delete",
				"Deletes the selected settings from the online database.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.MySettingsRefreshButton)] = new Text("Refresh",
				"Reads your saved settings from the online database again.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.MapToDropDownButton)] = new Text("Map To",
				"Which controller the selected settings load into.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.SummariesDataGridView)] = new Text("Most popular settings",
				"The settings other people use most for the controllers you have.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.GlobalSettingsLoadButton)] = new Text("Load",
				"Loads the selected popular settings into the chosen controller.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.GlobalSettingsRefreshButton)] = new Text("Refresh",
				"Reads the most popular settings from the online database again.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.PresetsDataGridView)] = new Text("Controller defaults",
				"Default settings for well-known controllers.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.PresetsLoadButton)] = new Text("Load",
				"Loads the selected defaults into the chosen controller.");
			d[nameof(ControllerSettingsUserControl) + "." + nameof(ControllerSettingsUserControl.PresetRefreshButton)] = new Text("Refresh",
				"Reads the controller defaults from the online database again.");
		}
	}
}
