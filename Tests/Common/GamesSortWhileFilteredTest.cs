// @under-test: App.v4/Controls/PadTabPages/GamesControl.cs, App.v4/Common/ControlHelper.cs
// @area: games   @layer: unit
using JocysCom.ClassLibrary.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// Sorting the games list while it shows only enabled or disabled games keeps the program up.
	/// </summary>
	/// <remarks>
	/// Clicking a column header sorts the list, the list is bound again with every row showing, and
	/// the filter hides some of them again before the sort puts the current cell back - on a row
	/// that is now hidden. Windows Forms refuses that with "Current cell cannot be set to an
	/// invisible cell", and the program closed (reported against 4.22.21.0).
	/// </remarks>
	[TestClass]
	public class GamesSortWhileFilteredTest
	{
		[TestMethod, TestCategory("games"), TestCategory("ui")]
		public void Sorting_a_filtered_games_list_does_not_fail()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var panel = new GamesGridUserControl())
				{
					form.Controls.Add(panel);
					form.Show();
					var grid = (DataGridView)panel.Controls.Find("GamesDataGridView", true).Single();
					var show = panel.Controls.OfType<ToolStrip>().SelectMany(x => x.Items.OfType<ToolStripDropDownButton>())
						.Single(x => x.Name == "ShowGamesDropDownButton");
					show.Text = "Show: Enabled";
					var games = new SortableBindingList<UserGame>();
					for (var i = 0; i < 8; i++)
						games.Add(new UserGame { FileName = "game" + i + ".exe", FullPath = @"C:\Games\game" + i + ".exe", IsEnabled = i % 2 == 0 });
					grid.DataSource = games;
					Application.DoEvents();
					Assert.IsTrue(grid.Rows.Cast<DataGridViewRow>().Any(x => !x.Visible), "The filter hid nothing, so this measured nothing.");
					// The current cell on the last game shown, so sorting moves the row it stands on.
					var current = grid.Rows.Cast<DataGridViewRow>().Last(x => x.Visible);
					grid.CurrentCell = current.Cells["FileNameColumn"];
					var column = grid.Columns["FileNameColumn"];
					grid.Sort(column, ListSortDirection.Descending);
					Application.DoEvents();
					grid.Sort(column, ListSortDirection.Ascending);
					Application.DoEvents();
					Assert.IsTrue(grid.Rows.Cast<DataGridViewRow>().Where(x => x.Visible)
						.All(x => ((UserGame)x.DataBoundItem).IsEnabled), "A disabled game shows after sorting.");
				}
			});
		}
	}
}
