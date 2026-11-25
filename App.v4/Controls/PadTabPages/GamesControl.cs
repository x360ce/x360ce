using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using x360ce.App.Plugins;
using x360ce.Engine;
using x360ce.Engine.Data;
using x360ce.Plugins;

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
			JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(GamesDataGridView);
			ScanProgressLevel0Label.Text = "";
			ScanProgressLevel1Label.Text = "";
			HandleCreated += (s, e) => AttachPluginManager(PluginManager.Current);
			HandleDestroyed += (s, e) => AttachPluginManager(null);
			// The game's pages lay themselves out with the window of their tabs, which can make this control's window, and
			// raise its Load, while InitializeComponent runs.
			if (IsHandleCreated)
				AttachPluginManager(PluginManager.Current);
		}

		internal bool IsDesignMode { get { return JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this); } }

		public void InitControl()
		{
			// Configure Games.
			var x = IsHandleCreated;
			SettingsManager.UserGames.Items.ListChanged += Games_Items_ListChanged;
			// WORKAROUND: Remove SelectionChanged event.
			GamesDataGridView.SelectionChanged -= GamesDataGridView_SelectionChanged;
			// Set where the list is bound: Load, which binds it, can come before the constructor's own lines.
			GamesDataGridView.AutoGenerateColumns = false;
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

		internal void Scanner_Progress(object sender, XInputMaskScannerEventArgs e)
		{
			var scanner = (XInputMaskScanner)sender;
			// Posted to the interface thread, never sent. A failure there is reported where it happened,
			// with its own stack; sent, it is thrown again on this worker, a thread-pool thread, where it
			// ends the program and the report names only the call that sent it. A game found or updated
			// is waited for, because the scan looks up its next file in the list that changes.
			var shown = ControlsHelper.BeginInvoke(() => ShowScanProgress(scanner, e));
			if (e.State == XInputMaskScannerState.GameFound || e.State == XInputMaskScannerState.GameUpdated)
				shown.Wait();
		}

		/// <summary>Shows or applies what the scan reports, on the interface thread. Stops the scan once the list has closed.</summary>
		void ShowScanProgress(XInputMaskScanner scanner, XInputMaskScannerEventArgs e)
		{
			if (IsDisposed)
			{
				scanner.IsStopping = true;
				return;
			}
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
						var dirFullName = e.GameFileInfo.Directory.FullName;
						// Get existing games in the same folder. A game listed without a path is in no folder.
						var oldGames = SettingsManager.UserGames.Items
							.Where(x => x.FullPath != null && x.FullPath.StartsWith(dirFullName, StringComparison.OrdinalIgnoreCase))
							.ToList();
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
					// A file added by hand that no default settings name comes with no program to take a name from.
					if (string.IsNullOrEmpty(e.Game.FileProductName) && e.Program != null && !string.IsNullOrEmpty(e.Program.FileProductName))
					{
						e.Game.FileProductName = e.Program.FileProductName;
					}
					break;
				case XInputMaskScannerState.DirectoryUpdate:
				case XInputMaskScannerState.FileUpdate:
					label.Text = ProgressText(e);
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


		/// <summary>
		/// What the scan says it is doing. A folder with nothing to scan reports an update with an
		/// empty list and an index of nought, so the index is checked before it is used.
		/// </summary>
		public static string ProgressText(XInputMaskScannerEventArgs e)
		{
			var sb = new StringBuilder();
			sb.AppendLine(e.Message);
			if (e.State == XInputMaskScannerState.DirectoryUpdate && e.Directories != null && e.DirectoryIndex >= 0 && e.DirectoryIndex < e.Directories.Count)
				sb.AppendFormat("Current Folder: {0}", e.Directories[e.DirectoryIndex].FullName);
			if (e.State == XInputMaskScannerState.FileUpdate && e.Files != null && e.FileIndex >= 0 && e.FileIndex < e.Files.Count)
			{
				var file = e.Files[e.FileIndex];
				// A name the folder lists but Windows cannot open has no size to give; asking ended the scan.
				var size = file.Exists ? file.Length / 1024 / 1024 : 0;
				sb.AppendFormat("Current File ({0:0.0} MB): {1} ", size, file.FullName);
			}
			if (e.Level == 0)
			{
				sb.AppendLine();
				sb.AppendFormat("Skipped = {0}, Added = {1}, Updated = {2}", e.Skipped, e.Added, e.Updated);
			}
			sb.AppendLine();
			return sb.ToString();
		}

		void ScanGames(object state)
		{
			var exe = state as string;
			ControlsHelper.BeginInvoke(() =>
			{
				if (IsDisposed)
					return;
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
			// The scanner updates a listed game of the same name rather than adding one. Shown a list
			// without that game, it adds this folder as a game of its own, which is what was asked for.
			IList<UserGame> games = SettingsManager.UserGames.Items;
			if (_AddSeparateGame && name != null)
				games = SettingsManager.UserGames.ItemsToArraySynchronized()
					.Where(x => !string.Equals(x.FileName, name, StringComparison.OrdinalIgnoreCase)).ToList();
			var programs = SettingsManager.Programs.Items;
			GameScanner.ScanGames(paths, games, programs, name);
		}

		/// <summary>Whether the file being added is to be listed beside a game of the same name rather than update it.</summary>
		bool _AddSeparateGame;

		#endregion

		#region Games (My Game Settings)

		internal void Games_Items_ListChanged(object sender, ListChangedEventArgs e)
		{
			ShowHideRowsAfterBinding();
		}

		private void GamesDataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
		{
			ShowHideRowsAfterBinding();
		}

		/// <summary>Shows the games the filter lets through, once whatever changed the list has finished.</summary>
		/// <remarks>
		/// Sorting changes the list and binds it again with every row showing, then puts the current cell back.
		/// Rows hidden in between leave that cell on a hidden row, which Windows Forms refuses by throwing.
		/// </remarks>
		void ShowHideRowsAfterBinding()
		{
			if (IsHandleCreated)
				BeginInvoke((Action)(() => ControlHelper.ShowHideAndSelectGridRows(GamesDataGridView, ShowGamesDropDownButton)));
			else
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
			ShowGamePlugins(item);
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
					// A program already listed under this name, and still there in its own folder, is
					// usually the same game: every copy shares one configuration. It is a different game
					// only if the person says so, as with the mods of one game that all carry its file.
					_AddSeparateGame = false;
					var other = SettingsManager.OtherGameWithSameName(AddGameOpenFileDialog.FileName);
					if (other != null)
					{
						var form = new MessageBoxForm();
						form.StartPosition = FormStartPosition.CenterParent;
						ControlsHelper.CheckTopMost(form);
						var answer = form.ShowForm(
							string.Format("{0} is already in the list:\r\n{1}\r\n\r\nAdd this folder as a separate game, or use the existing entry?", other.FileName, other.FullPath),
							"Add Game", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2,
							new[] { "Add Separate", "Use Existing" });
						form.Dispose();
						_AddSeparateGame = answer == DialogResult.Yes;
					}
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

		internal void GamesDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
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
				if (item.IsEnabled && !string.IsNullOrEmpty(item.FullPath))
				{
					var dirFullName = Path.GetDirectoryName(item.FullPath);
					// A root path such as "C:\" has no directory name; such a game is in no folder.
					if (!string.IsNullOrEmpty(dirFullName))
					{
						// Get games with different platform in the same folder. A game listed without a path is in no folder.
						var otherGames = SettingsManager.UserGames.Items
							.Where(x => x.IsEnabled && x.FullPath != null && x.FullPath.StartsWith(dirFullName, StringComparison.OrdinalIgnoreCase) && x.ProcessorArchitecture != item.ProcessorArchitecture)
							.ToList();
						// Disable other games, because used have to choose which
						foreach (var g in otherGames)
						{
							g.IsEnabled = false;
						}
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
			ControlsHelper.SetImage(ShowGamesDropDownButton, item.Image);
			ShowGamesDropDownButton.Text = item.Text;
			ControlHelper.ShowHideAndSelectGridRows(GamesDataGridView, ShowGamesDropDownButton);
		}

		void GamesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var item = AppHelper.BoundItem<x360ce.Engine.Data.UserGame>(grid, e.RowIndex);
			if (item is null)
				return;
			var column = grid.Columns[e.ColumnIndex];
			var isCurrent = GameDetailsControl.CurrentItem == item;
			if (column == MyIconColumn)
			{
				// The button's image is the size the tool strip shows it; a cell shows it at the screen's.
				e.Value = isCurrent ? ControlsHelper.ScaleImage(SaveGamesButton.Image) : Properties.Resources.empty_32x32;
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

		#region Game plugins

		/// <summary>The game plugins whose tabs follow x360ce's own, and the game file they were made for.</summary>
		PluginGameType[] _pluginTypes = new PluginGameType[0];
		string _pluginGamePath;

		/// <summary>The tab of each game plugin shown, and the instance made for it, both let go of with the next game.</summary>
		readonly List<TabPage> _pluginTabs = new List<TabPage>();
		readonly List<IPluginGame> _pluginGames = new List<IPluginGame>();

		PluginManager _pluginManager;

		/// <summary>Shows a tab, after x360ce's own settings, for each loaded game plugin that is for the game selected.</summary>
		/// <remarks>Made again only when the game's file or the plugins for it changed, so a list redrawn keeps the tabs as they are.</remarks>
		void ShowGamePlugins(UserGame game)
		{
			var manager = PluginManager.Current;
			var path = game == null ? null : game.FullPath;
			var types = manager == null || game == null
				? new PluginGameType[0]
				: manager.GameTypes.Where(x => !x.Faulted && x.IsFor(game.FileName)).ToArray();
			if (types.SequenceEqual(_pluginTypes) && string.Equals(path, _pluginGamePath, StringComparison.OrdinalIgnoreCase))
				return;
			var selected = GameTabControl.SelectedTab == null ? null : GameTabControl.SelectedTab.Name;
			RemoveGamePlugins();
			_pluginTypes = types;
			_pluginGamePath = path;
			foreach (var type in types)
			{
				var tab = NewPluginTab(type, path);
				_pluginTabs.Add(tab);
				GameTabControl.TabPages.Add(tab);
				// Once on the strip: the theme draws a page as the strip round it is drawn.
				JocysCom.ClassLibrary.Controls.Themes.FormsTheme.Apply(tab);
				UiTree.UiHelp.Attach(tab);
			}
			var again = GameTabControl.TabPages.Cast<TabPage>().FirstOrDefault(x => x.Name == selected);
			if (again != null)
				GameTabControl.SelectedTab = again;
			RootLayoutPanel.PerformLayout();
		}

		/// <summary>Takes away the game plugins' tabs, and disposes their pages and the instances made for them.</summary>
		void RemoveGamePlugins()
		{
			foreach (var tab in _pluginTabs)
			{
				GameTabControl.TabPages.Remove(tab);
				tab.Dispose();
			}
			_pluginTabs.Clear();
			foreach (var game in _pluginGames.OfType<IDisposable>())
				game.Dispose();
			_pluginGames.Clear();
			_pluginTypes = new PluginGameType[0];
			_pluginGamePath = null;
		}

		/// <summary>A game plugin's tab: its page, made with an instance for the game, then Help, its class's description.</summary>
		/// <remarks>
		/// As a plugin device's tab holds its pages. A page that cannot be made says why in its place. The tab is as tall as
		/// x360ce's own page of the game, so a longer page scrolls itself.
		/// </remarks>
		TabPage NewPluginTab(PluginGameType type, string path)
		{
			Control page = null;
			string fault = null;
			try
			{
				var game = type.NewInstance();
				_pluginGames.Add(game);
				game.GamePath = path;
				if (type.ControlType != null)
					page = (Control)Activator.CreateInstance(type.ControlType, game);
			}
			catch (Exception ex)
			{
				var inner = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
				fault = "The plugin’s page could not be made: " + inner.GetType().Name + ": " + inner.Message;
			}
			var pages = new TabControl { Name = "PluginTabControl", Dock = DockStyle.Fill };
			var content = page ?? new Label
			{
				Name = "PluginNoteLabel",
				AutoSize = true,
				Padding = AppHelper.ScaledPadding(8, 8, 8, 8),
				Text = fault ?? "The plugin has no page of its own.",
			};
			content.Dock = DockStyle.Fill;
			var text = page == null || string.IsNullOrWhiteSpace(page.Text) ? "Control" : page.Text;
			AddPluginPage(pages, "PluginPageTabPage", text, "The page the plugin adds for the game.", content);
			if (!string.IsNullOrWhiteSpace(type.Description))
			{
				// As a plugin device's Help page shows its help.
				var help = new RichTextBox
				{
					Name = "PluginHelpRichTextBox",
					Dock = DockStyle.Fill,
					BackColor = SystemColors.Window,
					BorderStyle = BorderStyle.None,
					ReadOnly = true,
					AccessibleName = "Help",
					AccessibleDescription = "The game plugin’s help, from its plugin: what it does for the game, and how to use it.",
				};
				AppHelper.ShowHelp(help, type.Description.Trim());
				AddPluginPage(pages, "PluginHelpTabPage", "Help", "The game plugin’s help, from its plugin.", help);
			}
			var tab = new TabPage
			{
				Name = type.Type.Name + "TabPage",
				Text = type.Name,
				AccessibleName = type.Name,
				AccessibleDescription = "What the " + type.Name + " plugin adds for the selected game, and its help.",
				UseVisualStyleBackColor = true,
				Padding = AppHelper.ScaledPadding(8, 8, 8, 8),
			};
			tab.Controls.Add(pages);
			return tab;
		}

		static void AddPluginPage(TabControl pages, string name, string text, string description, Control content)
		{
			var page = new TabPage { Name = name, Text = text, AccessibleName = text, AccessibleDescription = description, UseVisualStyleBackColor = true };
			page.Controls.Add(content);
			pages.TabPages.Add(page);
		}

		void AttachPluginManager(PluginManager manager)
		{
			if (ReferenceEquals(_pluginManager, manager))
				return;
			if (_pluginManager != null)
				_pluginManager.Changed -= PluginManager_Changed;
			_pluginManager = manager;
			if (_pluginManager != null)
				_pluginManager.Changed += PluginManager_Changed;
		}

		/// <summary>A plugin switched on or off: the selected game's tabs follow. Raised on any thread.</summary>
		void PluginManager_Changed(object sender, EventArgs e)
		{
			if (IsDisposed)
				return;
			if (InvokeRequired)
			{
				if (IsHandleCreated)
					BeginInvoke((Action)ShowSelectedGamePlugins);
				return;
			}
			ShowSelectedGamePlugins();
		}

		void ShowSelectedGamePlugins()
		{
			if (!IsDisposed)
				ShowGamePlugins(GameDetailsControl.CurrentItem as UserGame);
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
				AttachPluginManager(null);
				RemoveGamePlugins();
			}
			base.Dispose(disposing);
		}

		#endregion
	}

	/// <summary>Tabs as tall as their first page needs, so a table row sized to what it holds gives the rest of the height to the rows beside it.</summary>
	/// <remarks>
	/// A table row sized to what it holds asks each control for its preferred size, and a tab control otherwise answers with
	/// the size it was given. This one answers with its tabs and frame, and its first page: the page's padding and the
	/// preferred height of what it holds, at the page's width. On the Games tab the first page is x360ce's own settings of
	/// the game, so a game without a plugin looks as it always has; a plugin's longer page scrolls in the same room.
	///
	/// The room the tabs leave their pages is known only once the tabs' window exists, and asking for it makes the window,
	/// with every window round it, before the control that holds the tabs is ready. So until then it answers with its own
	/// size, and asks the table round it to lay it out again once the window is made.
	/// </remarks>
	public class GameTabControl : TabControl
	{
		/// <summary>The room the tabs leave their pages, or the whole control until the tabs' window exists.</summary>
		/// <remarks>
		/// The page round the tabs lays them out while it scales what it holds. Asked then, the base makes the tabs' window,
		/// which scales the pages for the screen a second time.
		/// </remarks>
		public override Rectangle DisplayRectangle
		{
			get { return IsHandleCreated ? base.DisplayRectangle : ClientRectangle; }
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			if (Parent != null)
				Parent.PerformLayout(this, nameof(Bounds));
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			if (!IsHandleCreated || TabPages.Count == 0)
				return base.GetPreferredSize(proposedSize);
			var display = DisplayRectangle;
			var page = TabPages[0];
			var width = display.Width - page.Padding.Horizontal;
			// A minimized window leaves the page no width, and what it holds, measured a word to a line, would stand
			// thousands of pixels tall: the tabs keep their size until there is room again.
			if (width <= 0)
				return Size;
			var content = page.Controls.Cast<Control>().Select(x => x.GetPreferredSize(new Size(width, 0)).Height).DefaultIfEmpty(0).Max();
			return new Size(Width, Height - display.Height + page.Padding.Vertical + content);
		}
	}
}
