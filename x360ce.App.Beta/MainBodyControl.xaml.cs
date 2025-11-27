using JocysCom.ClassLibrary.Controls;
using System;
//using System.Drawing;
using System.Linq;
//using System.Windows;

//using System.Reflection.Emit;
using System.Windows.Controls;
using System.Windows.Media;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.App
{
	/// <summary>
	/// Interaction logic for MainBodyControl.xaml
	/// </summary>
	public partial class MainBodyControl : UserControl
	{
		public MainBodyControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
			PadIcons = new ContentControl[]
			{
				Pad1TabIcon,
				Pad2TabIcon,
				Pad3TabIcon,
				Pad4TabIcon,
			};
			PadColors = new Color[4];
			PadControls = new PadControl[]
			{
				Pad1Panel,
				Pad2Panel,
				Pad3Panel,
				Pad4Panel,
			};
			Global.UpdateControlFromStates += Global_UpdateControlFromStates;
		}

		public PadControl[] PadControls;

		ContentControl[] PadIcons;
		Color[] PadColors;

		public void SetIconColor(int index, Color color)
		{
			if (PadColors[index] == color)
				return;
			PadColors[index] = color;
			var icon = PadIcons[index];
			var resource = Icons_Default.Current[Icons_Default.Icon_square_grey];
			if (color == Colors.Red)
				resource = Icons_Default.Current[Icons_Default.Icon_square_red];
			if (color == Colors.Green)
				resource = Icons_Default.Current[Icons_Default.Icon_square_green];
			if (color == Colors.Blue)
				resource = Icons_Default.Current[Icons_Default.Icon_square_blue];
			if (color == Colors.Yellow)
				resource = Icons_Default.Current[Icons_Default.Icon_square_yellow];
			icon.Content = resource;
		}

		#region ■ Show/Hide tabs.

		public void ShowTab(bool show, TabItem page)
		{
			// Hide TabPage instead of removing, otherwise Unload event won't trigger.
			page.Visibility = show ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
			//var tc = MainTabControl;
			//// If must hide then...
			//if (!show && tc.Items.Contains(page))
			//{
			//	// Hide and return.
			//	tc.Items.Remove(page);
			//	return;
			//}
			//// If must show then..
			//if (show && !tc.Items.Contains(page))
			//{
			//	// Create list of tabs to maintain same order when hiding and showing tabs.
			//	var tabs = new List<TabItem>() {
			//		ProgramsTabPage,
			//		SettingsTabPage,
			//		DevicesTabPage,
			//	};
			//	// Get index of always displayed tab.
			//	var index = tc.Items.IndexOf(GamesTabPage);
			//	// Get tabs in front of tab which must be inserted.
			//	var tabsBefore = tabs.Where(x => tabs.IndexOf(x) < tabs.IndexOf(page));
			//	// Count visible tabs.
			//	var countBefore = tabsBefore.Count(x => tc.Items.Contains(x));
			//	tc.Items.Insert(index + countBefore + 1, page);
			//}
		}

		public void ShowProgramsTab(bool show)
		{
			ShowTab(show, ProgramsTabPage);
		}

		public void ShowSettingsTab(bool show)
		{
			ShowTab(show, SettingsTabPage);
		}

		public void ShowDevicesTab(bool show)
		{
			ShowTab(show, DevicesTabPage);
		}

		#endregion

		#region ■ Issue Icon Timer

		//public System.Timers.Timer IssueIconTimer;

		//private void InitIssuesIcon()
		//{
		//	IssueIconTimer = new System.Timers.Timer
		//	{
		//		AutoReset = false,
		//		Interval = 1000
		//	};
		//	IssueIconTimer.Elapsed += IssueIconTimer_Elapsed;
		//	IssueIconTimer.Start();
		//}

		//private void IssueIconTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		//{
		//	ControlsHelper.BeginInvoke(() => {
		//		var key = IssuesTabPage.ImageKey;
		//		var moderateCount = IssuesPanel.ModerateIssuesCount;
		//		var criticalCount = IssuesPanel.CriticalIssuesCount ?? 0;
		//		var text = (moderateCount ?? 0) == 0
		//			? "Issues"
		//			: string.Format("{0} Issue{1}", moderateCount, moderateCount == 1 ? "" : "s");
		//		// If unknown then...
		//		if (!moderateCount.HasValue)
		//		{
		//			// Show refreshing icon.
		//			key = "refresh_16x16.png";
		//		}
		//		// If critical issues found then...
		//		if (criticalCount > 0)
		//		{
		//			// Make it blink.
		//			key = key == "fix_16x16.png"
		//				? "fix_off_16x16.png"
		//				: "fix_16x16.png";
		//		}
		//		else if (moderateCount > 0)
		//			key = "fix_16x16.png";
		//		else
		//			key = "ok_off_16x16.png";
		//		// Set tab image.
		//		if (IssuesTabPage.ImageKey != key)
		//			IssuesTabPage.ImageKey = key;
		//		// Set tab text.
		//		ControlsHelper.SetText(IssuesTabPage, text);
		//		if (Program.IsClosing)
		//			return;
		//		IssueIconTimer.Start();
		//	});
		//}

		#endregion

		private void Global_UpdateControlFromStates(object sender, EventArgs e)
		{
			var currentGameFileName = SettingsManager.CurrentGame?.FileName;
			var client = Nefarius.ViGEm.Client.ViGEmClient.Current;
			for (var i = 0; i < 4; i++)
			{
				var padControl = PadControls[i];
				// Get devices mapped to game and specific controller index.
				var devices = SettingsManager.GetDevices(currentGameFileName, (MapTo)(i + 1));
				// DInput instance is ON if active devices found.
				var diOn = devices.Count(x => x.IsOnline) > 0;
				// XInput instance is ON.
				var xiOn = client != null && client.IsControllerConnected((uint)i + 1);
				// Update LED of GamePad state.
				var image = diOn
					// DInput ON, XInput ON 
					? xiOn ? System.Windows.Media.Colors.Green
					// DInput ON, XInput OFF
					: System.Windows.Media.Colors.Red
					// DInput OFF, XInput ON
					: xiOn ? System.Windows.Media.Colors.Yellow
					// DInput OFF, XInput OFF
					: System.Windows.Media.Colors.Gray;
				SetIconColor(i, image);
			}
		}
		private void Global_AddGame(object sender, EventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				MainTabControl.SelectedItem = GamesTabPage;
				GamesPanel.ListPanel.AddNewGame();
			});
		}

		bool HelpInit;

		private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MainTabControl.SelectedItem == HelpTabPage && !HelpInit)
			{
				HelpInit = true;
				// Build updated help content with HidHide replacing HID Guardian.
				var help = string.Join("\n", new[] {
					"Xbox 360 Controller Emulator 4.x (uses ViGEmBus Virtual Gamepad Emulation Driver)",
					"If you want HELP and have questions about installation or configuration, please go to:",
					"X360CE Home Page: https://www.x360ce.com",
					"NGEmu X360CE Forum: https://www.ngemu.com/forums/x360ce.140/",
					"Solutions and tutorials on Google: https://www.google.com/search?q=x360ce",
					"Solutions and tutorials on YouTube: https://www.youtube.com/results?search_query=x360ce",
					"ViGEm Homepage: http://github.com/ViGEm/ViGEmBus",
					"HidHide Homepage: https://github.com/nefarius/HidHide",
					"",
					"IMPORTANT Notes",
					"1. There is no need to place x360ce.exe inside the game folder. You can keep single copy at one place on your PC.",
					"For example: C:\\Program Files\\x360ce\\x360ce.exe",
					"2. Do not close X360CE 4.x during the game, just minimise it to reduce CPU use.",
					"3. Make sure your game is set to use XInput Devices.",
					"For example: In \"Tom Clancy's Ghost Recon Wildlands\" you have to set:",
					"OPTIONS > CONTROLLER > ENABLE CONTROLLER: ONLY GAMEPADS",
					"",
					"Install and Use Instructions",
					"1. Download latest X360CE (same file for 32-bit and 64-bit Windows).",
					"2. Extract downloaded ZIP file and launch x360ce.exe.",
					"",
					"Installing ViGEmBus Virtual Gamepad Emulation Driver",
					"[Issues] tab in X360CE will start blinking if ViGEmBus Driver is missing.",
					"1. Select [Issues] tab and click on [Install] button to install ViGEmBus Driver.",
					"",
					"Adding DirectInput Device (Controller)",
					"1. Connect your DirectInput Device (controller) to computer.",
					"2. Select [Controller 1] tab and click on [Add...] button.",
					"4. Select controller you want to add-map and click on [OK] button.",
					"5. Enable controller by clicking on  [Enable # Mapped Device] inside [Controller 1] tab.",
					"",
					"Configuring and Mapping Buttons and Axes",
					"1. Select [Controller 1] tab → [General] tab.",
					"2. Click on [drop-down] (drop-down menu with options will appear).",
					"3. Map button or axis by selecting [Record] option and pressing button or moving axis on your controller.",
					"4. Click [Save All] button (at top right corner of application) when done.",
					"5. Minimise X360CE in order to reduce CPU use (program icon will be visible in tray).",
					"6. Launch the game and see how it works.",
					"",
					"How to Install or Uninstall ViGEmBus Virtual Gamepad Emulation Driver",
					"Install: [Options] tab → [Virtual Device] tab → ViGEm Bus [Install] button.",
					"Uninstall: [Options] tab → [Virtual Device] tab → ViGEm Bus [Uninstall] button.",
					"",
					"How to Hide Original DirectInput Devices (HidHide)",
					"Purpose of HidHide is to hide original controllers from games so that only virtual controllers are visible.",
					"Use HidHide if the original controller prevents the virtual controller from functioning properly in the game.",
					"Install: [Options] tab → [HidHide] tab → [Install HidHide] button (opens installer).",
					"Uninstall: Use Windows Settings → Apps → HidHide → Uninstall.",
					"Manual help and troubleshooting: https://github.com/nefarius/HidHide",
					"",
					"Problem: Application has failed to start because MSVCR100.dll was not found.",
					"Reason: Microsoft Visual C++ 2010 Redistributable Package is missing.",
					"Solution: Download and install Microsoft Visual C++ 2010 Redistributable Packages:",
					"(x86): https://www.microsoft.com/en-us/download/details.aspx?id=5555",
					"(x64): https://www.microsoft.com/en-us/download/details.aspx?id=14632",
					"Note: You must install both packages on Windows 64-bit!",
					"",
					"Wheel doesn't work in the game, but it works inside x360ce Application.",
					"Some games work only when controller is disguised as GamePad even if its Wheel. Try to:",
					"1. Run x360ce.exe.",
					"2. Select [tab] with your Wheel Controller.",
					"3. Open [Advanced] tab page.",
					"4. Set \"Device Type\" [drop-down] list value to: [GamePad].",
					"5. Click [Save] button.",
					"",
					"How to reduce wheel dead zone?",
					"1. Run x360ce.exe.",
					"2. Select [tab] with your Wheel Controller.",
					"3. Open [Advanced] tab page.",
					"4. Select \"Enabled (XInput, 80%)\" from \"AntiDeadZone\" [drop-down] to reduce dead zone by 80%.",
					"5. Click [Save] button.",
					"Note: Some games have control issues when deadzone is reduced by 100%.",
					"",
					"Gas and brake pedals are combined. How can I separate them?",
					"Solution 1: If you have Logitech wheel:",
					"1. Open \"Logitech Profiler\" Tool.",
					"2. From menu open: Device → Game Controllers...",
					"3. Select your controller and click [Properties] button.",
					"4. Select [Test] tab and click [Settings] button.",
					"5. Uncheck \"[x] Combined (single axis - used for most games)\" option.",
					"6. Click [Close] → [OK] → [OK] buttons.",
					"Solution 2: If you can't separate pedals:",
					"1. Open X360CE.",
					"2. Set LEFT \"Trigger\" [drop-down] value to: Sliders → Half → HSlider 1.",
					"3. Set RIGHT \"Trigger\" [drop-down] value to: Sliders → Inverted Half → IHSlider 1.",
					"4. Test pedals.",
					"",
					"What are real life steering wheel degrees?",
					"1080° (3.0 x 360°) - Heavy cars, trucks.",
					"900° (2.5 x 360°) - Average road cars, sports cars.",
					"720° (2.0 x 360°) - Drift cars. Multiple classes of Rally cars (group N).",
					"540° (1.5 x 360°) - GT1 and 3 spec race cars, WRC Rally cars.",
					"360° (1.0 x 360°) - Formula 1 cars.",
				});
				ControlsHelper.SetText(HelpRichTextBox, help);
			}
			else if (MainTabControl.SelectedItem == SettingsTabPage)
			{
				var o = SettingsManager.Options;
				if (o.InternetFeatures && o.InternetAutoLoad)
				{
					//SettingsDatabasePanel.RefreshGrid(true);
				}
			}
		}

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
			Global.AddGame += Global_AddGame;
		}

		private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			Global.AddGame -= Global_AddGame;
			// Dispose managed resources.
			Global.UpdateControlFromStates -= Global_UpdateControlFromStates;
			Array.Clear(PadControls, 0, 4);
			PadIcons?.ToList().ForEach(x => x.Content = null);
			Array.Clear(PadIcons, 0, 4);
			Array.Clear(PadColors, 0, 4);
			MainTabControl.Items.Clear();
		}

	}
}
