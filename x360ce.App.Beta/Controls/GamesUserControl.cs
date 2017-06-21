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
using JocysCom.ClassLibrary.Runtime;

namespace x360ce.App.Controls
{
	public partial class GamesUserControl : UserControl
	{

		#region Initialize

		public GamesUserControl()
		{
			InitializeComponent();
			if (IsDesignMode) return;
			GamesDataGridView.AutoGenerateColumns = false;
			ProgramsDataGridView.AutoGenerateColumns = false;
			ScanProgressLabel.Text = "";
			InitData();
		}

		internal bool IsDesignMode { get { return JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this); } }

		void InitData()
		{
			// Configure Programs.
			SettingsManager.Programs.Items.ListChanged += new ListChangedEventHandler(Programs_ListChanged);
			ProgramsDataGridView.DataSource = SettingsManager.Programs.Items;
			UpdateControlsFromPrograms();
			// Configure Games.
			SettingsManager.UserGames.Items.ListChanged += new ListChangedEventHandler(Games_ListChanged);
			GamesDataGridView.DataSource = SettingsManager.UserGames.Items;
			SettingsManager.UserGames.Items.ListChanged += Games_Items_ListChanged;
			UpdateControlsFromGames();
		}

		#endregion

		private void Games_Items_ListChanged(object sender, ListChangedEventArgs e)
		{
			ShowHideAndSelectGridRows();
		}

		#region Scan Games

		/// <summary>
		/// Scan for games
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ScanButton_Click(object sender, EventArgs e)
		{
			MessageBoxForm form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var result = form.ShowForm("Scan for games on your computer?", "Scan", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
			if (result == DialogResult.OK)
			{
				var success = System.Threading.ThreadPool.QueueUserWorkItem(ScanGames);
				if (!success) ScanProgressLabel.Text = "Scan failed!";
			}
		}

		void ScanGames(object state)
		{
			string[] paths = null;
			Invoke((MethodInvoker)delegate ()
			{
				ScanProgressLabel.Visible = true;
				ScanGamesButton.Enabled = false;
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
				AppHelper.GetFiles(di, ref exes, "*.exe", true);
				for (int f = 0; f < exes.Count; f++)
				{

					var exe = exes[f];
					var exeName = exe.Name.ToLower();
					var program = SettingsManager.Programs.Items.FirstOrDefault(x => x.FileName.ToLower() == exeName);
					// If file doesn't exist in the game list then continue.
					if (program == null)
					{
						skipped++;
					}
					else
					{
						// Get game by executable name.
						var game = SettingsManager.UserGames.Items.FirstOrDefault(x => x.FileName.ToLower() == exeName);
						// If file doesn't exist in the game list then continue.
						if (game == null)
						{
							Invoke((MethodInvoker)delegate ()
							{
								game = x360ce.Engine.Data.UserGame.FromDisk(exe.FullName);
								game.LoadDefault(program);
								SettingsManager.UserGames.Items.Add(game);
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
					Invoke((MethodInvoker)delegate ()
						{
							ScanProgressLabel.Text = string.Format("Scanning Path ({0}/{1}): {2}\r\nSkipped = {3}, Added = {4}, Updated = {5}", i + 1, paths.Length, path, skipped, added, updated);
						});
				}
				SettingsManager.Save();
			}
			Invoke((MethodInvoker)delegate ()
			{
				ScanGamesButton.Enabled = true;
				ScanProgressLabel.Visible = false;
			});
		}

		private void GameSettingsUserControl_Load(object sender, EventArgs e)
		{
			EngineHelper.EnableDoubleBuffering(GamesDataGridView);
			EngineHelper.EnableDoubleBuffering(ProgramsDataGridView);
			LoadProgramsFromLocalGdbFile();
			ProgramImageColumn.Visible = false;
		}

		void Programs_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromPrograms();
		}

		void UpdateControlsFromPrograms()
		{
			var enabled = SettingsManager.Programs.Items.Count > 0;
			if (ExportProgramsButton.Enabled != enabled) ExportProgramsButton.Enabled = enabled;
		}

		void Games_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromGames();
		}

		void UpdateControlsFromGames()
		{

		}

		#endregion

		#region Games (My Game Settings)


		private void GamesDataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
		{
			ShowHideAndSelectGridRows();
		}

		void GamesDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			SetGamesSelection();
		}

		private void GamesTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (GamesTabControl.SelectedTab == GlobalSettingsTabPage) SetProgramsSelection();
			else if (GamesTabControl.SelectedTab == GamesTabPage) SetGamesSelection();
		}

		void SetGamesSelection()
		{
			// List can't be empty, so return.
			// Issue: When DataSource is set then DataGridView fires the selectionChanged 3 times & it selects the first row. 
			var selected = GamesDataGridView.SelectedRows.Count > 0;
			StartGameButton.Enabled = selected;
			SaveGamesButton.Enabled = selected;
			DeleteGamesButton.Enabled = selected;
			if (selected)
			{
				var row = GamesDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
				var fileName = ((x360ce.Engine.Data.UserGame)row.DataBoundItem).FileName.ToLower();
				var item = SettingsManager.UserGames.Items.First(x => x.FileName.ToLower() == fileName);
				GameDetailsControl.CurrentGame = item;
			}
			else
			{
				GameDetailsControl.CurrentGame = null;
			}
		}

		private void AddGameButton_Click(object sender, EventArgs e)
		{
			AddNewGame();
		}

		void AddNewGame()
		{
			var fullPath = "";
			var row = GamesDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
			if (row != null)
			{
				var item = (x360ce.Engine.Data.UserGame)row.DataBoundItem;
				fullPath = item.FullPath;
			}

			var path = "";
			AddGameOpenFileDialog.DefaultExt = ".exe";
			if (!string.IsNullOrEmpty(fullPath))
			{
				var fi = new System.IO.FileInfo(fullPath);
				if (string.IsNullOrEmpty(path)) path = fi.Directory.FullName;
				AddGameOpenFileDialog.FileName = fi.Name;
			}
			AddGameOpenFileDialog.Filter = EngineHelper.GetFileDescription(".exe") + " (*.exe)|*.exe|All files (*.*)|*.*";
			AddGameOpenFileDialog.FilterIndex = 1;
			AddGameOpenFileDialog.RestoreDirectory = true;
			if (string.IsNullOrEmpty(path)) path = System.IO.Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			AddGameOpenFileDialog.InitialDirectory = path;
			AddGameOpenFileDialog.Title = "Browse for Executable";
			var result = AddGameOpenFileDialog.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK)
			{
				// Don't allow to add windows folder.
				var winFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				if (AddGameOpenFileDialog.FileName.StartsWith(winFolder))
				{
					MessageBoxForm.Show("Windows folders are not allowed.", "Windows Folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					ProcessExecutable(AddGameOpenFileDialog.FileName);
				}
			}
		}

		public void ProcessExecutable(string fileName)
		{
			SettingsManager.ProcessExecutable(fileName);
			GamesDataGridView.ClearSelection();
			ShowHideAndSelectGridRows(Path.GetFileName(fileName));
		}

		private void StartGameButton_Click(object sender, EventArgs e)
		{
			var game = GameDetailsControl.CurrentGame;
			EngineHelper.OpenPath(game.FullPath);
		}

		private void GamesDataGridView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete) DeleteSelectedGames();
			else if (e.KeyCode == Keys.Insert) AddNewGame();
		}

		private void GamesDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0) return;
			var grid = (DataGridView)sender;
			// If user clicked on the checkbox column then...
			if (e.ColumnIndex == grid.Columns[IsEnabledColumn.Name].Index)
			{
				var row = grid.Rows[e.RowIndex];
				var item = (x360ce.Engine.Data.UserGame)row.DataBoundItem;
				// Changed check (enabled state) of the current item.
				item.IsEnabled = !item.IsEnabled;
			}
		}

		private void OpenGameFolderButton_Click(object sender, EventArgs e)
		{
			var game = GameDetailsControl.CurrentGame;
			if (!File.Exists(game.FullPath)) return;
			EngineHelper.BrowsePath(game.FullPath);
		}

		private void SaveGamesButton_Click(object sender, EventArgs e)
		{
			SettingsManager.Save(true);
		}

		private void DeleteGamesButton_Click(object sender, EventArgs e)
		{
			DeleteSelectedGames();
		}

		void DeleteSelectedGames()
		{
			var grid = GamesDataGridView;
			var selection = JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<string>(grid, "FileName");
			var itemsToDelete = SettingsManager.UserGames.Items.Where(x => selection.Contains(x.FileName)).ToArray();
			MessageBoxForm form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			string message;
			if (itemsToDelete.Length == 1)
			{
				var item = itemsToDelete[0];
				message = string.Format("Are you sure you want to delete settings for?\r\n\r\n\tFile Name: {0}\r\n\tProduct Name: {1}",
					item.FileName,
					item.FileProductName);
			}
			else
			{
				message = string.Format("Delete {0} setting(s)?", itemsToDelete.Length);
			}
			var result = form.ShowForm(message, "Delete", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
			if (result == DialogResult.OK)
			{
				foreach (var item in itemsToDelete)
				{
					SettingsManager.UserGames.Items.Remove(item);
				}
				SettingsManager.Save();
				MainForm.Current.CloudPanel.Add(CloudAction.Delete, itemsToDelete);
			}
		}

		private void ShowGamesMenuItem_Click(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem)sender;
			ShowGamesDropDownButton.Image = item.Image;
			ShowGamesDropDownButton.Text = item.Text;
		}

		void GamesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var row = grid.Rows[e.RowIndex];
			var item = ((x360ce.Engine.Data.UserGame)row.DataBoundItem);
			var isCurrent = GameDetailsControl.CurrentGame != null && item.GameId == GameDetailsControl.CurrentGame.GameId;
			AppHelper.ApplyRowStyle(grid, e, item.IsEnabled);
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
				e.Value = isCurrent ? SaveGamesButton.Image : Properties.Resources.empty_16x16;
			}
			else
			{
				//var row = grid.Rows[e.RowIndex].Cells[MyIconColumn.Name];
				//e.CellStyle.BackColor = isCurrent ? currentColor : grid.DefaultCellStyle.BackColor;
			}
		}

		void ShowHideAndSelectGridRows(string selectFile = null)
		{
			var grid = GamesDataGridView;
			var selection = string.IsNullOrEmpty(selectFile)
				? JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<string>(grid, "FileName")
				: new List<string>() { selectFile };
			grid.CurrentCell = null;
			// Suspend Layout and CurrencyManager to avoid exceptions.
			grid.SuspendLayout();
			var cm = (CurrencyManager)BindingContext[grid.DataSource];
			cm.SuspendBinding();
			var rows = grid.Rows.Cast<DataGridViewRow>().ToArray();
			// Reverse order to hide/show bottom records first..
			Array.Reverse(rows);
			var showEnabled = ShowGamesDropDownButton.Text.Contains("Enabled");
			var showDisabled = ShowGamesDropDownButton.Text.Contains("Disabled");
			for (int i = 0; i < rows.Length; i++)
			{
				var item = (x360ce.Engine.Data.UserGame)rows[i].DataBoundItem;
				var show = true;
				if (showEnabled) show = (item.IsEnabled == true);
				if (showDisabled) show = (item.IsEnabled == false);
				if (rows[i].Visible != show)
				{
					rows[i].Visible = show;
				}
			}
			// Resume CurrencyManager and Layout.
			cm.ResumeBinding();
			grid.ResumeLayout();
			// Restore selection.
			JocysCom.ClassLibrary.Controls.ControlsHelper.RestoreSelection(grid, "FileName", selection);
		}

		#endregion

		#region Programs (Game Defaults)

		void LoadProgramsFromLocalGdbFile()
		{
			var path = GameDatabaseManager.Current.GdbFile.FullName;
			var ini = new Ini(path);
			var sections = ini.GetSections();
			foreach (var section in sections)
			{
				var program = SettingsManager.Programs.Items.FirstOrDefault(x => x.FileName.ToLower() == section.ToLower());
				if (program == null)
				{
					program = new Engine.Data.Program();
					program.FileName = section;
					program.HookMask = 0x00000002;
					program.XInputMask = 0x00000004;
					SettingsManager.Programs.Items.Add(program);
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

		void SetProgramsSelection()
		{
			// List can't be empty, so return.
			// Issue: When DataSource is set then DataGridView fires the selectionChanged 3 times & it selects the first row. 
			if (ProgramsDataGridView.SelectedRows.Count == 0) return;
			var row = ProgramsDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
			var item = (x360ce.Engine.Data.Program)row.DataBoundItem;
			GameDefaultDetailsControl.SetMask(false, (HookMask)item.HookMask, (DInputMask)0, (XInputMask)item.XInputMask, item.FileName, 0);
		}

		private void RefreshProgramsButton_Click(object sender, EventArgs e)
		{
			RefreshProgramsListFromCloud();
		}

		private void ProgramsDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			SetProgramsSelection();
		}

		private void ImportProgramsButton_Click(object sender, EventArgs e)
		{
			ImportPrograms();
		}

		private void ExportProgramsButton_Click(object sender, EventArgs e)
		{
			ExportPrograms();
		}

		private void DeleteProgramsButton_Click(object sender, EventArgs e)
		{
			DeleteSelectedPrograms();
		}


		/// <summary>
		/// Import Programs (Default Game Settings) from external file.
		/// </summary>
		void ImportPrograms()
		{
			var dialog = ImportOpenFileDialog;
			dialog.DefaultExt = "*.xml";
			dialog.Filter = "Game Settings (*.xml;*.xml.gz;*.ini;*.gdb)|*.xml;*.xml.gz;*.ini;*.gdb|All files (*.*)|*.*";
			dialog.FilterIndex = 1;
			dialog.RestoreDirectory = true;
			if (string.IsNullOrEmpty(dialog.FileName)) dialog.FileName = "x360ce_Games";
			if (string.IsNullOrEmpty(dialog.InitialDirectory)) dialog.InitialDirectory = GameDatabaseManager.Current.GdbFile.Directory.FullName;
			dialog.Title = "Import Games Settings File";
			var result = dialog.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK)
			{
				List<x360ce.Engine.Data.Program> programs;
				if (dialog.FileName.EndsWith(".gz"))
				{
					var compressedBytes = System.IO.File.ReadAllBytes(dialog.FileName);
					var bytes = EngineHelper.Decompress(compressedBytes);
					var xml = System.Text.Encoding.UTF8.GetString(bytes);
					programs = Serializer.DeserializeFromXmlString<List<x360ce.Engine.Data.Program>>(xml, System.Text.Encoding.UTF8);
				}
				else if (dialog.FileName.EndsWith(".ini") || dialog.FileName.EndsWith(".gdb"))
				{
					programs = GameDatabaseManager.GetPrograms(dialog.FileName);
				}
				else
				{
					programs = Serializer.DeserializeFromXmlFile<List<x360ce.Engine.Data.Program>>(dialog.FileName);
				}
				ImportAndBindPrograms(programs);
			}

		}

		/// <summary>
		/// Export Programs (Default Game Settings) to external file.
		/// </summary>
		void ExportPrograms(){
			var dialog = ExportSaveFileDialog;
			dialog.DefaultExt = "*.xml";
			dialog.Filter = "Game Settings (*.xml)|*.xml|Compressed Game Settings (*.xml.gz)|*.gz|All files (*.*)|*.*";
			dialog.FilterIndex = 1;
			dialog.RestoreDirectory = true;
			if (string.IsNullOrEmpty(dialog.FileName)) dialog.FileName = "x360ce_Games";
			if (string.IsNullOrEmpty(dialog.InitialDirectory)) dialog.InitialDirectory = GameDatabaseManager.Current.GdbFile.Directory.FullName;
			dialog.Title = "Export Games Settings File";
			var result = dialog.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK)
			{
				var programs = SettingsManager.Programs.Items.ToList();
				foreach (var item in programs)
				{
					item.EntityKey = null;
					item.FileProductName = EngineHelper.FixName(item.FileProductName, item.FileName);
				}
				if (dialog.FileName.EndsWith(".gz"))
				{
					var s = Serializer.SerializeToXmlString(programs, System.Text.Encoding.UTF8, true);
					var bytes = System.Text.Encoding.UTF8.GetBytes(s);
					var compressedBytes = EngineHelper.Compress(bytes);
					System.IO.File.WriteAllBytes(dialog.FileName, compressedBytes);
				}
				else
				{
					Serializer.SerializeToXmlFile(programs, dialog.FileName, System.Text.Encoding.UTF8, true);
				}
			}

		}

		/// <summary>
		/// Delete selected Programs (Default Game Settings) from current settings.
		/// </summary>
		void DeleteSelectedPrograms()
		{
			var grid = ProgramsDataGridView;
			var selection = JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<string>(grid, "FileName");
			var itemsToDelete = SettingsManager.Programs.Items.Where(x => selection.Contains(x.FileName)).ToArray();
			MessageBoxForm form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			string message;
			if (itemsToDelete.Length == 1)
			{
				var item = itemsToDelete[0];
				message = string.Format("Are you sure you want to delete default settings for?\r\n\r\n\tFile Name: {0}\r\n\tProduct Name: {1}",
					item.FileName,
					item.FileProductName);
			}
			else
			{
				message = string.Format("Delete {0} default setting(s)?", itemsToDelete.Length);
			}
			var result = form.ShowForm(message, "Delete", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
			if (result == DialogResult.OK)
			{
				foreach (var item in itemsToDelete)
				{
					SettingsManager.Programs.Items.Remove(item);
				}
				SettingsManager.Save();
			}
		}

		/// <summary>
		/// Merge supplied list of programs with current settings.
		/// </summary>
		/// <param name="programs">Programs list to merge.</param>
		void ImportAndBindPrograms(List<x360ce.Engine.Data.Program> programs)
		{
			var grid = ProgramsDataGridView;
			var selection = JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<string>(grid, "FileName");
			var newItems = programs.ToArray();
			ProgramsDataGridView.DataSource = null;
			foreach (var newItem in newItems)
			{
				// Try to find existing item inside programs.
				var existingItems = SettingsManager.Programs.Items.Where(x => x.FileName.ToLower() == newItem.FileName.ToLower()).ToArray();
				// Remove existing items.
				for (int i = 0; i < existingItems.Length; i++)
				{
					SettingsManager.Programs.Items.Remove(existingItems[i]);
				}
				// Fix product name.
				var fixedProductName = EngineHelper.FixName(newItem.FileProductName, newItem.FileName);
				newItem.FileProductName = fixedProductName;
				// If new item is missing XInputMask setting then...
				if (newItem.XInputMask == (int)XInputMask.None)
				{
					// Assign default.
					newItem.XInputMask = (int)XInputMask.XInput13_x86;
				}
				// Add new one.
				SettingsManager.Programs.Items.Add(newItem);
			}
			MainForm.Current.SetHeaderBody(
				MessageBoxIcon.Information,
				"{0: yyyy-MM-dd HH:mm:ss}: '{1}' program(s) loaded.",
				DateTime.Now, programs.Count()
			);
			ProgramsDataGridView.DataSource = SettingsManager.Programs.Items;
			JocysCom.ClassLibrary.Controls.ControlsHelper.RestoreSelection(grid, "FileName", selection);
			SettingsManager.Save(true);
		}

		/// <summary>
		/// Refresh Programs (Default Game Settings) from the cloud.
		/// </summary>
		void RefreshProgramsListFromCloud()
		{
			var ws = new WebServiceClient();
			ws.Url = SettingsManager.Options.InternetDatabaseUrl;
			var enabled = EnabledState.None;
			if (IncludeEnabledCheckBox.CheckState == CheckState.Checked) enabled = EnabledState.Enabled;
			if (IncludeEnabledCheckBox.CheckState == CheckState.Unchecked) enabled = EnabledState.Disabled;
			var minInstances = (int)MinimumInstanceCountNumericUpDown.Value;
			ws.GetProgramsCompleted += ProgramsWebServiceClient_GetProgramsCompleted;
			System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
			{
				ws.GetProgramsAsync(enabled, minInstances);
			});
		}

		void ProgramsWebServiceClient_GetProgramsCompleted(object sender, ResultEventArgs e)
		{
			// Make sure method is executed on the same thread as this control.
			BeginInvoke((MethodInvoker)delegate ()
			{
				MainForm.Current.AddTask(TaskName.GetPrograms);
				if (e.Error != null)
				{
					var error = e.Error.Message;
					if (e.Error.InnerException != null) error += "\r\n" + e.Error.InnerException.Message;
					MainForm.Current.SetHeaderBody(MessageBoxIcon.Error, error);
				}
				else if (e.Result == null)
				{
					MainForm.Current.SetHeaderBody(MessageBoxIcon.Error, "No results were returned by the web service!");
				}
				else
				{
					var result = (List<x360ce.Engine.Data.Program>)e.Result;
					ImportAndBindPrograms(result);
				}
				MainForm.Current.RemoveTask(TaskName.GetPrograms);
			});
		}

		private void ProgramsDataGridView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete) DeleteSelectedPrograms();
			else if (e.KeyCode == Keys.Insert) ImportPrograms();
		}

		#endregion

	}
}
