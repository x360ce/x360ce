using JocysCom.ClassLibrary.Controls;
using System;
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
			InitializeComponent();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			MainForm.Current.MainTabControl.SelectedIndexChanged += MainTabControl_SelectionChanged;
			MainForm.Current.OptionsPanel.MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
			ControlsHelper.SetTextFromResource(HelpRichTextBox, "Documents.Help_HidGuardian.rtf");
			// Bind Controls.
			var o = SettingsManager.Options;
			SettingsManager.LoadAndMonitor(o, nameof(o.HidGuardianConfigureAutomatically), HidGuardianConfigureAutomaticallyCheckBox);
			RefreshStatus();
		}

		private void MainTabControl_SelectionChanged(object sender, EventArgs e)
		{
			var isSelected =
				MainForm.Current.MainTabControl.SelectedTab == MainForm.Current.OptionsTabPage &&
				MainForm.Current.OptionsPanel.MainTabControl.SelectedItem == MainForm.Current.OptionsPanel.HidGuardianTabPage;
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

	}
}
