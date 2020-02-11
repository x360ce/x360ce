using JocysCom.ClassLibrary.Web.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using x360ce.Engine;
using System.Linq;

namespace x360ce.App.Controls
{
	public partial class PresetsGridUserControl : UserControl
	{

		public PresetsGridUserControl()
		{
			InitializeComponent();
			// Make font more consistent with the rest of the interface.
			Controls.OfType<ToolStrip>().ToList().ForEach(x => x.Font = Font);
			JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(PresetsDataGridView);
			EngineHelper.EnableDoubleBuffering(PresetsDataGridView);
			PresetsDataGridView.AutoGenerateColumns = false;
		}

		public BaseFormWithHeader _ParentForm;

		public void InitPanel()
		{
			SettingsManager.Presets.Items.ListChanged += Presets_ListChanged;
			PresetsDataGridView.DataSource = SettingsManager.Presets.Items;
			UpdateControlsFromPresets();
		}

		public void UnInitPanel()
		{
			SettingsManager.Presets.Items.ListChanged -= Presets_ListChanged;
			PresetsDataGridView.DataSource = null;
		}


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
			_ParentForm.AddTask(TaskName.SearchPresets);
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

		void wsPresets_SearchSettingsCompleted(object sender, SoapHttpClientEventArgs e)
		{
			// Make sure method is executed on the same thread as this control.
			if (InvokeRequired)
			{
				var method = new EventHandler<SoapHttpClientEventArgs>(wsPresets_SearchSettingsCompleted);
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
				_ParentForm.SetHeaderError(error);
			}
			else if (e.Result == null)
			{
				_ParentForm.SetHeaderInfo("No default settings received.");
			}
			else
			{
				var result = (SearchResult)e.Result;
				AppHelper.UpdateList(result.Presets, SettingsManager.Presets.Items);
				SettingsManager.Current.UpsertPadSettings(result.PadSettings);
				SettingsManager.Current.CleanupPadSettings();
				var presetsCount = (result.Presets == null) ? 0 : result.Presets.Length;
				var padSettingsCount = (result.PadSettings == null) ? 0 : result.PadSettings.Length;
				_ParentForm.SetHeaderInfo("{0} default settings and {1} PAD settings received.", presetsCount, padSettingsCount);
			}
			_ParentForm.RemoveTask(TaskName.SearchPresets);
			PresetRefreshButton.Enabled = true;
		}

		private void PresetsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var item = (Preset)grid.Rows[e.RowIndex].DataBoundItem;
			var column = grid.Columns[e.ColumnIndex];
			if (column == PresetSidColumn)
			{
				e.Value = EngineHelper.GetID(item.PadSettingChecksum);
			}
		}

	}
}
