using JocysCom.ClassLibrary.Controls.IssuesControl;
using System.Collections.Generic;
using x360ce.App.Controls;
using x360ce.App.Forms;
using static JocysCom.ClassLibrary.Controls.UiTree.UiText;

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
			d[nameof(GamesGridUserControl) + "." + nameof(GamesGridUserControl.GamesDataGridView)] = new Text("Games",
				"Games this program is set up for. The tick says whether it is switched on.");
			d[nameof(GamesGridUserControl) + "." + nameof(GamesGridUserControl.GamesToolStrip)] = new Text("Game actions",
				"Finds, adds, starts and removes games.");
			d[nameof(GamesGridUserControl) + "." + nameof(GamesGridUserControl.ScanGamesButton)] = new Text("Scan",
				"Searches the folders listed in Options for games it knows.");
			d[nameof(GamesGridUserControl) + "." + nameof(GamesGridUserControl.AddGameButton)] = new Text("Add...",
				"Adds a game by choosing its program file.");
			d[nameof(GamesGridUserControl) + "." + nameof(GamesGridUserControl.DeleteGamesButton)] = new Text("Delete",
				"Removes the selected game from the list.");
			d[nameof(GamesGridUserControl) + "." + nameof(GamesGridUserControl.SaveGamesButton)] = new Text("Save",
				"Writes the settings file the selected game reads.");
			d[nameof(GamesGridUserControl) + "." + nameof(GamesGridUserControl.StartGameButton)] = new Text("Start",
				"Runs the selected game.");
			d[nameof(GamesGridUserControl) + "." + nameof(GamesGridUserControl.OpenGameButton)] = new Text("Open...",
				"Opens the folder the selected game is installed in.");
			d[nameof(GamesGridUserControl) + "." + nameof(GamesGridUserControl.ShowGamesDropDownButton)] = new Text("Show",
				"Limits the list to games that are switched on, or off.");
			d[nameof(GamesGridUserControl) + "." + nameof(GamesGridUserControl.ShowAllGamesMenuItem)] = new Text("Show: All", null);
			d[nameof(GamesGridUserControl) + "." + nameof(GamesGridUserControl.ShowEnabledGamesMenuItem)] = new Text("Show: Enabled", null);
			d[nameof(GamesGridUserControl) + "." + nameof(GamesGridUserControl.ShowDisabledGamesMenuItem)] = new Text("Show: Disabled", null);
		}

		/// <summary>How one game is set up: which libraries are replaced, and how.</summary>
		static void AddGameDetails(Dictionary<string, Text> d)
		{
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.XInputMaskGroupBox)] = new Text("XInput files",
				"Which XInput library files this program supplies to the game.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.DInputMaskGroupBox)] = new Text("DInput file",
				"Whether the Direct Input library is supplied to the game as well.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.AutoMapMaskGroupBox)] = new Text("Auto map",
				"Not available yet, so greyed out: the program does not map devices to controllers by itself.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.OtherOptionsGroupBox)] = new Text("Other options",
				"How the game is started, and what it is told about the controller.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.ProcessorArchitectureComboBox)] = new Text("Architecture",
				"Whether the game is a 32-bit or a 64-bit program.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.EmulationTypeComboBox)] = new Text("Emulation",
				"Whether the controller is presented by the virtual driver or by replaced library files.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.XInputPathTextBox)] = new Text("XInput path",
				"Folder the XInput library is written into.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.DInputFileTextBox)] = new Text("DInput file",
				"Name of the Direct Input library written for the game.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.HookModeFakeVidTextBox)] = new Text("Fake vendor code",
				"Vendor code reported to a game that only accepts one make of controller.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.HookModeFakeVidNumericUpDown)] = new Text("Fake vendor code",
				"Vendor code reported to a game that only accepts one make of controller.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.HookModeFakePidTextBox)] = new Text("Fake product code",
				"Product code reported to a game that only accepts one model of controller.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.HookModeFakePidNumericUpDown)] = new Text("Fake product code",
				"Product code reported to a game that only accepts one model of controller.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.TimeoutNumericUpDown)] = new Text("Timeout",
				"How long to wait for the game before giving up.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.groupBox5)] = new Text("Help",
				"Searches the web for other people's settings for this game.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.GoogleSearchButton)] = new Text("Search on Google...",
				"Searches the web for this game.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.button3)] = new Text("Search on NGemu...",
				"Searches the NGemu forum for this game.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.button4)] = new Text("Open NGemu...",
				"Opens the NGemu forum.");
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.ActionGroupBox)] = new Text("Action", null);
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.ResetToDefaultButton)] = new Text("Reset to Default",
				"Puts this game's settings back the way they started.");
			AddHookFlags(d);
		}

		/// <summary>The hook mask and library boxes, shared with version 3, and the auto-map boxes only this version has.</summary>
		static void AddHookFlags(Dictionary<string, Text> d)
		{
			d[nameof(GameDetailsUserControl) + "." + nameof(GameDetailsUserControl.HookMaskGroupBox)] = new Text("Hook mask",
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
			d[nameof(UserDevicesUserControl) + "." + nameof(UserDevicesUserControl.DevicesDataGridView)] = new Text("Devices",
				"Every controller the program can see, once for each source it is read through: DirectInput rows first, tinted blue, "
				+ "then Raw Input rows, teal, then plugin devices, violet. Unplugged ones are dimmed. In the list Add opens, each "
				+ "device a loaded plugin offers is listed too, until it has been added.");
			d[nameof(UserDevicesUserControl) + "." + nameof(UserDevicesUserControl.ControllersToolStrip)] = new Text("Device actions",
				"Refreshes the list and works on the selected device.");
			d[nameof(XInputDevicesUserControl) + "." + nameof(XInputDevicesUserControl.DevicesToolStrip)] = new Text("Emulated controller actions",
				"Buttons that act on the emulated controllers shown below.");
			d[nameof(XInputDevicesUserControl) + "." + nameof(XInputDevicesUserControl.DevicesDataGridView)] = new Text("Emulated controllers",
				"Every emulated controller and the XInput place it holds, in the order games see them.");
			d[nameof(XInputDevicesUserControl) + "." + nameof(XInputDevicesUserControl.MoveUpColumn)] = new Text("Move Up",
				"Arrow on each row that moves that controller one place earlier.");
			d[nameof(XInputDevicesUserControl) + "." + nameof(XInputDevicesUserControl.MoveDownColumn)] = new Text("Move Down",
				"Arrow on each row that moves that controller one place later.");
			d[nameof(XInputDevicesUserControl) + "." + nameof(XInputDevicesUserControl.ApplyButton)] = new Text("Apply",
				"Recreates the controllers in the order shown. Needs Administrator.");
			d[nameof(XInputDevicesUserControl) + "." + nameof(XInputDevicesUserControl.AutoOrderButton)] = new Text("Auto-Order",
				"Orders the list so Controller N's virtual controller takes XInput N and real controllers take the places left, then applies it.");
			d[nameof(XInputDevicesUserControl) + "." + nameof(XInputDevicesUserControl.RefreshButton)] = new Text("Refresh",
				"Reads the controllers again, for when one has arrived or left.");
			d[nameof(XInputDevicesUserControl) + "." + nameof(XInputDevicesUserControl.PlanPanel)] = new Text("Order notice",
				"Opens under the list after Apply or Auto-Order: the steps that will be taken, each step as it is taken, then how it went.");
			d[nameof(XInputDevicesUserControl) + "." + nameof(XInputDevicesUserControl.PlanSubjectLabel)] = new Text("Notice subject",
				"What the notice is about: an order waiting for OK, the order being made, or how it went.");
			d[nameof(XInputDevicesUserControl) + "." + nameof(XInputDevicesUserControl.PlanBodyLabel)] = new Text("Notice text",
				"The steps, what happened at each, and why it stopped when it did.");
			d[nameof(XInputDevicesUserControl) + "." + nameof(XInputDevicesUserControl.PlanOkButton)] = new Text("OK",
				"Puts the controllers in the order shown. Once it is over, closes the notice.");
			d[nameof(XInputDevicesUserControl) + "." + nameof(XInputDevicesUserControl.PlanCancelButton)] = new Text("Cancel",
				"Closes the notice without touching any controller.");
			d[nameof(UserDevicesUserControl) + "." + nameof(UserDevicesUserControl.RefreshButton)] = new Text("Refresh",
				"Reads every device again.");
			d[nameof(UserDevicesUserControl) + "." + nameof(UserDevicesUserControl.ControllerDeleteButton)] = new Text("Delete",
				"Forgets the selected device and its settings.");
			d[nameof(UserDevicesUserControl) + "." + nameof(UserDevicesUserControl.HardwareButton)] = new Text("Hardware...",
				"Opens the selected device in Windows Device Manager.");
			d[nameof(UserDevicesUserControl) + "." + nameof(UserDevicesUserControl.TestButton)] = new Text("Test...",
				"Opens the Test Device window for the one connected device selected: a guided test of its sticks, triggers and buttons, graded in plain words. "
				+ "Controllers are paused while the test runs.");
			d[nameof(TestDeviceForm) + "." + nameof(TestDeviceForm.InstructionLabel)] = new Text("Instruction",
				"The one thing to do now. The step and its number are in the window's header.");
			d[nameof(TestDeviceForm) + "." + nameof(TestDeviceForm.CountdownLabel)] = new Text("Countdown",
				"The whole seconds left of a timed step, or of a step that waits without movement; empty without one.");
			d[nameof(TestDeviceForm) + "." + nameof(TestDeviceForm.CountdownCaptionLabel)] = new Text("Countdown caption",
				"What happens when the countdown ends, \"Moving on in\" or \"Finishing in\", while a step waits without movement; empty for a timed step.");
			d[nameof(TestDeviceForm) + "." + nameof(TestDeviceForm.HintLabel)] = new Text("Hint",
				"Why a countdown started again, the control to keep moving until its bar is full, or the next one.");
			d[nameof(TestDeviceForm) + "." + nameof(TestDeviceForm.ControlsFlowLayoutPanel)] = new Text("Sticks and triggers",
				"Each stick and axis while it is moved: a live picture, a progress bar and how full it is.");
			d[nameof(TestDeviceForm) + "." + nameof(TestDeviceForm.CardsFlowLayoutPanel)] = new Text("Results",
				"The overall grade and its weakest result, then each result with its value, grade and what it tells.");
			d[nameof(TestDeviceForm) + "." + nameof(TestDeviceForm.ButtonsHeadingLabel)] = new Text("Buttons",
				"How many buttons were pressed, which never were, and which registered one push twice; shown, not graded.");
			d[nameof(TestDeviceForm) + "." + nameof(TestDeviceForm.SummaryLabel)] = new Text("Rates",
				"The report rate while moving, the time between reports, the values a report carries, and the read rate.");
			d[nameof(TestDeviceForm) + "." + nameof(TestDeviceForm.ResultsDataGridView)] = new Text("Axes",
				"One row for each axis and slider: its range, centre, rest noise, the dead zone it needs, resolution, steps and update rate.");
			d[nameof(TestDeviceForm) + "." + nameof(TestDeviceForm.LimitsLabel)] = new Text("Limits",
				"What the test cannot measure.");
			d[nameof(TestDeviceForm) + "." + nameof(TestDeviceForm.StartButton)] = new Text("Start",
				"The large button under the instruction: starts the test, which then moves on by itself; reads Stop while it runs, which ends it at once, and Test again after the results.");
			d[nameof(TestDeviceForm) + "." + nameof(TestDeviceForm.SkipButton)] = new Text("Skip",
				"Beside Stop while the test waits on sticks, triggers or buttons: ends the step at once, and what is not done is marked skipped and not graded.");
			d[nameof(TestDeviceForm) + "." + nameof(TestDeviceForm.NotGradedLabel)] = new Text("Not graded",
				"The controls left out of the grades, and why: never moved, skipped, or unstable, a control that kept changing without getting fuller.");
			d[nameof(TestDeviceForm) + "." + nameof(TestDeviceForm.CloseButton)] = new Text("Close",
				"Closes the window, which resumes the controllers.");
			d[nameof(UserDevicesUserControl) + "." + nameof(UserDevicesUserControl.AddDemoDevice)] = new Text("Add Plugin Device",
				"Adds a device a plugin offers, such as the Test Controller, a pretend controller for trying the program without hardware. "
				+ "With several plugin devices it opens a list to pick from.");
			d[nameof(UserDevicesUserControl) + "." + nameof(UserDevicesUserControl.CleanupVirtualPadsButton)] = new Text("Remove Leftover Pads",
				"Removes emulated controllers left behind by runs that ended badly. Needs Administrator.");
			d[nameof(UserDevicesUserControl) + "." + nameof(UserDevicesUserControl.ShowSystemDevicesButton)] = new Text("Show System Devices",
				"Lists devices Windows files as system devices too, such as a Logitech G13, so they can be mapped. Shown when choosing a device for a controller.");
			d[nameof(UserDevicesUserControl) + "." + nameof(UserDevicesUserControl.HidHideDropDownButton)] = new Text("HID Hide",
				"Hides devices from games through HID Hide, which must be installed: see the HID Hide page on the Options tab.");
			d[nameof(UserDevicesUserControl) + "." + nameof(UserDevicesUserControl.GamingDevicesMenuItem)] = new Text("Show Gaming Devices",
				"Lists the devices HID Hide counts as game controllers, with the path it knows each by.");
			d[nameof(UserDevicesUserControl) + "." + nameof(UserDevicesUserControl.HiddenDevicesMenuItem)] = new Text("Show Hidden Devices",
				"Lists the devices HID Hide is hiding from games.");
			d[nameof(UserDevicesUserControl) + "." + nameof(UserDevicesUserControl.UnhideAllDevicesMenuItem)] = new Text("Unhide All Devices",
				"Makes every device HID Hide hides visible to games again, and clears every Hide box.");
			d[nameof(UserDevicesUserControl) + "." + nameof(UserDevicesUserControl.SynchronizeToHidHideMenuItem)] = new Text("Synchronize To HID Hide",
				"Makes HID Hide hide exactly the devices whose Hide box is ticked, and lets this program see them.");
			d[nameof(UserDevicesUserControl) + "." + nameof(UserDevicesUserControl.OpenHidHideMenuItem)] = new Text("Open HID Hide",
				"Opens HID Hide's own program, or its download page while it is not installed.");
		}

		static void AddCloudAndIssues(Dictionary<string, Text> d)
		{
			d[nameof(CloudUserControl) + "." + nameof(CloudUserControl.TasksDataGridView)] = new Text("Cloud tasks",
				"Settings waiting to be sent to or fetched from the online database. A task that fails waits longer "
				+ "before each try and stops after its last one; point at it to read why.");
			d[nameof(CloudUserControl) + "." + nameof(CloudUserControl.toolStrip1)] = new Text("Cloud actions",
				"Sends and fetches settings, and clears the queue.");
			d[nameof(CloudUserControl) + "." + nameof(CloudUserControl.RetryButton)] = new Text("Retry",
				"Sends the tasks that failed again now, each with all its tries.");
			d[nameof(CloudUserControl) + "." + nameof(CloudUserControl.UploadToCloudButton)] = new Text("Upload To Cloud",
				"Sends your settings to the online database.");
			d[nameof(CloudUserControl) + "." + nameof(CloudUserControl.DownloadFromCloudButton)] = new Text("Download From Cloud",
				"Fetches settings other people have shared.");
			d[nameof(CloudUserControl) + "." + nameof(CloudUserControl.DeleteButton)] = new Text("Delete",
				"Removes the selected task from the queue.");
			d[nameof(CloudUserControl) + "." + nameof(CloudUserControl.NextRunLabel)] = new Text("Next run",
				"How long until a task that failed is sent again, none while no task waits, and whether the network is on.");
			d[nameof(CloudUserControl) + "." + nameof(CloudUserControl.RunStateLabel)] = new Text("Queue state",
				"Whether the queue is running, waiting, or stopped.");

			d[nameof(IssuesUserControl) + "." + nameof(IssuesUserControl.WarningsDataGridView)] = new Text("Issues",
				"Problems the program found, with what to do about each one.");
			d[nameof(IssuesUserControl) + "." + nameof(IssuesUserControl.GamesToolStrip)] = new Text("Issue actions",
				"Hides issues you have decided to live with.");
			d[nameof(IssuesUserControl) + "." + nameof(IssuesUserControl.IgnoreButton)] = new Text("Ignore",
				"Stops reporting the selected issue.");
			d[nameof(IssuesUserControl) + "." + nameof(IssuesUserControl.IgnoreAllButton)] = new Text("Ignore All",
				"Stops reporting every issue listed.");
			d[nameof(IssuesUserControl) + "." + nameof(IssuesUserControl.ExceptionInfoButton)] = new Text("Exception Info",
				"Shows the fault behind the selected issue in full.");
			d[nameof(IssuesUserControl) + "." + nameof(IssuesUserControl.StatusLabel)] = new Text("Check state",
				"What the program is checking right now.");
			d[nameof(IssuesUserControl) + "." + nameof(IssuesUserControl.NextRunLabel)] = new Text("Next check",
				"How long until the checks run again.");
			d[nameof(IssuesUserControl) + "." + nameof(IssuesUserControl.ChecksButton)] = new Text("Run Checks",
				"Whether the checks run every few seconds. Off, no check runs and Next Run says off.");
		}

		static void AddAbout(Dictionary<string, Text> d)
		{
			d[nameof(AboutControl) + "." + nameof(AboutControl.AboutTabControl)] = new Text("About pages",
				"The program and who made it, what changed in each release, and the licence.");
			d[nameof(AboutControl) + "." + nameof(AboutControl.ProgramTabPage)] = new Text("Program",
				"What the program is, its website, source code and forum, who made it, and the projects and fonts it carries, with links.");
			d[nameof(AboutControl) + "." + nameof(AboutControl.ProgramRichTextBox)] = new Text("Program details",
				"What the program is, its website, source code and forum, who made it, and the projects and fonts it carries, with links.");
			d[nameof(AboutControl) + "." + nameof(AboutControl.ChangesTabPage)] = new Text("Changes",
				"What changed in each release.");
			d[nameof(AboutControl) + "." + nameof(AboutControl.ChangeLogTextBox)] = new Text("Change log",
				"What changed in each release.");
			d[nameof(AboutControl) + "." + nameof(AboutControl.LicenseTabPage)] = new Text("License",
				"Terms this program is given under.");
			d[nameof(AboutControl) + "." + nameof(AboutControl.LicenseTextBox)] = new Text("Licence text",
				"Terms this program is given under.");
		}
	}
}
