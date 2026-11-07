using JocysCom.ClassLibrary.Controls.IssuesControl;
using System.Collections.Generic;
using x360ce.App.Controls;
using static JocysCom.ClassLibrary.Controls.UiTree.UiText;

namespace x360ce.App.UiTree
{
	public static partial class UiCatalog
	{
		/// <summary>The individual settings on the General and Internet pages.</summary>
		static void AddSwitches(Dictionary<string, Text> d)
		{
			// Testing.
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.XInputEnableCheckBox)] = new Text(null,
				"Turns the emulated controllers on. Off leaves games with the real ones.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.ShowProgramsTabCheckBox)] = new Text(null,
				"Shows the Programs page, which lists what has been set up for each program.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.ShowDevicesTabCheckBox)] = new Text(null,
				"Shows the Devices page, which lists every controller found.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.ShowSettingsTabCheckBox)] = new Text(null,
				"Shows the Settings page, which lists every stored setting.");

			// Operation.
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.AllowOnlyOneCopyCheckBox)] = new Text(null,
				"Brings the running copy forward instead of starting a second one.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.MinimizeToTrayCheckBox)] = new Text(null,
				"Hides the window to the notification area instead of the taskbar.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.AlwaysOnTopCheckBox)] = new Text(null,
				"Keeps this window in front of other windows.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.StartWithWindowsCheckBox)] = new Text(null,
				"Starts this copy of the program when you sign in. The tick is Windows' own setting, the one Task Manager shows on its Startup apps page, " +
				"so it is unticked while Windows starts another copy or Task Manager has it switched off, and ticking it puts this copy in.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.ShowFormInfoCheckBox)] = new Text(null,
				"Shows what a control is called when you Ctrl+Shift+right-click it.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.ShowTestButtonCheckBox)] = new Text(null,
				"Shows the Test button on the bar along the top.");

			// Which devices are read.
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.ExcludeSupplementalDevicesCheckBox)] = new Text(null,
				"For Auto Map, which is not available yet: leaves out the extra parts a device reports beside its main controls. Nothing uses it until then.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.ExcludeVirtualDevicesCheckBox)] = new Text(null,
				"For Auto Map, which is not available yet: leaves out vJoy virtual devices, so they are not mapped to themselves. Nothing uses it until then.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.ReadEveryChangeCheckBox)] = new Text(null,
				"Reads every change a device reported, rather than only its position at each pass, on DirectInput and Raw Input devices alike, " +
				"so a tap that comes and goes between two passes still reaches the controller. " +
				"Turn it off only for a worn button whose chatter then comes through as extra presses.");

			// Internet page.
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.InternetFeaturesCheckBox)] = new Text(null,
				"Allows the program to contact the online settings database. While it is off, nothing is sent to the cloud.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.InternetAutoLoadCheckBox)] = new Text(null,
				"Meant to fetch settings for a device as soon as it is added. Nothing does that yet: use Download From Cloud on the Cloud page. "
				+ "Greyed out while Internet features are off.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.InternetAutoSaveCheckBox)] = new Text(null,
				"Shares your settings so other people with the same device can use them. Greyed out while Internet features are off.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.GetProgramsIncludeEnabledCheckBox)] = new Text(null,
				"Counts only games that are switched on when choosing a default.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.OpenSettingsFolderButton)] = new Text("Open",
				"Opens the folder the profile identifier is taken from.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.LoginButton)] = new Text("Log In",
				"Signs in with the username and password above.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.CreateButton)] = new Text("Create...",
				"Creates an account on the online database.");
			d[nameof(OptionsInternetUserControl) + "." + nameof(OptionsInternetUserControl.ResetButton)] = new Text("Reset...",
				"Sends a password reset to the address above.");
			d[nameof(OptionsUserControl) + "." + nameof(OptionsUserControl.AboutViGEmLinkLabel)] = new Text("Driver author",
				"Opens the page the virtual controller driver comes from.");

			AddRemainingCommands(d);
		}

		/// <summary>Menu entries and links which had a name from their caption but no purpose.</summary>
		static void AddRemainingCommands(Dictionary<string, Text> d)
		{
			d[nameof(XboxImageUserControl)] = new Text("Controller picture",
				"Lights up each part of the controller as it is used, so a mapping can be checked by eye.");
			d[nameof(GamesGridUserControl) + "." + nameof(GamesGridUserControl.ShowAllGamesMenuItem)] = new Text(null,
				"Lists every game.");
			d[nameof(GamesGridUserControl) + "." + nameof(GamesGridUserControl.ShowEnabledGamesMenuItem)] = new Text(null,
				"Lists only the games that are switched on.");
			d[nameof(GamesGridUserControl) + "." + nameof(GamesGridUserControl.ShowDisabledGamesMenuItem)] = new Text(null,
				"Lists only the games that are switched off.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.ActionGroupBox)] = new Text(null,
				"Undoes every change made to this game.");
			d[nameof(IssuesUserControl) + "." + nameof(IssuesUserControl.RunStateLabel)] = new Text("Check state",
				"Whether the checks are running or waiting.");
		}
	}
}
