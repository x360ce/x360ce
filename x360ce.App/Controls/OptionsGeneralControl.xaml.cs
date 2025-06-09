using JocysCom.ClassLibrary.Controls;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for OptionsGeneralControl.xaml
	/// </summary>
	public partial class OptionsGeneralControl : UserControl
	{
		public OptionsGeneralControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
			LocationFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
		}

		public void InitOptions()
		{
			DebugModeCheckBox_CheckedChanged(DebugModeCheckBox, null);
		}

		System.Windows.Forms.FolderBrowserDialog LocationFolderBrowserDialog;

		void DebugModeCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			var cbx = (CheckBox)sender;
			//// If debug mode then don't catch exceptions.
			//if (cbx.IsChecked ?? false)
			//	Application.ThreadException -= new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
			//else
			//	Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
		}

		public void UpdateSettingsMap()
		{
			var o = SettingsManager.Options;
			SettingsManager.LoadAndMonitor(o, nameof(Options.DebugMode), DebugModeCheckBox);
			ControlsHelper.SetItemsSource(GameScanLocationsListBox, o.GameScanLocations);
			SettingsManager.LoadAndMonitor(o, nameof(Options.GameScanLocations), GameScanLocationsListBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.StartWithWindows), StartWithWindowsCheckBox);
			StartWithWindowsStateComboBox.ItemsSource = Enum.GetValues(typeof(System.Windows.Forms.FormWindowState));
			SettingsManager.LoadAndMonitor(o, nameof(Options.StartWithWindowsState), StartWithWindowsStateComboBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.AlwaysOnTop), AlwaysOnTopCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.AllowOnlyOneCopy), AllowOnlyOneCopyCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.EnableShowFormInfo), ShowFormInfoCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.ShowTestButton), ShowTestButtonCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.GuideButtonAction), GuideButtonActionTextBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.AutoDetectForegroundWindow), AutoDetectForegroundWindowCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.IsProcessDPIAware), IsProcessDPIAwareCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.MinimizeToTray), MinimizeToTrayCheckBox);
            SettingsManager.LoadAndMonitor(o, nameof(Options.MinimizeOnClose), MinimizeOnClose);
            // Direct Input
            SettingsManager.LoadAndMonitor(o, nameof(Options.ExcludeVirtualDevices), ExcludeVirtualDevicesCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.ExcludeSupplementalDevices), ExcludeSupplementalDevicesCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.UseDeviceBufferedData), UseDeviceBufferedDataCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.AcquireHiddenDevicesInExclusiveMode), AcquireHiddenDevicesInExclusiveModeCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.AcquireMappedDevicesInExclusiveMode), AcquireMappedDevicesInExclusiveModeCheckBox);
			// Load other settings manually.
			SettingsManager.LoadAndMonitor(o, nameof(Options.ShowProgramsTab), ShowProgramsTabCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.ShowSettingsTab), ShowSettingsTabCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.ShowDevicesTab), ShowDevicesTabCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(Options.IncludeProductsInsideINI), IncludeProductsCheckBox);
		}

		private void LocationAddButton_Click(object sender, RoutedEventArgs e)
		{
			var path = LocationFolderBrowserDialog.SelectedPath;
			if (string.IsNullOrEmpty(path))
				path = (string)GameScanLocationsListBox.SelectedValue;
			if (string.IsNullOrEmpty(path))
				path = System.IO.Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			LocationFolderBrowserDialog.SelectedPath = path;
			LocationFolderBrowserDialog.Description = "Browse for Scan Location";
			var result = LocationFolderBrowserDialog.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK)
			{
				// Don't allow to add windows folder.
				var winFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				if (LocationFolderBrowserDialog.SelectedPath.StartsWith(winFolder, StringComparison.OrdinalIgnoreCase))
				{
					MessageBoxWindow.Show("Windows folders are not allowed.", "Windows Folder", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
				}
				else
				{
					if (!Contains(LocationFolderBrowserDialog.SelectedPath))
					{
						SettingsManager.Options.GameScanLocations.Add(LocationFolderBrowserDialog.SelectedPath);
						// Change selected index for change event to fire.
						GameScanLocationsListBox.SelectedItem = LocationFolderBrowserDialog.SelectedPath;
					}
				}
			}
		}

		private void LocationRemoveButton_Click(object sender, RoutedEventArgs e)
		{
			if (GameScanLocationsListBox.SelectedIndex == -1)
				return;
			var currentIndex = GameScanLocationsListBox.SelectedIndex;
			var currentItem = GameScanLocationsListBox.SelectedItem as string;
			SettingsManager.Options.GameScanLocations.Remove(currentItem);
			// Change selected index for change event to fire.
			GameScanLocationsListBox.SelectedIndex = Math.Min(currentIndex, GameScanLocationsListBox.Items.Count - 1);
		}

		private void ProgramScanLocationsListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			LocationRemoveButton.IsEnabled = GameScanLocationsListBox.SelectedIndex > -1;
		}

		bool Contains(string path)
		{
			return SettingsManager.Options.GameScanLocations
				.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase));
		}

		private void LocationRefreshButton_Click(object sender, RoutedEventArgs e)
		{
			var path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			if (!Contains(path))
				SettingsManager.Options.GameScanLocations.Add(path);
			path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			if (!Contains(path))
				SettingsManager.Options.GameScanLocations.Add(path);
			DriveInfo[] allDrives = DriveInfo.GetDrives();
			foreach (DriveInfo d in allDrives)
			{
				if (d.IsReady && d.DriveType == DriveType.Fixed)
				{
					try
					{
						var programDirs = d.RootDirectory.GetDirectories("Program Files*");
						for (int i = 0; i < programDirs.Count(); i++)
						{
							path = programDirs[i].FullName;
							if (!Contains(path))
								SettingsManager.Options.GameScanLocations.Add(path);
						}
					}
					catch (Exception) { }
				}
			}
		}

		private void ShowProgramsTabCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			Global._MainWindow.MainBodyPanel.ShowProgramsTab(ShowProgramsTabCheckBox.IsChecked ?? false);
		}

		private void ShowSettingsTabCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			Global._MainWindow.MainBodyPanel.ShowSettingsTab(ShowSettingsTabCheckBox.IsChecked ?? false);
		}

		private void ShowDevicesTabCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			Global._MainWindow.MainBodyPanel.ShowDevicesTab(ShowDevicesTabCheckBox.IsChecked ?? false);
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			SettingsManager.UnLoadMonitor(DebugModeCheckBox);
			ControlsHelper.SetItemsSource(GameScanLocationsListBox, null);
			SettingsManager.UnLoadMonitor(GameScanLocationsListBox);
			SettingsManager.UnLoadMonitor(StartWithWindowsCheckBox);
			StartWithWindowsStateComboBox.ItemsSource = null;
			SettingsManager.UnLoadMonitor(StartWithWindowsStateComboBox);
			SettingsManager.UnLoadMonitor(AlwaysOnTopCheckBox);
			SettingsManager.UnLoadMonitor(AllowOnlyOneCopyCheckBox);
			SettingsManager.UnLoadMonitor(ShowFormInfoCheckBox);
			SettingsManager.UnLoadMonitor(ShowTestButtonCheckBox);
			SettingsManager.UnLoadMonitor(GuideButtonActionTextBox);
			SettingsManager.UnLoadMonitor(AutoDetectForegroundWindowCheckBox);
			SettingsManager.UnLoadMonitor(IsProcessDPIAwareCheckBox);
			SettingsManager.UnLoadMonitor(MinimizeToTrayCheckBox);
			// Direct Input
			SettingsManager.UnLoadMonitor(ExcludeVirtualDevicesCheckBox);
			SettingsManager.UnLoadMonitor(ExcludeSupplementalDevicesCheckBox);
			SettingsManager.UnLoadMonitor(UseDeviceBufferedDataCheckBox);
			SettingsManager.UnLoadMonitor(AcquireHiddenDevicesInExclusiveModeCheckBox);
			SettingsManager.UnLoadMonitor(AcquireMappedDevicesInExclusiveModeCheckBox);
			// Load other settings manually.
			SettingsManager.UnLoadMonitor(ShowProgramsTabCheckBox);
			SettingsManager.UnLoadMonitor(ShowSettingsTabCheckBox);
			SettingsManager.UnLoadMonitor(ShowDevicesTabCheckBox);
			SettingsManager.UnLoadMonitor(IncludeProductsCheckBox);
			LocationFolderBrowserDialog?.Dispose();
			LocationFolderBrowserDialog = null;
		}

	}
}
