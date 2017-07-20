using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
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
				if (showEnabled) visible = (item.IsEnabled == true);
				if (showDisabled) visible = (item.IsEnabled == false);
				if (rows[i].Visible != visible)
				{
					needRebinding = true;
					break;
				}
			}
			// If there is no collmns to hide or show then...
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
					if (showEnabled) visible = (item.IsEnabled == true);
					if (showDisabled) visible = (item.IsEnabled == false);
					if (rows[i].Visible != visible)
					{
						rows[i].Visible = visible;
					}
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
				if (firstVisibleRow != null)
				{
					// Select first visible row.
					firstVisibleRow.Selected = true;
				}
			}
		}

		public static void ApplyCellStyle(DataGridView grid, DataGridViewCellFormattingEventArgs e, bool enabled)
		{
			e.CellStyle.ForeColor = enabled
				? grid.DefaultCellStyle.ForeColor
				: SystemColors.ControlDark;
			e.CellStyle.SelectionBackColor = enabled
			 ? grid.DefaultCellStyle.SelectionBackColor
			 : SystemColors.ControlDark;
			if (e.RowIndex > -1 && e.ColumnIndex > -1)
			{
				var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
				cell.Style.ForeColor = e.CellStyle.ForeColor;
				cell.Style.SelectionBackColor = e.CellStyle.SelectionBackColor;
			}
		}

		public static void ApplyBorderStyle(DataGridView grid)
		{
			grid.BackgroundColor = Color.White;
			grid.BorderStyle = BorderStyle.None;
			grid.EnableHeadersVisualStyles = false;
			grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
			grid.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
			grid.BackColor = SystemColors.Window;
			grid.DefaultCellStyle.BackColor = SystemColors.Window;
			grid.CellPainting += Grid_CellPainting;
			grid.SelectionChanged += Grid_SelectionChanged1;
		}

		private static void Grid_SelectionChanged1(object sender, EventArgs e)
		{
			// Sort issue with paint artifcats.
			var grid = (DataGridView)sender;
			grid.Invalidate();
		}

		private static void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			// Header and cell borders must be set to "Single" style.
			var grid = (DataGridView)sender;
			var firstVisibleColumn = grid.Columns.Cast<DataGridViewColumn>().Where(x => x.Displayed).Min(x => x.Index);
			var lastVisibleColumn = grid.Columns.Cast<DataGridViewColumn>().Where(x => x.Displayed).Max(x => x.Index);
			var selected = false;
			if (e.RowIndex > -1 && e.ColumnIndex > -1)
			{
				selected = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected;
			}
			e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Border);
			var bounds = e.CellBounds;
			var tl = new Point(bounds.X, bounds.Y);
			var tr = new Point(bounds.X + bounds.Width - 1, bounds.Y);
			var bl = new Point(bounds.X, bounds.Y + bounds.Height - 1);
			var br = new Point(bounds.X + bounds.Width - 1, bounds.Y + bounds.Height - 1);
			DataGridViewCellStyle style;
			// If column header then...
			if (e.RowIndex == -1)
				style = grid.ColumnHeadersDefaultCellStyle;
			// If row header then...
			else if (e.ColumnIndex == -1)
				style = grid.RowHeadersDefaultCellStyle;
			// If normal cell then...
			else
			{
				var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
				style = cell.HasStyle ? cell.InheritedStyle : grid.DefaultCellStyle;
			}
			//style = grid.DefaultCellStyle;
			// If header then
			var color = selected
			? style.SelectionBackColor
			: style.BackColor;
			// Cell background colour.
			var back = new Pen(color, 1);
			// Border colour.
			var border = new Pen(SystemColors.Control, 1);
			// Do not draw borders for selected device.
			Pen c;
			// Top
			e.Graphics.DrawLine(back, tl, tr);
			// Left (only if not first)
			c = !selected && e.ColumnIndex > firstVisibleColumn ? border : back;
			e.Graphics.DrawLine(c, bl, tl);
			// Right (only if not last column)
			c = !selected && e.ColumnIndex < lastVisibleColumn ? back : back;
			e.Graphics.DrawLine(c, tr, br);
			// Bottom (only if not last line or header if no rows)
			c = !selected && (grid.Rows.Count == 0 || e.RowIndex < grid.RowCount - 1) ? border : back;
			e.Graphics.DrawLine(c, bl, br);
			back.Dispose();
			border.Dispose();
			e.Handled = true;
		}

	}
}
