// @under-test: Engine/JocysCom/Controls/IssuesControl/IssuesUserControl.cs
// @area: diagnostics   @layer: unit
using JocysCom.ClassLibrary.Controls.IssuesControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace x360ce.Tests
{
	/// <summary>
	/// The Issues page's list, drawn while the issues behind it change.
	/// </summary>
	/// <remarks>
	/// An issue fixed or ignored leaves the list while the grid is still drawing the rows it last
	/// knew. Asking the binding for the item behind a leftover row closed the program from the Issues
	/// page (a 4.27.53.0 report, IndexOutOfRangeException in the grid's cell formatting), as the same
	/// question had closed it from the Games list in 4.20.43.0.
	/// </remarks>
	[TestClass]
	public class IssuesListShrinkTest
	{

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("The Issues list survives an issue leaving while its rows are drawn")]
		public void The_issues_list_survives_an_issue_leaving_while_it_draws()
		{
			Ui.OnUiThreadWatched(() =>
			{
				using (var form = new Form { ClientSize = new Size(800, 400) })
				using (var control = new IssuesUserControl { Dock = DockStyle.Fill })
				{
					form.Controls.Add(control);
					var grid = (DataGridView)control.Controls.Find("WarningsDataGridView", true).Single();
					form.Show();
					foreach (var i in Enumerable.Range(1, 3))
						control.Warnings.Add(new IssueItem { Name = "Issue " + i, FixName = "Fix " + i, Severity = IssueSeverity.Moderate });
					// The list tells the grid through the interface thread's queue.
					for (var i = 0; i < 20 && grid.Rows.Count < 3; i++)
					{
						Application.DoEvents();
						Thread.Sleep(50);
					}
					Assert.AreEqual(3, grid.Rows.Count, "The grid did not build its rows, so this proves nothing.");
					// The moment between a change and the grid hearing of it: the list is shorter, and
					// the grid still has the row.
					control.Warnings.RaiseListChangedEvents = false;
					control.Warnings.RemoveAt(2);
					// What the grid did in the report: it sized the leftover row before painting it,
					// which formats each of its cells.
					grid.AutoResizeRow(2, DataGridViewAutoSizeRowMode.AllCells);
					foreach (DataGridViewCell cell in grid.Rows[2].Cells)
						GC.KeepAlive(cell.FormattedValue);
					using (var bitmap = new Bitmap(grid.Width, grid.Height))
						grid.DrawToBitmap(bitmap, new Rectangle(0, 0, grid.Width, grid.Height));
					form.Close();
				}
			}, "Drawing the Issues list");
		}

	}
}
