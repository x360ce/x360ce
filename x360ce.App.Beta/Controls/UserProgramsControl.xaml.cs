using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using x360ce.Engine.Data;

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

		private void MainDataGrid_SelectionChanged(object sender, EventArgs e)
		{
			var grid = ListPanel.MainDataGrid;
			if (grid.SelectedItems.Count == 0)
				return;
			var item = grid.SelectedItems.Cast<Engine.Data.UserGame>().FirstOrDefault();
			ItemPanel.CurrentItem = item;
		}

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
			var grid = ListPanel.MainDataGrid;
			// Return if loaded already. Exception thrown: 'System.NullReferenceException' in PresentationFramework.dll
			if (grid.ItemsSource != null)
				return;
			grid.SelectionChanged += MainDataGrid_SelectionChanged;
			ControlsHelper.SetItemsSource(grid, SettingsManager.UserGames.Items);
			ControlsHelper.SetSelection(grid, nameof(UserGame.FileName), gridSelection, 0);
		}

		List<string> gridSelection = new List<string>();

		private void UserControl_Unloaded(object sender, EventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			var grid = ListPanel.MainDataGrid;
			gridSelection = ControlsHelper.GetSelection<string>(grid, nameof(UserGame.FileName));
			grid.SelectionChanged -= MainDataGrid_SelectionChanged;
			ControlsHelper.SetItemsSource(grid, null);
			ItemPanel.CurrentItem = null;
		}

	}
}
