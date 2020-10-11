using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	public partial class GamesGridUserControl : UserControl
	{

		#region Initialize

		public GamesGridUserControl()
		{
			InitializeComponent();
			if (IsDesignMode)
				return;
			// Make font more consistent with the rest of the interface.
			Controls.OfType<ToolStrip>().ToList().ForEach(x => x.Font = Font);
			JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(GamesDataGridView);
			//EngineHelper.EnableDoubleBuffering(GamesDataGridView);
			GamesDataGridView.AutoGenerateColumns = false;
			ScanProgressLevel0Label.Text = "";
			ScanProgressLevel1Label.Text = "";
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
			ControlsHelper.BeginInvoke(() =>
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
				ScanStarted = DateTime.Now;
				var success = System.Threading.ThreadPool.QueueUserWorkItem(ScanGames);
				if (!success)
				{
					ScanProgressLevel0Label.Text = "Scan failed!";
					ScanProgressLevel1Label.Text = "";
				}
			}
		}



		XInputMaskScanner GameScanner;
		DateTime ScanStarted;
		object GameAddLock = new object();

		private void Scanner_Progress(object sender, XInputMaskScannerEventArgs e)
		{
			if (MainForm.Current.InvokeRequired)
			{
				Invoke(new Action(() => 
					Scanner_Progress(sender, e)
				));
				return;
			}
			var scanner = (XInputMaskScanner)sender;
			var label = e.Level == 0
				? ScanProgressLevel0Label
				: ScanProgressLevel1Label;
			switch (e.State)
			{
				case XInputMaskScannerState.Started:
					label.Text = "Scanning...";
					break;
				case XInputMaskScannerState.GameFound:
					lock (GameAddLock)
					{
						// Get game to add.
						var game = e.Game;
						var dirFullName = e.GameFileInfo.Directory.FullName.ToLower();
						// Get existing games in the same folder.
						var oldGames = SettingsManager.UserGames.Items.Where(x => x.FullPath.ToLower().StartsWith(dirFullName)).ToList();
						var oldGame = oldGames.FirstOrDefault(x => x.IsEnabled && x.DateCreated < ScanStarted);
						var enabledGame = oldGames.FirstOrDefault(x => x.IsEnabled);
						// If this is 32-bit windows but game is 64-bit then...
						if (!Environment.Is64BitOperatingSystem && game.Is64Bit)
						{
							// Disable game.
							game.IsEnabled = false;
						}
						// If list contains enabled old game for other platform before scan started then...
						else if (oldGame != null)
						{
							// Enable if platform is the same, disable otherwise.
							game.IsEnabled = game.ProcessorArchitecture == oldGame.ProcessorArchitecture;
						}
						// At this point, oldGames list contains only new games added during the scan.
						// If this is 64-bit game then...
						else if (game.Is64Bit)
						{
							foreach (var g in oldGames)
							{
								// Disable non 64-bit games.
								if (g.IsEnabled && !g.Is64Bit)
									g.IsEnabled = false;
							}
						}
						// If contains enabled game then...
						else if (enabledGame != null)
						{
							// Enable if platform is the same, disable otherwise.
							game.IsEnabled = game.ProcessorArchitecture == enabledGame.ProcessorArchitecture;
						}
						SettingsManager.UserGames.Add(game);
					}
					break;
				case XInputMaskScannerState.GameUpdated:
					e.Game.FullPath = e.GameFileInfo.FullName;
					if (string.IsNullOrEmpty(e.Game.FileProductName) && !string.IsNullOrEmpty(e.Program.FileProductName))
					{
						e.Game.FileProductName = e.Program.FileProductName;
					}
					break;
				case XInputMaskScannerState.DirectoryUpdate:
				case XInputMaskScannerState.FileUpdate:
					var sb = new StringBuilder();
					sb.AppendLine(e.Message);
					if (e.State == XInputMaskScannerState.DirectoryUpdate && e.Directories != null)
					{
						sb.AppendFormat("Current Folder: {0}", e.Directories[e.DirectoryIndex].FullName);
					}
					if (e.State == XInputMaskScannerState.FileUpdate && e.Files != null)
					{
						var file = e.Files[e.FileIndex];
						var size = file.Length / 1024 / 1024;
						sb.AppendFormat("Current File ({0:0.0} MB): {1} ", size, file.FullName);
					}
					if (e.Level == 0)
					{
						sb.AppendLine();
						sb.AppendFormat("Skipped = {0}, Added = {1}, Updated = {2}", e.Skipped, e.Added, e.Updated);
					}
					sb.AppendLine();
					label.Text = sb.ToString();
					Application.DoEvents();
					break;
				case XInputMaskScannerState.Completed:
					ScanGamesButton.Enabled = true;
					ScanProgressPanel.Visible = false;
					SettingsManager.Save();
					break;
				default:
					break;
			}
		}


		void ScanGames(object state)
		{
			var exe = state as string;
			Invoke((Action)delegate ()
			{
				ScanProgressLevel0Label.Text = "...";
				ScanProgressLevel1Label.Text = "";
				ScanProgressPanel.Visible = true;
				ScanGamesButton.Enabled = false;
			});
			GameScanner = new XInputMaskScanner();
			GameScanner.Progress += Scanner_Progress;
			string[] paths;
			string name = null;
			if (string.IsNullOrEmpty(exe))
			{
				paths = SettingsManager.Options.GameScanLocations.ToArray();
			}
			else
			{
				// Set properties to scan single file.
				paths = new string[] { System.IO.Path.GetDirectoryName(exe) };
				name = System.IO.Path.GetFileName(exe);
			}
			var games = SettingsManager.UserGames.Items;
			var programs = SettingsManager.Programs.Items;
			GameScanner.ScanGames(paths, games, programs, name);
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
			if (GameDetailsControl.CurrentItem != item)
			{
				GameDetailsControl.CurrentItem = item;
			}
			ControlsHelper.SetEnabled(OpenGameButton, selected && exists);
			ControlsHelper.SetEnabled(StartGameButton, selected && exists);
			ControlsHelper.SetEnabled(SaveGamesButton, selected);
			ControlsHelper.SetEnabled(DeleteGamesButton, selected);
		}

		private void AddGameButton_Click(object sender, EventArgs e)
		{
			AddNewGame();
		}

		public void AddNewGame()
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
				if (string.IsNullOrEmpty(path))
					path = fi.Directory.FullName;
				AddGameOpenFileDialog.FileName = fi.Name;
			}
			AddGameOpenFileDialog.Filter = EngineHelper.GetFileDescription(".exe") + " (*.exe)|*.exe|All files (*.*)|*.*";
			AddGameOpenFileDialog.FilterIndex = 1;
			AddGameOpenFileDialog.RestoreDirectory = true;
			if (string.IsNullOrEmpty(path))
				path = System.IO.Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			AddGameOpenFileDialog.InitialDirectory = path;
			AddGameOpenFileDialog.Title = "Browse for Executable";
			var result = AddGameOpenFileDialog.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK)
			{
				// Don't allow to add windows folder.
				var winFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				if (AddGameOpenFileDialog.FileName.StartsWith(winFolder, StringComparison.OrdinalIgnoreCase))
				{
					MessageBoxForm.Show("Windows folders are not allowed.", "Windows Folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					ScanStarted = DateTime.Now;
					var success = System.Threading.ThreadPool.QueueUserWorkItem(ScanGames, AddGameOpenFileDialog.FileName);
					if (!success)
					{
						ScanProgressLevel0Label.Text = "Scan failed!";
						ScanProgressLevel1Label.Text = "";
					}
				}
			}
		}

		public void ProcessExecutable(string fileName)
		{
			var game = SettingsManager.ProcessExecutable(fileName);
			GamesDataGridView.ClearSelection();
			object selelectItemKey = null;
			if (game != null)
				selelectItemKey = game.GameId;
			ControlHelper.ShowHideAndSelectGridRows(GamesDataGridView, ShowGamesDropDownButton, null, selelectItemKey);
		}

		private void GamesDataGridView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
				DeleteSelectedGames();
			else if (e.KeyCode == Keys.Insert)
				AddNewGame();
		}

		private void GamesDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var column = grid.Columns[e.ColumnIndex];
			// If user clicked on the CheckBox column then...
			if (column == IsEnabledColumn)
			{
				var row = grid.Rows[e.RowIndex];
				var item = (x360ce.Engine.Data.UserGame)row.DataBoundItem;
				// Changed check (enabled state) of the current item.
				item.IsEnabled = !item.IsEnabled;
				// If game was enabled then...
				if (item.IsEnabled)
				{
					var dirFullName = Path.GetDirectoryName(item.FullPath).ToLower();
					// Get games with different platform in the same folder.
					var otherGames = SettingsManager.UserGames.Items
						.Where(x => x.IsEnabled && x.FullPath.ToLower().StartsWith(dirFullName) && x.ProcessorArchitecture != item.ProcessorArchitecture)
						.ToList();
					// Disable other games, because used have to choose which 
					foreach (var g in otherGames)
					{
						g.IsEnabled = false;
					}
				}
			}
		}

		private void OpenGameFolderButton_Click(object sender, EventArgs e)
		{
			var item = GamesDataGridView.SelectedRows
			  .Cast<DataGridViewRow>()
			  .Select(x => (x360ce.Engine.Data.UserGame)x.DataBoundItem)
			  .FirstOrDefault();
			if (item == null)
				return;
			if (!File.Exists(item.FullPath))
				return;
			EngineHelper.BrowsePath(item.FullPath);
		}


		private void StartGameButton_Click(object sender, EventArgs e)
		{
			var item = GamesDataGridView.SelectedRows
			  .Cast<DataGridViewRow>()
			  .Select(x => (x360ce.Engine.Data.UserGame)x.DataBoundItem)
			  .FirstOrDefault();
			if (item == null)
				return;
			ControlsHelper.OpenPath(item.FullPath);
		}

		private void SaveGamesButton_Click(object sender, EventArgs e)
		{
			SettingsManager.Save();
		}

		private void DeleteGamesButton_Click(object sender, EventArgs e)
		{
			DeleteSelectedGames();
		}

		void DeleteSelectedGames()
		{
			var grid = GamesDataGridView;
			var selection = JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<string>(grid, nameof(UserGame.FileName));
			var userGames = SettingsManager.UserGames.Items.Where(x => selection.Contains(x.FileName)).ToArray();
			MessageBoxForm form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			string message;
			if (userGames.Length == 1)
			{
				var item = userGames[0];
				message = string.Format("Are you sure you want to delete settings for?\r\n\r\n\tFile Name: {0}\r\n\tProduct Name: {1}",
					item.FileName,
					item.FileProductName);
			}
			else
			{
				message = string.Format("Delete {0} setting(s)?", userGames.Length);
			}
			var result = form.ShowForm(message, "Delete", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
			if (result == DialogResult.OK)
			{
				// Remove from local settings.
				foreach (var item in userGames)
					SettingsManager.UserGames.Items.Remove(item);
				SettingsManager.Save();
				// Remove from cloud settings.
				Task.Run(new Action(() =>
				{
					foreach (var item in userGames)
						Global.CloudClient.Add(CloudAction.Delete, new UserGame[] { item });
				}));
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
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var row = grid.Rows[e.RowIndex];
			var column = grid.Columns[e.ColumnIndex];
			var item = ((x360ce.Engine.Data.UserGame)row.DataBoundItem);
			var isCurrent = GameDetailsControl.CurrentItem == item;
			if (column == MyIconColumn)
			{
				e.Value = isCurrent ? SaveGamesButton.Image : Properties.Resources.empty_16x16;
			}
			else if (column == FileFolderColumn)
			{
				e.Value = Path.GetDirectoryName(item.FullPath);
			}
			else if (column == PlatformColumn)
			{
				var platform = (ProcessorArchitecture)item.ProcessorArchitecture;
				switch (platform)
				{
					case ProcessorArchitecture.MSIL:
						e.Value = "MSIL";
						break;
					case ProcessorArchitecture.X86:
						e.Value = "32-bit";
						break;
					case ProcessorArchitecture.IA64:
					case ProcessorArchitecture.Amd64:
						e.Value = "64-bit";
						break;
					default:
						e.Value = "";
						break;
				}
			}
		}

		#endregion

		private void GamesGridUserControl_Load(object sender, EventArgs e)
		{
			InitControl();
		}

		#region Import

		/// <summary>
		/// Merge supplied list of items with current settings.
		/// </summary>
		/// <param name="items">List to merge.</param>
		public void ImportAndBindItems(IList<Engine.Data.UserGame> items)
		{
			var grid = GamesDataGridView;
			var key = nameof(UserGame.FileName);
			var list = SettingsManager.UserGames.Items;
			var selection = JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<string>(grid, key);
			var newItems = items.ToArray();
			grid.DataSource = null;
			foreach (var newItem in newItems)
			{
				// Fix product name.
				newItem.FileProductName = EngineHelper.FixName(newItem.FileProductName, newItem.FileName);
				// If new item is missing XInputMask setting then assign default.
				if (newItem.XInputMask == (int)XInputMask.None)
					newItem.XInputMask = (int)XInputMask.XInput13_x86;
				// Try to find existing item inside the list.
				var existingItems = list.Where(x => x.FileName.ToLower() == newItem.FileName.ToLower()).ToArray();
				// Remove extra items.
				for (int i = 1; i < existingItems.Length; i++)
					list.Remove(existingItems[i]);
				// Get extra item.
				var existingItem = existingItems.FirstOrDefault();
				// If item found then update propertied without removing.
				if (existingItem != null)
					JocysCom.ClassLibrary.Runtime.RuntimeHelper.CopyProperties(newItem, existingItem);
			}
			MainForm.Current.SetHeaderInfo("{0} {1}(s) loaded.", items.Count(), typeof(Engine.Data.UserGame).Name);
			grid.DataSource = list;
			ControlsHelper.RestoreSelection(grid, key, selection);
			SettingsManager.Save();
		}

		#endregion

		#region Dispose

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
				var gs = GameScanner;
				if (gs != null)
				{
					gs.IsStopping = true;
				}
			}
			base.Dispose(disposing);
		}

		#endregion
	}
}
