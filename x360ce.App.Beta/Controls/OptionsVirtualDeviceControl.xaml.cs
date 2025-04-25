using JocysCom.ClassLibrary.Controls;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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

		private void MainTabControl_SelectionChanged(object sender, EventArgs e)
		{
			var window = Global._MainWindow;
			if (window == null)
				return;
			var isSelected =
				window.MainBodyPanel.MainTabControl.SelectedItem == window.MainBodyPanel.OptionsTabPage &&
				window.OptionsPanel.MainTabControl.SelectedItem == window.OptionsPanel.RemoteControllerTabPage;
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

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
			Global._MainWindow.MainBodyPanel.MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
			Global._MainWindow.OptionsPanel.MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;

			var bytes = JocysCom.ClassLibrary.Helper.FindResource<byte[]>("Documents.Help_ViGEmBus.rtf");
			ControlsHelper.SetTextFromResource(HelpRichTextBox, bytes);

			// Bind Controls.
			var o = SettingsManager.Options;
			PollingRateComboBox.ItemsSource = Enum.GetValues(typeof(UpdateFrequency));
			SettingsManager.LoadAndMonitor(o, nameof(o.PollingRate), PollingRateComboBox);
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
			SettingsManager.UnLoadMonitor(PollingRateComboBox);
			PollingRateComboBox.ItemsSource = null;
		}
	}
}
