using JocysCom.ClassLibrary.Controls;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for UserProgramsControl.xaml
	/// </summary>
	public partial class UserProgramsControl : UserControl
	{
		public UserProgramsControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			ListPanel.MainDataGrid.SelectionChanged += MainDataGrid_SelectionChanged;
			var userGamesView = new BindingListCollectionView(SettingsManager.UserGames.Items);
			ListPanel.MainDataGrid.ItemsSource = userGamesView;
		}

		private void MainDataGrid_SelectionChanged(object sender, EventArgs e)
		{
			var grid = ListPanel.MainDataGrid;
			if (grid.SelectedItems.Count == 0)
				return;
			var item = grid.SelectedItems.Cast<Engine.Data.UserGame>().FirstOrDefault();
			ItemPanel.CurrentItem = item;
		}

		private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
		{
			ListPanel.MainDataGrid.SelectionChanged -= MainDataGrid_SelectionChanged;
			((BindingListCollectionView)ListPanel.MainDataGrid.ItemsSource).DetachFromSourceCollection();
			//ListPanel.MainDataGrid.ItemsSource = null;
			ItemPanel.CurrentItem = null;
		}
	}
}
