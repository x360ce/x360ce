using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using x360ce.Engine.Data;
using x360ce.Engine;
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Runtime;

namespace x360ce.App.Controls
{
	public partial class SettingsGridUserControl : UserControl
	{
		public SettingsGridUserControl()
		{
			InitializeComponent();
			JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(SettingsDataGridView);
			EngineHelper.EnableDoubleBuffering(SettingsDataGridView);
			SettingsDataGridView.AutoGenerateColumns = false;
		}

		public BaseFormWithHeader _ParentForm;

		public void InitPanel()
		{
			SettingsManager.UserSettings.Items.ListChanged += Settings_ListChanged;
			// WORKAROUND: Remove SelectionChanged event.
			SettingsDataGridView.SelectionChanged -= SettingsDataGridView_SelectionChanged;
			SettingsDataGridView.DataSource = SettingsManager.UserSettings.Items;
			// WORKAROUND: Use BeginInvoke to prevent SelectionChanged firing multiple times.
			//BeginInvoke((MethodInvoker)delegate ()
			//{
			SettingsDataGridView.SelectionChanged += SettingsDataGridView_SelectionChanged;
			SettingsDataGridView_SelectionChanged(SettingsDataGridView, new EventArgs());
			//});
			UpdateControlsFromSettings();
		}

		public void UnInitPanel()
		{
			SettingsManager.UserSettings.Items.ListChanged -= Settings_ListChanged;
			SettingsDataGridView.DataSource = null;
		}

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
			var item = (UserSetting)grid.Rows[e.RowIndex].DataBoundItem;
			var column = grid.Columns[e.ColumnIndex];
			if (column == SettingsSidColumn)
			{
				e.Value = EngineHelper.GetID(item.PadSettingChecksum);
			}
			else if (column == SettingsMapToColumn)
			{
				e.Value = Attributes.GetDescription((MapTo)item.MapTo);
			}
		}


		private void SettingsDeleteButton_Click(object sender, EventArgs e)
		{
			var grid = SettingsDataGridView;
			var userSettings = grid.SelectedRows.Cast<DataGridViewRow>().Select(x => (UserSetting)x.DataBoundItem).ToArray();
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var result = form.ShowForm("Do you want to delete selected settings?", "X360CE - Delete Settings", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
			if (result == DialogResult.Yes)
			{
				foreach (var item in userSettings)
				{
					SettingsManager.UserSettings.Items.Remove(item);
				}
				SettingsManager.Save();
				MainForm.Current.CloudPanel.Add(CloudAction.Delete, userSettings, true);
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
				.Select(x => (UserSetting)x.DataBoundItem)
				.FirstOrDefault();
			var note = item == null ? string.Empty : item.Comment ?? "";
			ControlsHelper.SetText(CommentLabel, note);
			ControlsHelper.SetVisible(CommentPanel, !string.IsNullOrEmpty(note));
		}

		private void SettingsEditNoteButton_Click(object sender, EventArgs e)
		{
			var item = SettingsDataGridView.SelectedRows
				.Cast<DataGridViewRow>()
				.Select(x => (UserSetting)x.DataBoundItem)
				.FirstOrDefault();
			if (item == null)
			{
				return;
			}
			var note = item.Comment ?? "";
			var form = new PromptForm();
			form.EditTextBox.MaxLength = 1024;
			form.EditTextBox.Text = note;
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
			_ParentForm.AddTask(TaskName.SearchSettings);
			SettingsRefreshButton.Enabled = false;
			var sp = new List<SearchParameter>();
			SettingsManager.Current.FillSearchParameterWithInstances(sp);
			SettingsManager.Current.FillSearchParameterWithFiles(sp);
			var ws = new WebServiceClient();
			ws.Url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
			ws.SearchSettingsCompleted += ws_SearchSettingsCompleted;
			// Make sure it runs on another thread.
			System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
			{
				ws.SearchSettingsAsync(sp.ToArray());
			});
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
				_ParentForm.SetHeaderError(error);
			}
			else if (e.Result == null)
			{
				_ParentForm.SetHeaderInfo("No user settings received.");
			}
			else
			{
				var result = (SearchResult)e.Result;
				// Reorder Settings.
				result.Settings = result.Settings.OrderBy(x => x.ProductName).ThenBy(x => x.FileName).ThenBy(x => x.FileProductName).ToArray();
				SettingsManager.Current.UpsertSettings(result.Settings);
				// Insert pad settings which are used by settings.
				SettingsManager.Current.UpsertPadSettings(result.PadSettings);
				// Remove unused pad settings.
				SettingsManager.Current.CleanupPadSettings();
				// Display results about operation.
				var settingsCount = (result.Settings == null) ? 0 : result.Settings.Length;
				var padSettingsCount = (result.PadSettings == null) ? 0 : result.PadSettings.Length;
				_ParentForm.SetHeaderInfo("{0} user settings and {1} PAD settings received.", settingsCount, padSettingsCount);
			}
			_ParentForm.RemoveTask(TaskName.SearchSettings);
			SettingsRefreshButton.Enabled = true;
		}


	}
}
