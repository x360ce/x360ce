using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX.DirectInput;
using System.IO;
using x360ce.App.com.x360ce.localhost;

namespace x360ce.App.Controls
{
	public partial class OnlineUserControl : UserControl
	{
		public OnlineUserControl()
		{
			InitializeComponent();
			_Settings = new SortableBindingList<Setting>();
			_Summaries = new SortableBindingList<Summary>();
		}

		MainForm mainForm { get { return (MainForm)Parent.Parent.Parent; } }

		private void OnlineUserControl_Load(object sender, EventArgs e)
		{
			MySettingsDataGridView.AutoGenerateColumns = false;
			GlobalSettingsDataGridView.AutoGenerateColumns = false;

			MySettingsDataGridView.DataSource = _Settings;
			GlobalSettingsDataGridView.DataSource = _Summaries;
			OnlineCheckBox_CheckedChanged(null, null);
		}

		private void OnlineCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdateActionButtons();
		}


		List<DeviceInstance> _devices;

		/// <summary>
		/// Update DataGridView is such way that it won't loose selection.
		/// </summary>
		void UpdateList<T>(IList<T> source, IList<T> destination)
		{
			var sCount = source.Count;
			var dCount = destination.Count;
			var length = Math.Min(sCount, dCount);
			for (int i = 0; i < length; i++) destination[i] = source[i];
			// Add extra rows.
			if (sCount > dCount)
			{
				for (int i = dCount; i < sCount; i++) destination.Add(source[i]);
			}
			else if (dCount > sCount)
			{
				for (int i = dCount - 1; i >= sCount; i--) destination.RemoveAt(i);
			}
			UpdateActionButtons();
		}

		public void BindDevices(List<DeviceInstance> list)
		{

			// Check if new device is available
			KeyValuePair selection = null;
			if (_devices != null && _devices.Count < list.Count)
			{
				for (int i = 0; i < list.Count; i++)
				{
					var newDevice = list[i];
					bool isFound = false;
					foreach (var oldDevice in _devices)
					{
						if (newDevice.InstanceGuid.Equals(oldDevice.InstanceGuid))
						{
							isFound = true;
							break;
						}
					}
					if (!isFound)
					{
						selection = new KeyValuePair((i + 1) + ". " + newDevice.ProductName, newDevice.InstanceGuid.ToString());
						break;
					}

				}
			}
			_devices = list;
			// Fill FakeWmi ComboBox.
			var options = new List<KeyValuePair>();
			for (int i = 0; i < list.Count; i++)
			{
				options.Add(new KeyValuePair((i + 1) + ". " + list[i].ProductName, list[i].InstanceGuid.ToString()));
			}
			ControllerComboBox.DataSource = options;
			ControllerComboBox.DisplayMember = "Key";
			ControllerComboBox.ValueMember = "Value";
			ControllerComboBox.Enabled = ControllerComboBox.Items.Count > 1;
			// Restore selection.
			if (selection != null)
			{
				for (int i = 0; i < options.Count; i++)
				{
					if (options[i].Value == selection.Value)
					{
						ControllerComboBox.SelectedIndex = i;
						break;
					}
				}
				UpdateActionButtons();
			}
		}

		List<System.Diagnostics.FileVersionInfo> _files;

		public void BindFiles()
		{
			// Store current selection.
			KeyValuePair selection = null;
			if (GameComboBox.SelectedIndex > -1) selection = (KeyValuePair)GameComboBox.SelectedItem;
			_files = new List<System.Diagnostics.FileVersionInfo>();
			var names = Directory.GetFiles(".", "*.exe");
			var list = new List<FileInfo>();
			foreach (var name in names)
			{
				if (name.EndsWith("\\x360ce.exe")) continue;
				list.Add(new FileInfo(name));
			}
			var options = new List<KeyValuePair>();
			options.Add(new KeyValuePair("", ""));
			foreach (var item in list)
			{
				var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(item.FullName);
				_files.Add(vi);
				var productName = (item.Name + ": " + vi.ProductName).Trim(new char[] { ':', ' ' });
				options.Add(new KeyValuePair(productName, item.Length.ToString()));
			}
			GameComboBox.DataSource = options;
			GameComboBox.DisplayMember = "Key";
			GameComboBox.ValueMember = "Value";
			GameComboBox.Enabled = GameComboBox.Items.Count > 1;
			if (selection == null)
			{
				// Select best option automatically.
				var rxUppercase = new System.Text.RegularExpressions.Regex("[A-Z]");
				var rxLowercase = new System.Text.RegularExpressions.Regex("[A-Z]");
				for (int i = 0; i < options.Count; i++)
				{
					var s = options[i].Key;
					var v = options[i].Value;
					if (rxUppercase.IsMatch(s) && rxLowercase.IsMatch(s) && s.Contains(" ")) { GameComboBox.SelectedIndex = i; break; }
					else if (rxLowercase.IsMatch(s) && s.Contains(" ")) { GameComboBox.SelectedIndex = i; break; }
					else if (rxLowercase.IsMatch(s)) { GameComboBox.SelectedIndex = i; break; }
					// Select if value is not empty.
					else if (rxLowercase.IsMatch(v) || rxUppercase.IsMatch(v)) { GameComboBox.SelectedIndex = i; break; }
				}
			}
			else
			{
				// Restore selection.
				for (int i = 0; i < options.Count; i++)
				{
					if (options[i].Key == selection.Key && options[i].Value == selection.Value)
					{
						GameComboBox.SelectedIndex = i;
						break;
					}
				}
			}
			UpdateActionButtons();
		}

		private void ControllerComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateActionButtons();
		}

		private void GameComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateActionButtons();
		}

		Setting CurrentSetting;

		void UpdateActionButtons()
		{
			SaveButton.Enabled = ControllerComboBox.Items.Count > 0;
			LoadButton.Enabled = ControllerComboBox.Items.Count > 0 && MySettingsDataGridView.SelectedRows.Count == 1;
			DeleteButton.Enabled = ControllerComboBox.Items.Count > 0 && MySettingsDataGridView.SelectedRows.Count == 1;
			CurrentSetting = GetCurrentSetting();
			SaveButton.Image = ContainsSetting(CurrentSetting) ? Properties.Resources.save_16x16 : Properties.Resources.save_add_16x16;
			MySettingsDataGridView.Refresh();
		}

		bool ContainsSetting(Setting setting)
		{

			for (int i = 0; i < _Settings.Count; i++)
			{
				var s = _Settings[i];
				if (setting.InstanceGuid == s.InstanceGuid && setting.FileName == s.FileName && setting.FileProductName == s.FileProductName)
				{
					return true;
				}
			}
			return false;
		}

		#region Web Services

		Setting GetCurrentSetting()
		{
			var s = new Setting();
			if (ControllerComboBox.SelectedIndex > -1)
			{
				s.InstanceGuid = _devices[ControllerComboBox.SelectedIndex].InstanceGuid;
				if (GameComboBox.SelectedIndex > 0)
				{
					var fi = _files[GameComboBox.SelectedIndex - 1];
					s.FileName = System.IO.Path.GetFileName(fi.FileName);
					s.FileProductName = fi.ProductName;
				}
				else
				{
					s.FileName = "";
					s.FileProductName = "";
				}
			}
			return s;
		}

		private void SaveButton_Click(object sender, EventArgs e)
		{
			mainForm.LoadingCircle = true;
			var s = new Setting();
			var di = _devices[ControllerComboBox.SelectedIndex];
			s.Comment = CommentTextBox.Text;
			s.InstanceGuid = di.InstanceGuid;
			s.InstanceName = di.InstanceName;
			s.ProductGuid = di.ProductGuid;
			s.ProductName = di.ProductName;
			s.DeviceType = (int)di.DeviceType;
			s.IsEnabled = true;
			if (GameComboBox.SelectedIndex > 0)
			{
				var fi = _files[GameComboBox.SelectedIndex - 1];
				s.FileName = System.IO.Path.GetFileName(fi.FileName);
				s.FileProductName = fi.ProductName;
			}
			else
			{
				s.FileName = "";
				s.FileProductName = "";
			}
			var padSectionName = SettingManager.Current.GetInstanceSection(di.InstanceGuid);
			var ps = SettingManager.Current.GetPadSetting(padSectionName);
			var ws = new com.x360ce.localhost.x360ce();
			ws.Url = OnlineDatabaseUrlTextBox.Text;
			ws.SaveSettingCompleted += new SaveSettingCompletedEventHandler(ws_SaveSettingCompleted);
			ws.SaveSettingAsync(s, ps);
		}

		void ws_SaveSettingCompleted(object sender, SaveSettingCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				mainForm.UpdateHelpHeader(e.Error.Message, MessageBoxIcon.Error);
				throw e.Error;
			}
			else if (!string.IsNullOrEmpty(e.Result))
			{
				mainForm.UpdateHelpHeader(e.Result, MessageBoxIcon.Error);
			}
			else
			{
				var name = ((KeyValuePair)ControllerComboBox.SelectedItem).Key;
				mainForm.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: '{1}' settings saved successfully.", DateTime.Now, name), MessageBoxIcon.Information);
			}
			RefreshGrid(false);
		}

		private void DeleteButton_Click(object sender, EventArgs e)
		{
			mainForm.LoadingCircle = true;
			var setting = (Setting)MySettingsDataGridView.SelectedRows[0].DataBoundItem;
			var ws = new com.x360ce.localhost.x360ce();
			ws.Url = OnlineDatabaseUrlTextBox.Text;
			ws.DeleteSettingCompleted += new DeleteSettingCompletedEventHandler(ws_DeleteSettingCompleted);
			ws.DeleteSettingAsync(setting);
		}

		void ws_DeleteSettingCompleted(object sender, DeleteSettingCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				mainForm.UpdateHelpHeader(e.Error.Message, MessageBoxIcon.Error);
				throw e.Error;
			}
			else if (!string.IsNullOrEmpty(e.Result))
			{
				mainForm.UpdateHelpHeader(e.Result, MessageBoxIcon.Error);
			}
			else
			{
				var name = ((KeyValuePair)ControllerComboBox.SelectedItem).Key;
				mainForm.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: '{1}' setting deleted successfully.", DateTime.Now, name), MessageBoxIcon.Information);
			}
			RefreshGrid(false);
		}

		private void RefreshButton_Click(object sender, EventArgs e)
		{
			RefreshGrid(true);
		}

		private void RefreshGrid(bool showResult)
		{
			mainForm.LoadingCircle = true;
			var sp = new List<SearchParameter>();
			FillSearchParameterWithDevices(sp);
			FillSearchParameterWithFiles(sp);
			var ws = new com.x360ce.localhost.x360ce();
			ws.Url = OnlineDatabaseUrlTextBox.Text;
			ws.SearchSettingsCompleted += new SearchSettingsCompletedEventHandler(ws_SearchSettingsCompleted);
			ws.SearchSettingsAsync(sp.ToArray(), showResult);
		}


		public void FillSearchParameterWithDevices(List<SearchParameter> sp)
		{
			SearchParameter p;
			// Add controllers.
			for (int i = 0; i < _devices.Count; i++)
			{
				p = new SearchParameter();
				p.ProductGuid = _devices[i].ProductGuid;
				p.InstanceGuid = _devices[i].InstanceGuid;
				sp.Add(p);
			}
		}

		public void FillSearchParameterWithFiles(List<SearchParameter> sp)
		{
			SearchParameter p;
			// Add files.
			for (int i = 0; i < _files.Count; i++)
			{
				p = new SearchParameter();
				p.FileName = System.IO.Path.GetFileName(_files[i].FileName);
				p.FileProductName = _files[i].ProductName;
				sp.Add(p);
			}
		}


		private void LoadSetting()
		{
			mainForm.UpdateTimer.Stop();
			if (MySettingsDataGridView.SelectedRows.Count == 0) return;
			var s = (Setting)MySettingsDataGridView.SelectedRows[0].DataBoundItem;
			var name = ((KeyValuePair)ControllerComboBox.SelectedItem).Key;
			var message = "Do you want to load configuration:";
			message += "\r\n    " + s.ProductName;
			if (!string.IsNullOrEmpty(s.FileName)) message += "\r\n    " + s.FileName;
			if (!string.IsNullOrEmpty(s.FileProductName)) message += ": " + s.FileProductName;
			message += "\r\nfor:\r\n    " + name + "?";
			var result = MessageBox.Show(message, "Load Configuration", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
			if (result == DialogResult.Yes)
			{
				LoadSetting(s.PadSettingChecksum);
			}
			else
			{
				mainForm.UpdateTimer.Start();
			}
		}

		public void LoadSetting(Guid padSettingChecksum)
		{
			var ws = new com.x360ce.localhost.x360ce();
			ws.Url = OnlineDatabaseUrlTextBox.Text;
			ws.LoadSettingCompleted += new LoadSettingCompletedEventHandler(ws_LoadSettingCompleted);
			ws.LoadSettingAsync(new Guid[] { padSettingChecksum });
		}

		void ws_LoadSettingCompleted(object sender, LoadSettingCompletedEventArgs e)
		{
			if (e.Result.PadSettings.Length == 0)
			{
				mainForm.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: Setting was not found.", DateTime.Now), MessageBoxIcon.Information);
			}
			else
			{
				var di = _devices[ControllerComboBox.SelectedIndex];
				var padSectionName = SettingManager.Current.GetInstanceSection(di.InstanceGuid);
				SettingManager.Current.SetPadSetting(padSectionName, e.Result.PadSettings[0]);
				MainForm.Current.SuspendEvents();
				SettingManager.Current.ReadPadSettings(SettingManager.Current.iniFile, padSectionName, ControllerComboBox.SelectedIndex);
				MainForm.Current.ResumeEvents();
				var name = ((KeyValuePair)ControllerComboBox.SelectedItem).Key;
				mainForm.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: Settings loaded into '{1}' successfully.", DateTime.Now, name), MessageBoxIcon.Information);
				// Save setting and notify if vaue changed.
				if (SettingManager.Current.SaveSettings()) MainForm.Current.NotifySettingsChange();

			
			}
		}

		BindingList<Setting> _Settings;
		BindingList<Summary> _Summaries;

		void ws_SearchSettingsCompleted(object sender, SearchSettingsCompletedEventArgs e)
		{
			if (e.Result == null)
			{
				UpdateList(new List<Setting>(), _Settings);
				UpdateList(new List<Summary>(), _Summaries);
				if ((bool)e.UserState)
				{
					mainForm.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: No data received.", DateTime.Now), MessageBoxIcon.Information);
				}
			}
			else
			{
				UpdateList(e.Result.Settings, _Settings);
				UpdateList(e.Result.Summaries, _Summaries);
				if ((bool)e.UserState)
				{
					mainForm.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: {1} Your Settings and {2} General Settings received.", DateTime.Now, e.Result.Settings.Length, e.Result.Summaries.Length), MessageBoxIcon.Information);
				}
			}
			
			mainForm.LoadingCircle = false;
		}

		private void LoadButton_Click(object sender, EventArgs e)
		{
			LoadSetting();
		}

		#endregion

		void UpdateCellStyle(DataGridView grid, DataGridViewCellFormattingEventArgs e, Guid? checksum)
		{
			var v = e.Value.ToString().Substring(0, 8).ToUpper();
			var s = checksum.HasValue ? checksum.Value.ToString().Substring(0, 8).ToUpper() : null;
			var match = v == s;
			e.Value = e.Value.ToString().Substring(0, 8).ToUpper();
			e.CellStyle.BackColor = match ? System.Drawing.Color.FromArgb(255, 222, 225, 231) : e.CellStyle.BackColor = grid.DefaultCellStyle.BackColor;
		}

		#region My Settings Grid

		Setting MySettingSelection;

		Color currentColor = System.Drawing.Color.FromArgb(255, 191, 210, 249);

		private void MySettingsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var setting = ((Setting)grid.Rows[e.RowIndex].DataBoundItem);
			var isCurrent = setting.InstanceGuid == CurrentSetting.InstanceGuid && setting.FileName == CurrentSetting.FileName && setting.FileProductName == CurrentSetting.FileProductName;
			if (e.ColumnIndex == grid.Columns[MySidColumn.Name].Index)
			{
				UpdateCellStyle(grid, e, MySettingSelection == null ? null : (Guid?)MySettingSelection.PadSettingChecksum);
			}
			else if (e.ColumnIndex == grid.Columns[MyIconColumn.Name].Index)
			{
				e.Value = isCurrent ? SaveButton.Image : Properties.Resources.empty_16x16;
			}
			else
			{
				//var row = grid.Rows[e.RowIndex].Cells[MyIconColumn.Name];

				//e.CellStyle.BackColor = isCurrent ? currentColor : grid.DefaultCellStyle.BackColor;
			}
		}

		private void MySettingsDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			var grid = (DataGridView)sender;
			MySettingSelection = grid.SelectedRows.Count == 0 ? null : (Setting)grid.SelectedRows[0].DataBoundItem;
			CommentSelectedTextBox.Text = grid.SelectedRows.Count == 0 ? "" : MySettingSelection.Comment;
			grid.Refresh();
		}

		#endregion

		#region Global Settings Grid

		Summary GlobalSettingSelection;

		private void GlobalSettingsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (e.ColumnIndex == grid.Columns[SidColumn.Name].Index)
			{
				UpdateCellStyle(grid, e, GlobalSettingSelection == null ? null : (Guid?)GlobalSettingSelection.PadSettingChecksum);
			}
		}

		private void GlobalSettingsDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			var grid = (DataGridView)sender;
			GlobalSettingSelection = grid.SelectedRows.Count == 0 ? null : (Summary)grid.SelectedRows[0].DataBoundItem;
			grid.Refresh();
		}

		#endregion

		private void OnlineUserControl_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.R:
					if (e.Control) RefreshGrid(true);
					break;
				case Keys.F5:
					RefreshGrid(true);
					break;
				case Keys.M:
					if (e.Alt) SettingsListTabControl.SelectedTab = MySettingsTabPage;
					break;
				case Keys.G:
					if (e.Alt) SettingsListTabControl.SelectedTab = GlobalSettingsTabPage;
					break;
				default:
					break;
			}
		}

		private void MySettingsDataGridView_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			LoadSetting();
		}



	}
}
