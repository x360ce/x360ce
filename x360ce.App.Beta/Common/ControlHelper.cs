using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App
{
	public class ControlHelper
	{

		public static void ShowHideAndSelectGridRows(DataGridView grid, ToolStripDropDownButton button, string primaryKey = null, object selectItemKey = null)
		{
			var rows = grid.Rows.Cast<DataGridViewRow>().ToArray();
			var showEnabled = button.Text.Contains("Enabled");
			var showDisabled = button.Text.Contains("Disabled");
			// Check if row needs rebinding.
			var needRebinding = false;
			for (int i = 0; i < rows.Length; i++)
			{
				var item = (IDisplayName)rows[i].DataBoundItem;
				var visible = true;
				if (showEnabled)
					visible = item.IsEnabled;
				if (showDisabled)
					visible = !item.IsEnabled;
				if (rows[i].Visible != visible)
				{
					needRebinding = true;
					break;
				}
			}
			// If there is no columns to hide or show then...
			if (needRebinding)
			{
				var selection = selectItemKey == null
				? JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<object>(grid, primaryKey)
				: new List<object>() { selectItemKey };
				grid.CurrentCell = null;
				// Suspend Layout and CurrencyManager to avoid exceptions.
				grid.SuspendLayout();
				var cm = (CurrencyManager)grid.BindingContext[grid.DataSource];
				cm.SuspendBinding();
				// Reverse order to hide/show bottom records first..
				Array.Reverse(rows);
				for (int i = 0; i < rows.Length; i++)
				{
					var item = (IDisplayName)rows[i].DataBoundItem;
					var visible = true;
					if (showEnabled)
						visible = item.IsEnabled;
					if (showDisabled)
						visible = !item.IsEnabled;
					if (rows[i].Visible != visible)
						rows[i].Visible = visible;
				}
				// Resume CurrencyManager and Layout.
				cm.ResumeBinding();
				grid.ResumeLayout();
				// Restore selection.
				JocysCom.ClassLibrary.Controls.ControlsHelper.RestoreSelection(grid, primaryKey, selection, true);
			}
			// If nothing is selected then...
			else if (grid.SelectedRows.Count == 0)
			{
				var firstVisibleRow = rows.FirstOrDefault(x => x.Visible);
				// Select first visible row.
				if (firstVisibleRow != null)
					firstVisibleRow.Selected = true;
			}
		}

	}
}
