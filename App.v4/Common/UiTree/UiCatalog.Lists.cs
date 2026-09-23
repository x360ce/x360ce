using System.Collections.Generic;
using static x360ce.Engine.UiTree.UiText;

namespace x360ce.App.UiTree
{
	public static partial class UiCatalog
	{
		/// <summary>The pages that are mostly a list: games, devices, cloud, issues, about.</summary>
		static void AddLists(Dictionary<string, Text> d)
		{
			AddGames(d);
			AddGameDetails(d);
			AddDevices(d);
			AddCloudAndIssues(d);
			AddAbout(d);
		}

		static void AddGames(Dictionary<string, Text> d)
		{
			d["GamesGridUserControl.GamesDataGridView"] = new Text("Games",
				"Games this program is set up for. The tick says whether it is switched on.");
			d["GamesGridUserControl.GamesToolStrip"] = new Text("Game actions",
				"Finds, adds, starts and removes games.");
			d["GamesGridUserControl.ScanGamesButton"] = new Text("Scan",
				"Searches the folders listed in Options for games it knows.");
			d["GamesGridUserControl.AddGameButton"] = new Text("Add...",
				"Adds a game by choosing its program file.");
			d["GamesGridUserControl.DeleteGamesButton"] = new Text("Delete",
				"Removes the selected game from the list.");
			d["GamesGridUserControl.SaveGamesButton"] = new Text("Save",
				"Writes the settings file the selected game reads.");
			d["GamesGridUserControl.StartGameButton"] = new Text("Start",
				"Runs the selected game.");
			d["GamesGridUserControl.OpenGameButton"] = new Text("Open...",
				"Opens the folder the selected game is installed in.");
			d["GamesGridUserControl.ShowGamesDropDownButton"] = new Text("Show",
				"Limits the list to games that are switched on, or off.");
			d["GamesGridUserControl.ShowAllGamesMenuItem"] = new Text("Show: All", null);
			d["GamesGridUserControl.ShowEnabledGamesMenuItem"] = new Text("Show: Enabled", null);
			d["GamesGridUserControl.ShowDisabledGamesMenuItem"] = new Text("Show: Disabled", null);
		}

		/// <summary>How one game is set up: which libraries are replaced, and how.</summary>
		static void AddGameDetails(Dictionary<string, Text> d)
		{
			d["GameDetailsUserControl.XInputMaskGroupBox"] = new Text("XInput files",
				"Which XInput library files this program supplies to the game.");
			d["GameDetailsUserControl.DInputMaskGroupBox"] = new Text("DInput file",
				"Whether the Direct Input library is supplied to the game as well.");
			d["GameDetailsUserControl.AutoMapMaskGroupBox"] = new Text("Auto map",
				"Which of the four controllers are assigned to a device by the program itself.");
			d["GameDetailsUserControl.OtherOptionsGroupBox"] = new Text("Other options",
				"How the game is started, and what it is told about the controller.");
			d["GameDetailsUserControl.ProcessorArchitectureComboBox"] = new Text("Architecture",
				"Whether the game is a 32-bit or a 64-bit program.");
			d["GameDetailsUserControl.EmulationTypeComboBox"] = new Text("Emulation",
				"Whether the controller is presented by the virtual driver or by replaced library files.");
			d["GameDetailsUserControl.XInputPathTextBox"] = new Text("XInput path",
				"Folder the XInput library is written into.");
			d["GameDetailsUserControl.DInputFileTextBox"] = new Text("DInput file",
				"Name of the Direct Input library written for the game.");
			d["GameDetailsUserControl.HookModeFakeVidTextBox"] = new Text("Fake vendor code",
				"Vendor code reported to a game that only accepts one make of controller.");
			d["GameDetailsUserControl.HookModeFakeVidNumericUpDown"] = new Text("Fake vendor code",
				"Vendor code reported to a game that only accepts one make of controller.");
			d["GameDetailsUserControl.HookModeFakePidTextBox"] = new Text("Fake product code",
				"Product code reported to a game that only accepts one model of controller.");
			d["GameDetailsUserControl.HookModeFakePidNumericUpDown"] = new Text("Fake product code",
				"Product code reported to a game that only accepts one model of controller.");
			d["GameDetailsUserControl.TimeoutNumericUpDown"] = new Text("Timeout",
				"How long to wait for the game before giving up.");
			d["GameDetailsUserControl.groupBox5"] = new Text("Help",
				"Searches the web for other people's settings for this game.");
			d["GameDetailsUserControl.GoogleSearchButton"] = new Text("Search on Google...",
				"Searches the web for this game.");
			d["GameDetailsUserControl.button3"] = new Text("Search on NGemu...",
				"Searches the NGemu forum for this game.");
			d["GameDetailsUserControl.button4"] = new Text("Open NGemu...",
				"Opens the NGemu forum.");
			d["GameDetailsUserControl.ActionGroupBox"] = new Text("Action", null);
			d["GameDetailsUserControl.ResetToDefaultButton"] = new Text("Reset to Default",
				"Puts this game's settings back the way they started.");
			AddHookFlags(d);
		}

		/// <summary>The hook mask and library boxes, shared with version 3, and the auto-map boxes only this version has.</summary>
		static void AddHookFlags(Dictionary<string, Text> d)
		{
			d["GameDetailsUserControl.HookMaskGroupBox"] = new Text("Hook mask",
				"Which questions a game asks about controllers this program answers for it.");
			Engine.UiTree.UiGameFlags.Add(d, "GameDetailsUserControl");
			for (var i = 1; i <= 4; i++)
			{
				d["GameDetailsUserControl.Controller" + i + "CheckBox"] = new Text(
					"Auto map controller " + i,
					"Lets the program assign controller " + i + " to a device when this game starts.");
			}
		}

		static void AddDevices(Dictionary<string, Text> d)
		{
			d["UserDevicesUserControl.DevicesDataGridView"] = new Text("Devices",
				"Every controller the program can see. Unplugged ones are dimmed.");
			d["UserDevicesUserControl.ControllersToolStrip"] = new Text("Device actions",
				"Refreshes the list and works on the selected device.");
			d["XInputDevicesUserControl.DevicesToolStrip"] = new Text("Emulated controller actions",
				"Buttons that act on the emulated controllers shown below.");
			d["XInputDevicesUserControl.DevicesDataGridView"] = new Text("Emulated controllers",
				"Every emulated controller and the XInput place it holds, in the order games see them.");
			d["XInputDevicesUserControl.MoveUpColumn"] = new Text("Move Up",
				"Arrow on each row that moves that controller one place earlier.");
			d["XInputDevicesUserControl.MoveDownColumn"] = new Text("Move Down",
				"Arrow on each row that moves that controller one place later.");
			d["XInputDevicesUserControl.ApplyButton"] = new Text("Apply",
				"Recreates the controllers in the order shown. Needs Administrator.");
			d["XInputDevicesUserControl.RefreshButton"] = new Text("Refresh",
				"Reads the controllers again, for when one has arrived or left.");
			d["UserDevicesUserControl.RefreshButton"] = new Text("Refresh",
				"Reads every device again.");
			d["UserDevicesUserControl.ControllerDeleteButton"] = new Text("Delete",
				"Forgets the selected device and its settings.");
			d["UserDevicesUserControl.HardwareButton"] = new Text("Hardware...",
				"Opens the selected device in Windows Device Manager.");
			d["UserDevicesUserControl.AddDemoDevice"] = new Text("Add Demo Device",
				"Adds a pretend controller, for trying the program without hardware.");
			d["UserDevicesUserControl.CleanupVirtualPadsButton"] = new Text("Remove Leftover Pads",
				"Removes emulated controllers left behind by runs that ended badly. Needs Administrator.");
			d["UserDevicesUserControl.ShowSystemDevicesButton"] = new Text("Show System Devices",
				"Lists devices Windows files as system devices too, such as a Logitech G13, so they can be mapped. Shown when choosing a device for a controller.");
			d["UserDevicesUserControl.toolStripDropDownButton1"] = new Text("HID Guardian",
				"Actions for the obsolete HID Guardian tool.");
			d["UserDevicesUserControl.EnumeratedDevicesButton"] = new Text("Show Enumerated Devices",
				"Lists the devices HID Guardian knows about.");
			d["UserDevicesUserControl.HiddenDevicesMenuItem"] = new Text("Show Hidden Devices",
				"Lists the devices HID Guardian is hiding from games.");
			d["UserDevicesUserControl.UnhideAllDevicesMenuItem"] = new Text("Unhide All Devices",
				"Makes every hidden device visible to games again.");
			d["UserDevicesUserControl.synchronizeToHidGuardianToolStripMenuItem"] = new Text(
				"Synchronize To HID Guardian",
				"Hides exactly the devices that are mapped to a controller.");
		}

		static void AddCloudAndIssues(Dictionary<string, Text> d)
		{
			d["CloudUserControl.TasksDataGridView"] = new Text("Cloud tasks",
				"Settings waiting to be sent to or fetched from the online database.");
			d["CloudUserControl.toolStrip1"] = new Text("Cloud actions",
				"Sends and fetches settings, and clears the queue.");
			d["CloudUserControl.toolStripButton1"] = new Text("Refresh",
				"Reads the queue again.");
			d["CloudUserControl.UploadToCloudButton"] = new Text("Upload To Cloud",
				"Sends your settings to the online database.");
			d["CloudUserControl.DownloadFromCloudButton"] = new Text("Download From Cloud",
				"Fetches settings other people have shared.");
			d["CloudUserControl.DeleteButton"] = new Text("Delete",
				"Removes the selected task from the queue.");
			d["CloudUserControl.NextRunLabel"] = new Text("Next run",
				"How long until the queue is worked through again.");
			d["CloudUserControl.RunStateLabel"] = new Text("Queue state",
				"Whether the queue is running, waiting, or stopped.");

			d["IssuesUserControl.WarningsDataGridView"] = new Text("Issues",
				"Problems the program found, with what to do about each one.");
			d["IssuesUserControl.GamesToolStrip"] = new Text("Issue actions",
				"Hides issues you have decided to live with.");
			d["IssuesUserControl.IgnoreButton"] = new Text("Ignore",
				"Stops reporting the selected issue.");
			d["IssuesUserControl.IgnoreAllButton"] = new Text("Ignore All",
				"Stops reporting every issue listed.");
			d["IssuesUserControl.ExceptionInfoButton"] = new Text("Exception Info",
				"Shows the fault behind the selected issue in full.");
			d["IssuesUserControl.StatusLabel"] = new Text("Check state",
				"What the program is checking right now.");
			d["IssuesUserControl.NextRunLabel"] = new Text("Next check",
				"How long until the checks run again.");
		}

		static void AddAbout(Dictionary<string, Text> d)
		{
			d["AboutControl.AboutTabControl"] = new Text("About pages",
				"What changed in each release, and the licence.");
			d["AboutControl.ChangesTabPage"] = new Text("Changes",
				"What changed in each release.");
			d["AboutControl.ChangeLogTextBox"] = new Text("Change log",
				"What changed in each release.");
			d["AboutControl.LicenseTabPage"] = new Text("License",
				"Terms this program is given under.");
			d["AboutControl.LicenseTextBox"] = new Text("Licence text",
				"Terms this program is given under.");
			d["AboutControl.x360ceLinkLabel"] = new Text("Program website", null);
			d["AboutControl.GoogleProjectLinkLabel"] = new Text("Source code", null);
			d["AboutControl.AboutJocysLinkLabel"] = new Text("Publisher website", null);
			d["AboutControl.AboutVirusLinkLabel"] = new Text("Contributor", null);
			d["AboutControl.AboutViGEmLinkLabel"] = new Text("Virtual controller driver author", null);
			d["AboutControl.AboutTocaEditLinkLabel"] = new Text("Original author", null);
		}
	}
}
