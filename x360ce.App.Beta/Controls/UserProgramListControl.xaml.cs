using JocysCom.ClassLibrary.Controls;
using Microsoft.Win32;
using System.Windows.Controls;
using System;
using System.Linq;
using System.Text;
using System.Windows;
using x360ce.Engine;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for UserProgramListControl.xaml
	/// </summary>
	public partial class UserProgramListControl : UserControl
	{
		public UserProgramListControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
			ScanProgressLevel0Label.Text = "";
			ScanProgressLevel1Label.Text = "";
		}

		OpenFileDialog AddGameOpenFileDialog { get; } = new OpenFileDialog();


		public void UnInitControl()
		{
			var gs = GameScanner;
			if (gs != null)
				gs.IsStopping = true;
		}

		#region ■ Scan Games

		/// <summary>
		/// Scan for games
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ScanButton_Click(object sender, RoutedEventArgs e)
		{
			var form = new MessageBoxWindow();
			var result = form.ShowDialog("Scan for games on your computer?", "Scan", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Question);
			if (result == System.Windows.MessageBoxResult.OK)
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
			if (ControlsHelper.InvokeRequired)
			{
				ControlsHelper.Invoke(() =>
					Scanner_Progress(sender, e)
				);
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
						sb.AppendFormat("Current Folder: {0}", e.Directories[(int)e.DirectoryIndex].FullName);
					}
					if (e.State == XInputMaskScannerState.FileUpdate && e.Files != null)
					{
						var file = e.Files[(int)e.FileIndex];
						var size = file.Length / 1024 / 1024;
						sb.AppendFormat("Current File ({0:0.0} MB): {1} ", size, file.FullName);
					}
					if (e.Level == 0)
					{
						sb.AppendLine();
						sb.AppendFormat("Skipped = {0}, Added = {1}, Updated = {2}", e.Skipped, e.Added, e.Updated);
					}
					sb.AppendLine();
					ControlsHelper.Invoke(() =>
					{
						label.Text = sb.ToString();
					});
					break;
				case XInputMaskScannerState.Completed:
					ControlsHelper.Invoke(() =>
					{
						ScanButton.IsEnabled = true;
						ScanProgressPanel.Visibility = Visibility.Collapsed;
					});
					SettingsManager.Save();
					break;
				default:
					break;
			}
		}

		void ScanGames(object state)
		{
			var exe = state as string;
			ControlsHelper.Invoke(() =>
			{
				ScanProgressLevel0Label.Text = "...";
				ScanProgressLevel1Label.Text = "";
				ScanProgressPanel.Visibility = Visibility.Visible;
				ScanButton.IsEnabled = false;
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

		#region ■ Games (My Game Settings)

		x360ce.Engine.Data.UserGame SelectedItem
			=> MainDataGrid.SelectedItems.Cast<x360ce.Engine.Data.UserGame>().FirstOrDefault();


		void SetGamesSelection()
		{
			// List can't be empty, so return.
			// Issue: When DataSource is set then DataGridView fires the selectionChanged 3 times & it selects the first row. 
			var item = SelectedItem;
			var selected = item != null;
			var exists = false;
			if (selected)
				exists = File.Exists(item.FullPath);
			ControlsHelper.SetEnabled(OpenButton, selected && exists);
			ControlsHelper.SetEnabled(StartButton, selected && exists);
			ControlsHelper.SetEnabled(SaveButton, selected);
			ControlsHelper.SetEnabled(DeleteButton, selected);
		}

		public void AddNewGame()
		{
			var fullPath = "";
			var item = SelectedItem;
			if (item != null)
				fullPath = item.FullPath;
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
			if (result == true)
			{
				// Don't allow to add windows folder.
				var winFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				if (AddGameOpenFileDialog.FileName.StartsWith(winFolder, StringComparison.OrdinalIgnoreCase))
				{
					MessageBoxWindow.Show("Windows folders are not allowed.", "Windows Folder", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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
			MainDataGrid.SelectedItems.Clear();
			object selelectItemKey = null;
			if (game != null)
				selelectItemKey = game.GameId;
			//ControlHelper.ShowHideAndSelectGridRows(GamesDataGridView, ShowGamesDropDownButton, null, selelectItemKey);
		}

		//private void GamesDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
		//{
		//	if (e.RowIndex < 0 || e.ColumnIndex < 0)
		//		return;
		//	var grid = (DataGrid)sender;
		//	var column = grid.Columns[e.ColumnIndex];
		//	// If user clicked on the CheckBox column then...
		//	if (column == IsEnabledColumn)
		//	{
		//		var row = grid.Rows[e.RowIndex];
		//		var item = (x360ce.Engine.Data.UserGame)row.DataBoundItem;
		//		// Changed check (enabled state) of the current item.
		//		item.IsEnabled = !item.IsEnabled;
		//		// If game was enabled then...
		//		if (item.IsEnabled)
		//		{
		//			var dirFullName = Path.GetDirectoryName(item.FullPath).ToLower();
		//			// Get games with different platform in the same folder.
		//			var otherGames = SettingsManager.UserGames.Items
		//				.Where(x => x.IsEnabled && x.FullPath.ToLower().StartsWith(dirFullName) && x.ProcessorArchitecture != item.ProcessorArchitecture)
		//				.ToList();
		//			// Disable other games, because used have to choose which 
		//			foreach (var g in otherGames)
		//			{
		//				g.IsEnabled = false;
		//			}
		//		}
		//	}
		//}

		void DeleteSelectedGames()
		{
			var grid = MainDataGrid;
			var userGames = MainDataGrid.SelectedItems.Cast<x360ce.Engine.Data.UserGame>().ToList();
			var form = new MessageBoxWindow();
			string message;
			if (userGames.Count == 1)
			{
				var item = userGames[0];
				message = string.Format("Are you sure you want to delete settings for?\r\n\r\n\tFile Name: {0}\r\n\tProduct Name: {1}",
					item.FileName,
					item.FileProductName);
			}
			else
			{
				message = string.Format("Delete {0} setting(s)?", userGames.Count);
			}
			var result = form.ShowDialog(message, "Delete", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
			if (result == MessageBoxResult.OK)
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

		//void GamesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		//{
		//	if (e.RowIndex < 0 || e.ColumnIndex < 0)
		//		return;
		//	var grid = (DataGridView)sender;
		//	var row = grid.Rows[e.RowIndex];
		//	var column = grid.Columns[e.ColumnIndex];
		//	var item = ((x360ce.Engine.Data.UserGame)row.DataBoundItem);
		//	var isCurrent = GameDetailsControl.CurrentItem == item;
		//	if (column == MyIconColumn)
		//	{
		//		e.Value = isCurrent ? SaveGamesButton.Image : Properties.Resources.empty_16x16;
		//	}
		//	else if (column == FileFolderColumn)
		//	{
		//		e.Value = Path.GetDirectoryName(item.FullPath);
		//	}
		//	else if (column == PlatformColumn)
		//	{
		//		var platform = (ProcessorArchitecture)item.ProcessorArchitecture;
		//		switch (platform)
		//		{
		//			case ProcessorArchitecture.MSIL:
		//				e.Value = "MSIL";
		//				break;
		//			case ProcessorArchitecture.X86:
		//				e.Value = "32-bit";
		//				break;
		//			case ProcessorArchitecture.IA64:
		//			case ProcessorArchitecture.Amd64:
		//				e.Value = "64-bit";
		//				break;
		//			default:
		//				e.Value = "";
		//				break;
		//		}
		//	}
		//}

		#endregion

		#region ■ Import

		/// <summary>
		/// Merge supplied list of items with current settings.
		/// </summary>
		/// <param name="items">List to merge.</param>
		public void ImportAndBindItems(IList<Engine.Data.UserGame> items)
		{
			var grid = MainDataGrid;
			//var key = nameof(UserGame.FileName);
			var list = SettingsManager.UserGames.Items;
			//var selection = JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<string>(grid, key);
			var newItems = items.ToArray();
			grid.ItemsSource = null;
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
			Global.HMan.SetBodyInfo("{0} {1}(s) loaded.", items.Count(), typeof(Engine.Data.UserGame).Name);
			grid.ItemsSource = list;
			//ControlsHelper.RestoreSelection(grid, key, selection);
			SettingsManager.Save();
		}

		#endregion

		private void AddButton_Click(object sender, RoutedEventArgs e)
			=> AddNewGame();

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
			=> DeleteSelectedGames();

		private void SaveButton_Click(object sender, RoutedEventArgs e)
			=> SettingsManager.Save();

		private void ShowMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			//var icon = (ContentControl)item.Icon;
			ShowMenuItem.Icon = ControlsHelper.Clone(item.Icon);
			ShowMenuItem.Header = item.Header;
			//ControlHelper.ShowHideAndSelectGridRows(GamesDataGridView, ShowGamesDropDownButton);
		}

		private void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			var item = SelectedItem;
			if (item == null)
				return;
			if (!File.Exists(item.FullPath))
				return;
			EngineHelper.BrowsePath(item.FullPath);
		}

		private void StartButton_Click(object sender, RoutedEventArgs e)
		{
			var item = SelectedItem;
			if (item == null)
				return;
			ControlsHelper.OpenPath(item.FullPath);
		}

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
			=> SetGamesSelection();

		private void MainDataGrid_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Delete)
				DeleteSelectedGames();
			else if (e.Key == System.Windows.Input.Key.Insert)
				AddNewGame();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
		}

	}
}
