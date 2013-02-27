using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App.Controls
{
	public partial class ProgramsControl : UserControl
	{
		public ProgramsControl()
		{
			InitializeComponent();
			SettingsDataGridView.AutoGenerateColumns = false;
		}

		private void RefreshAllButton_Click(object sender, EventArgs e)
		{
			MainForm.Current.LoadingCircle = true;
			var ws = new com.x360ce.localhost.x360ce();
			ws.Url = MainForm.Current.InternetDatabaseUrlTextBox.Text;
			bool? enabled = null;
			int? minInstances = null;
			if (IncludeEnabledCheckBox.CheckState == CheckState.Checked) enabled = true;
			if (IncludeEnabledCheckBox.CheckState == CheckState.Unchecked) enabled = false;
			if (MinimumInstanceCountNumericUpDown.Value > 0)
			{
				minInstances = (int)MinimumInstanceCountNumericUpDown.Value;
			}
			ws.GetProgramsCompleted += ws_GetProgramsCompleted;
			ws.GetProgramsAsync(enabled, minInstances);
		}

		void ws_GetProgramsCompleted(object sender, com.x360ce.localhost.GetProgramsCompletedEventArgs e)
		{
			MainForm.Current.LoadingCircle = false;
			if (e.Error != null)
			{
				MainForm.Current.UpdateHelpHeader(e.Error.Message, MessageBoxIcon.Error);
				throw e.Error;
			}
			else if (e.Result == null)
			{
				MainForm.Current.UpdateHelpHeader("", MessageBoxIcon.Error);
			}
			else
			{
				int count = e.Result.Length;
				SettingsDataGridView.DataSource = e.Result;
				MainForm.Current.UpdateHelpHeader(string.Format("{0: yyyy-MM-dd HH:mm:ss}: '{1}' program(s) loaded.", DateTime.Now, count), MessageBoxIcon.Information);
			}
		}

		com.x360ce.localhost.Program CurrentProgram;

		void SettingsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var setting = ((com.x360ce.localhost.Program)grid.Rows[e.RowIndex].DataBoundItem);
			var isCurrent = CurrentProgram != null && setting.ProgramId == CurrentProgram.ProgramId;
			//if (e.ColumnIndex == grid.Columns[ProgramIdColumn.Name].Index)
			//{
			//	UpdateCellStyle(grid, e, SettingSelection == null ? null : (Guid?)SettingSelection.PadSettingChecksum);
			//}
			//else
			if (e.ColumnIndex == grid.Columns[MyIconColumn.Name].Index)
			{
				e.Value = isCurrent ? SaveButton.Image : Properties.Resources.empty_16x16;
			}
			else
			{
				//var row = grid.Rows[e.RowIndex].Cells[MyIconColumn.Name];
				//e.CellStyle.BackColor = isCurrent ? currentColor : grid.DefaultCellStyle.BackColor;
			}
		}

		//void UpdateCellStyle(DataGridView grid, DataGridViewCellFormattingEventArgs e, Guid? checksum)
		//{
		//	var v = e.Value.ToString().Substring(0, 8).ToUpper();
		//	var s = checksum.HasValue ? checksum.Value.ToString().Substring(0, 8).ToUpper() : null;
		//	var match = v == s;
		//	e.Value = e.Value.ToString().Substring(0, 8).ToUpper();
		//	e.CellStyle.BackColor = match ? System.Drawing.Color.FromArgb(255, 222, 225, 231) : e.CellStyle.BackColor = grid.DefaultCellStyle.BackColor;
		//}


		//	InstallFilesX360ceCheckBox.Checked = System.IO.File.Exists(SettingManager.IniFileName);
		//InstallFilesXinput13CheckBox.Checked = System.IO.File.Exists(dllFile3);
		//InstallFilesX360ceCheckBox.Enabled = IsFileSame(SettingManager.IniFileName);
		//InstallFilesXinput910CheckBox.SuspendLayout();
		//InstallFilesXinput11CheckBox.SuspendLayout();
		//InstallFilesXinput12CheckBox.SuspendLayout();
		//InstallFilesXinput910CheckBox.Checked = System.IO.File.Exists(dllFile0);
		//InstallFilesXinput11CheckBox.Checked = System.IO.File.Exists(dllFile1);
		//InstallFilesXinput12CheckBox.Checked = System.IO.File.Exists(dllFile2);
		//InstallFilesXinput910CheckBox.ResumeLayout(false);
		//InstallFilesXinput11CheckBox.ResumeLayout(false);
		//InstallFilesXinput12CheckBox.ResumeLayout(false);
		////InstallFilesXinput910CheckBox.Enabled = IsFileSame(dllFile0);
		////InstallFilesXinput11CheckBox.Enabled = IsFileSame(dllFile1);
		////InstallFilesXinput12CheckBox.Enabled = IsFileSame(dllFile2);
		//InstallFilesXinput13CheckBox.Enabled = IsFileSame(dllFile3);


		//private void InstallFilesXinput910CheckBox_CheckedChanged(object sender, EventArgs e)
		//{

		//}

		//void InstallFilesXinput12CheckBox_CheckedChanged(object sender, EventArgs e)
		//{
		//	CreateDllFile(InstallFilesXinput12CheckBox.Checked, dllFile2);
		//}

		//void InstallFilesXinput11CheckBox_CheckedChanged(object sender, EventArgs e)
		//{
		//	CreateDllFile(InstallFilesXinput11CheckBox.Checked, dllFile1);
		//}

		//void InstallFilesXinput910CheckBox_CheckedChanged(object sender, EventArgs e)
		//{
		//	CreateDllFile(InstallFilesXinput910CheckBox.Checked, dllFile0);
		//}
	}
}
