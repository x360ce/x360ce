using System.Collections.Generic;
using JocysCom.ClassLibrary.Mcp;
using x360ce.App.Controls;
using static JocysCom.ClassLibrary.Controls.UiTree.UiText;

namespace x360ce.App.UiTree
{
	public static partial class UiCatalog
	{
		/// <summary>The Options tab and the pages inside it.</summary>
		static void AddOptions(Dictionary<string, Text> d)
		{
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.MainTabControl)] = new Text("Options pages",
				"Settings for the program itself, rather than for one controller.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.GeneralTabPage)] = new Text("General",
				"How the program starts and runs, and which tabs it shows.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.InternetOptionsTabPage)] = new Text("Internet",
				"Whether settings are shared with the online database, and the account used.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.VirtualDeviceTabPage)] = new Text("Virtual Device",
				"The driver that presents the emulated controllers to Windows.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HidHideTabPage)] = new Text("HID Hide",
				"Hides the real controller from games, so only the emulated one is seen.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HidGuardianTabPage)] = new Text("HID Guardian (obsolete)",
				"The tool HID Hide replaced. Kept so an old installation can be removed.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.SettingsTabPage)] = new Text("Settings",
				"Where your settings are kept, and how to move them somewhere else.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.UpdateTabPage)] = new Text("Update",
				"Looks for a newer version of the program and installs it.");

			// General page.
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.TestingGroupBox)] = new Text("Testing",
				"Switching the emulated controllers off to try the real ones, and the pages that show more detail.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.OperationGroupBox)] = new Text("Operation",
				"How the program behaves while it runs.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.DevelopingGroupBox)] = new Text("Developing",
				"Aids for working on the program itself.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.DirectInputDevicesGroupBox)] = new Text("Input Devices",
				"Which devices the program lists and reads.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.ConfigurationGroupBox)] = new Text("Configuration",
				"Switching settings to the game in front.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.AutoDetectForegroundWindowCheckBox)] = new Text(
				"Auto switch configuration when game focused",
				"Loads the settings of whichever game you switch to.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.GuideButtonGroupBox)] = new Text("Guide Button",
				"What happens when the Guide button is pressed.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.GuideButtonActionTextBox)] = new Text("Guide button action",
				"Program or command run when the Guide button is pressed.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HotkeysGroupBox)] = new Text("Hotkeys",
				"Keys and controller buttons that reach this program from inside a game.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.EmulationHotkeyCheckBox)] = new Text("Emulation",
				"Turns the hotkey on. Off by default, so the keys stay with other programs until you choose.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.EmulationHotkeyTextBox)] = new Text("Emulation hotkey",
				"The keys that turn the emulated controllers on and off from inside a game, once the box beside them is ticked. Click, then press other keys to change them, which ticks the box. Backspace clears it.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.EmulationOverlayCheckBox)] = new Text("Show a note on the screen when pressed",
				"Answers a hotkey or emulation buttons press with a short note at the top of the screen, over the game, where the usual notification would be hidden. Greyed out while both are off.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.EmulationButtonsCheckBox)] = new Text("Controller",
				"Turns the emulation buttons on. Off by default, so the buttons do only what the game makes of them until you choose.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.EmulationButtonsTextBox)] = new Text("Emulation buttons",
				"Buttons of an emulated controller that, held together, turn the emulated controllers on and off from inside a game, once the box beside them is ticked. "
				+ "Read from every mapped device, also while the emulated controllers are off. Click, then hold two buttons or more and let go, which ticks the box. Backspace clears it.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.DeviceChangeOverlayCheckBox)] = new Text("Show a note when a device disconnects",
				"Shows a short note at the top of the screen, over the game, when a device mapped to the current game disconnects or connects again.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.StartWithWindowsStateComboBox)] = new Text("Start with Windows",
				"How the window appears when the program starts with Windows. Greyed out while it does not start with Windows.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.AppearanceGroupBox)] = new Text("Appearance",
				"The program's colours, and the font and size its windows are drawn in.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.ThemeComboBox)] = new Text("Theme",
				"Light or dark colours. System follows the Windows setting for apps.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.FontComboBox)] = new Text("Font",
				"The font the program's windows are drawn in: the default, Windows' own; Selawik, built in; or an installed font. "
				+ "Takes effect when x360ce starts again.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.SizeComboBox)] = new Text("Size",
				"The size the program's text is drawn at, in points and in pixels on this screen: the default, Windows' own for its "
				+ "own font and Windows' text size for any other, "
				+ "or one size for each pixel from three quarters of it to half as large again. Applies to every font, and the windows "
				+ "grow or shrink with it. Takes effect when x360ce starts again.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.RestartButton)] = new Text("Restart x360ce",
				"Closes x360ce, saving its settings, and starts it again in the font and size chosen. Shown while the font or size chosen is not the one in use.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.ProgramScanLocationsTabControl)] = new Text("Scan locations",
				"Folders searched when looking for installed games.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.GameScanLocationsTabPage)] = new Text("Game Scan Locations",
				"Folders searched when looking for installed games.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.GameScanLocationsListBox)] = new Text("Scanned folders",
				"Folders searched when looking for installed games.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.LocationsToolStrip)] = new Text("Scan location actions",
				"Adds and removes the folders searched for games.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.AddLocationButton)] = new Text("Add...",
				"Adds a folder to search for games.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.RemoveLocationButton)] = new Text("Remove",
				"Stops searching the selected folder.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.RefreshLocationsButton)] = new Text("Refresh",
				"Reads the folder list again.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.DeveloperToolsButton)] = new Text("Developer Tools...",
				"Opens a window of aids for working on the program.");

			// Virtual device page.
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.VirtualDeviceGroupBox)] = new Text("Virtual controller driver",
				"The driver that presents the emulated controllers to Windows.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.PollingRateComboBox)] = new Text("Polling rate",
				"How often the emulated controllers are refreshed.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.ViGEmBusTextBox)] = new Text("Driver version",
				"Which version of the virtual controller driver is installed.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.ViGEmBusInstallButton)] = new Text("Install",
				"Installs the virtual controller driver. Needs Administrator.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.ViGEmBusUninstallButton)] = new Text("Uninstall",
				"Removes the virtual controller driver. Needs Administrator.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.ViGEmBusRefreshButton)] = new Text("Refresh",
				"Checks the driver again.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.AllowRemoteControllersGroupBox)] = new Text("Allow Remote Controllers",
				"Lets another computer on the network work these controllers.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.RemoteEnabledCheckBox)] = new Text("Enabled",
				"Accepts controllers from another computer.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.RemotePasswordTextBox)] = new Text("Remote password",
				"Password another computer must give before it may work these controllers.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.RemotePortNumericUpDown)] = new Text("Remote port",
				"Network port listened on for a remote controller.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.AllowRemote1CheckBox)] = new Text("Allow remote controller 1",
				"Lets a remote computer work controller 1.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.AllowRemote2CheckBox)] = new Text("Allow remote controller 2",
				"Lets a remote computer work controller 2.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.AllowRemote3CheckBox)] = new Text("Allow remote controller 3",
				"Lets a remote computer work controller 3.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.AllowRemote4CheckBox)] = new Text("Allow remote controller 4",
				"Lets a remote computer work controller 4.");

			// The AI page: the shared page's names and purposes, under the tab that is this program's own.
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.AiTabPage)] = new Text("AI",
				"The skill that teaches AI agents to use this program, AI assistant access, and the tunnel for ChatGPT on the web.");
			AiAccessModel.AddCatalog(d, AiAccessModel.FormsView);

			// Hiding the real controller.
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HidHideGroupBox)] = new Text("HID Hide",
				"Hides the real controller from games, so only the emulated one is seen.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HidHideStatusTextBox)] = new Text("HID Hide state",
				"Whether HID Hide is installed, which version, whether a newer one was found, and each step of an install as it happens.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HidHideRefreshButton)] = new Text("Refresh",
				"Checks HID Hide again.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HidHideInstallButton)] = new Text("Install HID Hide",
				"Installs HID Hide from its latest release on GitHub: downloads the setup, checks it is the file the release describes and signed by its authors, then runs it. "
				+ "Windows asks for Administrator. Once installed it reads Check for Update, and Update HID Hide when a newer version was found.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HidHideConfigureButton)] = new Text("Open Configuration",
				"Opens the HID Hide program, where hidden devices are chosen.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HidHideDownloadLinkLabel)] = new Text("HID Hide releases",
				"Opens the page HID Hide's setups are downloaded from, to download one by hand.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HidHideHelpRichTextBox)] = new Text("HID Hide notes",
				"What HID Hide is for, installing it, choosing what it hides, and what to do if it causes trouble.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HidHideConfigureAutomaticallyCheckBox)] = new Text("Configure automatically",
				"Hides a device from games through HID Hide while it is mapped to a controller, and shows it again as this program closes. "
				+ "Greyed out while HID Hide is not installed.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.groupBox1)] = new Text("HID Guardian",
				"The tool HID Hide replaced. Kept so an old installation can be removed.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HidGuardianTextBox)] = new Text("HID Guardian state",
				"Whether HID Guardian is still installed.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HidGuardianInstallButton)] = new Text("Install",
				"Installs HID Guardian. Use HID Hide instead.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HidGuardianUninstallButton)] = new Text("Uninstall",
				"Removes HID Guardian. Needs Administrator.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HidGuardianRefreshButton)] = new Text("Refresh",
				"Checks HID Guardian again.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.HelpRichTextBox)] = new Text("HID Guardian notes",
				"Explains why HID Guardian is no longer recommended.");

			AddInternetOptions(d);
			AddSettingsOptions(d);
			AddPluginsOptions(d);
		}

		/// <summary>The Plugins page: the plugins found, what became of each, and whether others may load.</summary>
		static void AddPluginsOptions(Dictionary<string, Text> d)
		{
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.PluginsTabPage)] = new Text("Plugins",
				"The plugins found, the devices each adds, whether plugins from others may load, and whether AutoHotkey scripts may change controllers.");
			d[nameof(OptionsPluginsUserControl) + "." + nameof(OptionsPluginsUserControl.AutoHotkeyGroupBox)] = new Text("AutoHotkey scripts",
				"Whether an AutoHotkey script may read the devices and change what their controllers read, and how quickly x360ce notices one that stopped.");
			d[nameof(OptionsPluginsUserControl) + "." + nameof(OptionsPluginsUserControl.AllowAutoHotkeyScriptsCheckBox)] = new Text("Allow AutoHotkey scripts",
				"Lets an AutoHotkey v2 script that includes x360ce.ahk read the devices and change what their controllers read: press buttons, hold axes and sliders. " +
				"Turning it on writes x360ce.ahk to Documents\\AutoHotkey\\Lib. When the script closes or crashes, everything it held is let go of at once.");
			d[nameof(OptionsPluginsUserControl) + "." + nameof(OptionsPluginsUserControl.ScriptsNoteLabel)] = new Text("About AutoHotkey scripts",
				"How a script reaches x360ce, and where the library it includes is written.");
			d[nameof(OptionsPluginsUserControl) + "." + nameof(OptionsPluginsUserControl.HeartbeatUpDown)] = new Text("Heartbeat every (ms)",
				"How often a script tells x360ce it still runs, in milliseconds. AutoHotkey's timers step every 15.6 ms, so less beats no faster. Greyed out while AutoHotkey scripts are not allowed.");
			d[nameof(OptionsPluginsUserControl) + "." + nameof(OptionsPluginsUserControl.HangTimeoutUpDown)] = new Text("Let go of a hung script after (ms)",
				"How long x360ce waits for a heartbeat before it lets go of everything a script that stopped responding held, and at least three heartbeats. " +
				"Shorter lets go sooner, but a script busy for longer, such as in a long Critical section, then loses what it held. Greyed out while AutoHotkey scripts are not allowed.");
			d[nameof(OptionsPluginsUserControl) + "." + nameof(OptionsPluginsUserControl.AllowThirdPartyPluginsCheckBox)] = new Text("Allow third-party plugins",
				"Loads plugins not signed by the publisher of x360ce. A plugin runs with all the rights of x360ce, so allow this only for plugins from people you trust. Unticking it takes effect when x360ce starts again.");
			d[nameof(OptionsPluginsUserControl) + "." + nameof(OptionsPluginsUserControl.OpenFolderButton)] = new Text("Open Folder",
				"Opens the folder plugins load from. A plugin put there in a folder of its own, MyDevice\\MyDevice.dll, loads at once.");
			d[nameof(OptionsPluginsUserControl) + "." + nameof(OptionsPluginsUserControl.FolderTextBox)] = new Text("Plugins folder",
				"The folder plugins load from: a folder for each plugin, holding the DLL named like it and the DLLs it needs.");
			d[nameof(OptionsPluginsUserControl) + "." + nameof(OptionsPluginsUserControl.NoteLabel)] = new Text("About plugins",
				"Which plugins load by themselves, and why plugins from others need permission.");
			d[nameof(OptionsPluginsUserControl) + "." + nameof(OptionsPluginsUserControl.PluginsDataGridView)] = new Text("Plugins",
				"Each plugin found: the devices it adds, whether it loaded, who signed it, and why it did not load or was switched off.");
		}

		/// <summary>The Internet page: the online database and the account used with it.</summary>
		static void AddInternetOptions(Dictionary<string, Text> d)
		{
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.InternetGroupBox)] = new Text("Internet",
				"Whether the program contacts the online settings database at all.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.InternetDatabaseUrlComboBox)] = new Text("Web service address",
				"Address of the online settings database.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.TestServiceButton)] = new Text("Test",
				"Asks the web service at that address who it is and says whether it answers.");
			// What it is for is the option's own description, which the box takes when it is bound.
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.SdlDatabaseUrlComboBox)] = new Text("SDL database address", null);
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.UpdateSdlDatabaseButton)] = new Text("Update SDL Database",
				"Downloads SDL's controller database from that address into the settings folder, as the button on each controller's SDL tab does, and says in the header how it went.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.GamesGroupBox)] = new Text("Default settings",
				"How settings shared by other people are chosen.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.GetProgramsMinInstancesUpDown)] = new Text(
				"Minimum instances",
				"How many people must use a setting before it is offered as the default.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.OnlineAccountGroupBox)] = new Text("Online account",
				"Identifies this computer to the online database.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.ComputerIdTextBox)] = new Text("Computer identifier",
				"Anonymous identifier for this computer.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.ProfileIdTextBox)] = new Text("Profile identifier",
				"Anonymous identifier for this profile.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.ComputerDiskTextBox)] = new Text("Computer disk",
				"Disk the computer identifier is taken from.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.ProfilePathTextBox)] = new Text("Profile path",
				"Folder the profile identifier is taken from.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.OnlineAccountLoginGroupBox)] = new Text("Sign in",
				"Signs in, so settings can be kept with an account instead of this computer.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.UsernameTextBox)] = new Text("Username",
				"E-mail address the account was created with.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.PasswordTextBox)] = new Text("Password",
				"Password for the account.");
		}

		/// <summary>The Update page: finding and installing a newer version.</summary>
		static void AddUpdateOptions(Dictionary<string, Text> d)
		{
			d[nameof(OptionsUpdateUserControl)] = new Text("Update",
				"Looks for a newer version of the program and installs it, step by step, in the log below.");
			d[nameof(OptionsUpdateUserControl) + "." + nameof(OptionsUpdateUserControl.CheckForUpdatesCheckBox)] = new Text(null,
				"Looks for a newer version once a day after the program starts. Off, the program makes no request.");
			d[nameof(OptionsUpdateUserControl) + "." + nameof(OptionsUpdateUserControl.PrivacyLabel)] = new Text("What the check sends",
				"When the check runs and the one thing it sends: this program's version, to github.com.");
			d[nameof(OptionsUpdateUserControl) + "." + nameof(OptionsUpdateUserControl.CheckButton)] = new Text("Check now",
				"Looks for a newer version now, downloads it, checks it and installs it.");
			d[nameof(OptionsUpdateUserControl) + "." + nameof(OptionsUpdateUserControl.CheckDigitalSignatureCheckBox)] = new Text(null,
				"Installs only a download that carries a trusted digital signature. On by default; your choice is kept. "
				+ "The download is always checked against the SHA-256 GitHub publishes for it.");
			d[nameof(OptionsUpdateUserControl) + "." + nameof(OptionsUpdateUserControl.CheckVersionCheckBox)] = new Text(null,
				"Installs only a download whose version is newer than this one and matches the release. On by default; your choice is kept. "
				+ "A release older than or the same as this one is never offered.");
			d[nameof(OptionsUpdateUserControl) + "." + nameof(OptionsUpdateUserControl.LogTextBox)] = Live("Update log",
				"Each step of the last check and what it found.");
		}

		/// <summary>The Settings page: where settings are kept.</summary>
		static void AddSettingsOptions(Dictionary<string, Text> d)
		{
			d[nameof(OptionsSettingsUserControl) + "." + nameof(OptionsSettingsUserControl.CurrentPathTextBox)] = new Text("Settings folder in use",
				"Folder the settings are being read from and written to.");
			d[nameof(OptionsSettingsUserControl) + "." + nameof(OptionsSettingsUserControl.LocationComboBox)] = new Text("Keep settings in",
				"Which folder to keep settings in. Your own user folder cannot be locked by another account.");
			d[nameof(OptionsSettingsUserControl) + "." + nameof(OptionsSettingsUserControl.MoveModeComboBox)] = new Text("What to do with existing settings",
				"Whether the settings you have are copied to the new folder, or left behind.");
			d[nameof(OptionsSettingsUserControl) + "." + nameof(OptionsSettingsUserControl.ApplyButton)] = new Text("Apply",
				"Moves the settings to the chosen folder and starts using it.");
			d[nameof(OptionsSettingsUserControl) + "." + nameof(OptionsSettingsUserControl.OpenFolderButton)] = new Text("Open Folder",
				"Opens the settings folder in Explorer.");
		}
	}
}
