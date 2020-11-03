using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using x360ce.Engine.Data;
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
			UserMacrosTabPage.Header = string.Format("User Macros for {0}", userSetting.InstanceName);
			var userMacros = SettingsManager.UserMacros.Items.Where(x => x.SettingId == _UserSetting.SettingId).ToList();
			SettingsManager.Current.InitNewUserKeyboardMapForGame(userSetting);
			Data = new SortableBindingList<UserMacro>(userMacros);
			MainDataGrid.ItemsSource = Data;
			// https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.keys?view=netcore-3.1
			AllKeys = ((Key[])Enum.GetValues(typeof(Key))).ToList();
			AddKeys(FKeysComboBox, AllKeys.Where(x => x >= Key.F1 && x <= Key.F24));
			AddKeys(Control1KeysComboBox, new Key[] {
				Key.Shift, Key.ShiftKey, Key.LShiftKey, Key.RShiftKey,
				Key.Control, Key.ControlKey, Key.LControlKey, Key.RControlKey,
				Key.Alt, Key.Menu, Key.RMenu, Key.LMenu,
				Key.LWin, Key.RWin,
			});
			AddKeys(MouseKeysComboBox, new Key[] { Key.LButton, Key.MButton, Key.RButton, Key.XButton1, Key.XButton2 });
			AddKeys(Control2KeysComboBox, new Key[] {
				Key.Escape, Key.Tab, Key.CapsLock, Key.Back, Key.Enter,
				Key.Insert, Key.Delete, Key.Home, Key.End, Key.PageUp, Key.PageDown, Key.NumLock });
			AddKeys(NumPadKeysComboBox, AllKeys.Where(x => x >= Key.NumPad0 && x <= Key.NumPad9));
			AddKeys(CharKeysComboBox, AllKeys.Where(x => x >= Key.A && x <= Key.Z || x == Key.Space ));
			// Add remaining keys
			AddKeys(OtherKeysComboBox, AllKeys);
		}

		#endregion

		public SortableBindingList<UserMacro> Data;

		List<Key> AllKeys;


		void AddKeys(ComboBox cb, IEnumerable<Key> keys)
		{
			var ks = keys.ToList();
			AllKeys.RemoveAll(x => ks.Contains(x));
			var sKeys = ks.Select(x => "{" + x + "}").ToList();
			cb.ItemsSource = sKeys;
			cb.SelectionChanged += KeysComboBox_SelectionChanged;
		}

		private void KeysComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var cb = (ComboBox)sender;
			if (cb.SelectedIndex < 0)
				return;
			var value = (string)cb.SelectedItem;
			cb.SelectedIndex = -1;
			ScriptText.Text += value;
		}

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;

		}
	}
}
