// @under-test: App.v4/Common/AppHelper.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Windows.Forms;

namespace x360ce.Tests
{
	/// <summary>
	/// A grid keeps its rows for a moment after the list behind them has shrunk, and goes on
	/// drawing and measuring them: the row under the pointer is measured on every mouse move.
	/// Asking such a row what it is showing closed the program, reported from 4.18.63.0 as
	/// "Index 0 does not have a value" while the pointer sat over the settings list.
	/// </summary>
	[TestClass]
	public class BoundItemTest
	{
		public sealed class Row
		{
			public string Name { get; set; }
		}

		static DataGridView GridOver(IList<Row> rows)
		{
			var grid = new DataGridView();
			grid.BindingContext = new BindingContext();
			grid.DataSource = rows;
			// Reading the rows makes the grid build them from the list as it is now.
			Assert.AreEqual(rows.Count, grid.Rows.Count,
				"The grid did not take its rows from the list, so this test proves nothing.");
			return grid;
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("A row whose item has gone reports nothing instead of failing")]
		public void A_row_the_list_no_longer_has_reports_nothing()
		{
			var rows = new List<Row>
			{
				new Row { Name = "A" },
				new Row { Name = "B" },
				new Row { Name = "C" },
			};
			using (var grid = GridOver(rows))
			{
				// A plain list says nothing when it changes, which is exactly the state a grid
				// is in for the moment between a change and the notice of it.
				rows.RemoveAt(2);
				Assert.IsNull(x360ce.App.AppHelper.BoundItem<Row>(grid, 2),
					"The grid still has a third row and the list does not. Asking that row what "
					+ "it shows is what closed the program.");
				Assert.IsNotNull(x360ce.App.AppHelper.BoundItem<Row>(grid, 0),
					"A row the list still has must report its item.");
			}
		}

		[TestMethod, TestCategory("ui")]
		[Description("Positions outside the list report nothing")]
		public void Positions_outside_the_list_report_nothing()
		{
			var rows = new List<Row> { new Row { Name = "A" } };
			using (var grid = GridOver(rows))
			{
				Assert.IsNull(x360ce.App.AppHelper.BoundItem<Row>(grid, -1));
				Assert.IsNull(x360ce.App.AppHelper.BoundItem<Row>(grid, 7));
			}
		}

		[TestMethod, TestCategory("ui")]
		[Description("A grid showing nothing reports nothing")]
		public void A_grid_with_no_list_reports_nothing()
		{
			using (var grid = new DataGridView())
				Assert.IsNull(x360ce.App.AppHelper.BoundItem<Row>(grid, 0));
		}
	}
}
