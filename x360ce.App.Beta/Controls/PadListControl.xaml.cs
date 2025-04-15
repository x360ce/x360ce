using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using System;
using System.ComponentModel;
//using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using x360ce.App.Converters;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for UserSettingMapListControl.xaml
	/// </summary>
	public partial class PadListControl : UserControl
	{
		public PadListControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			InitScrollFix();
			DevicesDataGrid.ItemsSource = mappedUserSettings;
			_MainDataGridFormattingConverter = (ItemFormattingConverter)DevicesDataGrid.Resources[nameof(_MainDataGridFormattingConverter)];
//			_MainDataGridFormattingConverter.ConvertFunction = _MainDataGridFormattingConverter_Convert;
		}

		ItemFormattingConverter _MainDataGridFormattingConverter;

		//object _MainDataGridFormattingConverter_Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		//{
		//	var sender = (FrameworkElement)values[0];
		//	var template = (FrameworkElement)values[1];
		//	var cell = (DataGridCell)(template ?? sender).Parent;
		//	var value = values[2];
		//	var item = (UserSetting)cell.DataContext;
		//	// Format ConnectionClassColumn value.
		//	if (cell.Column == ConnectionClassColumn)
		//	{
		//		var ud = SettingsManager.UserDevices.Items.FirstOrDefault(x => x.InstanceGuid == item.InstanceGuid);
		//		var imageSource = ConnectionClassToImageConverter.Convert(ud?.ConnectionClass ?? Guid.Empty);
		//		return imageSource;
		//	}
		//	else if (cell.Column == VendorNameColumn)
		//	{
		//		var ud = SettingsManager.UserDevices.Items.FirstOrDefault(x => x.InstanceGuid == item.InstanceGuid);
		//		return ud?.HidManufacturer;
		//	}
		//	return value;
		//}

		MapTo _MappedTo;
		SortableBindingList<Engine.Data.UserSetting> mappedUserSettings = new SortableBindingList<Engine.Data.UserSetting>();

		private void UserSettings_Items_ListChanged(object sender, ListChangedEventArgs e)
		{
			// Make sure there is no crash when function gets called from another thread.
			ControlsHelper.BeginInvoke(() =>
			{
				UpdateMappedUserSettings();
			});
		}

		object DevicesToMapDataGridViewLock = new object();

		void UpdateMappedUserSettings()
		{
			lock (DevicesToMapDataGridViewLock)
			{
				// If list not linked to any controller then return.
				if (_MappedTo == MapTo.None)
					return;
				var grid = DevicesDataGrid;
				var game = SettingsManager.CurrentGame;
				// Get rows which must be displayed on the list.
				var itemsToShow = SettingsManager.UserSettings.ItemsToArraySynchronized()
					// Filter devices by controller.	
					.Where(x => x.MapTo == (int)_MappedTo)
					// Filter devices by selected game (no items will be shown if game is not selected).
					.Where(x => game != null && x.FileName == game.FileName && x.FileProductName == game.FileProductName)
					.ToList();
				var itemsToRemove = mappedUserSettings.Except(itemsToShow).ToArray();
				var itemsToInsert = itemsToShow.Except(mappedUserSettings).ToArray();
				// If columns will be hidden or shown then...
				if (itemsToRemove.Length > 0 || itemsToInsert.Length > 0)
				{
					// Do removal.
					foreach (var item in itemsToRemove)
						mappedUserSettings.Remove(item);
					// Do adding.
					foreach (var item in itemsToInsert)
						mappedUserSettings.Add(item);
				}
				var itemToSelect = itemsToInsert.FirstOrDefault();
				// If item was inserted and not selected then...
				if (itemToSelect != null && !grid.SelectedItems.Contains(itemToSelect))
				{
					if (grid.SelectionMode == DataGridSelectionMode.Single)
					{
						grid.SelectedItem = itemToSelect;
					}
					else
					{
						// Clear current selection.
						grid.SelectedItems.Clear();
						// Select new item.
						grid.SelectedItems.Add(itemToSelect);
					}
				}
				var visibleCount = mappedUserSettings.Count();
				var title = string.Format("Enable {0} Mapped Device{1}", visibleCount, visibleCount == 1 ? "" : "s");
				if (mappedUserSettings.Count(x => x.IsEnabled) > 1)
					title += " (Combine)";
				EnabledCheckBox.Content = title;
			}
		}

		public void SetBinding(MapTo mappedTo)
		{
			_MappedTo = mappedTo;
			// Remove references which will allows form to be disposed.
			SettingsManager.UserSettings.Items.ListChanged -= UserSettings_Items_ListChanged;
			mappedUserSettings.Clear();
			if (mappedTo != MapTo.None)
			{
				SettingsManager.UserSettings.Items.ListChanged += UserSettings_Items_ListChanged;
				UserSettings_Items_ListChanged(null, null);
			}
		}

		public void UpdateFromCurrentGame()
		{
			// If list not linked to any controller then return.
			if (_MappedTo == MapTo.None)
				return;
			var game = SettingsManager.CurrentGame;
			var flag = AppHelper.GetMapFlag(_MappedTo);
			// Update Virtual.
			var virt = game != null && ((MapToMask)game.EnableMask).HasFlag(flag);
			EnabledCheckBox.IsChecked = virt;
			//EnabledContentControl.Content = virt
			//	? Icons_Default.Current[Icons_Default.Icon_checkbox]
			//	: Icons_Default.Current[Icons_Default.Icon_checkbox_unchecked];
			// Update AutoMap.
			var auto = game != null && ((MapToMask)game.AutoMapMask).HasFlag(flag);
			AutoMapCheckBox.IsChecked = auto;
			//AutoMapContentControl.Content = auto
			//	? Icons_Default.Current[Icons_Default.Icon_checkbox]
			//	: Icons_Default.Current[Icons_Default.Icon_checkbox_unchecked];
			DevicesDataGrid.IsEnabled = !auto;
			DevicesDataGrid.Background = auto
				? SystemColors.ControlBrush
				: SystemColors.WindowBrush;
			//MainDataGrid.DefaultCellStyle.BackColor = auto
			//	? SystemColors.Control
			//	: SystemColors.Window;
			if (auto)
			{
				foreach (var item in mappedUserSettings)
					item.MapTo = (int)MapTo.None;
			}
			UpdateMappedUserSettings();
			UpdateGridButtons();
		}


		void UpdateGridButtons()
		{
			var grid = DevicesDataGrid;
			var game = SettingsManager.CurrentGame;
			var flag = AppHelper.GetMapFlag(_MappedTo);
			var auto = game != null && ((MapToMask)game.AutoMapMask).HasFlag(flag);
			// Buttons must be disabled if AutoMapping enabled for the game.
			RemoveButton.IsEnabled = !auto && grid.SelectedItems.Count > 0;
			AddButton.IsEnabled = !auto;
		}

		private void EnabledCheckBox_Click(object sender, RoutedEventArgs e)
		{
			var box = (CheckBox)sender;
			var newValue = box.IsChecked ?? false;
			// ShowSystemDevicesButton.IsChecked = newValue;
			//EnabledContentControl.Content = newValue
			//	? Icons_Default.Current[Icons_Default.Icon_checkbox]
			//	: Icons_Default.Current[Icons_Default.Icon_checkbox_unchecked];
			// Process.
			var game = SettingsManager.CurrentGame;
			// If no game selected then ignore click.
			if (game == null)
				return;
			var flag = AppHelper.GetMapFlag(_MappedTo);
			var value = (MapToMask)game.EnableMask;
			var type = game.EmulationType;
			var autoMap = value.HasFlag(flag);
			// Invert flag value.
			var enableMask = autoMap
				// Remove AUTO.
				? (int)(value & ~flag)
				// Add AUTO.	
				: (int)(value | flag);
			// Update emulation type.
			EmulationType? newType = null;
			// If emulation enabled and game is not using virtual type then...
			if (enableMask > 0 && type != (int)EmulationType.Virtual)
				newType = EmulationType.Virtual;
			// If emulation disabled, but game use virtual emulation then...
			if (enableMask == 0 && type == (int)EmulationType.Virtual)
				newType = EmulationType.None;
			// Set values.
			game.EnableMask = enableMask;
			if (newType.HasValue)
				game.EmulationType = (int)newType.Value;
		}

		private void AutoMapCheckBox_Click(object sender, RoutedEventArgs e)
		{
			var box = (CheckBox)sender;
			var newValue = box.IsChecked ?? false;
			// ShowSystemDevicesButton.IsChecked = newValue;
			//AutoMapContentControl.Content = newValue
			//	? Icons_Default.Current[Icons_Default.Icon_checkbox]
			//	: Icons_Default.Current[Icons_Default.Icon_checkbox_unchecked];
			// Process.
			var game = SettingsManager.CurrentGame;
			// If no game selected then ignore click.
			if (game == null)
				return;
			var flag = AppHelper.GetMapFlag(_MappedTo);
			var value = (MapToMask)game.AutoMapMask;
			var autoMap = value.HasFlag(flag);
			// If AUTO enabled then...
			game.AutoMapMask = autoMap
				// Remove AUTO.
				? (int)(value & ~flag)
				// Add AUTO.
				: (int)(value | flag);
		}

		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			var game = SettingsManager.CurrentGame;
			// Return if game is not selected.
			if (game == null)
				return;
			// Show form which allows to select device.
			var selectedUserDevices = Global._MainWindow.ShowDeviceForm();
			// Return if no devices were selected.
			if (selectedUserDevices == null)
				return;
			// Check if device already have old settings before adding new ones.
			var noOldSettings = SettingsManager.GetSettings(game.FileName, _MappedTo).Count == 0;
			SettingsManager.MapGamePadDevices(game, _MappedTo, selectedUserDevices,
				SettingsManager.Options.HidGuardianConfigureAutomatically);
			var hasNewSettings = SettingsManager.GetSettings(game.FileName, _MappedTo).Count > 0;
			// If new devices mapped and button is not enabled then...
			if (noOldSettings && hasNewSettings && EnabledCheckBox.IsChecked != true)
			{
				// Enable mapping.
				EnabledCheckBox.IsChecked = true;
				EnabledCheckBox_Click(EnabledCheckBox, null);
			}
			SettingsManager.Current.RaiseSettingsChanged(null);
		}

		private void RemoveButton_Click(object sender, RoutedEventArgs e)
		{
			var win = new MessageBoxWindow();
			win.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFf0f0f0");
			var text = "Do you really want to remove selected user setting?";
			var result = win.ShowDialog(text,
				"X360CE - Remove?", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question, System.Windows.MessageBoxResult.No);
			if (result != System.Windows.MessageBoxResult.Yes)
				return;
			var game = SettingsManager.CurrentGame;
			// Return if game is not selected.
			if (game == null)
				return;
			var settingsOld = SettingsManager.GetSettings(game.FileName, _MappedTo);
			var userSetting = (UserSetting)DevicesDataGrid.SelectedItem;
			SettingsManager.UnMapGamePadDevices(game, userSetting,
				SettingsManager.Options.HidGuardianConfigureAutomatically);
			var settingsNew = SettingsManager.GetSettings(game.FileName, _MappedTo);
			// if all devices unmapped and mapping is enabled then...
			if (settingsOld.Count > 0 && settingsNew.Count == 0 && (EnabledCheckBox.IsChecked == true))
			{
				// Disable mapping.
				EnabledCheckBox.IsChecked = false;
				EnabledCheckBox_Click(EnabledCheckBox, null);
			}
		}

		private void UseXInputStateCheckBox_Click(object sender, RoutedEventArgs e)
		{
			var box = (CheckBox)sender;
			var newValue = box.IsChecked ?? false;
			// ShowSystemDevicesButton.IsChecked = newValue;
			//UseXInputStateContentControl.Content = newValue
			//	? Icons_Default.Current[Icons_Default.Icon_checkbox]
			//	: Icons_Default.Current[Icons_Default.Icon_checkbox_unchecked];

			ControlsHelper.BeginInvoke(() =>
			{
				SettingsManager.Options.GetXInputStates = !SettingsManager.Options.GetXInputStates;
			});
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateGridButtons();



			

			// Get the currently selected item
			var selected = DevicesDataGrid.SelectedItem as UserSetting;

			if (selected != null)
			{
				//Active.Content = selected.IsOnline.ToString();
				//IsEnabled.Content = selected.IsEnabled.ToString();
				ProductName.Content = selected.ProductName;
				InstanceId.Content = selected.InstanceId;
				Completion.Content = selected.Completion;
				PadSettingChecksum.Content = EngineHelper.GetID(selected.PadSettingChecksum); ;

				var ud = SettingsManager.UserDevices.Items.FirstOrDefault(x => x.InstanceGuid == selected.InstanceGuid);
				VendorName.Content = ud?.HidManufacturer.ToString();

				var imageSource = ConnectionClassToImageConverter.Convert(selected.InstanceGuid);
				ConnectionClassImage.Source = imageSource;
				ConnectionClassImage.ToolTip = imageSource.ToString();
			}
			else
			{
				// Clear the labels when nothing is selected
				//Active.Content = "";
				//IsEnabled.Content = "";
				ProductName.Content = "";
				InstanceId.Content = "";
				VendorName.Content = "";
				PadSettingChecksum.Content = "";
				ConnectionClassImage.Source = null;
				Completion.Content = "";
			}
		}

		/*
		private void MappedDevicesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var viewRow = grid.Rows[e.RowIndex];
			var column = grid.Columns[e.ColumnIndex];
			var item = (Engine.Data.UserSetting)viewRow.DataBoundItem;
			if (column == IsOnlineColumn)
			{
				e.Value = item.IsOnline
					? Properties.Resources.bullet_square_glass_green
					: Properties.Resources.bullet_square_glass_grey;
			}
			else if (column == ConnectionClassColumn)
			{
				var device = SettingsManager.GetDevice(item.InstanceGuid);
				e.Value = device.ConnectionClass == Guid.Empty
					? new Bitmap(16, 16)
					: JocysCom.ClassLibrary.IO.DeviceDetector.GetClassIcon(device.ConnectionClass, 16)?.ToBitmap();
			}
			else if (column == InstanceIdColumn)
			{
				// Hide device Instance GUID from public eyes. Show part of checksum.
				e.Value = EngineHelper.GetID(item.InstanceGuid);
			}
			else if (column == SettingIdColumn)
			{
				// Hide device Setting GUID from public eyes. Show part of checksum.
				e.Value = EngineHelper.GetID(item.PadSettingChecksum);
			}
			else if (column == VendorNameColumn)
			{
				var device = SettingsManager.GetDevice(item.InstanceGuid);
				e.Value = device == null
					? ""
					: device.DevManufacturer;
			}
		}

		private void MappedDevicesDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var column = grid.Columns[e.ColumnIndex];
			// If user clicked on the CheckBox column then...
			if (column == IsEnabledColumn)
			{
				var row = grid.Rows[e.RowIndex];
				var item = (Engine.Data.UserSetting)row.DataBoundItem;
				// Changed check (enabled state) of the current item.
				item.IsEnabled = !item.IsEnabled;
			}
		}

		*/

		#region Fix column width with scroll.

		System.Timers.Timer _Timer;

		void InitScrollFix()
		{
			var grid = DevicesDataGrid;
			_Timer = new System.Timers.Timer();
			_Timer.AutoReset = false;
			_Timer.Interval = 200;
			// Stop and start times every time new row is loaded.
			grid.LoadingRow += MainDataGrid_LoadingRow;
			// Resize rows when timer stops restarting.
			_Timer.Elapsed += _Timer_Elapsed;
		}

		private void MainDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			_Timer.Stop();
			_Timer.Start();
		}

		void UnInitScrollFix()
		{
			var grid = DevicesDataGrid;
			grid.LoadingRow -= MainDataGrid_LoadingRow;
			if (_Timer != null)
			{
				_Timer.Elapsed -= _Timer_Elapsed;
				_Timer.Dispose();
				_Timer = null;
			}
		}

		private void _Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			var grid = DevicesDataGrid;
			Dispatcher.BeginInvoke(new Action(() =>
			{
				var widths = grid.Columns.Select(x => x.Width).ToArray();
				for (int i = 0; i < grid.Columns.Count; i++)
					grid.Columns[i].Width = 0;
				grid.UpdateLayout();
				for (int i = 0; i < grid.Columns.Count; i++)
					grid.Columns[i].Width = widths[i];
			}), new object[0]);
		}

		#endregion

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
			var o = SettingsManager.Options;
			SettingsManager.LoadAndMonitor(o, nameof(o.GetXInputStates), EnabledCheckBox, null, null, System.Windows.Data.BindingMode.OneWay);
			UpdateGridButtons();
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().

		}

		public void ParentWindow_Unloaded()
		{
			UnInitScrollFix();
			SetBinding(MapTo.None);
			SettingsManager.UnLoadMonitor(EnabledCheckBox);
			_MainDataGridFormattingConverter = null;
			//UseXInputStateContentControl.Content = null;
			mappedUserSettings.Clear();
		}
	}
}
