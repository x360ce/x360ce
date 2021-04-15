using JocysCom.ClassLibrary.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for OptionsInternetControl.xaml
	/// </summary>
	public partial class OptionsInternetControl : UserControl
	{
		public OptionsInternetControl()
		{
			InitializeComponent();
		}

		void InternetCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			InternetAutoLoadCheckBox.IsEnabled = InternetFeaturesCheckBox.IsChecked ?? false;
		}

		public void UpdateSettingsMap()
		{
			var o = SettingsManager.Options;
			SettingsManager.LoadAndMonitor(o, nameof(o.GetProgramsMinInstances), GetProgramsMinInstancesUpDown);
			//SettingsManager.LoadAndMonitor(o, nameof(o.GetProgramsIncludeEnabled), GetProgramsIncludeEnabledCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(o.InternetAutoLoad), InternetAutoLoadCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(o.InternetAutoSave), InternetAutoSaveCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(o.InternetFeatures), InternetFeaturesCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(o.CheckForUpdates), CheckForUpdatesCheckBox);
			InternetDatabaseUrlComboBox.ItemsSource = o.InternetDatabaseUrls;
			SettingsManager.LoadAndMonitor(o, nameof(o.InternetDatabaseUrl), InternetDatabaseUrlComboBox);
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

		private void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.Current.ShowUpdateForm();
		}

		private void OpenSettingsFolderButton_Click(object sender, RoutedEventArgs e)
		{
			EngineHelper.BrowsePath(EngineHelper.AppDataPath);
		}

		private void LoginButton_Click(object sender, RoutedEventArgs e)
		{
			// Secure login over insecure web services.
			if (LoginButton.Content as string == "Log In")
			{
				var o = SettingsManager.Options;
				var saveOptions = false;
				if (o.CheckAndFixUserRsaKeys())
					SettingsManager.OptionsData.Save();
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
					SettingsManager.OptionsData.Save();
				var cmd2 = new CloudMessage(CloudAction.LogIn);
				CloudHelper.ApplySecurity(cmd2, o.UserRsaPublicKey, o.CloudRsaPublicKey, UsernameTextBox.Text, PasswordTextBox.Text);
				cmd2.Values.Add(CloudKey.ComputerId, o.ComputerId, true);
				cmd2.Values.Add(CloudKey.ProfileId, o.ProfileId, true);
				results = ws.Execute(cmd2);
				if (results.ErrorCode != 0)
					MessageBoxWindow.Show(results.ErrorMessage, string.Format("{0} Result", CloudAction.LogIn), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
				else
					MessageBoxWindow.Show(string.Format("Authorized: {0}", results.ErrorMessage), string.Format("{0} Result", CloudAction.LogIn), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
			}
		}

		private void CreateButton_Click(object sender, RoutedEventArgs e)
		{
			var o = SettingsManager.Options;
			var url = o.InternetDatabaseUrl;
			var pql = new Uri(url).PathAndQuery.Length;
			var navigateUrl = url.Substring(0, url.Length - pql) + "/Security/Login.aspx?ShowLogin=0&ShowReset=0";
			var form = new Forms.WebBrowserForm();
			form.Size = new System.Drawing.Size(400, 500);
			form.Text = "Create Login";
			form.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			form.NavigateUrl = navigateUrl;
			ControlsHelper.CheckTopMost(form);
			form.ShowDialog();
			form.Dispose();
		}

		private void ResetButton_Click(object sender, RoutedEventArgs e)
		{
			var o = SettingsManager.Options;
			var url = o.InternetDatabaseUrl;
			var pql = new Uri(url).PathAndQuery.Length;
			var navigateUrl = url.Substring(0, url.Length - pql) + "/Security/Login.aspx?ShowLogin=0&ShowCreate=0";
			var form = new Forms.WebBrowserForm();
			form.Size = new System.Drawing.Size(400, 300);
			form.Text = "Reset Login";
			form.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			form.NavigateUrl = navigateUrl;
			ControlsHelper.CheckTopMost(form);
			form.ShowDialog();
			form.Dispose();
		}

	}
}
