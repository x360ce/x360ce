// @under-test: App.v4/Controls/PadControl.cs, App.v4/MainForm.cs, App.v4/Controls/XInputDevicesUserControl.cs
// @area: startup   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.DInput;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>What is already in place is not put there again.</summary>
	/// <remarks>
	/// Each of these sets off a long chain of events: a selection loads every box on a controller page, a rebuilt list raises
	/// its selection, and a table laid out again lays out all that is on it.
	/// </remarks>
	[TestClass]
	public class AppliedOnceTest
	{
		readonly DeviceOnTwoTabsTest _lists = new DeviceOnTwoTabsTest();

		[TestInitialize]
		public void Before()
		{
			_lists.Before();
		}

		[TestCleanup]
		public void After()
		{
			_lists.After();
		}

		/// <summary>A row for a list to show.</summary>
		class Row
		{
			public string Name { get; set; }
		}

		/// <summary>A page of tabs on the window that is not open, as every page but one is while the program loads.</summary>
		static TabPage HiddenPage(Form form)
		{
			var tabs = new TabControl { Dock = DockStyle.Fill };
			var page = new TabPage("Hidden");
			tabs.TabPages.Add(new TabPage("Open"));
			tabs.TabPages.Add(page);
			form.Controls.Add(tabs);
			return page;
		}

		[TestMethod, TestCategory("startup"), TestCategory("critical")]
		[Description("A controller page loads its device's settings once, then keeps them while its windows are made and it is shown unseen")]
		public void A_page_loads_its_selection_once_however_its_windows_are_made()
		{
			Ui.OnUiThread(() =>
			{
				var oldStatus = SettingsManager.Current.NotifySettingsStatus;
				SettingsManager.Current.NotifySettingsStatus = count => { };
				var game = DeviceOnTwoTabsTest.NewGame();
				var wheel = DeviceOnTwoTabsTest.NewDevice("Wheel");
				try
				{
					DeviceOnTwoTabsTest.Map(game, wheel, MapTo.Controller2);
					SettingsManager.CurrentGame = game;
					using (var form = new Form { ShowInTaskbar = false, Width = 900, Height = 700 })
					using (var pad = new PadControl(MapTo.Controller2) { Dock = DockStyle.Fill })
					{
						var page = HiddenPage(form);
						page.Controls.Add(pad);
						pad.InitPadControl();
						pad.UpdateSettingsMap();
						var loads = 0;
						pad.OnSettingChanged += (s, e) => loads++;
						pad.InitPadData();
						form.Show();
						Application.DoEvents();
						Assert.AreEqual(1, loads, "The page did not load its device's settings exactly once.");

						foreach (var step in MainForm.MakeWindows(MainForm.ShownWith(page)))
						{
						}
						Application.DoEvents();
						Assert.AreEqual(1, loads, "Making the page's windows loaded the settings again.");

						page.Visible = true;
						page.Visible = false;
						Application.DoEvents();
						Assert.AreEqual(1, loads, "Showing the page unseen loaded the settings again.");
					}
				}
				finally
				{
					SettingsManager.Current.NotifySettingsStatus = oldStatus;
				}
			});
		}

		[TestMethod, TestCategory("startup"), TestCategory("critical")]
		[Description("A list on a page not open rebuilds its rows at most once while its windows are made, whatever stands above it")]
		public void Making_windows_rebuilds_the_rows_of_a_list_at_most_once()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form { ShowInTaskbar = false })
				{
					var page = HiddenPage(form);
					var panel = new Panel { Dock = DockStyle.Fill };
					var inner = new Panel { Dock = DockStyle.Fill };
					var grid = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false };
					inner.Controls.Add(grid);
					panel.Controls.Add(inner);
					page.Controls.Add(panel);
					grid.DataSource = new System.ComponentModel.BindingList<Row> { new Row { Name = "One" } };
					form.Show();
					var rebuilt = 0;
					grid.DataBindingComplete += (s, e) => rebuilt++;

					foreach (var step in MainForm.MakeWindows(MainForm.ShownWith(page)))
					{
					}
					Application.DoEvents();

					Assert.IsTrue(rebuilt <= 1, string.Format("The list rebuilt its rows {0} times while its windows were made.", rebuilt));
					Assert.AreEqual(1, grid.Rows.Count, "The list lost its row.");
				}
			});
		}

		[TestMethod, TestCategory("startup"), TestCategory("critical")]
		[Description("Opening the General tab lays its table out once or twice, not once for each panel that comes into view")]
		public void Opening_the_general_tab_lays_its_table_out_once()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form { ShowInTaskbar = false, Width = 900, Height = 700 })
				using (var pad = new PadControl(MapTo.Controller1) { Dock = DockStyle.Fill })
				{
					form.Controls.Add(pad);
					form.Show();
					Application.DoEvents();
					var general = pad.PadTabControl.SelectedTab;
					pad.PadTabControl.SelectedTab = pad.PadTabControl.TabPages[1];
					Application.DoEvents();
					var laidOut = 0;
					pad.GeneralTableLayoutPanel.Layout += (s, e) => laidOut++;

					pad.PadTabControl.SelectedTab = general;
					Application.DoEvents();

					Assert.IsTrue(laidOut <= 2, string.Format("Opening the tab laid its table out {0} times.", laidOut));
				}
			});
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A device mapped shows in its row of the Devices list, and the rows and the selection stay where they are")]
		public void A_device_mapped_shows_in_the_list_of_places_without_rebuilding_it()
		{
			Ui.OnUiThread(() =>
			{
				var game = DeviceOnTwoTabsTest.NewGame();
				var wheel = DeviceOnTwoTabsTest.NewDevice("Wheel");
				SettingsManager.CurrentGame = game;
				using (var form = new Form { ShowInTaskbar = false })
				{
					var control = new XInputDevicesUserControl { Dock = DockStyle.Fill };
					form.Controls.Add(control);
					form.Show();
					// The answer of reading the machine arrives as a message this thread does not pump, so this stays the list.
					control._entries.Add(new XInputReorderPlan.Entry { HardwareId = "c1", Name = "c1", IsVirtual = true, IsOurs = true, Place = 0, Pad = 1 });
					control.Bind();
					var grid = control.DevicesDataGridView;
					var mapped = control.MappedColumn.Index;
					Assert.AreEqual("Nothing mapped", grid.Rows[0].Cells[mapped].Value);
					var removed = 0;
					grid.RowsRemoved += (s, e) => removed++;

					DeviceOnTwoTabsTest.Map(game, wheel, MapTo.Controller1);

					StringAssert.Contains((string)grid.Rows[0].Cells[mapped].Value, "Wheel", "The list does not show the device mapped.");
					Assert.AreEqual(0, removed, "The list rebuilt its rows for a device mapped.");
				}
			});
		}
	}
}
