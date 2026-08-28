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
			AppHelper.LoadHelp(HelpRichTextBox, "Documents.Help.HidGuardian.md");
#if DEBUG
			// Install stays available in development builds so that the removal path can
			// be tested. The confirmation dialog states that this is a development build.
#else
			// HID Guardian install is not supported: a misconfigured HID filter driver
			// can lock the user out of keyboard and mouse. Only uninstall is available.
			HidGuardianInstallButton.Visible = false;
#endif
		}

		/// <summary>
		/// Read driver status when its own tab is opened.
		/// </summary>
		/// <remarks>
		/// This used to happen only when the main window switched to the options page, so the
		/// control could not tell whether it was visible without asking the main form. Watching its
		/// own tabs means the status is right whoever is hosting it, and the checks stay off the
		/// start-up path because they enumerate devices.
		/// </remarks>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (ControlsHelper.IsDesignMode(this))
				return;
			MainTabControl.SelectedIndexChanged += DriverTab_SelectedIndexChanged;
			RefreshSelectedDriverTab();
		}

		private void DriverTab_SelectedIndexChanged(object sender, EventArgs e)
		{
			RefreshSelectedDriverTab();
		}

		void RefreshSelectedDriverTab()
		{
			var tab = MainTabControl.SelectedTab;
			if (tab == VirtualDeviceTabPage)
				RefreshViGEmBusStatus();
			else if (tab == HidGuardianTabPage)
				RefreshHidGuardianStatus();
			else if (tab == HidHideTabPage)
				RefreshHidHideStatus();
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
				RefreshHidHideStatus();
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
			// Recording a failure is not the same as swallowing it, so debug mode does not turn
			// reporting off. It used to: this runs once at start while the box still carries the
			// value it was designed with, which detached the handler for everyone. The framework
			// then had to build its own error window, and a failure that happened when no window
			// could be created was reported as that failure instead of the one that caused it.
			// A debugger still stops at the throw, long before this is reached.
			Application.ThreadException -= new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
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
			// Stored inside XML now.
			var o = SettingsManager.Options;
			SettingsManager.LoadAndMonitor(x => x.GameScanLocations, GameScanLocationsListBox, o.GameScanLocations);
			SettingsManager.LoadAndMonitor(x => x.PollingRate, PollingRateComboBox, Enum.GetValues(typeof(UpdateFrequency)));
			SettingsManager.LoadAndMonitor(x => x.StartWithWindows, StartWithWindowsCheckBox);
			SettingsManager.LoadAndMonitor(x => x.StartWithWindowsState, StartWithWindowsStateComboBox, Enum.GetValues(typeof(FormWindowState)));
			SettingsManager.LoadAndMonitor(x => x.AlwaysOnTop, AlwaysOnTopCheckBox);
			SettingsManager.LoadAndMonitor(x => x.AllowOnlyOneCopy, AllowOnlyOneCopyCheckBox);
			SettingsManager.LoadAndMonitor(x => x.RemoteEnabled, RemoteEnabledCheckBox);
			SettingsManager.LoadAndMonitor(x => x.EnableShowFormInfo, ShowFormInfoCheckBox);
			SettingsManager.LoadAndMonitor(x => x.ShowTestButton, ShowTestButtonCheckBox);
			SettingsManager.LoadAndMonitor(x => x.UseDeviceBufferedData, UseDeviceBufferedDataCheckBox);
			SettingsManager.LoadAndMonitor(x => x.HidGuardianConfigureAutomatically, HidGuardianConfigureAutomaticallyCheckBox);
			SettingsManager.LoadAndMonitor(x => x.GuideButtonAction, GuideButtonActionTextBox);
			SettingsManager.LoadAndMonitor(x => x.AutoDetectForegroundWindow, AutoDetectForegroundWindowCheckBox);
			// Load other settings manually.
			LoadSettings();
			// Attach event which will save form settings before Save().
			SettingsManager.OptionsData.Saving += OptionsData_Saving;
			// This was never attached, so none of the settings it applies ever took effect:
			// always on top, start with Windows, the remote port box and the info window.
			// Detached first so a second call cannot apply everything twice.
			SettingsManager.Options.PropertyChanged -= Options_PropertyChanged;
			SettingsManager.Options.PropertyChanged += Options_PropertyChanged;
		}

		private void Options_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var o = SettingsManager.Options;
			// Update controls by specific property.
			switch (e.PropertyName)
			{
				case nameof(Options.AlwaysOnTop):
					MainForm.Current.TopMost = o.AlwaysOnTop;
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
			// The same button installs a driver that is missing and repairs one that is there. A bus
			// can stop working while still reporting itself healthy, and the moment somebody wants
			// that put right is the moment this button used to be greyed out.
			var present = DInput.VirtualDriverInstaller.GetInstalledViGEmBusVersion() != null;
			if (present)
			{
				ViGEmBusTextBox.Text = "Repairing. Please Wait...";
				Program.RunElevated(AdminCommand.RepairViGEmBus);
			}
			else
			{
				ViGEmBusTextBox.Text = "Installing. Please Wait...";
				DInput.DInputHelper.CheckInstallVirtualDriver();
			}
			RefreshViGEmBusStatus();
		}

		private void ViGEmBusUninstallButton_Click(object sender, EventArgs e)
		{
			var installed = DInput.VirtualDriverInstaller.GetInstalledViGEmBusVersion();
			if (installed == null)
			{
				ViGEmBusTextBox.Text = "Not installed. Nothing to remove.";
				return;
			}
			var embedded = DInput.VirtualDriverInstaller.EmbeddedViGEmBusVersion;
			// A different version was installed by something else. Removing it here would
			// leave that installer's own records pointing at a driver which is gone.
			if (!Equals(installed, embedded))
			{
				MessageBoxForm.Show(
					string.Format(
						"Installed version {0} does not match the version supplied with this " +
						"application ({1}), so it was installed by something else.\r\n\r\n" +
						"Remove it through the installer that put it there, or through " +
						"Apps & Features. This application will not touch it.",
						installed, embedded),
					"Remove Virtual Gamepad Bus", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			var text =
				string.Format("Remove the virtual gamepad bus driver (version {0})?\r\n\r\n", installed) +
				"This driver is shared. Other applications that create virtual controllers, " +
				"for example DS4Windows, stop working until it is installed again.\r\n\r\n" +
				"The driver package stays in the Windows driver store, so Windows may " +
				"recreate the device. Use the official installer to remove it completely.";
			if (!Confirm(text, "Remove Virtual Gamepad Bus"))
				return;
			ViGEmBusTextBox.Text = "Uninstalling. Please Wait...";
			// Disable Virtual mode first.
			MainForm.Current.ChangeCurrentGameEmulationType(EmulationType.None);
			DInput.DInputHelper.CheckUnInstallVirtualDriver();
			// Report the state reached, measured here rather than trusted.
			var remaining = DInput.VirtualDriverInstaller.GetInstalledViGEmBusVersion();
			MessageBoxForm.Show(
				remaining == null
					? "Virtual gamepad bus removed."
					: string.Format("Virtual gamepad bus is still present (version {0}).", remaining),
				"Uninstall result", MessageBoxButtons.OK,
				remaining == null ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
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
					// Always available, because a bus that is present is exactly the one that might
					// need putting right. The word changes so it is clear which of the two it will do.
					ViGEmBusInstallButton.Enabled = true;
					ViGEmBusInstallButton.Text = bus.DriverVersion == 0 ? "Install" : "Repair";
					ViGEmBusUninstallButton.Enabled = bus.DriverVersion != 0;
				});
			});
			var t = new System.Threading.Thread(ts);
			t.Start();
		}

		#endregion

		#region HID Guardian


		#region HID Hide

		/// <summary>
		/// Show whether HID Hide is installed, and offer only what makes sense from here.
		/// </summary>
		/// <remarks>
		/// HID Hide is a separate product with its own installer and its own configuration
		/// program. This application only reports what it finds and opens the right place;
		/// installing or configuring somebody else's driver behind the user's back would be
		/// the wrong thing to do with a kernel driver.
		/// </remarks>
		void RefreshHidHideStatus()
		{
			var installed = DInput.VirtualDriverInstaller.IsHidHideDevicePresent();
			var version = DInput.VirtualDriverInstaller.GetHidHideVersion();
			var client = DInput.VirtualDriverInstaller.GetHidHideClientPath();
			if (installed)
			{
				HidHideStatusTextBox.Text = string.IsNullOrEmpty(version)
					? "Installed."
					: string.Format("Installed. Version {0}.", version);
			}
			else
			{
				HidHideStatusTextBox.Text = Environment.Is64BitOperatingSystem
					? "Not installed."
					: "Not installed. HID Hide requires 64-bit Windows 10 or 11.";
			}
			// Nothing to open until its own setup has put the program on disk.
			HidHideConfigureButton.Enabled = !string.IsNullOrEmpty(client);
			HidHideDownloadButton.Text = installed ? "Check for Updates..." : "Download HID Hide...";
		}

		private void HidHideRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshHidHideStatus();
		}

		private void HidHideDownloadButton_Click(object sender, EventArgs e)
		{
			ControlsHelper.OpenUrl(DInput.VirtualDriverInstaller.HidHideDownloadUrl);
		}

		private void HidHideConfigureButton_Click(object sender, EventArgs e)
		{
			var client = DInput.VirtualDriverInstaller.GetHidHideClientPath();
			if (string.IsNullOrEmpty(client))
			{
				HidHideStatusTextBox.Text = "Configuration program not found. Install HID Hide first.";
				return;
			}
			ControlsHelper.OpenPath(client);
		}

		#endregion

		private void HidGuardianInstallButton_Click(object sender, EventArgs e)
		{
#if DEBUG
			var text =
				"Install HID Guardian?\r\n\r\n" +
				"DEVELOPMENT BUILD ONLY. HID Guardian is a filter on every HID device. " +
				"If it is left registered while its driver is missing, keyboard and mouse " +
				"can stop working and recovery needs safe mode and a registry edit.\r\n\r\n" +
				"The driver is installed first and the class filter is added only after the " +
				"driver is confirmed present.";
			if (!Confirm(text, "Install HID Guardian"))
				return;
			HidGuardianTextBox.Text = "Installing. Please Wait...";
			Program.RunElevated(AdminCommand.InstallHidGuardian);
			ReportHidGuardianState("Install");
			RefreshHidGuardianStatus();
#else
			// Install is not supported. Only uninstall is available.
			HidGuardianTextBox.Text = "Install is not supported by this version. Only uninstall is available.";
#endif
		}

		private void HidGuardianRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshHidGuardianStatus();
		}

		private void HidGuardianUninstallButton_Click(object sender, EventArgs e)
		{
			var filter = DInput.VirtualDriverInstaller.IsHidGuardianClassFilterPresent();
			var device = DInput.VirtualDriverInstaller.IsHidGuardianDevicePresent();
			if (!filter && !device)
			{
				HidGuardianTextBox.Text = "Not installed. Nothing to remove.";
				return;
			}
			var text =
				"Remove HID Guardian?\r\n\r\n" +
				string.Format("HID class filter: {0}\r\nDriver: {1}\r\n\r\n", filter ? "present" : "not present", device ? "present" : "not present") +
				"The class filter is removed first and checked, and the driver is removed " +
				"only after the filter is confirmed gone. Nothing else is touched. If the " +
				"filter cannot be removed the driver is left in place, because a filter that " +
				"names a missing driver can stop keyboard and mouse from working.";
			if (!Confirm(text, "Remove HID Guardian"))
				return;
			HidGuardianTextBox.Text = "Uninstalling. Please Wait...";
			Program.RunElevated(AdminCommand.UninstallHidGuardian);
			ReportHidGuardianState("Uninstall");
			RefreshHidGuardianStatus();
		}

		/// <summary>Report the driver state reached, measured here rather than trusted.</summary>
		void ReportHidGuardianState(string action)
		{
			var filter = DInput.VirtualDriverInstaller.IsHidGuardianClassFilterPresent();
			var device = DInput.VirtualDriverInstaller.IsHidGuardianDevicePresent();
			// The unsafe combination: filter registered while the driver is gone.
			if (filter && !device)
			{
				var script = DInput.VirtualDriverInstaller.GetHidGuardianRemoveScript();
				MessageBoxForm.Show(
					"HID Guardian is still registered as a HID class filter but its driver is not " +
					"installed. Devices may fail to start after a restart.\r\n\r\n" +
					"Run the recovery script from an administrative command prompt before " +
					"restarting:\r\n\r\n" + (script ?? "HidGuardian_Remove.ps1 could not be extracted."),
					action + " incomplete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			MessageBoxForm.Show(
				string.Format("HID class filter: {0}\r\nDriver: {1}",
					filter ? "present" : "not present", device ? "present" : "not present"),
				action + " result", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		/// <summary>Ask before changing driver state. Cancel is the default answer.</summary>
		static bool Confirm(string text, string caption)
		{
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			ControlsHelper.CheckTopMost(form);
			var result = form.ShowForm(text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
			form.Dispose();
			return result == DialogResult.Yes;
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
#if DEBUG
					// Development builds can install, so that removal can be tested.
					HidGuardianInstallButton.Enabled = hid.DriverVersion == 0;
#else
					HidGuardianInstallButton.Enabled = false;
#endif
					// Offer removal while either the driver or the class filter is present.
					HidGuardianUninstallButton.Enabled = hid.DriverVersion != 0
						|| DInput.VirtualDriverInstaller.IsHidGuardianClassFilterPresent();
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
