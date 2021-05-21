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
			ListPanel.MainDataGrid.ItemsSource = SettingsManager.Programs.Items;
		}

		private void MainDataGrid_SelectionChanged(object sender, EventArgs e)
		{
			var grid = ListPanel.MainDataGrid;
			if (grid.SelectedItems.Count == 0)
				return;
			var item = grid.SelectedItems.Cast<Engine.Data.Program>().FirstOrDefault();
			ItemPanel.CurrentItem = item;
		}

	}
}
