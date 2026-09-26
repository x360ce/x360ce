using System.Collections.Generic;
using static x360ce.Engine.UiTree.UiText;

namespace x360ce.App.UiTree
{
	/// <summary>
	/// The name and purpose of each part of version 3's interface, given to
	/// <see cref="x360ce.Engine.UiTree.UiText"/>. The controls linked to a setting are named after
	/// the setting and are listed here only where the setting's own words say too little.
	/// </summary>
	public static partial class UiCatalog
	{
		/// <summary>Every entry, keyed "OwningType.FieldName".</summary>
		public static Dictionary<string, Text> Build()
		{
			var items = new Dictionary<string, Text>();
			AddMainWindow(items);
			AddOptions(items);
			AddDialogs(items);
			AddControllerPage(items);
			AddDeviceDetails(items);
			AddLists(items);
			return items;
		}

		/// <summary>The window itself: its tabs, the game list along the top, the status line and the tray menu.</summary>
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
				"Settings for the program itself.");
			d["MainForm.GameSettingsTabPage"] = new Text("Game Settings",
				"Games the emulator is set up for, and how it hooks each one.");
			d["MainForm.ControllerSettingsTabPage"] = new Text("Controller Settings",
				"Settings saved for your controllers, and settings other people shared online.");
			d["MainForm.HelpTabPage"] = new Text("Help",
				"Instructions for setting up a controller, and answers to common problems.");
			d["MainForm.HelpRichTextBox"] = new Text("Help text",
				"Instructions for setting up a controller, and answers to common problems.");
			d["MainForm.AboutTabPage"] = new Text("About",
				"Version, licence, and what changed in each release.");
			d["MainForm.GameToCustomizeComboBox"] = new Text("Game",
				"Which game the emulator is set up for: the folder whose x360ce.ini the pages below edit.");
			d["MainForm.HelpBodyLabel"] = Live("Help text",
				"What whatever the mouse is over is for.");

			// The line along the bottom.
			d["MainForm.MainStatusStrip"] = new Text("Status bar",
				"What the program is doing, how fast it is doing it, and which files it uses.");
			d["MainForm.StatusTimerLabel"] = Live("Last action",
				"The most recent thing the program did.");
			d["MainForm.InterfaceFrequencyLabel"] = Live("Interface rate",
				"Times a second the window reads the controllers and redraws them. Games read them on their own, through the emulator library.");
			d["MainForm.StatusEventsLabel"] = Live("Events",
				"Whether changes on the pages are being written to x360ce.ini.");
			d["MainForm.StatusSaveLabel"] = Live("Saves",
				"How many times the settings were written since the program started.");
			d["MainForm.StatusIsAdminLabel"] = Live("Elevated",
				"Whether the program runs as Administrator.");
			d["MainForm.StatusIniLabel"] = Live("Settings file",
				"The settings file the pages edit.");
			d["MainForm.StatusDllLabel"] = Live("Library",
				"The XInput library the program loaded, and its version.");

			// The tray menu.
			d["MainForm.OpenApplicationToolStripMenuItem"] = new Text("Open Application",
				"Brings the window back from the notification area.");
			d["MainForm.ExitToolStripMenuItem"] = new Text("Exit",
				"Closes the program.");
		}

		/// <summary>The Options page, including the door an assistant comes through.</summary>
		static void AddOptions(Dictionary<string, Text> d)
		{
			d["OptionsControl.XInputEnableCheckBox"] = new Text("Enable XInput",
				"Not used by this version of the program.");
			d["OptionsControl.MinimizeToTrayCheckBox"] = new Text("Minimize to Tray",
				"Hides the window in the notification area instead of the taskbar when it is minimised.");
			d["OptionsControl.InternetDatabaseUrlComboBox"] = new Text("Settings database address",
				"Web address of the online database the Controller Settings page searches and saves to.");
			d["OptionsControl.GameScanLocationsListBox"] = new Text("Game scan locations",
				"Folders the Scan button searches for games.");
			d["OptionsControl.LocationsToolStrip"] = new Text("Scan location actions",
				"Adds, removes and fills in the folders the Scan button searches.");
			d["OptionsControl.ExcludeSupplementalDevicesCheckBox"] = new Text("Exclude Supplemental Devices",
				"Leaves supplemental devices, such as the pedals of a wheel, out of the devices the program reads.");
			d["OptionsControl.ExcludeVirtualDevicesCheckBox"] = new Text("Exclude Virtual Devices",
				"Leaves vJoy devices and the controllers this program feeds out of the devices it reads, so its output is not read back as input.");
			d["OptionsControl.InternetAutoloadCheckBox"] = new Text("Auto Load Settings When Tab Selected",
				"Searches the online database for settings for your controllers each time the Controller Settings page is opened.");
			d["OptionsControl.RefreshLocationsButton"] = new Text("Refresh",
				"Adds the Program Files folders of every fixed drive to the scan locations.");
			d["OptionsControl.AddLocationButton"] = new Text("Add...",
				"Adds a folder to the scan locations.");
			d["OptionsControl.RemoveLocationButton"] = new Text("Remove",
				"Removes the selected folder from the scan locations.");
			d["OptionsControl.ConfigurationVersionTextBox"] = new Text("Settings format version",
				"Version of the x360ce.ini layout this program writes.");
			d["OptionsControl.SaveSettingsButton"] = new Text("Save",
				"Writes every setting to x360ce.ini now.");
			d["OptionsControl.OpenSettingsFolderButton"] = new Text("Open Settings Folder...",
				"Opens the folder with the game list and the other files the program keeps.");

			// AI assistant access. Changed only here, by a person; the door refuses these controls.
			d["OptionsControl.AiAccessTabPage"] = new Text("AI Assistant Access",
				"Lets an AI assistant or a script read and operate this program, off unless switched on here.");
			d["OptionsControl.AiAccessEnabledCheckBox"] = new Text("Allow AI assistants",
				"Opens the program to an AI assistant or a script on this computer, through the token below.");
			d["OptionsControl.AiAccessComboBox"] = new Text("Access",
				"How much a connected assistant may do: Read, Configure or Administer.");
			d["OptionsControl.AiAccessPortNumericUpDown"] = new Text("Port",
				"Port on this computer the assistant connects to.");
			d["OptionsControl.AiAccessTokenTextBox"] = new Text("Token",
				"What a caller must present to be let in. Regenerate it to shut out everyone who has the old one.");
			d["OptionsControl.AiAccessRegenerateButton"] = new Text("Regenerate",
				"Makes a new token, which shuts out every caller that has the old one.");
			d["OptionsControl.AiAccessWindowsCheckBox"] = new Text("Register with Windows",
				"Registers the program with the Windows agent registry, so agents such as Copilot find it by themselves.");
			d["OptionsControl.AiAccessCopyButton"] = new Text("Copy MCP Settings",
				"Copies what an assistant needs to start the program as an MCP server.");
			d["OptionsControl.AiAccessLogButton"] = new Text("Log",
				"Opens the record of everything done through the door.");
			d["OptionsControl.AiAccessStatusLabel"] = Live("Door",
				"Whether the door is open, where, and why not when it could not open.");
		}

		/// <summary>The windows that open over the main one: warnings, a new device, messages.</summary>
		static void AddDialogs(Dictionary<string, Text> d)
		{
			d["WarningsForm.WarningsTabControl"] = new Text("Warnings pages",
				"The list of problems found.");
			d["WarningsForm.WarningsDataGridView"] = new Text("Warnings",
				"Problems that stop the emulator from working, each with a button that fixes it.");
			d["WarningsForm.IgnoreButton"] = new Text("Ignore All",
				"Closes the list without fixing anything. The program closes if a problem it cannot work without remains.");
			d["WarningsForm.Closebutton"] = new Text("Cancel",
				"Closes the list; it opens again while a problem remains.");

			d["NewDeviceForm.WizzardTabControl"] = new Text("Steps",
				"The two steps of setting up a controller the program has not seen before.");
			d["NewDeviceForm.SearchRadioButton"] = new Text("Search automatically for settings",
				"Looks for settings for this controller beside the program, and online when Search the Internet is ticked.");
			d["NewDeviceForm.BrowseRadioButton"] = new Text("Browse my computer for settings",
				"Looks for settings for this controller in the x360ce.ini files of a folder you choose.");
			d["NewDeviceForm.SearchTheInternetCheckBox"] = new Text("Search the Internet",
				"Includes the online settings database in the search.");
			d["NewDeviceForm.FolderPathTextBox"] = new Text("Folder",
				"Folder to look in for an x360ce.ini with settings for this controller.");
			d["NewDeviceForm.BrowseButton"] = new Text("Browse...",
				"Chooses the folder to look in.");
			d["NewDeviceForm.IncludeSubfoldersCheckBox"] = new Text("Include subfolders",
				"Also looks in the folders inside the chosen one.");
			d["NewDeviceForm.MySettingsDataGridView"] = new Text("Settings found",
				"Settings found for this controller. Choose one and press Finish to use it.");
			d["NewDeviceForm.BackButton"] = new Text("< Back",
				"Returns to the choice of where to look.");
			d["NewDeviceForm.NextButton"] = new Text("Next >",
				"Goes to the next step, and on the last step uses the chosen settings.");
			d["NewDeviceForm.CloseButton"] = new Text("Cancel",
				"Closes the window without using any of the settings found.");

			d["AboutControl.ChangeLogTextBox"] = new Text("Changes",
				"What changed in each release of this program.");
			d["AboutControl.LicenseTextBox"] = new Text("License",
				"The licence this program is released under.");
		}
	}
}
