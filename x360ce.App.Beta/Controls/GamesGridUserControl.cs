using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	public partial class GamesGridUserControl : UserControl
	{

		#region Initialize

		public GamesGridUserControl()
		{
			InitializeComponent();
			if (IsDesignMode) return;
			ControlHelper.ApplyBorderStyle(GamesDataGridView);
			//EngineHelper.EnableDoubleBuffering(GamesDataGridView);
			GamesDataGridView.AutoGenerateColumns = false;
			ScanProgressLabel.Text = "";
		}

		internal bool IsDesignMode { get { return JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this); } }

		public void InitControl()
		{
			// Configure Games.
			var x = IsHandleCreated;
			SettingsManager.UserGames.Items.ListChanged += Games_Items_ListChanged;
			// WORKAROUND: Remove SelectionChanged event.
			GamesDataGridView.SelectionChanged -= GamesDataGridView_SelectionChanged;
			GamesDataGridView.DataSource = SettingsManager.UserGames.Items;
			// WORKAROUND: Use BeginInvoke to prevent SelectionChanged firing multiple times.
			BeginInvoke((MethodInvoker)delegate ()
			{
				GamesDataGridView.SelectionChanged += GamesDataGridView_SelectionChanged;
				GamesDataGridView_SelectionChanged(GamesDataGridView, new EventArgs());
			});
		}

		#endregion

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

		#endregion

		#region Games (My Game Settings)

		private void Games_Items_ListChanged(object sender, ListChangedEventArgs e)
		{
			ControlHelper.ShowHideAndSelectGridRows(GamesDataGridView, ShowGamesDropDownButton);
		}

		private void GamesDataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
		{
			ControlHelper.ShowHideAndSelectGridRows(GamesDataGridView, ShowGamesDropDownButton);
		}

		void GamesDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			SetGamesSelection();
		}

		void SetGamesSelection()
		{
			// List can't be empty, so return.
			// Issue: When DataSource is set then DataGridView fires the selectionChanged 3 times & it selects the first row. 
			var item = GamesDataGridView.SelectedRows
				.Cast<DataGridViewRow>()
				.Select(x => (x360ce.Engine.Data.UserGame)x.DataBoundItem)
				.FirstOrDefault();
			var selected = item != null;
			var exists = false;
			if (selected)
			{
				exists = File.Exists(item.FullPath);
			}
			if (GameDetailsControl.CurrentGame != item)
			{
				GameDetailsControl.CurrentGame = item;
			}
			AppHelper.SetEnabled(OpenGameButton, selected && exists);
			AppHelper.SetEnabled(StartGameButton, selected && exists);
			AppHelper.SetEnabled(SaveGamesButton, selected);
			AppHelper.SetEnabled(DeleteGamesButton, selected);
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
			var game = SettingsManager.ProcessExecutable(fileName);
			GamesDataGridView.ClearSelection();
			object selelectItemKey = null;
			if (game != null) selelectItemKey = game.GameId;
			ControlHelper.ShowHideAndSelectGridRows(GamesDataGridView, ShowGamesDropDownButton, null, selelectItemKey);
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
			ControlHelper.ShowHideAndSelectGridRows(GamesDataGridView, ShowGamesDropDownButton);
		}

		void GamesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var row = grid.Rows[e.RowIndex];
			var item = ((x360ce.Engine.Data.UserGame)row.DataBoundItem);
			var isCurrent = GameDetailsControl.CurrentGame != null && item.GameId == GameDetailsControl.CurrentGame.GameId;
			ControlHelper.ApplyRowStyle(grid, e, item.IsEnabled);
			if (e.ColumnIndex == grid.Columns[MyIconColumn.Name].Index)
			{
				e.Value = isCurrent ? SaveGamesButton.Image : Properties.Resources.empty_16x16;
			}
			else if (e.ColumnIndex == grid.Columns[FileFolderColumn.Name].Index)
			{
				e.Value = Path.GetDirectoryName(item.FullPath);
			}
		}

		#endregion

		private void GamesGridUserControl_Load(object sender, EventArgs e)
		{
			InitControl();
		}
	}
}
