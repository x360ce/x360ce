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
using x360ce.App.Properties;

namespace x360ce.App.Controls
{
	public partial class OptionsUserControl : UserControl
	{
		public OptionsUserControl()
		{
			InitializeComponent();




			if (DesignMode) return;
		}

		public void InitOptions()
		{
			DebugModeCheckBox_CheckedChanged(DebugModeCheckBox, null);
		}

		void DebugModeCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			var cbx = (CheckBox)sender;
			if (!cbx.Checked) Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
			else Application.ThreadException -= new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
		}

		void XInputEnableCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			MainForm.Current.XInputEnable(XInputEnableCheckBox.Checked);
		}

		/// <summary>
		/// </summary>
		public void UpdateSettingsManager()
		{
			// Link control with INI key. Value/Text of control will be automatically tracked and INI file updated.
			// INI setting keys with controls.
			string section = SettingsManager.OptionsSection;
			SettingsManager.AddMap(section, () => SettingName.UseInitBeep, UseInitBeepCheckBox);
			SettingsManager.AddMap(section, () => SettingName.DebugMode, DebugModeCheckBox);
			SettingsManager.AddMap(section, () => SettingName.Log, EnableLoggingCheckBox);
			SettingsManager.AddMap(section, () => SettingName.Console, ConsoleCheckBox);
			SettingsManager.AddMap(section, () => SettingName.Version, ConfigurationVersionTextBox);
			SettingsManager.AddMap(section, () => SettingName.CombineEnabled, CombineEnabledCheckBox);
			SettingsManager.AddMap(section, () => SettingName.ExcludeSupplementalDevices, ExcludeSupplementalDevicesCheckBox);
			SettingsManager.AddMap(section, () => SettingName.ExcludeVirtualDevices, ExcludeVirtualDevicesCheckBox);
			// Stored inside XML now.
			SettingsManager.AddMap(section, () => SettingName.InternetDatabaseUrl, InternetDatabaseUrlComboBox);
			SettingsManager.AddMap(section, () => SettingName.InternetFeatures, InternetCheckBox);
			SettingsManager.AddMap(section, () => SettingName.InternetAutoLoad, InternetAutoLoadCheckBox);
			SettingsManager.AddMap(section, () => SettingName.InternetAutoSave, InternetAutoSaveCheckBox);
			SettingsManager.AddMap(section, () => SettingName.AllowOnlyOneCopy, AllowOnlyOneCopyCheckBox);
			SettingsManager.AddMap(section, () => SettingName.ProgramScanLocations, GameScanLocationsListBox);
			LoadSettings();
		}

		void InternetCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			InternetAutoLoadCheckBox.Enabled = InternetCheckBox.Checked;
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
				if (LocationFolderBrowserDialog.SelectedPath.StartsWith(winFolder))
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
			// Change selected index for change event to fire.
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

		public void LoadSettings()
		{
			// Load XML settings into control.
			var o = SettingsManager.Options;
			AllowOnlyOneCopyCheckBox.Checked = o.AllowOnlyOneCopy;
			InternetCheckBox.Checked = o.InternetFeatures;
			InternetAutoLoadCheckBox.Checked = o.InternetAutoLoad;
			InternetAutoSaveCheckBox.Checked = o.InternetAutoSave;
			InternetDatabaseUrlComboBox.DataSource = o.InternetDatabaseUrls;
			InternetDatabaseUrlComboBox.SelectedItem = o.InternetDatabaseUrl;
			GameScanLocationsListBox.Items.AddRange(o.GameScanLocations.ToArray());
			DiskIdTextBox.Text = o.DiskId;
			UsernameTextBox.Text = o.Username;
			HashedDiskIdTextBox.Text = o.HashedDiskId.ToString();
		}

		private void SaveSettingsButton_Click(object sender, EventArgs e)
		{
			// Save XML settings into control.
			var o = SettingsManager.Options;
			o.AllowOnlyOneCopy = AllowOnlyOneCopyCheckBox.Checked;
			o.InternetFeatures = InternetCheckBox.Checked;
			o.InternetAutoLoad = InternetAutoLoadCheckBox.Checked;
			o.InternetAutoSave = InternetAutoSaveCheckBox.Checked;
			o.InternetDatabaseUrls = (List<string>)InternetDatabaseUrlComboBox.DataSource;
			o.InternetDatabaseUrl = (string)InternetDatabaseUrlComboBox.SelectedItem;
			o.GameScanLocations = GameScanLocationsListBox.Items.Cast<string>().ToList();
			o.Username = UsernameTextBox.Text;
			SettingsManager.OptionsData.Save();
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

		private void LoginButton_Click(object sender, EventArgs e)
		{
			// Secure login over insecure webservices.
			if (LoginButton.Text == "Log In")
			{
				var o = SettingsManager.Options;
				var saveOptions = false;
				// If user RSA keys are missing then...
				if (string.IsNullOrEmpty(o.UserRsaPublicKey))
				{
					// Create new RSA keys which will be used to send encrypted credentials.
					var rsa = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.User);
					var keys = rsa.RsaNewKeys(2048);
					o.UserRsaPublicKey = keys.Public;
					o.UserRsaPrivateKey = keys.Private;
					saveOptions = true;
				}
				var ws = new WebServiceClient();
				var url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
				ws.Url = url;
				CloudMessage results;
				// If cloud RSA keys are missing then...
				if (string.IsNullOrEmpty(o.CloudRsaPublicKey))
				{
					// Step 1: Get Server's Public RSA key for encryption.
					var msg = CloudHelper.NewMessage(CloudAction.GetPublicRsaKey);

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
				var cmd2 = CloudHelper.NewMessage(CloudAction.LogIn, o.UserRsaPublicKey, o.CloudRsaPublicKey, UsernameTextBox.Text, PasswordTextBox.Text);
				results = ws.Execute(cmd2);
			}
			else
			{

			}
		}

	}
}
