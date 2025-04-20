using JocysCom.ClassLibrary.Controls;
using System;
//using System.Drawing;
using System.Linq;
using System.Windows;

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
				var bytes = JocysCom.ClassLibrary.Helper.FindResource<byte[]>("Documents.Help.rtf");
				ControlsHelper.SetTextFromResource(HelpRichTextBox, bytes);
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
