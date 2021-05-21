using JocysCom.ClassLibrary.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for OptionsVirtualDeviceControl.xaml
	/// </summary>
	public partial class OptionsVirtualDeviceControl : UserControl
	{
		public OptionsVirtualDeviceControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			Global._MainWindow.MainBodyPanel.MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
			Global._MainWindow.OptionsPanel.MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
			ControlsHelper.SetTextFromResource(HelpRichTextBox, "Documents.Help_ViGEmBus.rtf");
			// Bind Controls.
			var o = SettingsManager.Options;
			PollingRateComboBox.ItemsSource = Enum.GetValues(typeof(UpdateFrequency));
			SettingsManager.LoadAndMonitor(o, nameof(o.PollingRate), PollingRateComboBox);
			RefreshStatus();
		}

		private void MainTabControl_SelectionChanged(object sender, EventArgs e)
		{
			var isSelected =
				Global._MainWindow.MainBodyPanel.MainTabControl.SelectedItem == Global._MainWindow.MainBodyPanel.OptionsTabPage &&
				Global._MainWindow.OptionsPanel.MainTabControl.SelectedItem == Global._MainWindow.OptionsPanel.RemoteControllerTabPage;
			// If HidGuardian Tab was selected then refresh.
			if (isSelected)
				RefreshStatus();
		}

		private void InstallButton_Click(object sender, RoutedEventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				StatusTextBox.Text = "Installing. Please Wait...";
				DInput.DInputHelper.CheckInstallVirtualDriver();
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
				// Disable Virtual mode first.
				Global._MainWindow.ChangeCurrentGameEmulationType(EmulationType.None);
				DInput.DInputHelper.CheckUnInstallVirtualDriver();
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
				var bus = DInput.VirtualDriverInstaller.GetViGemBusDriverInfo();
				ControlsHelper.BeginInvoke(() =>
				{
					// Update Bus status.
					var busStatus = bus.DriverVersion == 0
						? "Not installed"
						: string.Format("{0} {1}", bus.Description, bus.GetVersion());
					ControlsHelper.SetText(StatusTextBox, busStatus);
					InstallButton.IsEnabled = bus.DriverVersion == 0;
					UninstallButton.IsEnabled = bus.DriverVersion != 0;
				});
			});
			var t = new System.Threading.Thread(ts);
			t.Start();
		}

	}
}
