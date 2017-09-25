using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
            JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(GamesDataGridView);
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
                ScanStarted = DateTime.Now;
                var success = System.Threading.ThreadPool.QueueUserWorkItem(ScanGames);
                if (!success) ScanProgressLabel.Text = "Scan failed!";
            }
        }



        XInputMaskScanner GameScanner;
        DateTime ScanStarted;
        object GameAddLock = new object();

        private void Scanner_Progress(object sender, XInputMaskScannerEventArgs e)
        {
            if (MainForm.Current.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate ()
                {
                    Scanner_Progress(sender, e);
                });
                return;
            }
            var scanner = (XInputMaskScanner)sender;
            switch (e.State)
            {
                case XInputMaskScannerState.Started:
                    ScanProgressLabel.Text = "Scanning...";
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
                            game.IsEnabled = game.ProcessorArchitecture == enabledGame.ProcessorArchitecture; ;
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
                        sb.AppendFormat("Current File: {0}", e.Files[e.FileIndex].FullName);
                    }
                    sb.AppendLine();
                    sb.AppendFormat("Skipped = {0}, Added = {1}, Updated = {2}", e.Skipped, e.Added, e.Updated);
                    sb.AppendLine();
                    ScanProgressLabel.Text = sb.ToString();
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
            Invoke((MethodInvoker)delegate ()
            {
                ScanProgressPanel.Visible = true;
                ScanGamesButton.Enabled = false;
            });
            GameScanner = new XInputMaskScanner();
            GameScanner.Progress += Scanner_Progress;
            string[] paths;
            string name = null;
            if (string.IsNullOrEmpty(exe))
            {
                paths = MainForm.Current.OptionsPanel.GameScanLocationsListBox.Items.Cast<string>().ToArray();
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
                    ScanStarted = DateTime.Now;
                    var success = System.Threading.ThreadPool.QueueUserWorkItem(ScanGames, AddGameOpenFileDialog.FileName);
                    if (!success) ScanProgressLabel.Text = "Scan failed!";
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
                MainForm.Current.CloudPanel.Add(CloudAction.Delete, itemsToDelete, true);
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
            //var cell = row.Cells[e.ColumnIndex];
            //grid.InvalidateCell(cell);
            if (e.ColumnIndex == grid.Columns[MyIconColumn.Name].Index)
            {
                e.Value = isCurrent ? SaveGamesButton.Image : Properties.Resources.empty_16x16;
            }
            else if (e.ColumnIndex == grid.Columns[FileFolderColumn.Name].Index)
            {
                e.Value = Path.GetDirectoryName(item.FullPath);
            }
            else if (e.ColumnIndex == grid.Columns[PlatformColumn.Name].Index)
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
            var key = "FileName";
            var list = SettingsManager.UserGames.Items;
            var selection = JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<string>(grid, key);
            var newItems = items.ToArray();
            grid.DataSource = null;
            foreach (var newItem in newItems)
            {
                // Try to find existing item inside the list.
                var existingItems = list.Where(x => x.FileName.ToLower() == newItem.FileName.ToLower()).ToArray();
                // Remove existing items.
                for (int i = 0; i < existingItems.Length; i++)
                {
                    list.Remove(existingItems[i]);
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
                list.Add(newItem);
            }
            MainForm.Current.SetHeaderBody("{0} {1}(s) loaded.", items.Count(), typeof(Engine.Data.UserGame).Name);
            grid.DataSource = list;
            JocysCom.ClassLibrary.Controls.ControlsHelper.RestoreSelection(grid, key, selection);
            SettingsManager.Save(true);
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
