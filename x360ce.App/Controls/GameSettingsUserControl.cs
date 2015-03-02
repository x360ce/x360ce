using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using x360ce.Engine;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace x360ce.App.Controls
{
	public partial class GameSettingsUserControl : UserControl
	{
		public GameSettingsUserControl()
		{
			InitializeComponent();
			if (DesignMode) return;
			MySettingsDataGridView.AutoGenerateColumns = false;
			GlobalSettingsDataGridView.AutoGenerateColumns = false;
			ScanProgressLabel.Text = "";
			InitDefaultList();
		}

		void GetPrograms()
		{
			MainForm.Current.LoadingCircle = true;
			var ws = new WebServiceClient();
			ws.Url = MainForm.Current.OptionsPanel.InternetDatabaseUrlComboBox.Text;
			EnabledState enabled = EnabledState.None;
			if (IncludeEnabledCheckBox.CheckState == CheckState.Checked) enabled = EnabledState.Enabled;
			if (IncludeEnabledCheckBox.CheckState == CheckState.Unchecked) enabled = EnabledState.Disabled;
			int minInstances = (int)MinimumInstanceCountNumericUpDown.Value;
			ws.GetProgramsCompleted += ws_GetProgramsCompleted;
			ws.GetProgramsAsync(enabled, minInstances);
		}

		void ws_GetProgramsCompleted(object sender, ResultEventArgs e)
		{
			MainForm.Current.LoadingCircle = false;
			if (e.Error != null)
			{
				var error = e.Error.Message;
				if (e.Error.InnerException != null) error += "\r\n" + e.Error.InnerException.Message;
				MainForm.Current.UpdateHelpHeader(error, MessageBoxIcon.Error);
			}
			else if (e.Result == null)
			{
				MainForm.Current.UpdateHelpHeader("No results were returned by the web service!", MessageBoxIcon.Error);
			}
			else
			{
				var selection = JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<string>(GlobalSettingsDataGridView, "FileName");
				SettingsFile.Current.Programs.Clear();
				var result = (List<x360ce.Engine.Data.Program>)e.Result;
				foreach (var item in result) SettingsFile.Current.Programs.Add(item);
				var header = string.Format("{0: yyyy-MM-dd HH:mm:ss}: '{1}' program(s) loaded.", DateTime.Now, result.Count());
				MainForm.Current.UpdateHelpHeader(header, MessageBoxIcon.Information);
				JocysCom.ClassLibrary.Controls.ControlsHelper.RestoreSelection<string>(GlobalSettingsDataGridView, "FileName", selection);
			}
		}

		void MySettingsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var item = ((x360ce.Engine.Data.Game)grid.Rows[e.RowIndex].DataBoundItem);
			var isCurrent = GameDetailsControl.CurrentGame != null && item.GameId == GameDetailsControl.CurrentGame.GameId;
			e.CellStyle.ForeColor = item.IsEnabled
					? grid.DefaultCellStyle.ForeColor
					: System.Drawing.SystemColors.ControlDark;
			e.CellStyle.SelectionBackColor = item.IsEnabled
			 ? grid.DefaultCellStyle.SelectionBackColor
			 : System.Drawing.SystemColors.ControlDark;
			//e.CellStyle.ForeColor = string.IsNullOrEmpty(item.FullPath)
			//	? System.Drawing.Color.Gray
			//	: grid.DefaultCellStyle.ForeColor;
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

		void InitDefaultList()
		{
			SettingsFile.Current.Load();
			MySettingsDataGridView.DataSource = SettingsFile.Current.Games;
			GlobalSettingsDataGridView.DataSource = SettingsFile.Current.Programs;
		}

		void ProgramsDataGridView_DataSourceChanged(object sender, EventArgs e)
		{
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



		private void RefreshButton_Click(object sender, EventArgs e)
		{
			var games = SettingsFile.Current.Games;
			foreach (var game in games)
			{
				game.Refresh();
			}
			MySettingsDataGridView.Invalidate();
			//ws.GetProgram()
			//ws.LoadSettingCompleted += ws_LoadSettingCompleted;
			//ws.LoadSettingAsync(new Guid[] { new Guid("45dec622-d819-2fdc-50a1-34bdf63647fb") }, null);

		}

		void ws_LoadSettingCompleted(object sender, ResultEventArgs e)
		{
			//var x = e;
		}

		private void ExportButton_Click(object sender, EventArgs e)
		{

		}

		private void ProgramOpenFileDialog_FileOk(object sender, CancelEventArgs e)
		{

		}

		void ProcessExecutable(string filePath)
		{
			var fi = new System.IO.FileInfo(filePath);
			if (!fi.Exists) return;
			// Check if item already exists.
			var game = SettingsFile.Current.Games.FirstOrDefault(x => x.FileName == fi.Name);
			if (game == null)
			{
				game = x360ce.Engine.Data.Game.FromDisk(fi.FullName);
				// Load default settings.
				var program = SettingsFile.Current.Programs.FirstOrDefault(x => x.FileName == game.FileName);
				game.LoadDefault(program);
				SettingsFile.Current.Games.Add(game);
			}
			else
			{
				game.FullPath = fi.FullName;
			}
			SettingsFile.Current.Save();
		}

		/// <summary>
		/// Scan for games
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ScanButton_Click(object sender, EventArgs e)
		{
			var success = System.Threading.ThreadPool.QueueUserWorkItem(ScanFunction);
			if (!success) ScanProgressLabel.Text = "Scan failed!";
		}

		void ScanFunction(object state)
		{
			string[] paths = null;
			Invoke((MethodInvoker)delegate()
			{
				ScanProgressLabel.Visible = true;
				ScanButton.Enabled = false;
				paths = MainForm.Current.OptionsPanel.GameScanLocationsListBox.Items.Cast<string>().ToArray();
				ScanProgressLabel.Text = "Scanning...";
			});
			var skipped = 0;
			var added = 0;
			var updated = 0;
			for (int i = 0; i < paths.Length; i++)
			{
				var path = (string)paths[i];
				// Don't allow to scan windows folder.
				var winFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				if (path.StartsWith(winFolder)) continue;
				var di = new System.IO.DirectoryInfo(path);
				// Skip folders if don't exists.
				if (!di.Exists) continue;
				var exes = new List<FileInfo>();
				SearchDirectory(di, exes, "*.exe");
				for (int f = 0; f < exes.Count; f++)
				{

					var exe = exes[f];
					var program = SettingsFile.Current.Programs.FirstOrDefault(x => x.FileName == exe.Name);
					// If file doesn't exist in the game list then continue.
					if (program == null)
					{
						skipped++;
					}
					else
					{
						// Get game my game by executable name.
						var game = SettingsFile.Current.Games.FirstOrDefault(x => x.FileName.ToLower() == exe.Name.ToLower());
						// If file doesn't exist in the game list then continue.
						if (game == null)
						{
							Invoke((MethodInvoker)delegate()
							{
								game = x360ce.Engine.Data.Game.FromDisk(exe.FullName);
								game.LoadDefault(program);
								SettingsFile.Current.Games.Add(game);
								added++;
							});
						}
						else
						{
							game.FullPath = exe.FullName;
							if (string.IsNullOrEmpty(game.FileProductName) && !string.IsNullOrEmpty(program.FileProductName))
							{
								game.FileProductName = program.FileProductName;
							}
							updated++;
						}
					}
					Invoke((MethodInvoker)delegate()
						{
							ScanProgressLabel.Text = string.Format("Scanning Path ({0}/{1}): {2}\r\nSkipped = {3}, Added = {4}, Updated = {5}", i + 1, paths.Length, path, skipped, added, updated);
						});
				}
				SettingsFile.Current.Save();
			}
			Invoke((MethodInvoker)delegate()
			{
				//ScanProgressLabel.Text = "Scan Completed";
				ScanButton.Enabled = true;
				ScanProgressLabel.Visible = false;
			});
		}

		private void SearchDirectory(DirectoryInfo di, List<FileInfo> fileList, string searchPattern)
		{
			try
			{
				foreach (DirectoryInfo subDi in di.GetDirectories())
				{
					SearchDirectory(subDi, fileList, searchPattern);
				}
			}
			catch { }
			try
			{
				foreach (FileInfo fi in di.GetFiles(searchPattern))
				{
					fileList.Add(fi);
				}
			}
			catch { }
		}

		private void GamesControl_Load(object sender, EventArgs e)
		{
			Helper.EnableDoubleBuffering(MySettingsDataGridView);
			Helper.EnableDoubleBuffering(GlobalSettingsDataGridView);
			GlobalSettingsDataGridView.DataSource = SettingsFile.Current.Programs;
			LoadProgramsFromLocalFile();
		}

		void LoadProgramsFromLocalFile()
		{
			var ini = new Ini(SettingManager.GdbFileName);
			var sections = ini.GetSections();
			foreach (var section in sections)
			{
				var program = SettingsFile.Current.Programs.FirstOrDefault(x => x.FileName.ToLower() == section.ToLower());
				if (program == null)
				{
					program = new Engine.Data.Program();
					program.FileName = section;
					program.HookMask = 0x00000002;
					program.XInputMask = 0x00000004;
					SettingsFile.Current.Programs.Add(program);
				}
				program.FileProductName = ini.GetValue(section, "Name", section);
				int hookMask;
				var hookMaskValue = ini.GetValue(section, "HookMask", "0x00000002");
				if (int.TryParse(hookMaskValue, out hookMask))
				{
					program.HookMask = hookMask;
				}
			}
		}

		private void GlobalSettingsRefreshButton_Click(object sender, EventArgs e)
		{
			GetPrograms();
		}

		private void GlobalSettingsDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			SetGlobalSelection();
		}

		void ProgramsDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			SetGamesSelection();
		}

		private void GamesTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (GamesTabControl.SelectedTab == GlobalSettingsTabPage) SetGlobalSelection();
			else if (GamesTabControl.SelectedTab == GamesTabPage) SetGamesSelection();
		}

		void SetGamesSelection()
		{
			// List can't be empty, so return.
			// Issue: When DataSource is set then DataGridView fires the selectionChanged 3 times & it selects the first row. 
			var selected = MySettingsDataGridView.SelectedRows.Count > 0;
			UpdateButton.Enabled = selected;
			StartButton.Enabled = selected;
			SaveButton.Enabled = selected;
			DeleteButton.Enabled = selected;
			if (selected)
			{
				var row = MySettingsDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
				var item = (x360ce.Engine.Data.Game)row.DataBoundItem;
				GameDetailsControl.CurrentGame = item;
				if (item.IsEnabled)
				{
				}
				else
				{
				}
			}
			else
			{
				GameDetailsControl.CurrentGame = null;
				UpdateButton.Visible = false;
			}
		}

		void SetGlobalSelection()
		{
			// List can't be empty, so return.
			// Issue: When DataSource is set then DataGridView fires the selectionChanged 3 times & it selects the first row. 
			if (GlobalSettingsDataGridView.SelectedRows.Count == 0) return;
			var row = GlobalSettingsDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
			var item = (x360ce.Engine.Data.Program)row.DataBoundItem;
			GameDefaultDetailsControl.SetMask(false, (HookMask)item.HookMask, (XInputMask)item.XInputMask, item.FileName, 0);
		}

		private void MyGamesAddButton_Click(object sender, EventArgs e)
		{
			var fullPath = "";
			var row = MySettingsDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
			if (row != null)
			{
				var item = (x360ce.Engine.Data.Game)row.DataBoundItem;
				fullPath = item.FullPath;
			}

			var path = "";
			GameApplicationOpenFileDialog.DefaultExt = ".exe";
			if (!string.IsNullOrEmpty(fullPath))
			{
				var fi = new System.IO.FileInfo(fullPath);
				if (string.IsNullOrEmpty(path)) path = fi.Directory.FullName;
				GameApplicationOpenFileDialog.FileName = fi.Name;
			}
			GameApplicationOpenFileDialog.Filter = Helper.GetFileDescription(".exe") + " (*.exe)|*.exe|All files (*.*)|*.*";
			GameApplicationOpenFileDialog.FilterIndex = 1;
			GameApplicationOpenFileDialog.RestoreDirectory = true;
			if (string.IsNullOrEmpty(path)) path = System.IO.Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			GameApplicationOpenFileDialog.InitialDirectory = path;
			GameApplicationOpenFileDialog.Title = "Browse for Executable";
			var result = GameApplicationOpenFileDialog.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK)
			{
				// Don't allow to add windows folder.
				var winFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				if (GameApplicationOpenFileDialog.FileName.StartsWith(winFolder))
				{
					MessageBoxForm.Show("Windows folders are not allowed.", "Windows Folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					// = GameApplicationOpenFileDialog.FileName;
					ProcessExecutable(GameApplicationOpenFileDialog.FileName);
				}
			}
		}

		private void StartButton_Click(object sender, EventArgs e)
		{
			var game = GameDetailsControl.CurrentGame;
			OpenPath(game.FullPath);
		}

		void OpenPath(string path, string arguments = null)
		{
			try
			{
				var fi = new System.IO.FileInfo(path);
				//if (!fi.Exists) return;
				// Brings up the "Windows cannot open this file" dialog if association not found.
				var psi = new ProcessStartInfo(path);
				psi.UseShellExecute = true;
				psi.WorkingDirectory = fi.Directory.FullName;
				psi.ErrorDialog = true;
				if (arguments != null) psi.Arguments = arguments;
				Process.Start(psi);
			}
			catch (Exception) { }
		}

		private void EnableButton_Click(object sender, EventArgs e)
		{
			var item = GameDetailsControl.CurrentGame;
			if (item != null) item.IsEnabled = true;
		}

		private void DisableButton_Click(object sender, EventArgs e)
		{
			var item = GameDetailsControl.CurrentGame;
			if (item != null) item.IsEnabled = false;
		}

		private void MySettingsDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0) return;
			var grid = (DataGridView)sender;
			if (e.ColumnIndex == grid.Columns[EnabledColumn.Name].Index)
			{
				var item = (x360ce.Engine.Data.Game)grid.Rows[e.RowIndex].DataBoundItem;
				item.IsEnabled = !item.IsEnabled;
				grid.Invalidate();
			}
		}

		private void FolderButton_Click(object sender, EventArgs e)
		{
			var game = GameDetailsControl.CurrentGame;
			if (!File.Exists(game.FullPath)) return;
			string argument = @"/select, " + game.FullPath;
			System.Diagnostics.Process.Start("explorer.exe", argument);
			// OpenPath(game.FullPath);
		}

	}
}
