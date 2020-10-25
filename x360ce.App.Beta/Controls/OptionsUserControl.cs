using JocysCom.ClassLibrary.Controls;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Forms;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	public partial class OptionsUserControl : UserControl
	{
		public OptionsUserControl()
		{
			InitializeComponent();
			if (DesignMode)
				return;
			// Make font more consistent with the rest of the interface.
			Controls.OfType<ToolStrip>().ToList().ForEach(x => x.Font = Font);
			LocationsToolStrip.Font = Font;
			AppHelper.LoadHelp(HelpRichTextBox, "Documents.Help_HidGuardian.rtf");
		}

		public void InitOptions()
		{
			DebugModeCheckBox_CheckedChanged(DebugModeCheckBox, null);
			MainForm.Current.MainTabControl.SelectedIndexChanged += MainTabControl_SelectedIndexChanged;
		}

		private void MainTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (MainForm.Current.MainTabControl.SelectedTab == MainForm.Current.OptionsPanel.Parent)
			{
				RefreshViGEmBusStatus();
				RefreshHidGuardianStatus();
			}
		}

		#region Operation 

		/// <summary>
		/// Requires no special permissions, because current used have full access to CurrentUser 'Run' registry key.
		/// </summary>
		/// <param name="enabled">Start with Windows after Sign-In.</param>
		/// <param name="startState">Start Mode.</param>
		public void UpdateWindowsStartRegistry(bool enabled, FormWindowState? startState = null)
		{
			startState = startState ?? SettingsManager.Options.StartWithWindowsState;
			var runKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
			if (enabled)
			{
				// Add the value in the registry so that the application runs at start-up
				string command = string.Format("\"{0}\" /{1}={2}", Application.ExecutablePath, Program.arg_WindowState, startState.ToString());
				var value = (string)runKey.GetValue(Application.ProductName);
				if (value != command)
					runKey.SetValue(Application.ProductName, command);
			}
			else
			{
				// Remove the value from the registry so that the application doesn't start
				if (runKey.GetValueNames().Contains(Application.ProductName))
					runKey.DeleteValue(Application.ProductName, false);
			}
			runKey.Close();
		}

		#endregion


		void DebugModeCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			var cbx = (CheckBox)sender;
			// If debug mode then don't catch exceptions.
			if (cbx.Checked)
				Application.ThreadException -= new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
			else
				Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
		}

		public void UpdateSettingsMap()
		{
			// Link control with INI key. Value/Text of control will be automatically tracked and INI file updated.
			// INI setting keys with controls.
			var section = SettingsManager.OptionsSection;
			SettingsManager.AddMap(section, () => SettingName.DebugMode, DebugModeCheckBox);
			SettingsManager.AddMap(section, () => SettingName.Log, EnableLoggingCheckBox);
			SettingsManager.AddMap(section, () => SettingName.Console, ConsoleCheckBox);
			SettingsManager.AddMap(section, () => SettingName.Version, ConfigurationVersionTextBox);
			// Attach property monitoring first.
			SettingsManager.Options.PropertyChanged += Options_PropertyChanged;
			// Stored inside XML now.
			var o = SettingsManager.Options;
			ControlHelper.LoadAndMonitor(x => x.GameScanLocations, GameScanLocationsListBox, o.GameScanLocations);
			ControlHelper.LoadAndMonitor(x => x.PollingRate, PollingRateComboBox, Enum.GetValues(typeof(UpdateFrequency)));
			ControlHelper.LoadAndMonitor(x => x.StartWithWindows, StartWithWindowsCheckBox);
			ControlHelper.LoadAndMonitor(x => x.StartWithWindowsState, StartWithWindowsStateComboBox, Enum.GetValues(typeof(FormWindowState)));
			ControlHelper.LoadAndMonitor(x => x.AlwaysOnTop, AlwaysOnTopCheckBox);
			ControlHelper.LoadAndMonitor(x => x.AllowOnlyOneCopy, AllowOnlyOneCopyCheckBox);
			ControlHelper.LoadAndMonitor(x => x.RemoteEnabled, RemoteEnabledCheckBox);
			ControlHelper.LoadAndMonitor(x => x.EnableShowFormInfo, ShowFormInfoCheckBox);
			ControlHelper.LoadAndMonitor(x => x.ShowTestButton, ShowTestButtonCheckBox);
			ControlHelper.LoadAndMonitor(x => x.UseDeviceBufferedData, UseDeviceBufferedDataCheckBox);
			ControlHelper.LoadAndMonitor(x => x.HidGuardianConfigureAutomatically, HidGuardianConfigureAutomaticallyCheckBox);
			// Load other settings manually.
			LoadSettings();
			// Attach event which will save form settings before Save().
			SettingsManager.OptionsData.Saving += OptionsData_Saving;
		}


		private void Options_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var o = SettingsManager.Options;
			SettingsManager.Sync(o, e.PropertyName);
			// Update controls by specific property.
			switch (e.PropertyName)
			{
				case nameof(Options.AlwaysOnTop):
					MainForm.Current.TopMost = o.AlwaysOnTop;
					break;
				case nameof(Options.PollingRate):
					Global.DHelper.Frequency = o.PollingRate;
					break;
				case nameof(Options.StartWithWindows):
				case nameof(Options.StartWithWindowsState):
					UpdateWindowsStartRegistry(o.StartWithWindows, o.StartWithWindowsState);
					break;
				case nameof(Options.RemoteControllers):
					RemotePortNumericUpDown.Enabled = o.RemoteControllers == MapToMask.None;
					break;
				case nameof(Options.EnableShowFormInfo):
					InfoForm.MonitorEnabled = o.EnableShowFormInfo;
					break;
				default:
					break;
			}
		}

		private void AddLocationButton_Click(object sender, EventArgs e)
		{
			var path = LocationFolderBrowserDialog.SelectedPath;
			if (string.IsNullOrEmpty(path))
				path = GameScanLocationsListBox.Text;
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
					MessageBoxForm.Show("Windows folders are not allowed.", "Windows Folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

		private void RemoveLocationButton_Click(object sender, EventArgs e)
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
			RemoveLocationButton.Enabled = GameScanLocationsListBox.SelectedIndex > -1;
		}

		bool Contains(string path)
		{
			return SettingsManager.Options.GameScanLocations
				.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase));
		}

		private void RefreshLocationsButton_Click(object sender, EventArgs e)
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
				if (d.IsReady == true && d.DriveType == DriveType.Fixed)
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

		public void LoadSettings()
		{
			// Load XML settings into control.
			var o = SettingsManager.Options;
			// Other option.
			ShowProgramsTabCheckBox.Checked = o.ShowProgramsTab;
			ShowSettingsTabCheckBox.Checked = o.ShowSettingsTab;
			ShowDevicesTabCheckBox.Checked = o.ShowDevicesTab;
			IncludeProductsCheckBox.Checked = o.IncludeProductsInsideINI;
			ExcludeSupplementalDevicesCheckBox.Checked = o.ExcludeSupplementalDevices;
			ExcludeVirtualDevicesCheckBox.Checked = o.ExcludeVirtualDevices;
			// Remote Options.
			AllowRemote1CheckBox.Checked = o.RemoteControllers.HasFlag(MapToMask.Controller1);
			AllowRemote2CheckBox.Checked = o.RemoteControllers.HasFlag(MapToMask.Controller2);
			AllowRemote3CheckBox.Checked = o.RemoteControllers.HasFlag(MapToMask.Controller3);
			AllowRemote4CheckBox.Checked = o.RemoteControllers.HasFlag(MapToMask.Controller4);
			RemotePasswordTextBox.Text = o.RemotePassword;
			if (o.RemotePort >= RemotePortNumericUpDown.Minimum && o.RemotePort <= RemotePortNumericUpDown.Maximum)
				RemotePortNumericUpDown.Value = o.RemotePort;
		}

		private void OptionsData_Saving(object sender, EventArgs e)
		{
			// Save XML settings into control.
			var o = SettingsManager.Options;
			// Other options.
			o.ShowProgramsTab = ShowProgramsTabCheckBox.Checked;
			o.ShowSettingsTab = ShowSettingsTabCheckBox.Checked;
			o.ShowDevicesTab = ShowDevicesTabCheckBox.Checked;
			o.IncludeProductsInsideINI = IncludeProductsCheckBox.Checked;
			o.ExcludeSupplementalDevices = ExcludeSupplementalDevicesCheckBox.Checked;
			o.ExcludeVirtualDevices = ExcludeVirtualDevicesCheckBox.Checked;
			// Remote Options.
			var remoteControllers = MapToMask.None;
			remoteControllers |= AllowRemote1CheckBox.Checked ? MapToMask.Controller1 : MapToMask.None;
			remoteControllers |= AllowRemote2CheckBox.Checked ? MapToMask.Controller2 : MapToMask.None;
			remoteControllers |= AllowRemote3CheckBox.Checked ? MapToMask.Controller3 : MapToMask.None;
			remoteControllers |= AllowRemote4CheckBox.Checked ? MapToMask.Controller4 : MapToMask.None;
			o.RemoteControllers = remoteControllers;
			o.RemotePassword = RemotePasswordTextBox.Text;
			o.RemotePort = (int)RemotePortNumericUpDown.Value;
		}

		private void ShowProgramsTabCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			MainForm.Current.ShowProgramsTab(ShowProgramsTabCheckBox.Checked);
		}

		private void ShowSettingsTabCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			MainForm.Current.ShowSettingsTab(ShowSettingsTabCheckBox.Checked);
		}

		private void ShowDevicesTabCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			MainForm.Current.ShowDevicesTab(ShowDevicesTabCheckBox.Checked);
		}

		DeveloperToolsForm _ToolsForm;

		private void DeveloperToolsButton_Click(object sender, EventArgs e)
		{
			if (_ToolsForm == null)
				_ToolsForm = new DeveloperToolsForm();
			_ToolsForm.ShowPanel();
		}

		#region ViGemBus Driver

		private void ViGEmBusInstallButton_Click(object sender, EventArgs e)
		{
			ViGEmBusTextBox.Text = "Installing. Please Wait...";
			DInput.DInputHelper.CheckInstallVirtualDriver();
			RefreshViGEmBusStatus();
		}

		private void ViGEmBusUninstallButton_Click(object sender, EventArgs e)
		{
			ViGEmBusTextBox.Text = "Uninstalling. Please Wait...";
			// Disable Virtual mode first.
			MainForm.Current.ChangeCurrentGameEmulationType(EmulationType.None);
			DInput.DInputHelper.CheckUnInstallVirtualDriver();
			RefreshViGEmBusStatus();
		}


		private void ViGEmBusRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshViGEmBusStatus();
		}

		void RefreshViGEmBusStatus()
		{
			ControlsHelper.SetText(ViGEmBusTextBox, "Please wait...");
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
					ControlsHelper.SetText(ViGEmBusTextBox, busStatus);
					ViGEmBusInstallButton.Enabled = bus.DriverVersion == 0;
					ViGEmBusUninstallButton.Enabled = bus.DriverVersion != 0;
				});
			});
			var t = new System.Threading.Thread(ts);
			t.Start();
		}

		#endregion

		#region HID Guardian

		private void HidGuardianInstallButton_Click(object sender, EventArgs e)
		{
			HidGuardianTextBox.Text = "Installing. Please Wait...";
			Program.RunElevated(AdminCommand.InstallHidGuardian);
			ViGEm.HidGuardianHelper.InsertCurrentProcessToWhiteList();
			RefreshHidGuardianStatus();
		}

		private void HidGuardianRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshHidGuardianStatus();
		}

		private void HidGuardianUninstallButton_Click(object sender, EventArgs e)
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
					HidGuardianInstallButton.Enabled = hid.DriverVersion == 0;
					HidGuardianUninstallButton.Enabled = hid.DriverVersion != 0;
				});
			});
			var t = new System.Threading.Thread(ts);
			t.Start();
		}

		#endregion

		void AboutViGEmLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ControlsHelper.OpenUrl(((Control)sender).Text);
		}

	}
}
