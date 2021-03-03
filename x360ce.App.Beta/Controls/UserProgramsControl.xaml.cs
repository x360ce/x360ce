using System;
using System.Linq;
using System.Windows.Controls;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for UserProgramsControl.xaml
	/// </summary>
	public partial class UserProgramsControl : UserControl
	{
		public UserProgramsControl()
		{
			InitializeComponent();
			ListPanel.MainDataGrid.SelectionChanged += MainDataGrid_SelectionChanged;
			ListPanel.MainDataGrid.ItemsSource = SettingsManager.UserGames.Items;
		}

		private void MainDataGrid_SelectionChanged(object sender, EventArgs e)
		{
			var grid = ListPanel.MainDataGrid;
			if (grid.SelectedItems.Count == 0)
				return;
			var item = grid.SelectedItems.Cast<Engine.Data.UserGame>().FirstOrDefault();
			ItemPanel.CurrentItem = item;
		}
	}
}
