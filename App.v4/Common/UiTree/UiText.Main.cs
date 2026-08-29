using System.Collections.Generic;

namespace x360ce.App.UiTree
{
	public static partial class UiText
	{
		/// <summary>The window itself: its tabs, its bar along the top, and the status line.</summary>
		static void AddMainWindow(Dictionary<string, Text> d)
		{
			d["MainForm.MainTabControl"] = new Text("Main pages",
				"The four controllers, and the pages for everything else.");
			d["MainForm.Pad1TabPage"] = new Text("Controller 1",
				"Settings for the first emulated Xbox controller.");
			d["MainForm.Pad2TabPage"] = new Text("Controller 2",
				"Settings for the second emulated Xbox controller.");
			d["MainForm.Pad3TabPage"] = new Text("Controller 3",
				"Settings for the third emulated Xbox controller.");
			d["MainForm.Pad4TabPage"] = new Text("Controller 4",
				"Settings for the fourth emulated Xbox controller.");
			d["MainForm.OptionsTabPage"] = new Text("Options",
				"Settings for the program itself, and for the drivers it needs.");
			d["MainForm.GamesTabPage"] = new Text("Games",
				"Games this program is set up for, and what it does for each one.");
			d["MainForm.DevicesTabPage"] = new Text("Devices",
				"Every controller the program can see, whether mapped or not.");
			d["MainForm.CloudTabPage"] = new Text("Cloud",
				"Settings waiting to be sent to or fetched from the online database.");
			d["MainForm.HelpTabPage"] = new Text("Help",
				"Instructions for setting up a controller, and answers to common problems.");
			d["MainForm.HelpRichTextBox"] = new Text("Help text",
				"Instructions for setting up a controller, and answers to common problems.");
			d["MainForm.AboutTabPage"] = new Text("About",
				"Version, licence, and what changed in each release.");
			d["MainForm.IssuesTabPage"] = new Text("Issues",
				"Problems the program found, and what to do about each one.");

			// The bar along the top.
			d["MainForm.GamesToolStrip"] = new Text("Game bar",
				"Chooses the game being set up, and saves the settings.");
			d["MainForm.GameToCustomizeComboBox"] = new Text("Game",
				"Which game the settings on every page below belong to.");
			d["MainForm.SaveAllButton"] = new Text("Save All",
				"Writes every setting to disk now.");
			d["MainForm.AddGameButton"] = new Text("Add Game...",
				"Sets this program up for another game.");
			d["MainForm.TestButton"] = new Text("Test...",
				"Opens a window for trying the emulated controller without a game.");

			// The help header, which follows the mouse.
			d["MainForm.HelpSubjectLabel"] = Live("Help subject",
				"Name of whatever the mouse is over.");
			d["MainForm.HelpBodyLabel"] = Live("Help text",
				"What whatever the mouse is over is for.");
			d["MainForm.HeaderPictureBox"] = new Text("Program logo", null);
			d["MainForm.HelpPictureBox"] = new Text("Help icon", null);

			// The line along the bottom.
			d["MainForm.MainStatusStrip"] = new Text("Status bar",
				"What the program is doing, and how fast it is doing it.");
			d["MainForm.StatusTimerLabel"] = Live("Last action",
				"The most recent thing the program did.");
			d["MainForm.UpdateFrequencyLabel"] = Live("Controller rate",
				"Times a second the program reads the controllers. Higher is better.");
			// The rate and the switch are one item: it reports how often the window redraws and
			// turns that off. Its caption carries the number, so it is named but not renamed.
			d["MainForm.InterfaceUpdatesButton"] = Live("Interface rate",
				"Times a second the window redraws itself. Press to stop it redrawing, which "
				+ "leaves the controllers being read as before.");
			d["MainForm.UpdateDevicesStatusLabel"] = Live("Device reads",
				"How many times the whole device list has been read again.");
			d["MainForm.CloudMessagesLabel"] = Live("Cloud messages",
				"Messages waiting to be sent to the online database.");
			d["MainForm.StatusEventsLabel"] = Live("Suspended events",
				"Setting changes held back while a page is being filled in.");
			d["MainForm.StatusSaveLabel"] = Live("Saving",
				"Shown while settings are being written to disk.");
			d["MainForm.StatusIsAdminLabel"] = Live("Administrator",
				"Whether the program is running with Administrator rights.");
			d["MainForm.StatusErrorsLabel"] = Live("Error reports",
				"How many faults were recorded. Opens the report window.");
			d["MainForm.StatusDllLabel"] = Live("XInput library",
				"Which XInput library the program loaded, and its version.");

			// The icon in the notification area.
			d["MainForm.TrayContextMenuStrip"] = new Text("Tray menu",
				"Reached by right-clicking the icon in the notification area.");
			d["MainForm.OpenApplicationToolStripMenuItem"] = new Text("Open Application",
				"Brings the window back from the notification area.");
			d["MainForm.ExitToolStripMenuItem"] = new Text("Exit",
				"Closes the program and stops the emulated controllers.");
		}
	}
}
