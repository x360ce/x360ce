using System.Collections.Generic;
using static JocysCom.ClassLibrary.Controls.UiTree.UiText;

namespace x360ce.App.UiTree
{
	public static partial class UiCatalog
	{
		/// <summary>The individual settings on the General and Internet pages.</summary>
		static void AddSwitches(Dictionary<string, Text> d)
		{
			// Testing.
			d["OptionsUserControl.XInputEnableCheckBox"] = new Text(null,
				"Turns the emulated controllers on. Off leaves games with the real ones.");
			d["OptionsUserControl.ShowProgramsTabCheckBox"] = new Text(null,
				"Shows the Programs page, which lists what has been set up for each program.");
			d["OptionsUserControl.ShowDevicesTabCheckBox"] = new Text(null,
				"Shows the Devices page, which lists every controller found.");
			d["OptionsUserControl.ShowSettingsTabCheckBox"] = new Text(null,
				"Shows the Settings page, which lists every stored setting.");

			// Operation.
			d["OptionsUserControl.AllowOnlyOneCopyCheckBox"] = new Text(null,
				"Brings the running copy forward instead of starting a second one.");
			d["OptionsUserControl.MinimizeToTrayCheckBox"] = new Text(null,
				"Hides the window to the notification area instead of the taskbar.");
			d["OptionsUserControl.AlwaysOnTopCheckBox"] = new Text(null,
				"Keeps this window in front of other windows.");
			d["OptionsUserControl.StartWithWindowsCheckBox"] = new Text(null,
				"Starts this copy of the program when you sign in. The tick is Windows' own setting, the one Task Manager shows on its Startup apps page, " +
				"so it is unticked while Windows starts another copy or Task Manager has it switched off, and ticking it puts this copy in.");
			d["OptionsUserControl.ShowFormInfoCheckBox"] = new Text(null,
				"Shows what a control is called when you Ctrl+Shift+right-click it.");
			d["OptionsUserControl.ShowTestButtonCheckBox"] = new Text(null,
				"Shows the Test button on the bar along the top.");

			// Which devices are read.
			d["OptionsUserControl.ExcludeSupplementalDevicesCheckBox"] = new Text(null,
				"For Auto Map, which is not available yet: leaves out the extra parts a device reports beside its main controls. Nothing uses it until then.");
			d["OptionsUserControl.ExcludeVirtualDevicesCheckBox"] = new Text(null,
				"For Auto Map, which is not available yet: leaves out vJoy virtual devices, so they are not mapped to themselves. Nothing uses it until then.");
			d["OptionsUserControl.ReadEveryChangeCheckBox"] = new Text(null,
				"Reads every change a device reported, rather than only its position at each pass, on DirectInput and Raw Input devices alike, " +
				"so a tap that comes and goes between two passes still reaches the controller. " +
				"Turn it off only for a worn button whose chatter then comes through as extra presses.");

			// Internet page.
			d["OptionsInternetUserControl.InternetFeaturesCheckBox"] = new Text(null,
				"Allows the program to contact the online settings database. While it is off, nothing is sent to the cloud.");
			d["OptionsInternetUserControl.InternetAutoLoadCheckBox"] = new Text(null,
				"Meant to fetch settings for a device as soon as it is added. Nothing does that yet: use Download From Cloud on the Cloud page. "
				+ "Greyed out while Internet features are off.");
			d["OptionsInternetUserControl.InternetAutoSaveCheckBox"] = new Text(null,
				"Shares your settings so other people with the same device can use them. Greyed out while Internet features are off.");
			d["OptionsInternetUserControl.GetProgramsIncludeEnabledCheckBox"] = new Text(null,
				"Counts only games that are switched on when choosing a default.");
			d["OptionsInternetUserControl.OpenSettingsFolderButton"] = new Text("Open",
				"Opens the folder the profile identifier is taken from.");
			d["OptionsInternetUserControl.LoginButton"] = new Text("Log In",
				"Signs in with the username and password above.");
			d["OptionsInternetUserControl.CreateButton"] = new Text("Create...",
				"Creates an account on the online database.");
			d["OptionsInternetUserControl.ResetButton"] = new Text("Reset...",
				"Sends a password reset to the address above.");
			d["OptionsUserControl.AboutViGEmLinkLabel"] = new Text("Driver author",
				"Opens the page the virtual controller driver comes from.");

			AddRemainingCommands(d);
		}

		/// <summary>Menu entries and links which had a name from their caption but no purpose.</summary>
		static void AddRemainingCommands(Dictionary<string, Text> d)
		{
			d["XboxImageUserControl"] = new Text("Controller picture",
				"Lights up each part of the controller as it is used, so a mapping can be checked by eye.");
			d["AxisMapUserControl.P_0_0_0_MenuItem"] = new Text(null,
				"Clears the dead zone, anti-dead zone and sensitivity.");
			AddPreset(d, "P_5_100_0_MenuItem", "5", "100");
			AddPreset(d, "P_0_100_0_MenuItem", "0", "100");
			AddPreset(d, "P_0_80_0_MenuItem", "0", "80");
			AddPreset(d, "P_0_60_0_MenuItem", "0", "60");
			AddPreset(d, "P_0_40_0_MenuItem", "0", "40");
			AddPreset(d, "P_0_20_0_MenuItem", "0", "20");

			d["GamesGridUserControl.ShowAllGamesMenuItem"] = new Text(null,
				"Lists every game.");
			d["GamesGridUserControl.ShowEnabledGamesMenuItem"] = new Text(null,
				"Lists only the games that are switched on.");
			d["GamesGridUserControl.ShowDisabledGamesMenuItem"] = new Text(null,
				"Lists only the games that are switched off.");
			d["GameDetailsUserControl.ActionGroupBox"] = new Text(null,
				"Undoes every change made to this game.");
			d["UserDevicesUserControl.EnumeratedDevicesButton"] = new Text(null,
				"Lists the devices HID Guardian knows about.");
			d["UserDevicesUserControl.HiddenDevicesMenuItem"] = new Text(null,
				"Lists the devices HID Guardian is hiding from games.");
			d["UserDevicesUserControl.UnhideAllDevicesMenuItem"] = new Text(null,
				"Makes every hidden device visible to games again.");
			d["UserDevicesUserControl.synchronizeToHidGuardianToolStripMenuItem"] = new Text(null,
				"Hides exactly the devices that are mapped to a controller.");
			d["IssuesUserControl.RunStateLabel"] = new Text("Check state",
				"Whether the checks are running or waiting.");

			d["AboutControl.x360ceLinkLabel"] = new Text(null, "Opens the program's website.");
			d["AboutControl.GoogleProjectLinkLabel"] = new Text(null, "Opens the source code.");
			d["AboutControl.AboutJocysLinkLabel"] = new Text(null, "Opens the publisher's website.");
			d["AboutControl.AboutVirusLinkLabel"] = new Text(null, "Opens a contributor's page.");
			d["AboutControl.AboutViGEmLinkLabel"] = new Text(null,
				"Opens the page the virtual controller driver comes from.");
			d["AboutControl.AboutTocaEditLinkLabel"] = new Text(null,
				"Opens the site of the person who wrote the first version.");
		}

		static void AddPreset(Dictionary<string, Text> d, string field, string dead, string anti)
		{
			d["AxisMapUserControl." + field] = new Text(null,
				"Sets the dead zone to " + dead + "% and the anti-dead zone to " + anti + "%.");
		}
	}
}
