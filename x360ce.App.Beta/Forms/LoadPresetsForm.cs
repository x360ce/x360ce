using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	public partial class LoadPresetsForm : BaseForm
	{
		public LoadPresetsForm()
		{
			InitializeComponent();
			ControlHelper.ApplyBorderStyle(SettingsDataGridView);
			ControlHelper.ApplyBorderStyle(SummariesDataGridView);
			ControlHelper.ApplyBorderStyle(PresetsDataGridView);
			_defaultPresetsTitle = PresetsTabPage.Text;
			_defaultSettingsTitle = SettingsTabPage.Text;
			EngineHelper.EnableDoubleBuffering(PresetsDataGridView);
			EngineHelper.EnableDoubleBuffering(SettingsDataGridView);
			PresetsDataGridView.AutoGenerateColumns = false;
			SettingsDataGridView.AutoGenerateColumns = false;
		}

		public void InitForm()
		{
			SettingsManager.Settings.Items.ListChanged += Settings_ListChanged;
			SettingsManager.Presets.Items.ListChanged += Presets_ListChanged;
			SettingsDataGridView.DataSource = SettingsManager.Settings.Items;
			PresetsDataGridView.DataSource = SettingsManager.Presets.Items;
			UpdateControlsFromPresets();
			UpdateControlsFromSettings();
		}

		public void UnInitForm()
		{
			SettingsManager.Settings.Items.ListChanged -= Settings_ListChanged;
			SettingsManager.Presets.Items.ListChanged -= Presets_ListChanged;
			SettingsDataGridView.DataSource = null;
			PresetsDataGridView.DataSource = null;
		}

		public PadSetting SelectedItem;

		private void OkButton_Click(object sender, EventArgs e)
		{
			Guid? checksum = null;
			if (MainTabControl.SelectedTab == PresetsTabPage)
			{
				var row = PresetsDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
				if (row != null)
				{
					var preset = (Preset)row.DataBoundItem;
					checksum = preset.PadSettingChecksum;
				}
			}
			if (MainTabControl.SelectedTab == SettingsTabPage)
			{
				var row = SettingsDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
				if (row != null)
				{
					var setting = (Setting)row.DataBoundItem;
					checksum = setting.PadSettingChecksum;
				}
			}
			if (checksum.HasValue)
			{
				SelectedItem = SettingsManager.PadSettings.Items.FirstOrDefault(x => x.PadSettingChecksum == checksum.Value);
			}
			DialogResult = DialogResult.OK;
		}

		private void MainTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			var tab = MainTabControl.SelectedTab;
			if (tab != null) SetHeaderSubject(tab.Text);
		}

		#region Presets

		string _defaultPresetsTitle;

		void UpdateControlsFromPresets()
		{
			var count = SettingsManager.Presets.Items.Count;
			PresetsTabPage.Text = count > 0
				? string.Format("{0} [{1}]", _defaultPresetsTitle, count)
				: _defaultPresetsTitle;
		}

		void Presets_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromPresets();
		}

		private void PresetRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshPresetsGrid(true);
		}

		public void RefreshPresetsGrid(bool showResult)
		{
			var sp = new List<SearchParameter>();
			sp.Add(new SearchParameter());
			var ws = new WebServiceClient();
			ws.Url = SettingsManager.Options.InternetDatabaseUrl;
			ws.SearchSettingsCompleted += wsPresets_SearchSettingsCompleted;
			// Make sure it starts completely on a separate thread.
			System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
			{
				ws.SearchSettingsAsync(sp.ToArray(), showResult);
			});
		}

		void wsPresets_SearchSettingsCompleted(object sender, ResultEventArgs e)
		{
			if (InvokeRequired)
			{
				var method = new EventHandler<ResultEventArgs>(wsPresets_SearchSettingsCompleted);
				BeginInvoke(method, new object[] { sender, e });
				return;
			}
			var ws = (WebServiceClient)sender;
			ws.SearchSettingsCompleted -= wsPresets_SearchSettingsCompleted;
			// Make sure method is executed on the same thread as this control.
			AddTask(TaskName.SearchPresets);
			if (e.Error != null)
			{
				var error = e.Error.Message;
				if (e.Error.InnerException != null) error += "\r\n" + e.Error.InnerException.Message;
				SetHeaderBody(MessageBoxIcon.Error,
						"{0: yyyy-MM-dd HH:mm:ss}: {1}",
						DateTime.Now, error);
			}
			else if (e.Result == null)
			{
				var showResult = (bool)e.UserState;
				if (showResult)
				{
					SetHeaderBody(
						MessageBoxIcon.Information,
						"{0: yyyy-MM-dd HH:mm:ss}: No Presets received.",
						DateTime.Now
					);
				}
			}
			else
			{
				var result = (SearchResult)e.Result;
				AppHelper.UpdateList(result.Presets, SettingsManager.Presets.Items);
				AppHelper.UpdateList(result.PadSettings, SettingsManager.PadSettings.Items);
				if ((bool)e.UserState)
				{
					var presetsCount = (result.Presets == null) ? 0 : result.Presets.Length;
					var padSettingsCount = (result.PadSettings == null) ? 0 : result.PadSettings.Length;
					SetHeaderBody(
						MessageBoxIcon.Information,
						"{0: yyyy-MM-dd HH:mm:ss}: {1} Presets and {2} PAD Settings received.",
						DateTime.Now, presetsCount, padSettingsCount
					);
				}
			}
			RemoveTask(TaskName.SearchPresets);
		}

		#endregion

		#region Settings

		string _defaultSettingsTitle;

		void UpdateControlsFromSettings()
		{
			var count = SettingsManager.Settings.Items.Count;
			SettingsTabPage.Text = count > 0
				? string.Format("{0} [{1}]", _defaultSettingsTitle, count)
				: _defaultSettingsTitle;
		}


		void Settings_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromSettings();
		}

		private void SettingsRefreshButton_Click(object sender, EventArgs e)
		{
			MainForm.Current.SettingsDatabasePanel.RefreshGrid(true);
		}

		void SettingsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var item = (Setting)grid.Rows[e.RowIndex].DataBoundItem;
			if (e.ColumnIndex == grid.Columns[MySidColumn.Name].Index)
			{
				e.Value = EngineHelper.GetID(item.PadSettingChecksum);
			}
			else if (e.ColumnIndex == grid.Columns[MapToColumn.Name].Index)
			{
				e.Value = JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription((MapTo)item.MapTo);
			}
		}

		#endregion

		private void PresetsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var item = (Preset)grid.Rows[e.RowIndex].DataBoundItem;
			if (e.ColumnIndex == grid.Columns[PresetSettingIdColumn.Name].Index)
			{
				e.Value = EngineHelper.GetID(item.PadSettingChecksum);
			}
		}
	}


}
