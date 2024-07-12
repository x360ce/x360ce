using JocysCom.ClassLibrary.Controls;
using System;
using System.Linq;
using System.Windows.Controls;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for ProgramControl.xaml
	/// </summary>
	public partial class ProgramsControl : UserControl
	{
		public ProgramsControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			ListPanel.MainDataGrid.SelectionChanged += MainDataGrid_SelectionChanged;
			ControlsHelper.SetItemsSource(ListPanel.MainDataGrid, SettingsManager.Programs.Items);
		}

		private void MainDataGrid_SelectionChanged(object sender, EventArgs e)
		{
			var grid = ListPanel.MainDataGrid;
			if (grid.SelectedItems.Count == 0)
				return;
			var item = grid.SelectedItems.Cast<Engine.Data.Program>().FirstOrDefault();
			ItemPanel.CurrentItem = item;
		}

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			ListPanel.MainDataGrid.SelectionChanged -= MainDataGrid_SelectionChanged;
			ControlsHelper.SetItemsSource(ListPanel.MainDataGrid, null);
		}

	}
}
