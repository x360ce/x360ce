using System;
using System.Linq;
using System.Windows.Controls;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for ProgramControl.xaml
	/// </summary>
	public partial class ProgramControl : UserControl
	{
		public ProgramControl()
		{
			InitializeComponent();
			ProgramListPanel.MainDataGrid.SelectionChanged += MainDataGrid_SelectionChanged;
		}

		private void MainDataGrid_SelectionChanged(object sender, EventArgs e)
		{
			var grid = ProgramListPanel.MainDataGrid;
			// List can't be empty, so return.
			// Issue: When DataSource is set then DataGridView fires the selectionChanged 3 times & it selects the first row. 
			if (grid.SelectedItems.Count == 0)
				return;
			var item = grid.SelectedItems.Cast<Engine.Data.Program>().FirstOrDefault();
			ProgramItemPanel.CurrentItem = item;
		}


	}
}
