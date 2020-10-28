using JocysCom.ClassLibrary.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for UserKeyboardMapsControl.xaml
	/// </summary>
	public partial class UserKeyboardMapsControl : UserControl
	{
		public UserKeyboardMapsControl()
		{
			InitializeComponent();
			var game = SettingsManager.CurrentGame;
			var fileName = (game == null) ? null : game.FileName;
			SettingsManager.Current.InitNewUserKeyboardMapForGame(game);
			Data = new SortableBindingList<UserKeyboardMap>(SettingsManager.UserKeyboardMaps.Items.Where(x => x.FileName == fileName));
			MainDataGrid.ItemsSource = Data;
		}

		public SortableBindingList<UserKeyboardMap> Data;


	}
}
