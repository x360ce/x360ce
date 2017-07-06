using JocysCom.ClassLibrary.Controls;
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
			EngineHelper.EnableDoubleBuffering(PresetsDataGridView);
			EngineHelper.EnableDoubleBuffering(SummariesDataGridView);
			EngineHelper.EnableDoubleBuffering(SettingsDataGridView);
			PresetsDataGridView.AutoGenerateColumns = false;
			SummariesDataGridView.AutoGenerateColumns = false;
			SettingsDataGridView.AutoGenerateColumns = false;
			SetHeaderBody(MessageBoxIcon.None, "", "");
		}

		public void InitForm()
		{
			SettingsManager.Settings.Items.ListChanged += Settings_ListChanged;
			SettingsManager.Presets.Items.ListChanged += Presets_ListChanged;
			SettingsManager.Summaries.Items.ListChanged += Summaries_ListChanged;
			SettingsDataGridView.DataSource = SettingsManager.Settings.Items;
			PresetsDataGridView.DataSource = SettingsManager.Presets.Items;
			SummariesDataGridView.DataSource = SettingsManager.Summaries.Items;
			UpdateControlsFromSettings();
			UpdateControlsFromSummaries();
			UpdateControlsFromPresets();
			UpdateControls();
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
			if (MainTabControl.SelectedTab == SummariesTabPage)
			{
				var row = SummariesDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
				if (row != null)
				{
					var summary = (Summary)row.DataBoundItem;
					checksum = summary.PadSettingChecksum;
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
			UpdateControls();
		}

		void UpdateControls()
		{
			var tab = MainTabControl.SelectedTab;
			if (tab != null) SetHeaderSubject(tab.Text);
			bool selected = false;
			if (MainTabControl.SelectedTab == PresetsTabPage)
			{
				selected = PresetsDataGridView.Rows.Count > 0;
			}
			if (MainTabControl.SelectedTab == SummariesTabPage)
			{
				selected = SummariesDataGridView.Rows.Count > 0;
			}
			if (MainTabControl.SelectedTab == SettingsTabPage)
			{
				selected = SettingsDataGridView.Rows.Count > 0;
			}
			ControlsHelper.SetEnabled(OkButton, selected);
		}

		#region Presets


		void UpdateControlsFromPresets()
		{
		}

		void Presets_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromPresets();
		}

		private void PresetRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshPresetsGrid();
		}

		public void RefreshPresetsGrid()
		{
			AddTask(TaskName.SearchPresets);
			PresetRefreshButton.Enabled = false;
			var sp = new List<SearchParameter>();
			sp.Add(new SearchParameter());
			var ws = new WebServiceClient();
			ws.Url = SettingsManager.Options.InternetDatabaseUrl;
			ws.SearchSettingsCompleted += wsPresets_SearchSettingsCompleted;
			// Make sure it starts completely on a separate thread.
			System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
			{
				ws.SearchSettingsAsync(sp.ToArray());
			});
		}

		void wsPresets_SearchSettingsCompleted(object sender, ResultEventArgs e)
		{
			// Make sure method is executed on the same thread as this control.
			if (InvokeRequired)
			{
				var method = new EventHandler<ResultEventArgs>(wsPresets_SearchSettingsCompleted);
				BeginInvoke(method, new object[] { sender, e });
				return;
			}
			// Detach event handler so resource could be released.
			var ws = (WebServiceClient)sender;
			ws.SearchSettingsCompleted -= wsPresets_SearchSettingsCompleted;
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
				SetHeaderBody(
					MessageBoxIcon.Information,
					"{0: yyyy-MM-dd HH:mm:ss}: No default settings received.",
					DateTime.Now
				);
			}
			else
			{
				var result = (SearchResult)e.Result;
				AppHelper.UpdateList(result.Presets, SettingsManager.Presets.Items);
				AppHelper.UpdateList(result.PadSettings, SettingsManager.PadSettings.Items);
				var presetsCount = (result.Presets == null) ? 0 : result.Presets.Length;
				var padSettingsCount = (result.PadSettings == null) ? 0 : result.PadSettings.Length;
				SetHeaderBody(
					MessageBoxIcon.Information,
					"{0: yyyy-MM-dd HH:mm:ss}: {1} default settings and {2} PAD settings received.",
					DateTime.Now, presetsCount, padSettingsCount
				);
			}
			RemoveTask(TaskName.SearchPresets);
			PresetRefreshButton.Enabled = true;
		}

		private void PresetsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var item = (Preset)grid.Rows[e.RowIndex].DataBoundItem;
			if (e.ColumnIndex == grid.Columns[PresetSidColumn.Name].Index)
			{
				e.Value = EngineHelper.GetID(item.PadSettingChecksum);
			}
		}

		#endregion

		#region Settings

		void UpdateControlsFromSettings()
		{
		}


		void Settings_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromSettings();
		}

		void SettingsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var item = (Setting)grid.Rows[e.RowIndex].DataBoundItem;
			if (e.ColumnIndex == grid.Columns[SettingsSidColumn.Name].Index)
			{
				e.Value = EngineHelper.GetID(item.PadSettingChecksum);
			}
			else if (e.ColumnIndex == grid.Columns[SettingsMapToColumn.Name].Index)
			{
				e.Value = JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription((MapTo)item.MapTo);
			}
		}


		private void SettingsDeleteButton_Click(object sender, EventArgs e)
		{
			var grid = SettingsDataGridView;
			var items = grid.SelectedRows.Cast<DataGridViewRow>().Select(x => (Setting)x.DataBoundItem).ToArray();
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var result = form.ShowForm("Do you want to delete selected settings?", "X360CE - Delete Settings", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
			if (result == DialogResult.Yes)
			{
				foreach (var item in items)
				{
					SettingsManager.Settings.Items.Remove(item);
				}
				//		mainForm.LoadingCircle = true;
				//		var setting = (Setting)MyDevicesDataGridView.SelectedRows[0].DataBoundItem;
				//		var ws = new WebServiceClient();
				//		ws.Url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
				//		ws.DeleteSettingCompleted += ws_DeleteSettingCompleted;
				//		ws.DeleteSettingAsync(setting);
			}
			form.Dispose();
			form = null;
		}

		private void SettingsDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			UpdateNoteLabel();
		}

		void UpdateNoteLabel()
		{
			var item = SettingsDataGridView.SelectedRows
				.Cast<DataGridViewRow>()
				.Select(x => (Setting)x.DataBoundItem)
				.FirstOrDefault();
			var note = item == null ? string.Empty : item.Comment ?? "";
			ControlsHelper.SetText(CommentLabel, note);
			ControlsHelper.SetVisible(CommentPanel, !string.IsNullOrEmpty(note));
		}

		private void SettingsEditNoteButton_Click(object sender, EventArgs e)
		{
			var item = SettingsDataGridView.SelectedRows
				.Cast<DataGridViewRow>()
				.Select(x => (Setting)x.DataBoundItem)
				.FirstOrDefault();
			if (item == null)
			{
				return;
			}
			var note = item.Comment ?? "";
			var form = new PromptForm();
			form.EditTextBox.MaxLength = 1024;
			form.StartPosition = FormStartPosition.CenterParent;
			form.Text = "X360CE - Edit Note";
			var result = form.ShowDialog();
			if (result == DialogResult.OK)
			{
				item.Comment = form.EditTextBox.Text.Trim();
				UpdateNoteLabel();
			}
		}

		private void SettingsRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshSettingsGrid();
		}

		public void RefreshSettingsGrid()
		{
			//mainForm.LoadingCircle = true;

			AddTask(TaskName.SearchSettings);
			SettingsRefreshButton.Enabled = false;
			var sp = new List<SearchParameter>();
			FillSearchParameterWithInstances(sp);
			FillSearchParameterWithFiles(sp);
			var ws = new WebServiceClient();
			ws.Url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
			ws.SearchSettingsCompleted += ws_SearchSettingsCompleted;
			System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
			{
				ws.SearchSettingsAsync(sp.ToArray());
			});
		}


		public void FillSearchParameterWithInstances(List<SearchParameter> sp)
		{
			// Select user devices as parameters to search.
			var userDevices = SettingsManager.Settings.Items
				.Select(x => x.InstanceGuid).Distinct()
				// Do not add empty records.
				.Where(x => x != Guid.Empty)
				.Select(x => new SearchParameter() { InstanceGuid = x })
				.ToArray();
			sp.AddRange(userDevices);
		}

		void ws_SearchSettingsCompleted(object sender, ResultEventArgs e)
		{
			// Make sure method is executed on the same thread as this control.
			if (InvokeRequired)
			{
				var method = new EventHandler<ResultEventArgs>(ws_SearchSettingsCompleted);
				BeginInvoke(method, new object[] { sender, e });
				return;
			}
			// Detach event handler so resource could be released.
			var ws = (WebServiceClient)sender;
			ws.SearchSettingsCompleted -= ws_SearchSettingsCompleted;
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
				SetHeaderBody(
					MessageBoxIcon.Information,
					"{0: yyyy-MM-dd HH:mm:ss}: No user settings received.",
					DateTime.Now
				);
			}
			else
			{
				var result = (SearchResult)e.Result;
				// Reorder Settings.
				result.Settings = result.Settings.OrderBy(x => x.ProductName).ThenBy(x => x.FileName).ThenBy(x => x.FileProductName).ToArray();
				AppHelper.UpdateList(result.Settings, SettingsManager.Settings.Items);
				AppHelper.UpdateList(result.PadSettings, SettingsManager.PadSettings.Items);
				var SettingsCount = (result.Settings == null) ? 0 : result.Settings.Length;
				var padSettingsCount = (result.PadSettings == null) ? 0 : result.PadSettings.Length;
				SetHeaderBody(
					MessageBoxIcon.Information,
					"{0: yyyy-MM-dd HH:mm:ss}: {1} user settings and {2} PAD settings received.",
					DateTime.Now, SettingsCount, padSettingsCount
				);
			}
			RemoveTask(TaskName.SearchSettings);
			SettingsRefreshButton.Enabled = true;
		}

		#endregion

		#region Summaries

		void UpdateControlsFromSummaries()
		{
			var count = SettingsManager.Summaries.Items.Count;
			// Allow refresh summaries.
			ControlsHelper.SetEnabled(SummariesRefreshButton, count > 0);
		}


		void Summaries_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromSummaries();
		}

		private void SummariesRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshSummariesGrid();
		}

		void SummariesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var item = (Summary)grid.Rows[e.RowIndex].DataBoundItem;
			if (e.ColumnIndex == grid.Columns[SummariesSidColumn.Name].Index)
			{
				e.Value = EngineHelper.GetID(item.PadSettingChecksum);
			}
		}


		public void RefreshSummariesGrid()
		{
			//mainForm.LoadingCircle = true;

			AddTask(TaskName.SearchSummaries);
			SummariesRefreshButton.Enabled = false;
			var sp = new List<SearchParameter>();
			FillSearchParameterWithProducts(sp);
			FillSearchParameterWithFiles(sp);
			var ws = new WebServiceClient();
			ws.Url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
			ws.SearchSettingsCompleted += ws_SearchSummariesCompleted;
			System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
			{
				ws.SearchSettingsAsync(sp.ToArray());
			});
		}


		public void FillSearchParameterWithProducts(List<SearchParameter> sp)
		{
			// Select user devices as parameters to search.
			var userDevices = SettingsManager.Settings.Items
				.Select(x => x.ProductGuid).Distinct()
				// Do not add empty records.
				.Where(x => x != Guid.Empty)
				.Select(x => new SearchParameter() { ProductGuid = x })
				.ToArray();
			sp.AddRange(userDevices);
		}

		public void FillSearchParameterWithFiles(List<SearchParameter> sp)
		{
			// Select user games as parameters to search.
			var settings = SettingsManager.Settings.Items.ToArray();
			foreach (var setting in settings)
			{
				var fileName = System.IO.Path.GetFileName(setting.FileName);
				// Do not add empty records.
				if (string.IsNullOrEmpty(fileName))
					continue;
				var fileTitle = EngineHelper.FixName(setting.ProductName, setting.FileName);
				// If search doesn't contain game then...
				if (!sp.Any(x => x.FileName == fileName && x.FileProductName == fileTitle))
				{
					// Add it to search parameters.
					var p = new SearchParameter();
					p.FileName = fileName;
					p.FileProductName = fileTitle;
					sp.Add(p);
				}
			}
		}

		void ws_SearchSummariesCompleted(object sender, ResultEventArgs e)
		{
			// Make sure method is executed on the same thread as this control.
			if (InvokeRequired)
			{
				var method = new EventHandler<ResultEventArgs>(ws_SearchSummariesCompleted);
				BeginInvoke(method, new object[] { sender, e });
				return;
			}
			// Detach event handler so resource could be released.
			var ws = (WebServiceClient)sender;
			ws.SearchSettingsCompleted -= ws_SearchSummariesCompleted;
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
				SetHeaderBody(
					MessageBoxIcon.Information,
					"{0: yyyy-MM-dd HH:mm:ss}: No default settings received.",
					DateTime.Now
				);
			}
			else
			{
				var result = (SearchResult)e.Result;
				// Reorder summaries.
				result.Summaries = result.Summaries.OrderBy(x => x.ProductName).ThenBy(x => x.FileName).ThenBy(x => x.FileProductName).ThenByDescending(x => x.Users).ToArray();
				AppHelper.UpdateList(result.Summaries, SettingsManager.Summaries.Items);
				AppHelper.UpdateList(result.PadSettings, SettingsManager.PadSettings.Items);
				var summariesCount = (result.Summaries == null) ? 0 : result.Summaries.Length;
				var padSettingsCount = (result.PadSettings == null) ? 0 : result.PadSettings.Length;
				SetHeaderBody(
					MessageBoxIcon.Information,
					"{0: yyyy-MM-dd HH:mm:ss}: {1} default settings and {2} PAD settings received.",
					DateTime.Now, summariesCount, padSettingsCount
				);
			}
			RemoveTask(TaskName.SearchSummaries);
			SummariesRefreshButton.Enabled = true;
		}

		#endregion

	}
}
