using System.Collections.Generic;
using x360ce.App.Controls;
using static JocysCom.ClassLibrary.Controls.UiTree.UiText;

namespace x360ce.App.UiTree
{
	/// <summary>The name and purpose of each part of this program's interface, given to <see cref="JocysCom.ClassLibrary.Controls.UiTree.UiText"/>.</summary>
	public static partial class UiCatalog
	{
		/// <summary>Every entry, keyed "OwningType.FieldName".</summary>
		public static Dictionary<string, Text> Build()
		{
			var items = new Dictionary<string, Text>();
			AddMainWindow(items);
			AddControllerPanel(items);
			AddInputPanel(items);
			AddMapping(items);
			AddOptions(items);
			AddUpdateOptions(items);
			AddLists(items);

			AddMappingPickers(items);

			AddDeviceDetails(items);

			AddSwitches(items);
			return items;
		}

		/// <summary>The window itself: its tabs, its bar along the top, and the status line.</summary>
		static void AddMainWindow(Dictionary<string, Text> d)
		{
			d[nameof(MainForm) + "." + nameof(MainForm.MainTabControl)] = new Text("Main pages",
				"The four controllers, and the pages for everything else.");
			d[nameof(MainForm) + "." + nameof(MainForm.Pad1TabPage)] = new Text("Controller 1",
				"Settings for the first emulated Xbox controller.");
			d[nameof(MainForm) + "." + nameof(MainForm.Pad2TabPage)] = new Text("Controller 2",
				"Settings for the second emulated Xbox controller.");
			d[nameof(MainForm) + "." + nameof(MainForm.Pad3TabPage)] = new Text("Controller 3",
				"Settings for the third emulated Xbox controller.");
			d[nameof(MainForm) + "." + nameof(MainForm.Pad4TabPage)] = new Text("Controller 4",
				"Settings for the fourth emulated Xbox controller.");
			d[nameof(MainForm) + "." + nameof(MainForm.OptionsTabPage)] = new Text("Options",
				"Settings for the program itself, and for the drivers it needs.");
			d[nameof(MainForm) + "." + nameof(MainForm.GamesTabPage)] = new Text("Games",
				"Games this program is set up for, and what it does for each one.");
			d[nameof(MainForm) + "." + nameof(MainForm.DevicesTabPage)] = new Text("Devices",
				"Every controller the program can see, whether mapped or not.");
			d[nameof(MainForm) + "." + nameof(MainForm.CloudTabPage)] = new Text("Cloud",
				"Settings waiting to be sent to or fetched from the online database.");
			d[nameof(MainForm) + "." + nameof(MainForm.HelpTabPage)] = new Text("Help",
				"Instructions for setting up a controller, and answers to common problems.");
			d[nameof(MainForm) + "." + nameof(MainForm.HelpRichTextBox)] = new Text("Help text",
				"Instructions for setting up a controller, and answers to common problems.");
			d[nameof(MainForm) + "." + nameof(MainForm.AboutTabPage)] = new Text("About",
				"Version, who made the program and what it carries, what changed in each release, and the licence.");
			d[nameof(MainForm) + "." + nameof(MainForm.IssuesTabPage)] = new Text("Issues",
				"Problems the program found, and what to do about each one.");
			d[nameof(RecorderUserControl)] = new Text(null,
				"Records what the program reads and sends, into a file to post with a bug report.");
			d[nameof(RecorderUserControl) + "." + nameof(RecorderUserControl.RecordGroupBox)] = new Text(null,
				"Records what the program reads from devices and sends to games and devices into a text file to post with a bug report. "
				+ "Names, folders, addresses and keys are left out, devices are written as D1, D2 and so on, and keyboards and mice are not recorded.");
			d[nameof(RecorderUserControl) + "." + nameof(RecorderUserControl.NoteLabel)] = new Text("What the recording leaves out",
				"Says that the file holds no names, folders or keys, so it can be posted in a GitHub issue.");
			d[nameof(RecorderUserControl) + "." + nameof(RecorderUserControl.SettingsCheckBox)] = new Text(null,
				"The recording holds the options, the game's settings, each controller's mapping and every device.");
			d[nameof(RecorderUserControl) + "." + nameof(RecorderUserControl.InputCheckBox)] = new Text(null,
				"The recording holds what each device reports: buttons, axes, sliders and hats as they change, devices going offline, and failed reads.");
			d[nameof(RecorderUserControl) + "." + nameof(RecorderUserControl.OutputCheckBox)] = new Text(null,
				"The recording holds what each controller sends to the game, whether its virtual controller is fed, and what XInput reads back.");
			d[nameof(RecorderUserControl) + "." + nameof(RecorderUserControl.ForcesCheckBox)] = new Text(null,
				"The recording holds the vibration each game asks for, the force sent to each device, and whether each device is held exclusively.");
			d[nameof(RecorderUserControl) + "." + nameof(RecorderUserControl.EngineCheckBox)] = new Text(null,
				"The recording holds how fast the engine runs each second, each device call that takes long, and one that does not return.");
			d[nameof(RecorderUserControl) + "." + nameof(RecorderUserControl.WindowCheckBox)] = new Text(null,
				"The recording holds whether the window answers, how it is shown, which program is in front, the handles and memory the program holds, and its errors.");
			d[nameof(RecorderUserControl) + "." + nameof(RecorderUserControl.RecordButton)] = new Text(null,
				"Starts a new recording, or stops the one that runs and keeps its file. A recording stops by itself after 30 minutes or 20 MB.");
			d[nameof(RecorderUserControl) + "." + nameof(RecorderUserControl.OpenFolderButton)] = new Text(null,
				"Opens the folder the recordings are saved in, with the last one selected.");
			d[nameof(RecorderUserControl) + "." + nameof(RecorderUserControl.StatusLabel)] = Live("Recording status",
				"How long the recording has run and how big it is, or the file it was saved to.");

			// The bar along the top.
			d[nameof(MainForm) + "." + nameof(MainForm.GamesToolStrip)] = new Text("Game bar",
				"Chooses the game being set up, and saves the settings.");
			d[nameof(MainForm) + "." + nameof(MainForm.GameToCustomizeComboBox)] = new Text("Game",
				"Which game the settings on every page below belong to.");
			d[nameof(MainForm) + "." + nameof(MainForm.SaveAllButton)] = new Text("Save All",
				"Writes every setting to disk now.");
			d[nameof(MainForm) + "." + nameof(MainForm.AddGameButton)] = new Text("Add Game...",
				"Sets this program up for another game.");
			d[nameof(MainForm) + "." + nameof(MainForm.TestButton)] = new Text("Test...",
				"Opens a window for trying the emulated controller without a game.");

			// The help header, which follows the mouse.
			d[nameof(MainForm) + "." + nameof(MainForm.HelpSubjectLabel)] = Live("Help subject",
				"Name of whatever the mouse is over.");
			d[nameof(MainForm) + "." + nameof(MainForm.HelpBodyLabel)] = Live("Help text",
				"What whatever the mouse is over is for.");
			d[nameof(MainForm) + "." + nameof(MainForm.HeaderPictureBox)] = new Text("Program logo", null);
			d[nameof(MainForm) + "." + nameof(MainForm.HelpPictureBox)] = new Text("Help icon", null);

			// The line along the bottom.
			d[nameof(MainForm) + "." + nameof(MainForm.MainStatusStrip)] = new Text("Status bar",
				"What the program is doing, and how fast it is doing it.");
			d[nameof(MainForm) + "." + nameof(MainForm.StatusTimerLabel)] = Live("Last action",
				"The most recent thing the program did.");
			d[nameof(MainForm) + "." + nameof(MainForm.UpdateFrequencyLabel)] = Live("Controller rate",
				"Times a second the program reads the controllers. Higher is better.");
			// The rate and the switch are one item: it reports how often the window redraws and
			// turns that off. Its caption carries the number, so it is named but not renamed.
			d[nameof(MainForm) + "." + nameof(MainForm.InterfaceUpdatesButton)] = Live("Interface rate",
				"Times a second the window redraws itself. Press to stop it redrawing, which "
				+ "leaves the controllers being read as before.");
			d[nameof(MainForm) + "." + nameof(MainForm.UpdateDevicesStatusLabel)] = Live("Device reads",
				"How many times the whole device list has been read again.");
			d[nameof(MainForm) + "." + nameof(MainForm.CloudMessagesLabel)] = Live("Cloud messages",
				"Messages waiting to be sent to the online database.");
			d[nameof(MainForm) + "." + nameof(MainForm.StatusEventsLabel)] = Live("Suspended events",
				"Setting changes held back while a page is being filled in.");
			d[nameof(MainForm) + "." + nameof(MainForm.StatusSaveLabel)] = Live("Saving",
				"Shown while settings are being written to disk.");
			d[nameof(MainForm) + "." + nameof(MainForm.StatusIsAdminLabel)] = Live("Administrator",
				"Whether the program is running with Administrator rights.");
			d[nameof(MainForm) + "." + nameof(MainForm.StatusAiAccessLabel)] = Live("AI assistant access",
				"Whether an assistant may read or change this program right now. Opens the Options tab.");
			d[nameof(MainForm) + "." + nameof(MainForm.StatusErrorsLabel)] = Live("Error reports",
				"How many faults were recorded. Opens the report window.");
			d[nameof(MainForm) + "." + nameof(MainForm.StatusDllLabel)] = Live("XInput library",
				"Which XInput library the program loaded, and its version.");

			// The icon in the notification area.
			d[nameof(MainForm) + "." + nameof(MainForm.TrayContextMenuStrip)] = new Text("Tray menu",
				"Reached by right-clicking the icon in the notification area.");
			d[nameof(MainForm) + "." + nameof(MainForm.OpenApplicationToolStripMenuItem)] = new Text("Open Application",
				"Brings the window back from the notification area.");
			d[nameof(MainForm) + "." + nameof(MainForm.EmulationToolStripMenuItem)] = new Text("Enable XInput",
				"Turns the emulated controllers on or off without opening the window.");
			d[nameof(MainForm) + "." + nameof(MainForm.ExitToolStripMenuItem)] = new Text("Exit",
				"Closes the program and stops the emulated controllers.");
		}
	}
}
