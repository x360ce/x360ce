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

        public BaseForm _ParentForm;

        public void InitPanel()
        {
            SettingsManager.Settings.Items.ListChanged += Settings_ListChanged;
            // WORKAROUND: Remove SelectionChanged event.
            SettingsDataGridView.SelectionChanged -= SettingsDataGridView_SelectionChanged;
            SettingsDataGridView.DataSource = SettingsManager.Settings.Items;
            // WORKAROUND: Use BeginInvoke to prevent SelectionChanged firing multiple times.
            BeginInvoke((MethodInvoker)delegate ()
            {
                SettingsDataGridView.SelectionChanged += SettingsDataGridView_SelectionChanged;
                SettingsDataGridView_SelectionChanged(SettingsDataGridView, new EventArgs());
            });
            UpdateControlsFromSettings();
        }

        public void UnInitPanel()
        {
            SettingsManager.Settings.Items.ListChanged -= Settings_ListChanged;
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
            //mainForm.LoadingCircle = true;

            _ParentForm.AddTask(TaskName.SearchSettings);
            SettingsRefreshButton.Enabled = false;
            var sp = new List<SearchParameter>();
            SettingsManager.Current.FillSearchParameterWithInstances(sp);
            SettingsManager.Current.FillSearchParameterWithFiles(sp);
            var ws = new WebServiceClient();
            ws.Url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
            ws.SearchSettingsCompleted += ws_SearchSettingsCompleted;
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
                _ParentForm.SetHeaderBody("No user settings received.");
            }
            else
            {
                var result = (SearchResult)e.Result;
                // Reorder Settings.
                result.Settings = result.Settings.OrderBy(x => x.ProductName).ThenBy(x => x.FileName).ThenBy(x => x.FileProductName).ToArray();
                SettingsManager.Current.UpsertSettings(result.Settings);
                SettingsManager.Current.UpsertPadSettings(result.PadSettings);
                var settingsCount = (result.Settings == null) ? 0 : result.Settings.Length;
                var padSettingsCount = (result.PadSettings == null) ? 0 : result.PadSettings.Length;
                _ParentForm.SetHeaderBody("{0} user settings and {1} PAD settings received.", settingsCount, padSettingsCount);
            }
            _ParentForm.RemoveTask(TaskName.SearchSettings);
            SettingsRefreshButton.Enabled = true;
        }


    }
}
