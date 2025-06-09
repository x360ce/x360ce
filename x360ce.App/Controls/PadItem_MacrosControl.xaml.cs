using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using x360ce.Engine;
using x360ce.Engine.Data;
using x360ce.Engine.Maps;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for UserMacrosControl.xaml
	/// </summary>
	public partial class PadItem_MacrosControl : UserControl
	{

		private IPadControl PadControl;

		public PadItem_MacrosControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
			_Recorder = new Recorder();
		}

		private void Parent_OnSettingChanged(object sender, JocysCom.ClassLibrary.EventArgs<UserSetting> e)
		{
			var setting = e.Data;
			LoadUserSetting(setting);
		}

		#region ■ IPadTabPage 

		UserSetting _UserSetting;

		void LoadUserSetting(UserSetting userSetting)
		{
			_UserSetting = userSetting;
			if (_UserSetting == null)
			{
				Data = null;
				return;
			}
			MapTypeComboBox.ItemsSource = new List<MapType>() { MapType.Button, MapType.Axis, MapType.Slider, MapType.POV }
				.Select(x => x.ToString()).ToList();
			MapTypeComboBox.SelectedItem = MapType.Button.ToString();
			MapEventTypeComboBox.ItemsSource = Enum.GetValues(typeof(MapEventType)).Cast<MapEventType>()
				.Select(x => x.ToString()).ToList();
			MapEventTypeComboBox.SelectedItem = MapEventType.EnterUpLeaveDown.ToString();
			MapRpmTypeComboBox.ItemsSource = Enum.GetValues(typeof(MapRpmType)).Cast<MapRpmType>()
				.Select(x => x.ToString()).ToList();
			MapRpmTypeComboBox.SelectedItem = MapRpmType.DownIncrease.ToString();
			UserMacrosTabPage.Header = string.Format("User Macros for {0}", userSetting.InstanceName);

			RefreshList();

			// https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.keys?view=netcore-3.1
			AllKeys = ((Key[])Enum.GetValues(typeof(Key))).ToList();
			AddKeys(Control1KeysComboBox, "Mod", new Key[] {
				Key.LeftShift, Key.RightShift,
				Key.LeftCtrl, Key.RightCtrl,
				Key.LeftAlt, Key.RightAlt,
				Key.LWin, Key.RWin,
			});
			AddKeys(CharKeysComboBox, "Key", AllKeys.Where(x => x >= Key.A && x <= Key.Z || x == Key.Space));
			AddKeys(NumPadKeysComboBox, "NumPad", AllKeys.Where(x => x >= Key.NumPad0 && x <= Key.NumPad9));
			AddKeys(FKeysComboBox, "F-Key", AllKeys.Where(x => x >= Key.F1 && x <= Key.F24));
			AddKeys(Control2KeysComboBox, "ControlBindedName", new Key[] {
				Key.Escape, Key.Tab, Key.CapsLock, Key.Back, Key.Enter,
				Key.Insert, Key.Delete, Key.Home, Key.End, Key.PageUp, Key.PageDown, Key.NumLock });
			// Add remaining keys
			AddKeys(OtherKeysComboBox, "Other", AllKeys);
			//AddKeys(MouseKeysComboBox, "Mouse", new MouseButton[] {
			//	MouseButton.Left, MouseButton.Middle, MouseButton.Right,
			//	MouseButton.XButton1, MouseButton.XButton2
			//});
			// Add X360CE buttons.
			var xKeys = Enum.GetValues(typeof(MapCode)).Cast<MapCode>()
				.Where(x => SettingsConverter.IsButtonOrDirection(x));
			var sKeys = xKeys.Select(x => x.ToString()).Distinct().ToList();
			//sKeys.Insert(0, "XInput");
			XButtonsComboBox.ItemsSource = sKeys;
			//XButtonsComboBox.SelectedIndex = alwaysSelectedIndex;
			XButtonsComboBox.SelectionChanged += KeysComboBox_SelectionChanged;
		}

		#endregion

		public SortableBindingList<UserMacro> Data;

		List<Key> AllKeys;


		void AddKeys(ComboBox cb, string name, IEnumerable<Key> keys)
		{
			var ks = keys.ToList();
			AllKeys.RemoveAll(x => ks.Contains(x));
			var sKeys = ks.Select(x => x.ToString()).Distinct().ToList();
			//sKeys.Insert(0, name);
			cb.ItemsSource = sKeys;
			//cb.SelectedIndex = alwaysSelectedIndex;
			cb.SelectionChanged += KeysComboBox_SelectionChanged;
		}

		int alwaysSelectedIndex = 0;

		private void KeysComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var cb = (ComboBox)sender;
			if (cb.SelectedIndex <= alwaysSelectedIndex)
				return;
			var value = (string)cb.SelectedItem;
			//cb.SelectedIndex = alwaysSelectedIndex;
			MacroText.Text += "{" + value + "}";
		}

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			var isSelected = MainDataGrid.SelectedItem != null;
			DeleteButton.IsEnabled = isSelected;
			SaveButton.IsEnabled = isSelected;
			Load((UserMacro)MainDataGrid.SelectedItem);
		}

		public void RefreshList(bool restoreDefault = false)
		{
			if (restoreDefault)
				SettingsManager.Current.InitNewUserKeyboardMapForGame(_UserSetting);
			var userMacros = SettingsManager.UserMacros.Items.Where(x => x.SettingId == _UserSetting.SettingId).ToList();
			Data = new SortableBindingList<UserMacro>(userMacros);
			MainDataGrid.ItemsSource = Data;
		}

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			var text = "Do you want to delete selected macros?";
			var caption = "X360CE - Delete Macros";
			var result = MessageBox.Show(text, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result != MessageBoxResult.Yes)
				return;
			// Use begin invoke or grid update will deadlock on same thread.
			ControlsHelper.BeginInvoke(() =>
			{
				var items = MainDataGrid.SelectedItems.Cast<UserMacro>().ToList();
				foreach (var item in items)
					if (SettingsManager.UserMacros.Items.Contains(item))
						SettingsManager.UserMacros.Items.Remove(item);
				RefreshList();
			});
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			// Use begin invoke or grid update will deadlock on same thread.
			ControlsHelper.BeginInvoke(() => RefreshList());
		}

		private void ResetButton_Click(object sender, RoutedEventArgs e)
		{
			// Use begin invoke or grid update will deadlock on same thread.
			ControlsHelper.BeginInvoke(() => RefreshList(true));
		}

		#region ■ Recording

		Recorder _Recorder;

		private void RecordButton_Click(object sender, RoutedEventArgs e)
		{
			// Must monitor direct input changes on this device.
			var device = (_UserSetting == null)
				? null
				: SettingsManager.GetDevice(_UserSetting.InstanceGuid);
			_Recorder.StartRecording(null);
		}

		private void Global_UpdateControlFromStates(object sender, EventArgs e)
		{
			//_Recorder.
		}

		#endregion

		#region ■ ToolBar buttons

		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			// Use begin invoke or grid update will deadlock on same thread.
			ControlsHelper.BeginInvoke(() =>
			{
				Upsert();
				RefreshList();
			});
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			// Use begin invoke or grid update will deadlock on same thread.
			ControlsHelper.BeginInvoke(() =>
			{
				var item = (UserMacro)MainDataGrid.SelectedItem;
				Upsert(item);
			});
		}

		private void Load(UserMacro item = null)
		{
			var isNew = item == null;
			if (isNew)
				item = new UserMacro();
			NameTextBox.Text = item.Name;
			MacroText.Text = item.Text;
			MapTypeComboBox.SelectedItem = (MapType)item.MapType;
			MapIndexTextBox.Text = item.MapIndex.ToString();
			MapEventTypeComboBox.SelectedItem = (MapEventType)item.MapEventType;
			MapRpmTypeComboBox.SelectedItem = (MapRpmType)item.MapRpmType;
			MapRangeMin.Text = item.MapRangeMin.ToString();
			MapRangeMax.Text = item.MapRangeMax.ToString();
			MapRpmMin.Text = item.MapRpmMin.ToString();
			MapRpmMax.Text = item.MapRpmMax.ToString();
		}


		private void Upsert(UserMacro item = null)
		{
			var isNew = item == null;
			if (isNew)
				item = new UserMacro();
			item.Name = NameTextBox.Text;
			item.Text = MacroText.Text;
			item.MapType = (int)SettingsParser.TryParseValue(MapTypeComboBox.Text, MapType.Button);
			item.MapIndex = SettingsParser.TryParseValue(MapIndexTextBox.Text, 0);
			item.MapEventType = (int)SettingsParser.TryParseValue(MapEventTypeComboBox.Text, MapEventType.EnterUpLeaveDown);
			item.MapRpmType = (int)SettingsParser.TryParseValue(MapRpmTypeComboBox.Text, MapRpmType.DownIncrease);
			item.MapRangeMin = SettingsParser.TryParseValue(MapRangeMin.Text, 0);
			item.MapRangeMax = SettingsParser.TryParseValue(MapRangeMin.Text, 0);
			item.MapRpmMin = SettingsParser.TryParseValue(MapRpmMin.Text, 0);
			item.MapRpmMax = SettingsParser.TryParseValue(MapRpmMax.Text, 0);
			// Assign to current controller.
			item.SettingId = _UserSetting.SettingId;
			if (isNew)
				SettingsManager.UserMacros.Add(item);
		}

		#endregion

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
			// Subscribe to global events.
			Global.UpdateControlFromStates += Global_UpdateControlFromStates;
			// Subscribe to parent control events.
			PadControl = ControlsHelper.GetParent<PadControl>(this);
			if (PadControl != null)
			{
				PadControl.OnSettingChanged += Parent_OnSettingChanged;
				// Load parent setting.
				var setting = PadControl.CurrentUserSetting;
				LoadUserSetting(setting);
			}
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
			Global.UpdateControlFromStates -= Global_UpdateControlFromStates;
			if (PadControl != null)
			{
				PadControl.OnSettingChanged -= Parent_OnSettingChanged;
				PadControl = null;
			}
			MainDataGrid.ItemsSource = null;
			// Dispose managed resources.
			_Recorder?.Dispose();
		}

	}
}
