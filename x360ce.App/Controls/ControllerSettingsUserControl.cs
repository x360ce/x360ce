using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using SharpDX.DirectInput;
using System.IO;
using System.Linq;
using x360ce.Engine.Data;
using x360ce.Engine;
using System.Text.RegularExpressions;
using JocysCom.ClassLibrary.Runtime;

namespace x360ce.App.Controls
{
	public partial class ControllerSettingsUserControl : UserControl
	{
		public ControllerSettingsUserControl()
		{
			InitializeComponent();
			MapToAutoMenuItem.Tag = MapTo.None;
			MapToController1MenuItem.Tag = MapTo.Controller1;
			MapToController2MenuItem.Tag = MapTo.Controller2;
			MapToController3MenuItem.Tag = MapTo.Controller3;
			MapToController4MenuItem.Tag = MapTo.Controller4;
			MapToDisabledMenuItem.Tag = MapTo.Disabled;
		}

		MainForm mainForm { get { return (MainForm)Parent.Parent.Parent; } }
		string _myControllersTitle = "";
		string _globalSummariesTitle = "";
		string _globalPresetsTitle = "";

		void InternetUserControl_Load(object sender, EventArgs e)
		{
			_myControllersTitle = MyDeviceSettingsTabPage.Text;
			_globalSummariesTitle = SummariesTabPage.Text;
			_globalPresetsTitle = PresetsTabPage.Text;
			EngineHelper.EnableDoubleBuffering(MyDevicesDataGridView);
			EngineHelper.EnableDoubleBuffering(SummariesDataGridView);
			EngineHelper.EnableDoubleBuffering(PresetsDataGridView);
			MyDevicesDataGridView.AutoGenerateColumns = false;
			SummariesDataGridView.AutoGenerateColumns = false;
			PresetsDataGridView.AutoGenerateColumns = false;
			InitData();
			InternetCheckBox_CheckedChanged(null, null);
		}

		void InitData() {
			// Configure Settings.
			SettingManager.Settings.Items.ListChanged += new ListChangedEventHandler(Settings_ListChanged);
			MyDevicesDataGridView.DataSource = SettingManager.Settings.Items;
			UpdateControlsFromSettings();
			// Configure Summaries.
			SettingManager.Summaries.Items.ListChanged += new ListChangedEventHandler(Summaries_ListChanged);
			SummariesDataGridView.DataSource = SettingManager.Summaries.Items;
			UpdateControlsFromSummaries();
			// Configure Presets.
			SettingManager.Presets.Items.ListChanged += new ListChangedEventHandler(Presets_ListChanged);
			PresetsDataGridView.DataSource = SettingManager.Presets.Items;
			UpdateControlsFromPresets();
		}

		void UpdateControlsFromSettings()
		{
			MyDeviceSettingsTabPage.Text = SettingManager.Settings.Items.Count == 0
				? _myControllersTitle
				: string.Format("{0} [{1}]", _myControllersTitle, SettingManager.Settings.Items.Count);
		}

		void Settings_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromSettings();
		}

		void UpdateControlsFromSummaries()
		{
			SummariesTabPage.Text = SettingManager.Summaries.Items.Count == 0
				? _globalSummariesTitle
				: string.Format("{0} [{1}]", _globalSummariesTitle, SettingManager.Summaries.Items.Count);
		}

		void Summaries_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromSummaries();
        }

		void UpdateControlsFromPresets()
		{
			PresetsTabPage.Text = SettingManager.Presets.Items.Count == 0
				? _globalSummariesTitle
				: string.Format("{0} [{1}]", _globalSummariesTitle, SettingManager.Presets.Items.Count);
		}

		void Presets_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromPresets();
		}


		void InternetCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdateActionButtons();
		}


		DeviceInstance[] _devices = new DeviceInstance[4];

		/// <summary>
		/// Update DataGridView is such way that it won't loose selection.
		/// </summary>
		void UpdateList<T>(IList<T> source, IList<T> destination)
		{
			if (source == null) source = new List<T>();
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

		public void BindDevices(DeviceInstance[] list)
		{

			// Check if new device is available
			KeyValuePair selection = null;
			for (int i = 0; i < 4; i++)
			{
				var newDevice = list[i];
				if (newDevice == null) continue;
				bool isFound = false;
				foreach (var oldDevice in _devices)
				{
					if (oldDevice == null) continue;
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
			_devices = list;
			// Fill FakeWmi ComboBox.
			var options = new List<KeyValuePair>();
			for (int i = 0; i < 4; i++)
			{
				if (list[i] != null)
				{
					options.Add(new KeyValuePair((i + 1) + ". " + list[i].ProductName, list[i].InstanceGuid.ToString()));
				}
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
			var names = AppHelper.GetFiles(".", "*.exe");
			var list = new List<FileInfo>();
			foreach (var name in names)
			{
				if (name.Contains("x360ce")) continue;
				list.Add(new FileInfo(name));
			}
			var options = new List<KeyValuePair>();
			options.Add(new KeyValuePair("", ""));
			foreach (var item in list)
			{
				var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(item.FullName);
				_files.Add(vi);
				var productName = (item.Name + ": " + EngineHelper.FixName(vi.ProductName, item.Name)).Trim(new char[] { ':', ' ' });
				options.Add(new KeyValuePair(productName, item.Length.ToString()));
			}
			GameComboBox.DataSource = options;
			GameComboBox.DisplayMember = "Key";
			GameComboBox.ValueMember = "Value";
			GameComboBox.Enabled = GameComboBox.Items.Count > 1;
			if (selection == null)
			{
				// Select best option automatically.
				var rxUppercase = new Regex("[A-Z]");
				var rxLowercase = new Regex("[A-Z]");
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

		void ControllerComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateActionButtons();
		}

		void GameComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateActionButtons();
		}

		Setting CurrentSetting;

		bool refreshed = false;

		void UpdateActionButtons()
		{
			var settingsSelected = MyDevicesDataGridView.SelectedRows.Count;
			var controllerSelected = ControllerComboBox.Items.Count > 0;
			MySettingsLoadButton.Enabled = controllerSelected && settingsSelected > 0;
			GlobalSettingsLoadButton.Enabled = controllerSelected && SummariesDataGridView.SelectedRows.Count > 0;
			PresetsLoadButton.Enabled = controllerSelected && PresetsDataGridView.SelectedRows.Count > 0;
			CurrentSetting = GetCurrentSetting();
			MySettingsSaveButton.Enabled = controllerSelected && refreshed;
			MySettingsSaveButton.Image = ContainsSetting(CurrentSetting)
				? Properties.Resources.save_16x16
				: Properties.Resources.save_add_16x16;
			MySettingsDeleteButton.Enabled = settingsSelected == 1;
			MapToDropDownButton.Enabled = settingsSelected > 0;
			MyDevicesDataGridView.Refresh();
		}

		bool ContainsSetting(Setting setting)
		{

			for (int i = 0; i < SettingManager.Settings.Items.Count; i++)
			{
				var s = SettingManager.Settings.Items[i];
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
				if (_devices[ControllerComboBox.SelectedIndex] != null)
				{
					s.InstanceGuid = _devices[ControllerComboBox.SelectedIndex].InstanceGuid;
				}
				if (GameComboBox.SelectedIndex > 0)
				{
					var fi = _files[GameComboBox.SelectedIndex - 1];
					var fileName = fi.FileName;
					s.FileName = System.IO.Path.GetFileName(fileName);
					s.FileProductName = EngineHelper.FixName(fi.ProductName, fileName);
				}
				else
				{
					s.FileName = "";
					s.FileProductName = "";
				}
			}
			return s;
		}

		void ws_SaveSettingCompleted(object sender, ResultEventArgs e)
		{
			if (e.Error != null)
			{
				mainForm.UpdateHelpHeader(e.Error.Message, MessageBoxIcon.Error);
				throw e.Error;
			}
			var result = (string)e.Result;
			if (!string.IsNullOrEmpty(result))
			{
				mainForm.UpdateHelpHeader(result, MessageBoxIcon.Error);
			}
			else
			{
				var name = ((KeyValuePair)ControllerComboBox.SelectedItem).Key;
				mainForm.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: '{1}' settings saved successfully.", DateTime.Now, name), MessageBoxIcon.Information);
			}
			RefreshGrid(false);
		}

		void ws_DeleteSettingCompleted(object sender, ResultEventArgs e)
		{
			if (e.Error != null)
			{
				mainForm.UpdateHelpHeader(e.Error.Message, MessageBoxIcon.Error);
				throw e.Error;
			}
			var result = (string)e.Result;
			if (!string.IsNullOrEmpty(result))
			{
				mainForm.UpdateHelpHeader(result, MessageBoxIcon.Error);
			}
			else
			{
				var name = ((KeyValuePair)ControllerComboBox.SelectedItem).Key;
				mainForm.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: '{1}' setting deleted successfully.", DateTime.Now, name), MessageBoxIcon.Information);
			}
			RefreshGrid(false);
		}

		public void RefreshGrid(bool showResult)
		{
			mainForm.LoadingCircle = true;
			var sp = new List<SearchParameter>();
			FillSearchParameterWithDevices(sp);
			FillSearchParameterWithFiles(sp);
			var ws = new WebServiceClient();
			ws.Url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
			ws.SearchSettingsCompleted += ws_SearchSettingsCompleted;
			System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
			{
				ws.SearchSettingsAsync(sp.ToArray(), showResult);
			});
		}

		public void FillSearchParameterWithDevices(List<SearchParameter> sp)
		{
			SearchParameter p;
			// Add controllers.
			for (int i = 0; i < 4; i++)
			{
				var device = _devices[i];
				if (device == null) continue;
                p = new SearchParameter();
				p.ProductGuid = device.ProductGuid;
				p.InstanceGuid = device.InstanceGuid;
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
				p.FileProductName = EngineHelper.FixName(_files[i].ProductName, p.FileName);
				sp.Add(p);
			}
		}

		public void LoadPreset(string controllerName)
		{
			string name = controllerName.Replace(" ", "_");
			mainForm.LoadPreset(name, ControllerComboBox.SelectedIndex);
		}

		public void LoadSetting(Guid padSettingChecksum)
		{
			var ws = new WebServiceClient();
			ws.Url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
			ws.LoadSettingCompleted += ws_LoadSettingCompleted;
			ws.LoadSettingAsync(new Guid[] { padSettingChecksum });
		}

		void ws_LoadSettingCompleted(object sender, ResultEventArgs e)
		{
			var result = (SearchResult)e.Result;
			if (result.PadSettings.Length == 0)
			{
				mainForm.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: Setting was not found.", DateTime.Now), MessageBoxIcon.Information);
			}
			else
			{
				var di = _devices[ControllerComboBox.SelectedIndex];
				var padSectionName = SettingManager.Current.GetInstanceSection(di.InstanceGuid);
				SettingManager.Current.SetPadSetting(padSectionName, di);
				SettingManager.Current.SetPadSetting(padSectionName, result.PadSettings[0]);
				MainForm.Current.SuspendEvents();
				SettingManager.Current.ReadPadSettings(SettingManager.IniFileName, padSectionName, ControllerComboBox.SelectedIndex);
				MainForm.Current.ResumeEvents();
				var name = ((KeyValuePair)ControllerComboBox.SelectedItem).Key;
				mainForm.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: Settings loaded into '{1}' successfully.", DateTime.Now, name), MessageBoxIcon.Information);
				// Save setting and notify if value changed.
				SettingManager.Current.SaveSettings();
				MainForm.Current.NotifySettingsChange();
				mainForm.UpdateTimer.Start();
			}
		}

		void ws_SearchSettingsCompleted(object sender, ResultEventArgs e)
		{
			var ws = (WebServiceClient)sender;
			ws.SearchSettingsCompleted -= ws_SearchSettingsCompleted;
			// Make sure method is executed on the same thread as this control.
			BeginInvoke((MethodInvoker)delegate ()
			{
				refreshed = true;
				if (e.Error != null || e.Result == null)
				{
					UpdateList(new List<Setting>(), SettingManager.Settings.Items);
					UpdateList(new List<Summary>(), SettingManager.Summaries.Items);
					if ((bool)e.UserState)
					{
						mainForm.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: No data received.", DateTime.Now), MessageBoxIcon.Information);
					}
				}
				else
				{
					var result = (SearchResult)e.Result;
					// Reorder summaries.
					result.Summaries = result.Summaries.OrderBy(x => x.ProductName).ThenBy(x => x.FileName).ThenBy(x => x.FileProductName).ThenByDescending(x => x.Users).ToArray();
					UpdateList(result.Settings, SettingManager.Settings.Items);
					UpdateList(result.Summaries, SettingManager.Summaries.Items);
					if ((bool)e.UserState)
					{
						mainForm.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: {1} Your Settings and {2} General Settings received.", DateTime.Now, result.Settings.Length, result.Summaries.Length), MessageBoxIcon.Information);
					}
				}
				mainForm.LoadingCircle = false;
			});
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

		#region Settings Grid

		Setting SettingSelection;

		Color currentColor = System.Drawing.Color.FromArgb(255, 191, 210, 249);

		void SettingsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var setting = ((Setting)grid.Rows[e.RowIndex].DataBoundItem);
			var isCurrent = setting.InstanceGuid == CurrentSetting.InstanceGuid && setting.FileName == CurrentSetting.FileName && setting.FileProductName == CurrentSetting.FileProductName;
			if (e.ColumnIndex == grid.Columns[MySidColumn.Name].Index)
			{
				UpdateCellStyle(grid, e, SettingSelection == null ? null : (Guid?)SettingSelection.PadSettingChecksum);
			}
			else if (e.ColumnIndex == grid.Columns[MapToColumn.Name].Index)
			{
				e.Value = Attributes.GetDescription((MapTo)setting.MapTo);
			}
			else if (e.ColumnIndex == grid.Columns[MyIconColumn.Name].Index)
			{
				e.Value = isCurrent ? MySettingsSaveButton.Image : Properties.Resources.empty_16x16;
			}
			else
			{
				//var row = grid.Rows[e.RowIndex].Cells[MyIconColumn.Name];
				//e.CellStyle.BackColor = isCurrent ? currentColor : grid.DefaultCellStyle.BackColor;
			}
		}

		void SettingsDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			var grid = (DataGridView)sender;
			var rows = grid.SelectedRows;
            SettingSelection = rows.Count == 0 ? null : (Setting)rows[0].DataBoundItem;
			CommentSelectedTextBox.Text = rows.Count == 0 ? "" : SettingSelection.Comment;
			UpdateActionButtons();
			grid.Refresh();
		}

		#endregion

		#region Summaries Grid

		Summary SummariesSelection;

		void SummariesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (e.ColumnIndex == grid.Columns[SidColumn.Name].Index)
			{
				UpdateCellStyle(grid, e, SummariesSelection == null ? null : (Guid?)SummariesSelection.PadSettingChecksum);
			}
		}

		void SummariesDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			var grid = (DataGridView)sender;
			SummariesSelection = grid.SelectedRows.Count == 0 ? null : (Summary)grid.SelectedRows[0].DataBoundItem;
			grid.Refresh();
		}

		#endregion

		#region Presets Grid

		//BindingList<PresetItem> PresetsList = new BindingList<PresetItem>();

		//public void InitPresets()
		//{
		//	PresetsDataGridView.DataSource = null;
		//	PresetsList.Clear();
		//	var prefix = System.IO.Path.GetFileNameWithoutExtension(SettingManager.IniFileName);
		//	var ext = System.IO.Path.GetExtension(SettingManager.IniFileName);
		//	string name;
		//	// Presets: Embedded.
		//	var embeddedPresets = new List<string>();
		//	var assembly = System.Reflection.Assembly.GetExecutingAssembly();
		//	string[] files = assembly.GetManifestResourceNames();
		//	var pattern = string.Format("Presets\\.{0}\\.(?<name>.*?){1}", prefix, ext);
		//	Regex rx = new Regex(pattern);
		//	for (int i = 0; i < files.Length; i++)
		//	{
		//		if (rx.IsMatch(files[i]))
		//		{
		//			name = rx.Match(files[i]).Groups["name"].Value.Replace("_", " ");
		//			embeddedPresets.Add(name);
		//		}
		//	}
		//	// Presets: Custom.
		//	var dir = new System.IO.DirectoryInfo(".");
		//	var fis = dir.GetFiles(string.Format("{0}.*{1}", prefix, ext));
		//	List<string> customPresets = new List<string>();
		//	for (int i = 0; i < fis.Length; i++)
		//	{
		//		name = fis[i].Name.Substring(prefix.Length + 1);
		//		name = name.Substring(0, name.Length - ext.Length);
		//		name = name.Replace("_", " ");
		//		if (!embeddedPresets.Contains(name)) customPresets.Add(name);
		//	}
		//	string[] cNames = customPresets.ToArray();
		//	Array.Sort(cNames);
		//	foreach (var item in cNames) PresetsList.Add(new PresetItem("Custom", item));
		//	PresetTypeColumn.Visible = cNames.Count() > 0;
		//	string[] eNames = embeddedPresets.ToArray();
		//	Array.Sort(eNames);
		//	foreach (var item in eNames)
		//	{
		//		if (item != "Clear") PresetsList.Add(new PresetItem("Embedded", item));
		//	}
		//	PresetsDataGridView.DataSource = PresetsList;
		//}

		#endregion

		void InternetUserControl_KeyDown(object sender, KeyEventArgs e)
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
					if (e.Alt) SettingsListTabControl.SelectedTab = MyDeviceSettingsTabPage;
					break;
				case Keys.G:
					if (e.Alt) SettingsListTabControl.SelectedTab = SummariesTabPage;
					break;
				default:
					break;
			}
		}

		void SettingsListTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateActionButtons();
		}

		private void PresetsDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			UpdateActionButtons();
		}

		private void MySettingsSaveButton_Click(object sender, EventArgs e)
		{
			mainForm.LoadingCircle = true;
			var s = new Setting();
			var di = _devices[ControllerComboBox.SelectedIndex];
			s.Comment = CommentTextBox.Text;
			s.InstanceGuid = di.InstanceGuid;
			s.InstanceName = di.InstanceName;
			s.ProductGuid = di.ProductGuid;
			s.ProductName = di.ProductName;
			s.DeviceType = (int)di.Type;
			s.IsEnabled = true;
			if (GameComboBox.SelectedIndex > 0)
			{
				var fi = _files[GameComboBox.SelectedIndex - 1];
				s.FileName = System.IO.Path.GetFileName(fi.FileName);
				s.FileProductName = EngineHelper.FixName(fi.ProductName, s.FileName);
			}
			else
			{
				s.FileName = "";
				s.FileProductName = "";
			}
			var padSectionName = SettingManager.Current.GetInstanceSection(di.InstanceGuid);
			var ps = SettingManager.Current.GetPadSetting(padSectionName);
			var ws = new WebServiceClient();
			ws.Url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
			ws.SaveSettingCompleted += ws_SaveSettingCompleted;
			ws.SaveSettingAsync(s, ps);
		}

		private void MySettingsDeleteButton_Click(object sender, EventArgs e)
		{
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var result = form.ShowForm("Do you really want to delete selected setting from Internet Settings Database?", MainForm.Current.Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
			if (result == DialogResult.Yes)
			{
				mainForm.LoadingCircle = true;
				var setting = (Setting)MyDevicesDataGridView.SelectedRows[0].DataBoundItem;
				var ws = new WebServiceClient();
				ws.Url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
				ws.DeleteSettingCompleted += ws_DeleteSettingCompleted;
				ws.DeleteSettingAsync(setting);
			}
		}

		void LoadMySetting()
		{
			mainForm.UpdateTimer.Stop();
			if (ControllerComboBox.SelectedItem == null) return;
			var name = ((KeyValuePair)ControllerComboBox.SelectedItem).Key;
			if (MyDevicesDataGridView.SelectedRows.Count == 0) return;
			var title = "Load My Setting?";
			var setting = (Setting)MyDevicesDataGridView.SelectedRows[0].DataBoundItem;
			var message = "Do you want to load My Setting:";
			message += "\r\n\r\n    " + setting.ProductName;
			if (!string.IsNullOrEmpty(setting.FileName)) message += " | " + setting.FileName;
			if (!string.IsNullOrEmpty(setting.FileProductName)) message += " | " + setting.FileProductName;
			message += "\r\n\r\nfor \"" + name + "\" controller?";
			MessageBoxForm form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var result = form.ShowForm(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
			if (result == DialogResult.Yes) LoadSetting(setting.PadSettingChecksum);
			else mainForm.UpdateTimer.Start();
		}

		void SettingsDataGridView_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			LoadMySetting();
		}

		private void MySettingsLoadButton_Click(object sender, EventArgs e)
		{
			LoadMySetting();
		}

		private void MySettingsRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshGrid(true);
		}

		void LoadGlobalSetting()
		{
			mainForm.UpdateTimer.Stop();
			if (ControllerComboBox.SelectedItem == null) return;
			var name = ((KeyValuePair)ControllerComboBox.SelectedItem).Key;
			if (SummariesDataGridView.SelectedRows.Count == 0) return;
			var title = "Load Global Setting?";
			var summary = (Summary)SummariesDataGridView.SelectedRows[0].DataBoundItem;
			var message = "Do you want to load Global Setting:";
			message += "\r\n\r\n    " + summary.ProductName;
			message += "\r\n\r\nfor \"" + name + "\" controller?";
			MessageBoxForm form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var result = form.ShowForm(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
			if (result == DialogResult.Yes) LoadSetting(summary.PadSettingChecksum);
			else mainForm.UpdateTimer.Start();
		}

		private void GlobalSettingsLoadButton_Click(object sender, EventArgs e)
		{
			LoadGlobalSetting();
		}

		void SummariesDataGridView_DoubleClick(object sender, EventArgs e)
		{
			LoadGlobalSetting();
		}

		private void GlobalSettingsRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshGrid(true);
		}

		void LoadPreset()
		{
			mainForm.UpdateTimer.Stop();
			if (ControllerComboBox.SelectedItem == null) return;
			var name = ((KeyValuePair)ControllerComboBox.SelectedItem).Key;
			if (PresetsDataGridView.SelectedRows.Count == 0) return;
			var title = "Load Preset Setting?";
			var preset = (Preset)PresetsDataGridView.SelectedRows[0].DataBoundItem;
			var message = "Do you want to load Preset Setting:";
			message += "\r\n\r\n    " + preset.ProductName;
			message += "\r\n\r\nfor \"" + name + "\" controller?";
			MessageBoxForm form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var result = form.ShowForm(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
			if (result == DialogResult.Yes) LoadPreset(preset.ProductName);
			else mainForm.UpdateTimer.Start();
		}

		private void PresetsLoadButton_Click(object sender, EventArgs e)
		{
			LoadPreset();
		}

		private void PresetsDataGridView_DoubleClick(object sender, EventArgs e)
		{
			LoadPreset();
		}

		#region Presets.

		private void PresetRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshPresetsGrid(true);
		}

		public void RefreshPresetsGrid(bool showResult)
		{
			mainForm.LoadingCircle = true;
			var sp = new List<SearchParameter>();
			sp.Add(new SearchParameter());
			var ws = new WebServiceClient();
			ws.Url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
			ws.SearchSettingsCompleted += wsPresets_SearchSettingsCompleted;
			System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
			{
				ws.SearchSettingsAsync(sp.ToArray(), showResult);
			});
		}

		void wsPresets_SearchSettingsCompleted(object sender, ResultEventArgs e)
		{
			var ws = (WebServiceClient)sender;
			ws.SearchSettingsCompleted -= wsPresets_SearchSettingsCompleted;
			// Make sure method is executed on the same thread as this control.
			BeginInvoke((MethodInvoker)delegate ()
			{
				refreshed = true;
				if (e.Error != null || e.Result == null)
				{
					var showResult = (bool)e.UserState;
                    if (showResult)
					{
						mainForm.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: No Presets received.", DateTime.Now), MessageBoxIcon.Information);
					}
				}
				else
				{
					var result = (SearchResult)e.Result;
					UpdateList(result.Presets, SettingManager.Presets.Items);
					UpdateList(result.PadSettings, SettingManager.PadSettings.Items);
					if ((bool)e.UserState)
					{
						var presetsCount = (result.Presets == null) ? 0 : result.Presets.Length;
						var padSettingsCount = (result.PadSettings == null) ? 0 : result.PadSettings.Length;
						mainForm.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: {1} Presets and {2} PAD Settings received.", DateTime.Now, presetsCount, padSettingsCount), MessageBoxIcon.Information);
					}
				}
				mainForm.LoadingCircle = false;
			});
		}

		#endregion

		private void MapToMenuItem_Click(object sender, EventArgs e)
		{
			var v = (MapTo)((ToolStripMenuItem)sender).Tag;
			var items = MyDevicesDataGridView.SelectedRows.Cast<DataGridViewRow>().Select(x=>(Setting)x.DataBoundItem).ToArray();
			foreach (var item in items)
			{
				item.MapTo = (int)v;
			}
		}
	}
}
