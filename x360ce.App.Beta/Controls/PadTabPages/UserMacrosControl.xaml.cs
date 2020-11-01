using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using System.Linq;
using System.Windows.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

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
		}

		#endregion

		public SortableBindingList<UserMacro> Data;

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;

		}
	}
}
