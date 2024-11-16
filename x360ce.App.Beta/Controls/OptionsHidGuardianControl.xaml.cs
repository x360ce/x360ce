using JocysCom.ClassLibrary.Controls;
using System.Windows;
using System.Windows.Controls;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for OptionsHidGuardianControl.xaml
	/// </summary>
	public partial class OptionsHidGuardianControl : UserControl
	{
		public OptionsHidGuardianControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
		}

		private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var window = Global._MainWindow;
			if (window == null)
				return;
			var isSelected =
				window.MainBodyPanel.MainTabControl.SelectedItem == window.MainBodyPanel.OptionsTabPage &&
				window.OptionsPanel.MainTabControl.SelectedItem == window.OptionsPanel.HidGuardianTabPage;
			// If HidGuardian Tab was selected then refresh.
			if (isSelected)
				RefreshStatus();
		}

		private void InstallButton_Click(object sender, RoutedEventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				StatusTextBox.Text = "Installing. Please Wait...";
				Program.RunElevated(AdminCommand.InstallHidGuardian);
				ViGEm.HidGuardianHelper.InsertCurrentProcessToWhiteList();
				RefreshStatus();
			});
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			RefreshStatus();
		}

		private void UninstallButton_Click(object sender, RoutedEventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				StatusTextBox.Text = "Uninstalling. Please Wait...";
				Program.RunElevated(AdminCommand.UninstallHidGuardian);
				RefreshStatus();
			});
		}

		void RefreshStatus()
		{
			ControlsHelper.SetText(StatusTextBox, "Please wait...");
			// run in another thread, to make sure it is not freezing interface.
			var ts = new System.Threading.ThreadStart(delegate ()
			{
				// Get Virtual Bus and HID Guardian status.
				var hid = DInput.VirtualDriverInstaller.GetHidGuardianDriverInfo();
				ControlsHelper.BeginInvoke(() =>
				{
					// Update HID status.
					var hidStatus = hid.DriverVersion == 0
						? "Not installed"
						: string.Format("{0} {1}", hid.Description, hid.GetVersion());
					ControlsHelper.SetText(StatusTextBox, hidStatus);
					InstallButton.IsEnabled = hid.DriverVersion == 0;
					UninstallButton.IsEnabled = hid.DriverVersion != 0;
				});
			});
			var t = new System.Threading.Thread(ts);
			t.Start();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
			Global._MainWindow.MainBodyPanel.MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
			Global._MainWindow.OptionsPanel.MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
			var bytes = JocysCom.ClassLibrary.Helper.FindResource<byte[]>("Documents.Help_HidGuardian.rtf");
			ControlsHelper.SetTextFromResource(HelpRichTextBox, bytes);
			// Bind Controls.
			var o = SettingsManager.Options;
			SettingsManager.LoadAndMonitor(o, nameof(o.HidGuardianConfigureAutomatically), HidGuardianConfigureAutomaticallyCheckBox);
			RefreshStatus();
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			TabControl tc;
			tc = Global._MainWindow?.MainBodyPanel?.MainTabControl;
			if (tc != null)
				tc.SelectionChanged -= MainTabControl_SelectionChanged;
			tc = Global._MainWindow?.OptionsPanel?.MainTabControl;
			if (tc != null)
				tc.SelectionChanged -= MainTabControl_SelectionChanged;
			SettingsManager.UnLoadMonitor(HidGuardianConfigureAutomaticallyCheckBox);
		}
	}
}
