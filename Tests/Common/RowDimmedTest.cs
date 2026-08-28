// @under-test: Engine/JocysCom/Controls/ControlsHelper.Windows.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace x360ce.Tests
{
	/// <summary>
	/// Dimming the row of a device that is switched off or unplugged.
	/// </summary>
	/// <remarks>
	/// Two things have gone wrong here and both are held below.
	///
	/// The row never dimmed, because the grid setup wrote full strength over every cell on every
	/// paint and only ever asked whether the item was switched on, which a device never says.
	///
	/// Then judging the row cost the device thread its rate. The judgement runs for every cell of
	/// every row on every paint, and the device thread hands its list changes to the interface thread
	/// and waits for them, so work added here is paid for in polling rate. One extra read of a type's
	/// property list took the measured rate from a thousand a second to a hundred and thirty.
	/// </remarks>
	[TestClass]
	public class RowDimmedTest
	{

		private class Row
		{
			public string Name { get; set; }
			public bool IsOnline { get; set; }
		}

		private class SwitchableRow
		{
			public bool IsEnabled { get; set; }
			public bool IsOnline { get; set; }
		}

		private class PlainRow
		{
			public string Name { get; set; }
		}

		[TestMethod, TestCategory("devices"), TestCategory("ui-interactive")]
		[Description("A grid set up the usual way dims an absent device and undims it on return")]
		public void A_grid_dims_an_absent_device_and_undims_it_on_return()
		{
			// Through a real grid, painted for real, because what failed was the setup rather than the
			// colouring: the shared handler wrote full strength over every cell on every paint.
			OnUiThread(() =>
			{
				var away = new Row { Name = "Away", IsOnline = false };
				var items = new System.ComponentModel.BindingList<Row>
				{
					new Row { Name = "Present", IsOnline = true },
					away,
				};
				using (var form = new Form { ClientSize = new Size(400, 200) })
				using (var grid = new DataGridView { Dock = DockStyle.Fill, DataSource = items })
				{
					ControlsHelper.ApplyBorderStyle(grid);
					form.Controls.Add(grid);
					form.Show();
					Application.DoEvents();

					var present = Fore(grid, 0);
					var dimmed = Fore(grid, 1);
					away.IsOnline = true;
					grid.Refresh();
					Application.DoEvents();
					var returned = Fore(grid, 1);
					form.Close();

					Assert.AreEqual(SystemColors.ControlDark.ToArgb(), dimmed,
						"The row of an absent device is drawn at full strength, so a person cannot tell " +
						"which of their controllers is actually plugged in.");
					Assert.AreNotEqual(dimmed, present,
						"A present device has to look different from an absent one.");
					Assert.AreEqual(present, returned,
						"The row stayed dim after its device came back, which is what a person sees as " +
						"a connected controller greyed out.");
				}
			});
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A row is unavailable when it is switched off or its hardware is away")]
		public void A_row_is_unavailable_when_it_is_switched_off_or_its_hardware_is_away()
		{
			Assert.IsTrue(ControlsHelper.IsItemAvailable(new Row { IsOnline = true }));
			Assert.IsFalse(ControlsHelper.IsItemAvailable(new Row { IsOnline = false }),
				"An unplugged controller is what the dimming is for.");
			Assert.IsFalse(ControlsHelper.IsItemAvailable(new SwitchableRow { IsEnabled = false, IsOnline = true }),
				"Switched off still dims, as it did before.");
			Assert.IsTrue(ControlsHelper.IsItemAvailable(new PlainRow { Name = "x" }),
				"Lists whose items say nothing about availability must not all turn grey.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Judging a row does not search its type every time")]
		public void Judging_a_row_does_not_search_its_type_every_time()
		{
			// This is asked for every cell of every row on every paint, and the device thread waits on
			// this thread, so the cost lands on the polling rate rather than anywhere visible. Reading a
			// type's property list here once per call measured as a drop from 1000 Hz to 130 Hz.
			//
			// The budget is deliberately loose. It is far above a cached lookup and far below a search,
			// so it reports a return to searching rather than ordinary variation between machines.
			const int Calls = 200000;
			const int BudgetMs = 200;
			// A real device row, because the cost is the size of the type. A made-up item with two
			// properties understates it by an order of magnitude and lets the defect through.
			var item = new x360ce.Engine.Data.UserDevice();
			ControlsHelper.IsItemAvailable(item);
			var watch = Stopwatch.StartNew();
			for (var i = 0; i < Calls; i++)
				ControlsHelper.IsItemAvailable(item);
			watch.Stop();
			Console.WriteLine("{0} judgements in {1} ms", Calls, watch.ElapsedMilliseconds);
			Assert.IsTrue(watch.ElapsedMilliseconds < BudgetMs,
				Calls + " judgements took " + watch.ElapsedMilliseconds + " ms, over the " + BudgetMs
				+ " ms budget. Something in here is searching the item's type on every call, and the "
				+ "device polling rate is paying for it.");
		}

		static int Fore(DataGridView grid, int rowIndex)
		{
			return grid.Rows[rowIndex].Cells[0].InheritedStyle.ForeColor.ToArgb();
		}

		/// <summary>Interface objects need a single-threaded apartment to be created in.</summary>
		static void OnUiThread(Action action)
		{
			Exception failure = null;
			var thread = new Thread(() =>
			{
				try { action(); }
				catch (Exception ex) { failure = ex; }
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(30)), "The interface thread did not finish.");
			if (failure != null)
				throw new AssertFailedException(failure.Message, failure);
		}

	}
}
