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
			var window = System.Windows.Window.GetWindow(this);
		}

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			var grid = ListPanel.MainDataGrid;
			// Return if loaded already. Exception thrown: 'System.NullReferenceException' in PresentationFramework.dll
			if (grid.ItemsSource != null)
				return;
			grid.SelectionChanged += MainDataGrid_SelectionChanged;
			var view = new BindingListCollectionView(SettingsManager.UserGames.Items);
			grid.ItemsSource = view;
		}

		private void UserControl_Unloaded(object sender, EventArgs e)
		{
			var grid = ListPanel.MainDataGrid;
			// Return if not loaded.
			if (grid.ItemsSource == null)
				return;
			grid.SelectionChanged -= MainDataGrid_SelectionChanged;
			var source = ((BindingListCollectionView)grid.ItemsSource);
			grid.ItemsSource = null;
			source?.DetachFromSourceCollection();
			ItemPanel.CurrentItem = null;
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
