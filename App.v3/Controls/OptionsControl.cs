using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using x360ce.Engine;
using x360ce.Engine.Mcp;
using x360ce.App.Properties;

namespace x360ce.App.Controls
{
	public partial class OptionsControl : UserControl
	{
		public OptionsControl()
		{
			InitializeComponent();
			if (DesignMode) return;
		}

		public void InitOptions()
		{
			DebugModeCheckBox_CheckedChanged(DebugModeCheckBox, null);
			InitAiAccess();
		}

		#region AI Assistant Access

		/// <summary>
		/// Shows the stored AI assistant access settings and applies each change as it is made. These
		/// controls are the one place the door is changed: the door itself refuses them.
		/// </summary>
		void InitAiAccess()
		{
			var s = AiAccessSettings.Current;
			AiAccessComboBox.Items.AddRange(Enum.GetNames(typeof(AiAccess)));
			AiAccessEnabledCheckBox.Checked = s.Enabled;
			AiAccessComboBox.SelectedItem = s.Level.ToString();
			AiAccessPortNumericUpDown.Value = Math.Max(AiAccessPortNumericUpDown.Minimum, Math.Min(AiAccessPortNumericUpDown.Maximum, s.Port));
			AiAccessTokenTextBox.Text = s.Token ?? "";
			AiAccessWindowsCheckBox.Checked = s.Windows;
			// The Windows agent registry ships with newer Windows only.
			AiAccessWindowsCheckBox.Enabled = WindowsAgentRegistry.IsAvailable;
			if (!WindowsAgentRegistry.IsAvailable)
				AiAccessWindowsCheckBox.Text += " (needs a newer Windows)";
			AiAccessEnabledCheckBox.CheckedChanged += (sender, e) => ChangeAiAccess(x => x.Enabled = AiAccessEnabledCheckBox.Checked);
			AiAccessComboBox.SelectedIndexChanged += (sender, e) => ChangeAiAccess(x => x.Level = (AiAccess)Enum.Parse(typeof(AiAccess), (string)AiAccessComboBox.SelectedItem));
			// Taken when editing ends, and only when it changed: every spin click, or every visit to the
			// box, would otherwise restart the listener.
			AiAccessPortNumericUpDown.Validated += (sender, e) =>
			{
				if ((int)AiAccessPortNumericUpDown.Value != AiAccessSettings.Current.Port)
					ChangeAiAccess(x => x.Port = (int)AiAccessPortNumericUpDown.Value);
			};
			AiAccessWindowsCheckBox.CheckedChanged += (sender, e) => ChangeAiAccess(x => x.Windows = AiAccessWindowsCheckBox.Checked);
			AiAccessRegenerateButton.Click += (sender, e) => ChangeAiAccess(x => x.Token = McpListener.NewToken());
			AiAccessCopyButton.Click += (sender, e) => JocysCom.ClassLibrary.Controls.ControlsHelper.CopyToClipboardOrWarn(McpClient.ServerSettings(Application.ExecutablePath));
			AiAccessLogButton.Click += (sender, e) => McpLog.Open();
			UpdateAiAccessStatus(true);
		}

		void ChangeAiAccess(Action<AiAccessSettings> change)
		{
			var s = AiAccessSettings.Current;
			change(s);
			var saved = s.Save();
			s.Apply();
			AiAccessTokenTextBox.Text = s.Token ?? "";
			UpdateAiAccessStatus(saved);
		}

		/// <summary>Says whether the door is open, where, and why not when it could not open.</summary>
		void UpdateAiAccessStatus(bool saved)
		{
			var s = AiAccessSettings.Current;
			string status;
			if (!saved)
				status = "x360ce.ini could not be written, so this change is lost when the program closes.";
			else if (!s.Enabled)
				status = "Off.";
			else if (McpListener.LastError == null)
				status = "Open at " + McpListener.Prefix(McpListener.LoopbackAddress, s.Port) + " with " + s.Level + " access.";
			else
				status = "Not open: " + McpListener.LastError;
			if (s.Enabled && s.Windows && WindowsAgentRegistry.LastError != null)
				status += " Windows registration: " + WindowsAgentRegistry.LastError;
			AiAccessStatusLabel.Text = status;
		}

		#endregion

		void DebugModeCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			var cbx = (CheckBox)sender;
			if (!cbx.Checked) Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
			else Application.ThreadException -= new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
		}

		/// <summary>
		/// Link control with INI key. Value/Text of control will be automatically tracked and INI file updated.
		/// </summary>
		public void InitSettingsManager()
		{
			// INI setting keys with controls.
			string section = @"Options\";
			SettingManager.AddMap(section, () => SettingName.UseInitBeep, UseInitBeepCheckBox);
			SettingManager.AddMap(section, () => SettingName.DebugMode, DebugModeCheckBox);
			SettingManager.AddMap(section, () => SettingName.Log, EnableLoggingCheckBox);
			SettingManager.AddMap(section, () => SettingName.Console, ConsoleCheckBox);
			SettingManager.AddMap(section, () => SettingName.InternetDatabaseUrl, InternetDatabaseUrlComboBox);
			SettingManager.AddMap(section, () => SettingName.InternetFeatures, InternetCheckBox);
			SettingManager.AddMap(section, () => SettingName.AllowOnlyOneCopy, AllowOnlyOneCopyCheckBox);
			SettingManager.AddMap(section, () => SettingName.ProgramScanLocations, GameScanLocationsListBox);
			SettingManager.AddMap(section, () => SettingName.Version, ConfigurationVersionTextBox);
			SettingManager.AddMap(section, () => SettingName.CombineEnabled, CombineEnabledCheckBox);
			SettingManager.AddMap(section, "InternetAutoload", InternetAutoloadCheckBox, SettingManager.Current.SettingsMap);
			SettingManager.AddMap(section, "ExcludeSupplementalDevices", ExcludeSupplementalDevicesCheckBox, SettingManager.Current.SettingsMap);
			SettingManager.AddMap(section, "ExcludeVirtualDevices", ExcludeVirtualDevicesCheckBox, SettingManager.Current.SettingsMap);
		}

		void InternetCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			InternetAutoloadCheckBox.Enabled = InternetCheckBox.Checked;
		}

		private void AddLocationButton_Click(object sender, EventArgs e)
		{
			var path = LocationFolderBrowserDialog.SelectedPath;
			if (string.IsNullOrEmpty(path)) path = GameScanLocationsListBox.Text;
			if (string.IsNullOrEmpty(path)) path = System.IO.Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
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
						GameScanLocationsListBox.Items.Add(LocationFolderBrowserDialog.SelectedPath);
						// Change selected index for change event to fire.
						GameScanLocationsListBox.SelectedIndex = GameScanLocationsListBox.Items.Count - 1;
					}
				}
			}
		}

		private void RemoveLocationButton_Click(object sender, EventArgs e)
		{
			if (GameScanLocationsListBox.SelectedIndex == -1) return;
			var currentIndex = GameScanLocationsListBox.SelectedIndex;
			GameScanLocationsListBox.Items.RemoveAt(currentIndex);
			// Change selectd index for change event to fire.
			GameScanLocationsListBox.SelectedIndex = Math.Min(currentIndex, GameScanLocationsListBox.Items.Count - 1);
		}

		private void ProgramScanLocationsListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			RemoveLocationButton.Enabled = GameScanLocationsListBox.SelectedIndex > -1;
		}

		bool Contains(string path)
		{
			var paths = GameScanLocationsListBox.Items.Cast<string>().ToArray();
			for (int i = 0; i < paths.Length; i++)
			{
				if (paths[i].ToLower() == path.ToLower()) return true;
			}
			return false;
		}

		private void RefreshLocationsButton_Click(object sender, EventArgs e)
		{
			var path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			if (!Contains(path)) GameScanLocationsListBox.Items.Add(path);
			path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			if (!Contains(path)) GameScanLocationsListBox.Items.Add(path);
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
							if (!Contains(path)) GameScanLocationsListBox.Items.Add(path);
						}
					}
					catch (Exception) { }
				}
			}
		}

		private void SaveSettingsButton_Click(object sender, EventArgs e)
		{
			MainForm.Current.SaveSettings();
		}

		private void OpenSettingsFolderButton_Click(object sender, EventArgs e)
		{
			GameDatabaseManager.Current.CheckSettingsFolder();
			EngineHelper.BrowsePath(GameDatabaseManager.Current.GdbFile.FullName);
		}

		private void MinimizeToTrayCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			MainForm.Current.SetMinimizeToTray(!Settings.Default.MinimizeToTray);
		}
	}
}
