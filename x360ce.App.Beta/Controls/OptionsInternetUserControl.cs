using JocysCom.ClassLibrary.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;
using x360ce.App.Forms;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	public partial class OptionsInternetUserControl : UserControl
	{
		public OptionsInternetUserControl()
		{
			InitializeComponent();
		}

		void InternetCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			InternetAutoLoadCheckBox.Enabled = InternetFeaturesCheckBox.Checked;
		}

		public void UpdateSettingsMap()
		{
			var o = SettingsManager.Options;
			SettingsManager.LoadAndMonitor(x => x.GetProgramsMinInstances, GetProgramsMinInstancesUpDown);

			//SettingsManager.LoadAndMonitor(x => x.GetProgramsIncludeEnabled, GetProgramsIncludeEnabledCheckBox);
			SettingsManager.LoadAndMonitor(x => x.InternetAutoLoad, InternetAutoLoadCheckBox);
			SettingsManager.LoadAndMonitor(x => x.InternetAutoSave, InternetAutoSaveCheckBox);
			SettingsManager.LoadAndMonitor(x => x.InternetFeatures, InternetFeaturesCheckBox);
			SettingsManager.LoadAndMonitor(x => x.CheckForUpdates, CheckForUpdatesCheckBox);
			SettingsManager.LoadAndMonitor(x => x.InternetDatabaseUrl, InternetDatabaseUrlComboBox, o.InternetDatabaseUrls);
			// Load other settings manually.
			LoadSettings();
			// Attach event which will save form settings before Save().
			SettingsManager.OptionsData.Saving += OptionsData_Saving;

			SettingsManager.LoadAndMonitor(o, nameof(o.GetProgramsIncludeEnabled), GetProgramsIncludeEnabledCheckBox);
		}

		public void LoadSettings()
		{
			// Load XML settings into control.
			var o = SettingsManager.Options;
			ComputerDiskTextBox.Text = o.ComputerDisk;
			UsernameTextBox.Text = o.Username;
			ComputerIdTextBox.Text = o.ComputerId.ToString();
			ProfilePathTextBox.Text = o.ProfilePath.ToString();
			ProfileIdTextBox.Text = o.ProfileId.ToString();
		}

		private void OptionsData_Saving(object sender, EventArgs e)
		{
			// Save XML settings into control.
			var o = SettingsManager.Options;
			o.Username = UsernameTextBox.Text;
		}

		private void CheckUpdatesButton_Click(object sender, EventArgs e)
		{
			MainForm.Current.ShowUpdateForm();
		}

		private void OpenSettingsFolderButton_Click(object sender, EventArgs e)
		{
			EngineHelper.BrowsePath(EngineHelper.AppDataPath);
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
				var url = InternetDatabaseUrlComboBox.Text;
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
				if (results.ErrorCode != 0)
				{
					MessageBoxWindow.Show(results.ErrorMessage, string.Format("{0} Result", CloudAction.LogIn), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
				}
				else
				{
					MessageBoxWindow.Show(string.Format("Authorized: {0}", results.ErrorMessage), string.Format("{0} Result", CloudAction.LogIn), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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
			ControlsHelper.CheckTopMost(form);
			form.ShowDialog();
			form.Dispose();
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
			ControlsHelper.CheckTopMost(form);
			form.ShowDialog();
			form.Dispose();
		}

	}
}
