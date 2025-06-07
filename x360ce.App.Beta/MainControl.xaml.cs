using JocysCom.ClassLibrary.Controls;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using x360ce.App.Properties;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App
{
	/// <summary>
	/// Interaction logic for MainControl.xaml
	/// </summary>
	public partial class MainControl : UserControl
	{
		public MainControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
			// Hide status values.
			StatusTimerLabel.Content = "";
			StatusDllLabel.Content = "";
			// Set status labels.
			StatusIsAdminLabel.Content = JocysCom.ClassLibrary.Win32.WinAPI.IsVista
				? string.Format("Elevated: {0}", JocysCom.ClassLibrary.Win32.WinAPI.IsElevated())
				: "";
			// Initialize Debug panel.
			DebugPanel = new Forms.DebugWindow();
			ControlsHelper.BeginInvoke(() =>
			{
				if (SettingsManager.Options.ShowDebugPanel)
					DebugPanel.ShowPanel();
			});
			InitGameToCustomizeDataGrid();

			SettingsManager.Options.PropertyChanged += Options_PropertyChanged;
			LoadSettings();
		}

		private Forms.DebugWindow DebugPanel;

		private void AddGameButton_Click(object sender, RoutedEventArgs e)
			=> Global.OnAddGame(this);

		private void SaveAllButton_Click(object sender, RoutedEventArgs e)
		{
			Save();
		}

		private void TestButton_Click(object sender, RoutedEventArgs e)
		{
			DebugPanel.ShowPanel();
		}

		#region ■ Save and Synchronize Settings

		/// <summary>
		/// This method will be called during manual saving and automatically when form is closing.
		/// </summary>
		public void SaveAll()
		{
			Settings.Default.Save();
			SettingsManager.OptionsData.Save();
			SettingsManager.UserSettings.Save();
			SettingsManager.Summaries.Save();
			SettingsManager.Programs.Save();
			SettingsManager.UserGames.Save();
			SettingsManager.Presets.Save();
			SettingsManager.Layouts.Save();
			SettingsManager.UserDevices.Save();
			SettingsManager.UserMacros.Save();
			SettingsManager.PadSettings.Save();
			SettingsManager.UserInstances.Save();
			XInputMaskScanner.FileInfoCache.Save();
		}

		public void Save()
		{
			// Disable buttons to make sure that user is not pressing it twice.
			SaveAllButton.IsEnabled = false;
			// Save application settings.
			SaveAll();
			// Use timer to enable Save buttons after 520 ms.
			var timer = new System.Timers.Timer
			{
				AutoReset = false,
				Interval = 520,
			};
			timer.Elapsed += Timer_Elapsed;
			timer.Start();
		}

		private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				SaveAllButton.IsEnabled = true;
				// Dispose original timer.
				var timer = (System.Timers.Timer)sender;
				timer.Elapsed -= Timer_Elapsed;
				timer.Dispose();
			});
		}

		#endregion

		public void LoadSettings()
		{
			// Load XML settings into control.
			var o = SettingsManager.Options;
			ControlsHelper.SetVisible(TestButton, o.ShowTestButton);
		}

		private void Options_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (ControlsHelper.InvokeRequired)
			{
				ControlsHelper.Invoke(() => Options_PropertyChanged(sender, e));
				return;
			}
			var o = SettingsManager.Options;
			// Update controls by specific property.
			switch (e.PropertyName)
			{
				case nameof(Options.ShowTestButton):
					ControlsHelper.SetVisible(TestButton, o.ShowTestButton);
					break;
			}
		}
		#region ■ Current Game

		public BindingListCollectionView GameToCustomizeDataGridItemsSource { get; set; }

		private void InitGameToCustomizeDataGrid()
		{
			GameToCustomizeDataGridItemsSource = new BindingListCollectionView(SettingsManager.UserGames.Items);
			GameToCustomizeDataGrid.ItemsSource = GameToCustomizeDataGridItemsSource;
			GameToCustomizeDataGrid.SelectionChanged += GameToCustomizeDataGrid_SelectionChanged;
			// Set open game or 
			Global.FindAndSetOpenGame();
			// Assign selected game.
			GameToCustomizeDataGrid.SelectedItem = SettingsManager.CurrentGame;
			UpdateGameDetails(SettingsManager.CurrentGame);
			SettingsManager.CurrentGame_PropertyChanged += CurrentGame_PropertyChanged;
		}

		private void GameToCustomizeDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var game = (UserGame)GameToCustomizeDataGrid.SelectedItem;
			SettingsManager.UpdateCurrentGame(game);
			UpdateGameDetails(game);
		}

		private void CurrentGame_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (MainBodyPanel.PadControls == null)
				return;
			var game = SettingsManager.CurrentGame;
			UpdateGameDetails(game);
			if (game == null)
				return;
			foreach (var ps in MainBodyPanel.PadControls)
			{
				if (ps != null)
				{
					ps.PadListPanel.UpdateFromCurrentGame();
					var showAdvanced = game != null && game.EmulationType == (int)EmulationType.Library;
					ps.PadItemPanel.ShowTab(showAdvanced, ps.PadItemPanel.AdvancedTabPage);
				}
			}
			var selectedGame = (UserGame)GameToCustomizeDataGrid.SelectedItem;
			if (selectedGame != game)
			{
				GameToCustomizeDataGrid.SelectionChanged -= GameToCustomizeDataGrid_SelectionChanged;
				GameToCustomizeDataGrid.SelectedItem = game;
				GameToCustomizeDataGrid.SelectionChanged += GameToCustomizeDataGrid_SelectionChanged;
			}
			SettingsManager.Current.RaiseSettingsChanged(null);
		}

		private void UpdateGameDetails(UserGame game)
		{
			if (FileProductName == null || FileName == null || GameId == null)
				return;
			if (game == null)
			{
				FileProductName.Content = "";
				FileName.Content = "";
				GameId.Content = "";
			}
			else
			{
				FileProductName.Content = game.FileProductName;
				FileName.Content = game.FileName;
				GameId.Content = game.GameId.ToString();
			}
		}


		#endregion

		private void StatusErrorsLabel_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			Global._MainWindow.StatusErrorLabel_Click(null, null);
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
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			// Cleanup references which prevents disposal.
			SettingsManager.CurrentGame_PropertyChanged -= CurrentGame_PropertyChanged;
			SettingsManager.Options.PropertyChanged -= Options_PropertyChanged;
			GameToCustomizeDataGrid.SelectionChanged -= GameToCustomizeDataGrid_SelectionChanged;
			GameToCustomizeDataGrid.SelectedItem = null;
			((BindingListCollectionView)GameToCustomizeDataGrid.ItemsSource)?.DetachFromSourceCollection();
		}
	}
}
