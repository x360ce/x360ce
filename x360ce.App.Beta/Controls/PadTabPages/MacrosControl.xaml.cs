using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;
using x360ce.Engine.Maps;
using Key = System.Windows.Forms.Keys;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for UserMacrosControl.xaml
	/// </summary>
	public partial class UserMacrosControl : UserControl, IPadTabPage
	{
		public UserMacrosControl()
		{
			InitializeComponent();
		}

		#region IPadTabPage 

		UserSetting _UserSetting;

		public void LoadUserSetting(UserSetting userSetting)
		{
			_UserSetting = userSetting;
			if (_UserSetting == null)
			{
				Data = null;
				return;
			}
			SourceTypeComboBox.ItemsSource = new List<MapType>() { MapType.Button, MapType.Axis, MapType.Slider, MapType.POV }
				.Select(x => x.ToString()).ToList();
			SourceTypeComboBox.SelectedItem = MapType.Button.ToString();
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
				Key.Shift, Key.ShiftKey, Key.LShiftKey, Key.RShiftKey,
				Key.Control, Key.ControlKey, Key.LControlKey, Key.RControlKey,
				Key.Alt, Key.Menu, Key.RMenu, Key.LMenu,
				Key.LWin, Key.RWin,
			});
			AddKeys(CharKeysComboBox, "Key", AllKeys.Where(x => x >= Key.A && x <= Key.Z || x == Key.Space));
			AddKeys(NumPadKeysComboBox, "NumPad", AllKeys.Where(x => x >= Key.NumPad0 && x <= Key.NumPad9));
			AddKeys(FKeysComboBox, "F-Key", AllKeys.Where(x => x >= Key.F1 && x <= Key.F24));
			AddKeys(Control2KeysComboBox, "Control", new Key[] {
				Key.Escape, Key.Tab, Key.CapsLock, Key.Back, Key.Enter,
				Key.Insert, Key.Delete, Key.Home, Key.End, Key.PageUp, Key.PageDown, Key.NumLock });
			// Add remaining keys
			AddKeys(OtherKeysComboBox, "Other", AllKeys);
			AddKeys(MouseKeysComboBox, "Mouse", new Key[] { Key.LButton, Key.MButton, Key.RButton, Key.XButton1, Key.XButton2 });
			// Add X360CE buttons.
			var xKeys = Enum.GetValues(typeof(MapCode)).Cast<MapCode>()
				.Where(x => SettingsConverter.IsButtonOrDirection(x));
			var sKeys = xKeys.Select(x => x.ToString()).Distinct().ToList();
			sKeys.Insert(0, "XInput");
			XButtonsComboBox.ItemsSource = sKeys;
			XButtonsComboBox.SelectedIndex = alwaysSelectedIndex;
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
			sKeys.Insert(0, name);
			cb.ItemsSource = sKeys;
			cb.SelectedIndex = alwaysSelectedIndex;
			cb.SelectionChanged += KeysComboBox_SelectionChanged;
		}

		int alwaysSelectedIndex = 0;

		private void KeysComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var cb = (ComboBox)sender;
			if (cb.SelectedIndex <= alwaysSelectedIndex)
				return;
			var value = (string)cb.SelectedItem;
			cb.SelectedIndex = alwaysSelectedIndex;
			ScriptText.Text += "{" + value + "}";
		}

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			DeleteButton.IsEnabled = MainDataGrid.SelectedItems.Count > 0;
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

		private void RecordButton_Click(object sender, RoutedEventArgs e)
		{
			// Must monitor direct input changes on this device.
			var device = (_UserSetting == null)
				? null
				: SettingsManager.GetDevice(_UserSetting.InstanceGuid);
		}
	}
}
