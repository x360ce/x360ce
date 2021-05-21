using JocysCom.ClassLibrary.Controls;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
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
			_bwm = new BaseWithHeaderManager(HelpHeadLabel, HelpBodyLabel, LeftIcon, this);
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
			InitGameToCustomizeComboBox();
			var o = SettingsManager.Options;
			o.PropertyChanged += Options_PropertyChanged;
			LoadSettings();
		}

		public BaseWithHeaderManager _bwm;

		private Forms.DebugWindow DebugPanel;

		private void AddGameButton_Click(object sender, RoutedEventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				MainBodyPanel.MainTabControl.SelectedItem = MainBodyPanel.GamesTabPage;
				MainBodyPanel.GamesPanel.ListPanel.AddNewGame();
			});
		}

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

		private void InitGameToCustomizeComboBox()
		{
			GameToCustomizeComboBox.ItemsSource = SettingsManager.UserGames.Items;
			// Make sure that X360CE.exe is on top.
			GameToCustomizeComboBox.DisplayMemberPath = "DisplayName";
			GameToCustomizeComboBox.SelectionChanged += GameToCustomizeComboBox_SelectionChanged;
			// Set open game or 
			Global.FindAndSetOpenGame();
			// Assign selected game.
			GameToCustomizeComboBox.SelectedItem = SettingsManager.CurrentGame;
			// Enabled event handler.
			SettingsManager.CurrentGame_PropertyChanged += CurrentGame_PropertyChanged;

		}

		private void GameToCustomizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var game = (UserGame)GameToCustomizeComboBox.SelectedItem;
			SettingsManager.UpdateCurrentGame(game);
		}

		private void CurrentGame_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			// If pad controls not initializes yet then return.
			if (MainBodyPanel.PadControls == null)
				return;
			var game = SettingsManager.CurrentGame;
			if (game == null)
				return;
			// Update PAD Control.
			foreach (var ps in MainBodyPanel.PadControls)
			{
				if (ps != null)
				{
					ps.PadListPanel.UpdateFromCurrentGame();
					// Update emulation type.
					ps.ShowAdvancedTab(game != null && game.EmulationType == (int)EmulationType.Library);
				}
			}
			var selectedGame = (UserGame)GameToCustomizeComboBox.SelectedItem;
			if (selectedGame != game)
			{
				GameToCustomizeComboBox.SelectionChanged -= GameToCustomizeComboBox_SelectionChanged;
				GameToCustomizeComboBox.SelectedItem = game;
				GameToCustomizeComboBox.SelectionChanged += GameToCustomizeComboBox_SelectionChanged;
			}
			SettingsManager.Current.RaiseSettingsChanged(null);
		}


		#endregion

		private void StatusErrorsLabel_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			Global._MainWindow.StatusErrorLabel_Click(null, null);
		}
	}
}
