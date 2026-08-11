using JocysCom.ClassLibrary.Controls;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using System.IO;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Documents;
using x360ce.Engine;
using x360ce.App.Issues;

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
			if (!ViGEmBusSupport.OpenDriverHelp(out var error))
				StatusTextBox.Text = "Could not open driver help: " + error;
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			RefreshStatus();
		}

		private void UninstallButton_Click(object sender, RoutedEventArgs e)
		{
			StatusTextBox.Text = "ViGEmBus is managed by Windows and its external installer.";
		}

		CancellationTokenSource healthCancellation;

		async void RefreshStatus()
		{
			healthCancellation?.Cancel();
			healthCancellation?.Dispose();
			healthCancellation = new CancellationTokenSource();
			var token = healthCancellation.Token;
			ControlsHelper.SetText(StatusTextBox, "Please wait...");
			try
			{
				var probeTask = Task.Run(
					() => Nefarius.ViGEm.Client.ViGEmClient.GetBusHealth(true), token);
				var completed = await Task.WhenAny(probeTask, Task.Delay(TimeSpan.FromSeconds(5), token));
				if (completed != probeTask)
				{
					ControlsHelper.SetText(StatusTextBox, "ViGEmBus health check timed out. Mapping remains available.");
					InstallButton.IsEnabled = true;
					return;
				}

				var health = await probeTask;
				token.ThrowIfCancellationRequested();
				ControlsHelper.SetText(StatusTextBox, FormatHealth(health));
				InstallButton.IsEnabled = !health.IsUsable;
				UninstallButton.IsEnabled = false;
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				ControlsHelper.SetText(StatusTextBox, "ViGEmBus check failed: " + ex.Message);
				InstallButton.IsEnabled = true;
			}
		}

		static string FormatHealth(ViGEmBusHealthResult health)
		{
			var text = new StringBuilder();
			text.Append("Installed: ").Append(health.Installed ? "Yes" : "No");
			if (health.DriverVersion != null)
				text.Append(" (").Append(health.DriverVersion).Append(")");
			text.Append(" | Service: ").Append(health.ServiceState);
			text.Append(" | Client API: ").Append(health.ClientConnectionState);
			if (!string.IsNullOrWhiteSpace(health.ErrorMessage))
				text.Append(" | ").Append(health.ErrorMessage);
			return text.ToString();
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
			healthCancellation?.Cancel();
			healthCancellation?.Dispose();
			healthCancellation = null;
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
