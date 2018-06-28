using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using x360ce.Engine;
using x360ce.App.Forms;
using JocysCom.ClassLibrary.Controls;
using Microsoft.Win32;

namespace x360ce.App.Controls
{
	public partial class OptionsUserControl : UserControl
	{
		public OptionsUserControl()
		{
			InitializeComponent();
			if (DesignMode)
				return;
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
				RefreshViGEmStatus();
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
				{
					runKey.SetValue(Application.ProductName, command);
				}
			}
			else
			{
				if (runKey.GetValueNames().Contains(Application.ProductName))
				{
					// Remove the value from the registry so that the application doesn't start
					runKey.DeleteValue(Application.ProductName, false);
				}
			}
			runKey.Close();
		}

		#endregion


		void DebugModeCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			var cbx = (CheckBox)sender;
			if (!cbx.Checked)
				Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
			else Application.ThreadException -= new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
		}

		/// <summary>
		/// </summary>
		public void UpdateSettingsMap()
		{
			// Link control with INI key. Value/Text of control will be automatically tracked and INI file updated.
			// INI setting keys with controls.
			string section = SettingsManager.OptionsSection;
			SettingsManager.AddMap(section, () => SettingName.DebugMode, DebugModeCheckBox);
			SettingsManager.AddMap(section, () => SettingName.Log, EnableLoggingCheckBox);
			SettingsManager.AddMap(section, () => SettingName.Console, ConsoleCheckBox);
			SettingsManager.AddMap(section, () => SettingName.Version, ConfigurationVersionTextBox);
			// Attach property monitoring first.
			var o = SettingsManager.Options;
			SettingsManager.Options.PropertyChanged += Options_PropertyChanged;
			// Stored inside XML now.
			SettingsManager.AddMap<Options>(x => x.InternetDatabaseUrl, InternetDatabaseUrlComboBox);
			SettingsManager.AddMap<Options>(x => x.PollingRate, PollingRateComboBox);
			SettingsManager.AddMap<Options>(x => x.InternetFeatures, InternetCheckBox);
			SettingsManager.AddMap<Options>(x => x.InternetAutoLoad, InternetAutoLoadCheckBox);
			SettingsManager.AddMap<Options>(x => x.InternetAutoSave, InternetAutoSaveCheckBox);
			SettingsManager.AddMap<Options>(x => x.GameScanLocations, GameScanLocationsListBox);
			SettingsManager.AddMap<Options>(x => x.AlwaysOnTop, AlwaysOnTopCheckBox);
			SettingsManager.AddMap<Options>(x => x.AllowOnlyOneCopy, AllowOnlyOneCopyCheckBox);
			SettingsManager.AddMap<Options>(x => x.StartWithWindows, StartWithWindowsCheckBox);
			SettingsManager.AddMap<Options>(x => x.StartWithWindowsState, StartWithWindowsStateComboBox);
			//SettingsManager.AddMap<Options>(x => x.MinimizeToTray, MinimizeToTrayCheckBox);
			// Load settings into control.
			// This will trigger Options_PropertyChanged event handler and update controls.
			o.ReportPropertyChanged(x => x.InternetFeatures);
			o.ReportPropertyChanged(x => x.InternetAutoLoad);
			o.ReportPropertyChanged(x => x.InternetAutoSave);
			//o.ReportPropertyChanged(x => x.GameScanLocations);
			o.ReportPropertyChanged(x => x.AlwaysOnTop);
			o.ReportPropertyChanged(x => x.AllowOnlyOneCopy);
			// Monitor control changes.

			InternetCheckBox.CheckedChanged += Control_Changed;
			InternetAutoLoadCheckBox.CheckedChanged += Control_Changed;
			InternetAutoSaveCheckBox.CheckedChanged += Control_Changed;
			AlwaysOnTopCheckBox.CheckedChanged += Control_Changed;
			AllowOnlyOneCopyCheckBox.CheckedChanged += Control_Changed;
			GameScanLocationsListBox.DataSource = o.GameScanLocations;
			// Start with windows.
			o.ReportPropertyChanged(x => x.StartWithWindows);
			StartWithWindowsCheckBox.CheckedChanged += Control_Changed;
			// Update start with windows state.
			StartWithWindowsStateComboBox.DataSource = Enum.GetValues(typeof(FormWindowState));
			o.ReportPropertyChanged(x => x.StartWithWindowsState);
			StartWithWindowsStateComboBox.SelectedIndexChanged += Control_Changed;
			// Set ComboBox and attach event last, in order to prevent changing of original value.
			InternetDatabaseUrlComboBox.DataSource = o.InternetDatabaseUrls;
			o.ReportPropertyChanged(x => x.InternetDatabaseUrl);
			InternetDatabaseUrlComboBox.SelectedIndexChanged += Control_Changed;
			// Update pooling rate.
			PollingRateComboBox.DataSource = (UpdateFrequency[])Enum.GetValues(typeof(UpdateFrequency));
			o.ReportPropertyChanged(x => x.PollingRate);
			PollingRateComboBox.SelectedIndexChanged += Control_Changed;
			// Load other settings manually.
			LoadSettings();
			// Attach event which will save form settings before Save().
			SettingsManager.OptionsData.Saving += OptionsData_Saving;
		}

		private void Control_Changed(object sender, EventArgs e)
		{
			SettingsManager.Sync((Control)sender, SettingsManager.Options);
		}

		private void Options_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var o = SettingsManager.Options;
			SettingsManager.Sync(o, e.PropertyName);
			// Update controls by specific property.
			if (e.PropertyName == AppHelper.GetPropertyName<Options>(x => x.AlwaysOnTop))
			{
				// Apply setting.
				MainForm.Current.TopMost = o.AlwaysOnTop;
			}
			else if (e.PropertyName == AppHelper.GetPropertyName<Options>(x => x.PollingRate))
			{
				MainForm.Current.DHelper.Frequency = o.PollingRate;
			}
			else if (e.PropertyName == AppHelper.GetPropertyName<Options>(x => x.StartWithWindows) ||
				e.PropertyName == AppHelper.GetPropertyName<Options>(x => x.StartWithWindowsState))
			{
				UpdateWindowsStartRegistry(o.StartWithWindows, o.StartWithWindowsState);
			}
		}

		void InternetCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			InternetAutoLoadCheckBox.Enabled = InternetCheckBox.Checked;
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
			ComputerDiskTextBox.Text = o.ComputerDisk;
			UsernameTextBox.Text = o.Username;
			ComputerIdTextBox.Text = o.ComputerId.ToString();
			ProfilePathTextBox.Text = o.ProfilePath.ToString();
			ProfileIdTextBox.Text = o.ProfileId.ToString();
			IncludeProductsCheckBox.Checked = o.IncludeProductsInsideINI;
			ExcludeSupplementalDevicesCheckBox.Checked = o.ExcludeSupplementalDevices;
			ExcludeVirtualDevicesCheckBox.Checked = o.ExcludeVirtualDevices;
		}

		private void OptionsData_Saving(object sender, EventArgs e)
		{
			// Save XML settings into control.
			var o = SettingsManager.Options;
			// Other options.
			o.ShowProgramsTab = ShowProgramsTabCheckBox.Checked;
			o.ShowSettingsTab = ShowSettingsTabCheckBox.Checked;
			o.ShowDevicesTab = ShowDevicesTabCheckBox.Checked;
			o.Username = UsernameTextBox.Text;
			o.IncludeProductsInsideINI = IncludeProductsCheckBox.Checked;
			o.ExcludeSupplementalDevices = ExcludeSupplementalDevicesCheckBox.Checked;
			o.ExcludeVirtualDevices = ExcludeVirtualDevicesCheckBox.Checked;
		}

		private void OpenSettingsFolderButton_Click(object sender, EventArgs e)
		{
			GameDatabaseManager.Current.CheckSettingsFolder();
			EngineHelper.BrowsePath(GameDatabaseManager.Current.GdbFile.FullName);
		}

		private void LoginButton_Click(object sender, EventArgs e)
		{
			// Secure login over insecure web services.
			if (LoginButton.Text == "Log In")
			{
				var o = SettingsManager.Options;
				var saveOptions = false;
				if (o.CheckAndFixUserRsaKeys())
				{
					SettingsManager.OptionsData.Save();
				}
				var ws = new WebServiceClient();
				var url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
				ws.Url = url;
				CloudMessage results;
				// If cloud RSA keys are missing then...
				if (string.IsNullOrEmpty(o.CloudRsaPublicKey))
				{
					// Step 1: Get Server's Public RSA key for encryption.
					var msg = new CloudMessage(CloudAction.GetPublicRsaKey);
					CloudHelper.ApplySecurity(msg);
					msg.Values.Add(CloudKey.RsaPublicKey, o.UserRsaPublicKey);
					// Retrieve public RSA key.
					results = ws.Execute(msg);
					if (results.ErrorCode == 0)
					{
						o.CloudRsaPublicKey = results.Values.GetValue<string>(CloudKey.RsaPublicKey);
						saveOptions = true;
					}
				}
				if (saveOptions)
				{
					SettingsManager.OptionsData.Save();
				}
				var cmd2 = new CloudMessage(CloudAction.LogIn);
				CloudHelper.ApplySecurity(cmd2, o.UserRsaPublicKey, o.CloudRsaPublicKey, UsernameTextBox.Text, PasswordTextBox.Text);
				cmd2.Values.Add(CloudKey.ComputerId, o.ComputerId, true);
				cmd2.Values.Add(CloudKey.ProfileId, o.ProfileId, true);
				results = ws.Execute(cmd2);
				if (results.ErrorCode > 0)
				{
					MessageBoxForm.Show(results.ErrorMessage, string.Format("{0} Result", CloudAction.LogIn), MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					MessageBoxForm.Show(string.Format("Authorized: {0}", results.ErrorMessage), string.Format("{0} Result", CloudAction.LogIn), MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			else
			{

			}
		}

		private void CreateButton_Click(object sender, EventArgs e)
		{
			var o = SettingsManager.Options;
			var url = o.InternetDatabaseUrl;
			var pql = new Uri(url).PathAndQuery.Length;
			var navigateUrl = url.Substring(0, url.Length - pql) + "/Security/Login.aspx?ShowLogin=0&ShowReset=0";
			var form = new WebBrowserForm();
			form.Size = new Size(400, 500);
			form.Text = "Create Login";
			form.StartPosition = FormStartPosition.CenterParent;
			form.NavigateUrl = navigateUrl;
			form.ShowDialog();
			form.Dispose();
			form = null;
		}

		private void ResetButton_Click(object sender, EventArgs e)
		{
			var o = SettingsManager.Options;
			var url = o.InternetDatabaseUrl;
			var pql = new Uri(url).PathAndQuery.Length;
			var navigateUrl = url.Substring(0, url.Length - pql) + "/Security/Login.aspx?ShowLogin=0&ShowCreate=0";
			var form = new WebBrowserForm();
			form.Size = new Size(400, 300);
			form.Text = "Reset Login";
			form.StartPosition = FormStartPosition.CenterParent;
			form.NavigateUrl = navigateUrl;
			form.ShowDialog();
			form.Dispose();
			form = null;
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

		private void CheckForUpdatesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			var o = SettingsManager.Options;
			o.CheckForUpdates = CheckForUpdatesCheckBox.Checked;
		}

		private void CheckUpdatesButton_Click(object sender, EventArgs e)
		{
			MainForm.Current.ShowUpdateForm();

		}

		DeveloperToolsForm _ToolsForm;

		private void DeveloperToolsButton_Click(object sender, EventArgs e)
		{
			if (_ToolsForm == null)
				_ToolsForm = new DeveloperToolsForm();
			_ToolsForm.ShowPanel();
		}

		#region Virtual Drivers

		private void ViGEmBusInstallButton_Click(object sender, EventArgs e)
		{
			ViGEmBusTextBox.Text = "Installing. Please Wait...";
			DInput.DInputHelper.CheckInstallVirtualDriver();
			RefreshViGEmStatus();
		}

		private void ViGEmBusUninstallButton_Click(object sender, EventArgs e)
		{
			ViGEmBusTextBox.Text = "Uninstalling. Please Wait...";
			// Disable Virtual mode first.
			MainForm.Current.ChangeCurrentGameEmulationType(EmulationType.None);
			DInput.DInputHelper.CheckUnInstallVirtualDriver();
			RefreshViGEmStatus();
		}

		private void HidGuardianInstallButton_Click(object sender, EventArgs e)
		{
			HidGuardianTextBox.Text = "Installing. Please Wait...";
			Program.RunElevated(AdminCommand.InstallHidGuardian);
			ViGEm.HidGuardianHelper.InsertCurrentProcessToWhiteList();
			RefreshViGEmStatus();
		}

		private void HidGuardianUninstallButton_Click(object sender, EventArgs e)
		{
			HidGuardianTextBox.Text = "Uninstalling. Please Wait...";
			Program.RunElevated(AdminCommand.UninstallHidGuardian);
			RefreshViGEmStatus();
		}

		private void VirtualInfoRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshViGEmStatus();
		}

		void RefreshViGEmStatus()
		{
			ControlsHelper.SetText(ViGEmBusTextBox, "Please wait...");
			ControlsHelper.SetText(HidGuardianTextBox, "Please wait...");
			// run in another thread, to make sure it is not freezing interface.
			var ts = new System.Threading.ThreadStart(delegate ()
			{
				// Get Virtual Bus and HID Guardian status.
				var bus = DInput.VirtualDriverInstaller.GetViGemBusDriverInfo();
				var hid = DInput.VirtualDriverInstaller.GetHidGuardianDriverInfo();
				BeginInvoke((MethodInvoker)delegate ()
				{
					// Update Bus status.
					var busStatus = bus.DriverVersion == 0
						? "Not installed"
						: string.Format("{0} {1}", bus.Description, bus.GetVersion());
					ControlsHelper.SetText(ViGEmBusTextBox, busStatus);
					ViGEmBusInstallButton.Enabled = bus.DriverVersion == 0;
					ViGEmBusUninstallButton.Enabled = bus.DriverVersion != 0;
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
	}
}
