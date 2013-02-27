using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX.DirectInput;
using System.IO;
using Microsoft.Win32;
using x360ce.App.com.x360ce.localhost;
using System.Linq;

namespace x360ce.App.Controls
{
	public partial class NewDeviceForm : Form
	{
		public NewDeviceForm()
		{
			InitializeComponent();
		}

		void NewDeviceForm_Load(object sender, EventArgs e)
		{
			configs = new SortableBindingList<Summary>();
			MySettingsDataGridView.AutoGenerateColumns = false;
			MySettingsDataGridView.DataSource = configs;
			WizzardTabControl.TabPages.Remove(Step2TabPage);
			SearchRadioButton.Checked = true;
			FolderPathTextBox.Text = new FileInfo(Application.ExecutablePath).DirectoryName;
			SearchTheInternetCheckBox.Checked = SearchRadioButton.Checked && MainForm.Current.InternetCheckBox.Checked;
		}

		DeviceInstance _di;
		int _padIndex;

		public void LoadData(DeviceInstance di, int padIndex)
		{
			_di = di;
			_padIndex = padIndex;
			Text = string.Format(Text, di.ProductName);
			Step1TabPage.Text = string.Format("{0} - {1:N}", di.ProductName, di.InstanceGuid);
			Step2TabPage.Text = string.Format("{0} - {1:N}", di.ProductName, di.InstanceGuid);
			DescriptionLabel.Text = string.Format(DescriptionLabel.Text, di.ProductName, di.InstanceGuid.ToString("N"));
			BackButton.Visible = false;
			ResultsLabel.Text = "";
			UpdateButtons();
		}

		void BrowseButton_Click(object sender, EventArgs e)
		{
			SettingsFolderBrowserDialog.Description = string.Format("Browse for {0} (*.ini)", GetFileDescription(".ini"));
			SettingsFolderBrowserDialog.SelectedPath = FolderPathTextBox.Text;
			var result = SettingsFolderBrowserDialog.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK)
			{
				FolderPathTextBox.Text = SettingsFolderBrowserDialog.SelectedPath;
			}
		}

		void BrowseRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			UpdateButtons();
		}
		void SearchRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			UpdateButtons();
		}

		void UpdateButtons()
		{
			SearchPictureBox.Image = SearchRadioButton.Checked ? Properties.Resources.arrow_right_16x16 : Properties.Resources.empty_16x16;
			BrowsePictureBox.Image = BrowseRadioButton.Checked ? Properties.Resources.arrow_right_16x16 : Properties.Resources.empty_16x16;
			FolderPathTextBox.Enabled = BrowseRadioButton.Checked;
			BrowseButton.Enabled = BrowseRadioButton.Checked;
			IncludeSubfoldersCheckBox.Enabled = BrowseRadioButton.Checked;
			NextButton.Enabled = SearchRadioButton.Checked || BrowseRadioButton.Checked;
		}

		void NextButton_Click(object sender, EventArgs e)
		{
			if (WizzardTabControl.TabPages.Contains(Step1TabPage))
			{
				WizzardTabControl.TabPages.Remove(Step1TabPage);
				WizzardTabControl.TabPages.Add(Step2TabPage);
				NextButton.Text = "Finish";
				BackButton.Visible = true;
				LoadingCircle = true;
				configs.Clear();
				if (SearchRadioButton.Checked)
				{
					LoadSettingsFromFolder(new FileInfo(Application.ExecutablePath).DirectoryName);
				}
				else
				{
					LoadSettingsFromFolder(FolderPathTextBox.Text);
				}
			}
			else
			{
				// If nothing is selected then just exit
				if (MySettingsDataGridView.SelectedRows.Count == 0)
				{
					this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
				}
				else
				{
					var s = (Summary)MySettingsDataGridView.SelectedRows[0].DataBoundItem;
					// if this is file then...
					if (Guid.Empty.Equals(s.PadSettingChecksum))
					{
						var iniFile = s.FileName;
						var section = s.FileProductName;
						MainForm.Current.SuspendEvents();
						// preset will be stored in inside [PAD1] section;
						SettingManager.Current.ReadPadSettings(iniFile, section, _padIndex);
						MainForm.Current.ResumeEvents();
						this.DialogResult = System.Windows.Forms.DialogResult.OK;
					}
					else
					{
						// it points to internet.
						LoadSetting(s.PadSettingChecksum);
					}
				}
			}
		}

		#region Load Settings from the Internet

		public void LoadSetting(Guid padSettingChecksum)
		{
			var ws = new com.x360ce.localhost.x360ce();
			ws.Url = MainForm.Current.InternetDatabaseUrlTextBox.Text;
			ws.LoadSettingCompleted += new LoadSettingCompletedEventHandler(ws_LoadSettingCompleted);
			ws.LoadSettingAsync(new Guid[] { padSettingChecksum });
		}

		void ws_LoadSettingCompleted(object sender, LoadSettingCompletedEventArgs e)
		{
			if (e.Result.PadSettings.Length == 0)
			{
				MainForm.Current.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: Setting was not found.", DateTime.Now), MessageBoxIcon.Information);
			}
			else
			{
				var padSectionName = SettingManager.Current.GetInstanceSection(_di.InstanceGuid);
				SettingManager.Current.SetPadSetting(padSectionName, _di);
				SettingManager.Current.SetPadSetting(padSectionName, e.Result.PadSettings[0]);
				MainForm.Current.SuspendEvents();
				SettingManager.Current.ReadPadSettings(SettingManager.IniFileName, padSectionName, _padIndex);
				MainForm.Current.ResumeEvents();
				MainForm.Current.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: Settings loaded into '{1}' successfully.", DateTime.Now, (_padIndex + 1) + "." + _di.ProductName), MessageBoxIcon.Information);
			}
			this.DialogResult = System.Windows.Forms.DialogResult.OK;
		}

		#endregion

		void BackButton_Click(object sender, EventArgs e)
		{
			WizzardTabControl.TabPages.Remove(Step2TabPage);
			WizzardTabControl.TabPages.Add(Step1TabPage);
			NextButton.Text = "Next >";
			BackButton.Visible = false;
			LocalLabel.Text = LocalLabel.Text.Replace(" Done", "");
			InternetLabel.Text = InternetLabel.Text.Replace(" Done", "");
			LocalPictureBox.Image = Properties.Resources.check_disabled_16x16;
			InternetPictureBox.Image = Properties.Resources.check_disabled_16x16;
		}

		void SearchLabel_Click(object sender, EventArgs e)
		{
			SearchRadioButton.Checked = true;
		}

		void BrowseLabel_Click(object sender, EventArgs e)
		{
			BrowseRadioButton.Checked = true;
		}

		SortableBindingList<Summary> configs;

		void LoadSettingsFromFolder(string folderName)
		{
			var dir = new DirectoryInfo(folderName);
			SearchOption so = IncludeSubfoldersCheckBox.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
			var files = dir.GetFiles("*.ini", so);
			for (int i = 0; i < files.Length; i++)
			{
				string sectionName = null;
				// If INI File contains settings for instance then...
				if (SettingManager.Current.ContainsInstanceSection(_di.InstanceGuid, files[i].FullName, out sectionName))
				{
					var s = new Summary();
					s.DateUpdated = files[i].LastWriteTime;
					s.FileName = files[i].FullName;
					s.FileProductName = sectionName;
					configs.Add(s);
				}
				if (SettingManager.Current.ContainsInstanceSectionOld(_di.InstanceGuid, files[i].FullName, out sectionName))
				{
					var s = new Summary();
					s.DateUpdated = files[i].LastWriteTime;
					s.FileName = files[i].FullName;
					s.FileProductName = sectionName;
					configs.Add(s);
				}
			}
			LocalPictureBox.Image = Properties.Resources.check_16x16;
			LocalLabel.Text += " Done";
			if (SearchTheInternetCheckBox.Checked)
			{
				LoadSettingsFromInternet();
			}
			else
			{
				Complete();
			}
		}

		List<SearchParameter> _sp;

		void LoadSettingsFromInternet()
		{
			var ws = new com.x360ce.localhost.x360ce();
			ws.Url = MainForm.Current.InternetDatabaseUrlTextBox.Text;
			ws.SearchSettingsCompleted += new SearchSettingsCompletedEventHandler(ws_SearchSettingsCompleted);
			_sp = new List<SearchParameter>();
			_sp.Add(new SearchParameter() { InstanceGuid = _di.InstanceGuid, ProductGuid = _di.ProductGuid });
			MainForm.Current.onlineUserControl1.FillSearchParameterWithFiles(_sp);
			ws.SearchSettingsAsync(_sp.ToArray(), false);
		}

		SearchResult sr;

		void ws_SearchSettingsCompleted(object sender, SearchSettingsCompletedEventArgs e)
		{
			sr = null;
			if (e.Error != null)
			{
				InternetPictureBox.Image = Properties.Resources.delete_16x16;
				return;
			}
			InternetPictureBox.Image = Properties.Resources.check_16x16;
			InternetLabel.Text += " Done";
			sr = e.Result;
			// Reorder summaries
			sr.Summaries = sr.Summaries.OrderBy(x => x.ProductName).ThenBy(x => x.FileName).ThenBy(x => x.FileProductName).ThenByDescending(x => x.Users).ToArray();
			var s = GetBestSetting(e.Result);
			if (s != null) configs.Add(s);
			Complete();
		}

		Summary GetBestSetting(SearchResult sr)
		{
			var sum = new Summary();
			for (int i = 0; i < sr.Settings.Length; i++)
			{
				// Look inside user settings.
				var s = sr.Settings[sr.Settings.Length - i - 1];
				for (int f = 0; f < _sp.Count; f++)
				{
					// if instance, file name and file product name is match then...
					if (s.InstanceGuid.Equals(_di.InstanceGuid) && _sp[f].FileName == s.FileName && _sp[f].FileProductName == s.FileProductName)
					{
						sum.DateUpdated = s.DateUpdated;
						sum.FileName = string.Format("Internet: {0}: {1}", s.FileName, s.FileProductName);
						sum.PadSettingChecksum = s.PadSettingChecksum;
						return sum;
					}
				}
				for (int f = 0; f < _sp.Count; f++)
				{
					// if instance and file name match then...
					if (s.InstanceGuid.Equals(_di.InstanceGuid) && _sp[f].FileName == s.FileName)
					{
						sum.DateUpdated = s.DateUpdated;
						sum.FileName = string.Format("Internet: {0}: {1}", s.FileName, s.FileProductName);
						sum.PadSettingChecksum = s.PadSettingChecksum;
						return sum;
					}
				}
				for (int f = 0; f < _sp.Count; f++)
				{
					// if only instance match then...
					if (s.InstanceGuid.Equals(_di.InstanceGuid))
					{
						sum.DateUpdated = s.DateUpdated;
						sum.FileName = string.Format("Internet: {0}: {1}", s.FileName, s.FileProductName);
						sum.PadSettingChecksum = s.PadSettingChecksum;
						return sum;
					}
				}
			}
			// Order so non empty names and most popular record will come first.
			var summaries = sr.Summaries.OrderByDescending(x => x.ProductName).ThenByDescending(x => x.FileName).ThenByDescending(x => x.FileProductName).ThenByDescending(x => x.Users).ToArray();
			for (int i = 0; i < summaries.Length; i++)
			{
				var s2 = summaries[i];
				// Look inside global settings.
				for (int f = 0; f < _sp.Count; f++)
				{
					// if instance, file name and file product name is match then...
					if (s2.ProductGuid.Equals(_di.ProductGuid) && _sp[f].FileName == s2.FileName && _sp[f].FileProductName == s2.FileProductName)
					{
						sum.DateUpdated = s2.DateUpdated;
						sum.FileName = string.Format("Internet: {0}: {1}", s2.FileName, s2.FileProductName);
						sum.PadSettingChecksum = s2.PadSettingChecksum;
						return sum;
					}
				}
				for (int f = 0; f < _sp.Count; f++)
				{
					// if instance and file name match then...
					if (s2.ProductGuid.Equals(_di.ProductGuid) && _sp[f].FileName == s2.FileName)
					{
						sum.DateUpdated = s2.DateUpdated;
						sum.FileName = string.Format("Internet: {0}: {1}", s2.FileName, s2.FileProductName);
						sum.PadSettingChecksum = s2.PadSettingChecksum;
						return sum;
					}
				}
				for (int f = 0; f < _sp.Count; f++)
				{
					// if only instance match then...
					if (s2.ProductGuid.Equals(_di.ProductGuid))
					{
						sum.DateUpdated = s2.DateUpdated;
						sum.FileName = string.Format("Internet: {0}: {1}", s2.FileName, s2.FileProductName);
						sum.PadSettingChecksum = s2.PadSettingChecksum;
						return sum;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Gets a value that indicates the name of the associated application with the behavior to handle this extension.
		/// </summary>
		/// <param name="fileExtension">File extension.</param>
		public static string GetProgId(string fileExtension)
		{
			var key = Registry.ClassesRoot;
			key = key.OpenSubKey(fileExtension);
			if (key == null) return null;
			var val = key.GetValue("", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
			if (val == null) return string.Empty;
			return val.ToString();
		}

		public bool LoadingCircle
		{
			get { return BusyLoadingCircle.Active; }
			set
			{
				if (value)
				{
					BusyLoadingCircle.Color = System.Drawing.Color.SteelBlue;
					BusyLoadingCircle.InnerCircleRadius = 12;
					BusyLoadingCircle.NumberSpoke = 100;
					BusyLoadingCircle.OuterCircleRadius = 18;
					BusyLoadingCircle.RotationSpeed = 10;
					BusyLoadingCircle.SpokeThickness = 3;
				}
				BusyLoadingCircle.Active = value;
				BusyLoadingCircle.Visible = value;
			}
		}

		/// <summary>
		/// Gets a value that determines what the friendly name of the file is.
		/// </summary>
		/// <param name="fileExtension">File extension.</param>
		public static string GetFileDescription(string fileExtension)
		{
			var progId = GetProgId(fileExtension);
			if (string.IsNullOrEmpty(progId)) return string.Empty;
			var key = Registry.ClassesRoot;
			key = key.OpenSubKey(progId);
			if (key == null) return null;
			var val = key.GetValue("", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
			if (val == null) return string.Empty;
			return val.ToString();
		}

		void Complete()
		{
			SettingsListTabControl.Enabled = configs.Count > 0;
			if (configs.Count == 0)
			{
				ResultsLabel.Text = "Application was unable to find Settings for your Device";
			}
			else
			{
				ResultsLabel.Text = "Please select configuration to load from the list and click [Finish] to continue.";
			}
			LoadingCircle = false;
		}

		void SearchTheInternetCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			InternetPictureBox.Visible = SearchTheInternetCheckBox.Checked;
			InternetLabel.Visible = SearchTheInternetCheckBox.Checked;
		}

	}

}

