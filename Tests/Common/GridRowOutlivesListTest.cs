// @under-test: App.v4/Controls/UserDevicesUserControl.cs, App.v4/Controls/SummariesGridUserControl.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A grid row painted after the list behind it has shrunk shows nothing rather than failing.
	/// </summary>
	/// <remarks>
	/// A grid keeps its rows for a moment after the list behind them loses an item, and paints them
	/// in that moment. Asking such a row what it shows fails inside the binding, and on the Devices
	/// page that failure closed the program (reported against 4.22.21.0, "index 5 has no value").
	/// </remarks>
	[TestClass]
	public class GridRowOutlivesListTest
	{
		/// <summary>Binds a plain list, so the grid is not told when the list shrinks, then formats every row.</summary>
		static void FormatEveryRowAfterTheListShrinks<T>(DataGridView grid, List<T> items)
		{
			grid.AutoGenerateColumns = false;
			grid.DataSource = items;
			Application.DoEvents();
			Assert.AreEqual(items.Count, grid.Rows.Count, "The grid did not take the list.");
			items.RemoveAt(items.Count - 1);
			// The last row is still there, with nothing behind it now: what painting it asks for.
			for (var i = 0; i < grid.Rows.Count; i++)
			{
				for (var c = 0; c < grid.Columns.Count; c++)
				{
					// What painting asks the cell for.
					var shown = grid.Rows[i].Cells[c].FormattedValue;
				}
			}
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		public void A_devices_row_whose_device_has_gone_paints_without_failing()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var panel = new UserDevicesUserControl())
				{
					form.Controls.Add(panel);
					form.Show();
					var devices = new List<UserDevice>
					{
						new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "One" },
						new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Two" },
					};
					FormatEveryRowAfterTheListShrinks(panel.DevicesDataGridView, devices);
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		public void A_summaries_row_whose_summary_has_gone_paints_without_failing()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var panel = new SummariesGridUserControl())
				{
					form.Controls.Add(panel);
					form.Show();
					var summaries = new List<Summary>
					{
						new Summary { PadSettingChecksum = Guid.NewGuid(), ProductName = "One" },
						new Summary { PadSettingChecksum = Guid.NewGuid(), ProductName = "Two" },
					};
					FormatEveryRowAfterTheListShrinks(panel.SummariesDataGridView, summaries);
				}
			});
		}
	}
}
