using System.Collections.Generic;

namespace x360ce.App.UiTree
{
	public static partial class UiText
	{
		/// <summary>The Options tab and the pages inside it.</summary>
		static void AddOptions(Dictionary<string, Text> d)
		{
			d["OptionsUserControl.MainTabControl"] = new Text("Options pages",
				"Settings for the program itself, rather than for one controller.");
			d["OptionsUserControl.GeneralTabPage"] = new Text("General",
				"How the program starts, what it logs, and which tabs it shows.");
			d["OptionsUserControl.InternetOptionsTabPage"] = new Text("Internet",
				"Whether settings are shared with the online database, and the account used.");
			d["OptionsUserControl.VirtualDeviceTabPage"] = new Text("Virtual Device",
				"The driver that presents the emulated controllers to Windows.");
			d["OptionsUserControl.HidHideTabPage"] = new Text("HID Hide",
				"Hides the real controller from games, so only the emulated one is seen.");
			d["OptionsUserControl.HidGuardianTabPage"] = new Text("HID Guardian (obsolete)",
				"The tool HID Hide replaced. Kept so an old installation can be removed.");
			d["OptionsUserControl.SettingsTabPage"] = new Text("Settings",
				"Where your settings are kept, and how to move them somewhere else.");

			// General page.
			d["OptionsUserControl.TestingAndLoggingGroupBox"] = new Text("Testing and Logging",
				"Extra output, useful when something is not working and needs reporting.");
			d["OptionsUserControl.OperationGroupBox"] = new Text("Operation",
				"How the program behaves while it runs.");
			d["OptionsUserControl.DevelopingGroupBox"] = new Text("Developing",
				"Aids for working on the program itself.");
			d["OptionsUserControl.DirectInputDevicesGroupBox"] = new Text("Direct Input Devices",
				"Which devices the program lists and reads.");
			d["OptionsUserControl.ConfigurationGroupBox"] = new Text("Configuration",
				"What the settings file written for games contains.");
			d["OptionsUserControl.ConfigurationVersionTextBox"] = new Text("Configuration version",
				"Which version of the settings file is written for games.");
			d["OptionsUserControl.IncludeProductsCheckBox"] = new Text("Include [Products]",
				"Writes the device list into the settings file games read.");
			d["OptionsUserControl.AutoDetectForegroundWindowCheckBox"] = new Text(
				"Auto switch configuration when game focused",
				"Loads the settings of whichever game you switch to.");
			d["OptionsUserControl.GuideButtonGroupBox"] = new Text("Guide Button",
				"What happens when the Guide button is pressed.");
			d["OptionsUserControl.GuideButtonActionTextBox"] = new Text("Guide button action",
				"Program or command run when the Guide button is pressed.");
			d["OptionsUserControl.StartWithWindowsStateComboBox"] = new Text("Start with Windows",
				"How the window appears when the program starts with Windows.");
			d["OptionsUserControl.ProgramScanLocationsTabControl"] = new Text("Scan locations",
				"Folders searched when looking for installed games.");
			d["OptionsUserControl.GameScanLocationsTabPage"] = new Text("Game Scan Locations",
				"Folders searched when looking for installed games.");
			d["OptionsUserControl.GameScanLocationsListBox"] = new Text("Scanned folders",
				"Folders searched when looking for installed games.");
			d["OptionsUserControl.LocationsToolStrip"] = new Text("Scan location actions",
				"Adds and removes the folders searched for games.");
			d["OptionsUserControl.AddLocationButton"] = new Text("Add...",
				"Adds a folder to search for games.");
			d["OptionsUserControl.RemoveLocationButton"] = new Text("Remove",
				"Stops searching the selected folder.");
			d["OptionsUserControl.RefreshLocationsButton"] = new Text("Refresh",
				"Reads the folder list again.");
			d["OptionsUserControl.DeveloperToolsButton"] = new Text("Developer Tools...",
				"Opens a window of aids for working on the program.");

			// Virtual device page.
			d["OptionsUserControl.VirtualDeviceGroupBox"] = new Text("Virtual controller driver",
				"The driver that presents the emulated controllers to Windows.");
			d["OptionsUserControl.PollingRateComboBox"] = new Text("Polling rate",
				"How often the emulated controllers are refreshed.");
			d["OptionsUserControl.ViGEmBusTextBox"] = new Text("Driver version",
				"Which version of the virtual controller driver is installed.");
			d["OptionsUserControl.ViGEmBusInstallButton"] = new Text("Install",
				"Installs the virtual controller driver. Needs Administrator.");
			d["OptionsUserControl.ViGEmBusUninstallButton"] = new Text("Uninstall",
				"Removes the virtual controller driver. Needs Administrator.");
			d["OptionsUserControl.ViGEmBusRefreshButton"] = new Text("Refresh",
				"Checks the driver again.");
			d["OptionsUserControl.AllowRemoteControllersGroupBox"] = new Text("Allow Remote Controllers",
				"Lets another computer on the network work these controllers.");
			d["OptionsUserControl.RemoteEnabledCheckBox"] = new Text("Enabled",
				"Accepts controllers from another computer.");
			d["OptionsUserControl.RemotePasswordTextBox"] = new Text("Remote password",
				"Password another computer must give before it may work these controllers.");
			d["OptionsUserControl.RemotePortNumericUpDown"] = new Text("Remote port",
				"Network port listened on for a remote controller.");
			d["OptionsUserControl.AllowRemote1CheckBox"] = new Text("Allow remote controller 1",
				"Lets a remote computer work controller 1.");
			d["OptionsUserControl.AllowRemote2CheckBox"] = new Text("Allow remote controller 2",
				"Lets a remote computer work controller 2.");
			d["OptionsUserControl.AllowRemote3CheckBox"] = new Text("Allow remote controller 3",
				"Lets a remote computer work controller 3.");
			d["OptionsUserControl.AllowRemote4CheckBox"] = new Text("Allow remote controller 4",
				"Lets a remote computer work controller 4.");

			// Hiding the real controller.
			d["OptionsUserControl.HidHideGroupBox"] = new Text("HID Hide",
				"Hides the real controller from games, so only the emulated one is seen.");
			d["OptionsUserControl.HidHideStatusTextBox"] = new Text("HID Hide state",
				"Whether HID Hide is installed, and which version.");
			d["OptionsUserControl.HidHideRefreshButton"] = new Text("Refresh",
				"Checks HID Hide again.");
			d["OptionsUserControl.HidHideDownloadButton"] = new Text("Download HID Hide...",
				"Opens the page HID Hide is downloaded from.");
			d["OptionsUserControl.HidHideConfigureButton"] = new Text("Open Configuration",
				"Opens the HID Hide program, where hidden devices are chosen.");
			d["OptionsUserControl.groupBox1"] = new Text("HID Guardian",
				"The tool HID Hide replaced. Kept so an old installation can be removed.");
			d["OptionsUserControl.HidGuardianTextBox"] = new Text("HID Guardian state",
				"Whether HID Guardian is still installed.");
			d["OptionsUserControl.HidGuardianInstallButton"] = new Text("Install",
				"Installs HID Guardian. Use HID Hide instead.");
			d["OptionsUserControl.HidGuardianUninstallButton"] = new Text("Uninstall",
				"Removes HID Guardian. Needs Administrator.");
			d["OptionsUserControl.HidGuardianRefreshButton"] = new Text("Refresh",
				"Checks HID Guardian again.");
			d["OptionsUserControl.HidGuardianConfigureAutomaticallyCheckBox"] = new Text(
				"Configure automatically",
				"Hides a device from games as soon as it is mapped to a controller.");
			d["OptionsUserControl.HelpRichTextBox"] = new Text("HID Guardian notes",
				"Explains why HID Guardian is no longer recommended.");

			AddInternetOptions(d);
			AddSettingsOptions(d);
		}

		/// <summary>The Internet page: the online database and the account used with it.</summary>
		static void AddInternetOptions(Dictionary<string, Text> d)
		{
			d["OptionsInternetUserControl.InternetGroupBox"] = new Text("Internet",
				"Whether the program contacts the online settings database at all.");
			d["OptionsInternetUserControl.InternetDatabaseUrlComboBox"] = new Text("Web service address",
				"Address of the online settings database.");
			d["OptionsInternetUserControl.GamesGroupBox"] = new Text("Default settings",
				"How settings shared by other people are chosen.");
			d["OptionsInternetUserControl.GetProgramsMinInstancesUpDown"] = new Text(
				"Minimum instances",
				"How many people must use a setting before it is offered as the default.");
			d["OptionsInternetUserControl.OnlineAccountGroupBox"] = new Text("Online account",
				"Identifies this computer to the online database.");
			d["OptionsInternetUserControl.ComputerIdTextBox"] = new Text("Computer identifier",
				"Anonymous identifier for this computer.");
			d["OptionsInternetUserControl.ProfileIdTextBox"] = new Text("Profile identifier",
				"Anonymous identifier for this profile.");
			d["OptionsInternetUserControl.ComputerDiskTextBox"] = new Text("Computer disk",
				"Disk the computer identifier is taken from.");
			d["OptionsInternetUserControl.ProfilePathTextBox"] = new Text("Profile path",
				"Folder the profile identifier is taken from.");
			d["OptionsInternetUserControl.OnlineAccountLoginGroupBox"] = new Text("Sign in",
				"Signs in, so settings can be kept with an account instead of this computer.");
			d["OptionsInternetUserControl.UsernameTextBox"] = new Text("Username",
				"E-mail address the account was created with.");
			d["OptionsInternetUserControl.PasswordTextBox"] = new Text("Password",
				"Password for the account.");
			d["OptionsInternetUserControl.UpdateOptionsGroupBox"] = new Text("Updates",
				"Whether the program looks for a newer version.");
		}

		/// <summary>The Settings page: where settings are kept.</summary>
		static void AddSettingsOptions(Dictionary<string, Text> d)
		{
			d["OptionsSettingsUserControl.CurrentPathTextBox"] = new Text("Settings folder in use",
				"Folder the settings are being read from and written to.");
			d["OptionsSettingsUserControl.LocationComboBox"] = new Text("Keep settings in",
				"Which folder to keep settings in. Your own user folder cannot be locked by another account.");
			d["OptionsSettingsUserControl.MoveModeComboBox"] = new Text("What to do with existing settings",
				"Whether the settings you have are copied to the new folder, or left behind.");
			d["OptionsSettingsUserControl.ApplyButton"] = new Text("Apply",
				"Moves the settings to the chosen folder and starts using it.");
			d["OptionsSettingsUserControl.OpenFolderButton"] = new Text("Open Folder",
				"Opens the settings folder in Explorer.");
		}
	}
}
