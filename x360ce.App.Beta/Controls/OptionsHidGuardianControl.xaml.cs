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
			MainForm.Current.MainTabControl.SelectedIndexChanged += MainTabControl_SelectedIndexChanged;
			SettingsManager.LoadAndMonitor(x => x.HidGuardianConfigureAutomatically, HidGuardianConfigureAutomaticallyCheckBox);

			ControlsHelper.SetTextFromResource(HelpRichTextBox, "Documents.Help_HidGuardian.rtf");
		}

		private void MainTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (MainForm.Current.MainTabControl.SelectedTab == MainForm.Current.OptionsPanel.Parent)
			{
				RefreshHidGuardianStatus();
			}
		}

		private void HidGuardianInstallButton_Click(object sender, RoutedEventArgs e)
		{
			HidGuardianTextBox.Text = "Installing. Please Wait...";
			Program.RunElevated(AdminCommand.InstallHidGuardian);
			ViGEm.HidGuardianHelper.InsertCurrentProcessToWhiteList();
			RefreshHidGuardianStatus();
		}

		private void HidGuardianRefreshButton_Click(object sender, RoutedEventArgs e)
		{
			RefreshHidGuardianStatus();
		}

		private void HidGuardianUninstallButton_Click(object sender, RoutedEventArgs e)
		{
			HidGuardianTextBox.Text = "Uninstalling. Please Wait...";
			Program.RunElevated(AdminCommand.UninstallHidGuardian);
			RefreshHidGuardianStatus();
		}

		void RefreshHidGuardianStatus()
		{
			ControlsHelper.SetText(HidGuardianTextBox, "Please wait...");
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
					ControlsHelper.SetText(HidGuardianTextBox, hidStatus);
					HidGuardianInstallButton.IsEnabled = hid.DriverVersion == 0;
					HidGuardianUninstallButton.IsEnabled = hid.DriverVersion != 0;
				});
			});
			var t = new System.Threading.Thread(ts);
			t.Start();
		}

	}
}
