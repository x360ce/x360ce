// @under-test: Engine/JocysCom/Controls/Themes/Default.Forms.cs, App.v4/MainForm.Designer.cs, App.v4/Controls, App.v4/Forms, Engine/JocysCom/Controls, Engine/JocysCom/IO/HardwareControl.Designer.cs
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.IssuesControl;
using JocysCom.ClassLibrary.Controls.Themes;
using JocysCom.ClassLibrary.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.App.Forms;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The lists (grids) of every page and window: their column titles their text with 4 px above and below, and their first
	/// shown column with 4 px more room on the left, at 100 % and 150 %, in both themes. Where a list stands is the one gap
	/// every item keeps (<see cref="SpacingGridTest"/>).
	/// </summary>
	/// <remarks>
	/// The owner's report of 2026-11-04 (4.29): a list stood in from the sides of its page, its titles were cramped, and the
	/// text of its first column stood against its left edge. His report of 2026-11-08: the text of the About page's Program
	/// tab stood 1 px below its tab and 3 px in from the frame's other sides, and other lists and texts stood further in.
	/// </remarks>
	[TestClass]
	public class ListLayoutTest
	{
		public TestContext TestContext { get; set; }

		/// <summary>The room the owner asked for, in pixels at 100 %: above and below a title, and before the first column.</summary>
		const int Room = 4;

		/// <summary>
		/// The pages and windows that show a list, each made as the program makes it: a page of the main window inside its
		/// tab, a window as it is.
		/// </summary>
		/// <remarks>
		/// The Cloud page starts the program's cloud client as it is made, so it is looked at in the running program. The
		/// Devices page holds two lists, each made here on a page of its own.
		/// </remarks>
		static readonly Func<Control>[] Surfaces =
		{
			() => OptionsLayoutTest.MainWindowAtDefaultSize(new PadControl(MapTo.Controller1), "Controller 1"),
			() => OptionsLayoutTest.MainWindowAtDefaultSize(new GamesGridUserControl(), "Games"),
			() => OptionsLayoutTest.MainWindowAtDefaultSize(new XInputDevicesUserControl(), "Devices"),
			() => OptionsLayoutTest.MainWindowAtDefaultSize(new UserDevicesUserControl(), "Devices"),
			() => OptionsLayoutTest.MainWindowAtDefaultSize(new ProgramsGridUserControl(), "Programs"),
			() => OptionsLayoutTest.MainWindowAtDefaultSize(new SettingsGridUserControl(), "Settings"),
			() => OptionsLayoutTest.MainWindowAtDefaultSize(new IssuesUserControl(), "Issues"),
			() => OptionsLayoutTest.MainWindowAtDefaultSize(new OptionsUserControl(), "Options"),
			() => new MapDeviceToControllerForm(),
			() => new LoadPresetsForm(),
			() => new HardwareForm(),
			() => new InfoForm { SelectedControl = new Button { Name = "ProbeButton" } },
			() => OptionsLayoutTest.MainWindowAtDefaultSize(new AboutControl(), "About"),
		};

		[TestMethod, TestCategory("ui")]
		[Description("Every list's titles are their text with 4 px above and below, and its first shown column 4 px more on the left, at 100 % and 150 %, in both themes")]
		public void Lists_have_room_round_their_titles_and_first_column()
		{
			var report = new List<string>();
			var faults = new List<string>();
			var grids = 0;
			ButtonIconTest.OnEverySurface(Surfaces, (form, when) =>
			{
				foreach (var grid in ButtonIconTest.All(form).OfType<DataGridView>().ToList())
				{
					grids++;
					ShowWhereItStands(grid);
					var name = when + " " + ButtonIconTest.Path(grid);
					CheckTitles(grid, name, report, faults);
					CheckFirstColumn(grid, name, report, faults);
				}
			});
			foreach (var line in report)
				TestContext.WriteLine(line);
			Assert.IsTrue(grids > 0, "No list was found to look at.");
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s) in " + grids + " lists:" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("The Map Device window, themed as the program themes a dialog once it is active, opens wide enough for every button of its list's tool bar to stand on the bar, none in its overflow menu, and no wider than its screen's working area, in both themes")]
		public void Map_device_window_shows_every_tool_bar_button()
		{
			var report = new List<string>();
			var faults = new List<string>();
			ButtonIconTest.OnEverySurface(new Func<Control>[] { () => new MapDeviceToControllerForm() }, (form, when) =>
			{
				var strip = (ToolStrip)form.Controls.Find(nameof(UserDevicesUserControl.ControllersToolStrip), true).Single();
				var shown = strip.Items.Cast<ToolStripItem>().Where(x => x.Available).ToList();
				var overflow = shown.Where(x => x.IsOnOverflow).Select(x => x.Text).ToList();
				var work = Screen.FromControl(form).WorkingArea;
				var line = string.Format("{0}: window {1} px wide, tool bar {2} px of the {3} its {4} buttons take, screen {5} px",
					when, form.Width, strip.Width, strip.PreferredSize.Width, shown.Count, work.Width);
				report.Add(line);
				if (overflow.Count > 0)
					faults.Add(line + "; in the overflow menu: " + string.Join(", ", overflow));
				if (form.Width > work.Width)
					faults.Add(line + "; wider than the screen.");
			}, themedOnceActive: true);
			foreach (var line in report)
				TestContext.WriteLine(line);
			Assert.AreEqual(2, report.Count, "The window was not looked at in both themes.");
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("A list's first shown column has the room whatever it holds, text, a picture or a check box, and the room follows the column that is first, in both themes")]
		public void First_column_room_follows_the_first_column_whatever_it_holds()
		{
			var faults = new List<string>();
			Ui.OnUiThread(() =>
			{
				try
				{
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
						foreach (var kind in new[] { "text", "picture", "check box" })
						{
							FormsTheme.SetTheme(theme);
							using (var image = new Bitmap(16, 16))
							using (var form = new Form { ClientSize = new Size(360, 140), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
							{
								// Grey, so the picture stands out from its cell in both themes: black is lost on the dark field.
								using (var g = Graphics.FromImage(image))
									g.Clear(Color.Gray);
								var grid = new DataGridView { Dock = DockStyle.Fill, RowHeadersVisible = false, AllowUserToAddRows = false };
								DataGridViewColumn first = kind == "text" ? new DataGridViewTextBoxColumn()
									: kind == "picture" ? (DataGridViewColumn)new DataGridViewImageColumn { AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells }
									: new DataGridViewCheckBoxColumn { AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells };
								first.HeaderText = "On";
								grid.Columns.Add(first);
								grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name" });
								form.Controls.Add(grid);
								form.Show();
								FormsTheme.Apply(form);
								grid.Rows.Add(kind == "text" ? (object)"Wheel" : kind == "picture" ? (object)image : true, "Logitech");
								var name = theme + " " + kind;
								var faultsBefore = faults.Count;
								CheckFirstColumn(grid, name, new List<string>(), faults);
								if (faults.Count == faultsBefore && Inset(grid, first.Index, 0) < 0)
									faults.Add(name + ": the first cell shows nothing to measure.");
								// Hidden, the first column passes the room to the one now first.
								first.Visible = false;
								Application.DoEvents();
								var shown = Inset(grid, 1, -1);
								first.Visible = true;
								Application.DoEvents();
								var second = Inset(grid, 1, -1);
								if (shown - second != Pixels(Room))
									faults.Add(string.Format("{0}: with the first column hidden, the next title stands {1} px in, and {2} px when it is second.", name, shown, second));
							}
						}
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
				}
			});
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("The Hardware window's device list takes the devices it is given after the theme reached it, columns made from them among its own, and gives its first column the room, in both themes")]
		public void Hardware_list_takes_its_devices_after_the_theme()
		{
			var faults = new List<string>();
			Ui.OnUiThread(() =>
			{
				try
				{
					var bind = typeof(JocysCom.ClassLibrary.IO.HardwareControl).GetMethod(nameof(JocysCom.ClassLibrary.IO.HardwareControl.BindDeviceList),
						System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
					Assert.IsNotNull(bind, "The Hardware window gives its list its devices some other way.");
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
						using (var form = new Form { ClientSize = new Size(700, 400), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
						{
							FormsTheme.SetTheme(theme);
							var hardware = new JocysCom.ClassLibrary.IO.HardwareControl { Dock = DockStyle.Fill };
							form.Controls.Add(hardware);
							FormsTheme.Apply(form);
							form.Show();
							var grid = (DataGridView)hardware.Controls.Find(nameof(HardwareControl.DeviceDataGridView), true).Single();
							ShowWhereItStands(grid);
							// As its timer does, once and again with other devices.
							var devices = new[] { "Wheel", "Pad" }.Select(x => new JocysCom.ClassLibrary.IO.DeviceInfo { Description = x, DeviceId = @"HID\" + x }).ToList();
							bind.Invoke(hardware, new object[] { devices.Take(1).ToList() });
							bind.Invoke(hardware, new object[] { devices });
							Application.DoEvents();
							// The window's own timer may have listed this machine's devices meanwhile.
							if (grid.Rows.Count == 0)
								faults.Add(theme + ": the list shows no rows for " + devices.Count + " devices.");
							else
								CheckFirstColumn(grid, theme + " Hardware", new List<string>(), faults);
						}
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
				}
			});
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		/// <summary>
		/// Whether a control is a list box, or a text of many lines, that fills what holds it without an edge of its own: a box
		/// with a frame, which the theme draws in the window's edge, is a box to type in, which keeps its room from the frame
		/// round the page.
		/// </summary>
		internal static bool IsFillingText(Control control)
		{
			if (control.Dock != DockStyle.Fill || control.Parent == null || !(control is ListBox
				|| (control is TextBoxBase box && (box.Multiline || box is RichTextBox))))
				return false;
			return control.RectangleToScreen(control.ClientRectangle).Left == control.Parent.RectangleToScreen(control.Bounds).Left;
		}

		/// <summary>Selects the tab of every tab strip the grid stands on, so it is laid out and drawn as it is seen.</summary>
		static void ShowWhereItStands(Control control)
		{
			for (var c = control; c != null; c = c.Parent)
				if (c is TabPage page && page.Parent is TabControl tabs && tabs.SelectedTab != page)
					tabs.SelectedTab = page;
			Application.DoEvents();
		}

		/// <summary>
		/// The title row is as tall as its text with the room above and below it, as Default.xaml's column headers: the titles'
		/// font's height and twice the room.
		/// </summary>
		static void CheckTitles(DataGridView grid, string name, List<string> report, List<string> faults)
		{
			var columns = grid.Columns.Cast<DataGridViewColumn>().Where(x => x.Visible).ToList();
			if (!grid.ColumnHeadersVisible || columns.Count == 0)
				return;
			var text = (grid.ColumnHeadersDefaultCellStyle.Font ?? grid.Font).Height;
			var wanted = text + 2 * Pixels(Room);
			var line = string.Format("{0}: titles {1} px tall, their text {2} px, {3} px wanted ({4})",
				name, grid.ColumnHeadersHeight, text, wanted, grid.ColumnHeadersHeightSizeMode);
			report.Add(line);
			if (grid.ColumnHeadersHeight != wanted)
				faults.Add(line);
		}

		/// <summary>
		/// The first shown column's title, and its first cell where the list has rows, start the room further right than the
		/// same column's do when it is shown second: all of it for content at the left, which a check box always is, or in a
		/// column the room widens; half of it for content in the middle of a column as wide as before, and none for content at
		/// its right.
		/// </summary>
		static void CheckFirstColumn(DataGridView grid, string name, List<string> report, List<string> faults)
		{
			if (!grid.Visible || grid.Width <= 0 || grid.Height <= 0)
				return;
			var first = grid.Columns.GetFirstColumn(DataGridViewElementStates.Visible);
			var second = first == null ? null : grid.Columns.GetNextColumn(first, DataGridViewElementStates.Visible, DataGridViewElementStates.None);
			if (second == null)
				return;
			grid.ClearSelection();
			try { grid.CurrentCell = null; }
			catch (InvalidOperationException) { }
			var rows = grid.ColumnHeadersVisible ? new[] { -1 } : new int[0];
			if (grid.Rows.Count > 0)
				rows = rows.Concat(new[] { 0 }).ToArray();
			foreach (var row in rows)
			{
				var asFirst = Inset(grid, first.Index, row);
				if (asFirst < 0)
				{
					report.Add(string.Format("{0}: {1} of {2} shows nothing to measure.", name, row < 0 ? "the title" : "the first cell", first.Name));
					continue;
				}
				var index = first.DisplayIndex;
				var widthFirst = first.Width;
				first.DisplayIndex = second.DisplayIndex;
				Application.DoEvents();
				int asSecond;
				var grown = 0;
				try
				{
					asSecond = Inset(grid, first.Index, row);
					grown = widthFirst - first.Width;
				}
				finally
				{
					first.DisplayIndex = index;
					Application.DoEvents();
				}
				var room = Pixels(Room);
				var align = row < 0 ? first.HeaderCell.InheritedStyle.Alignment : grid.Rows[row].Cells[first.Index].InheritedStyle.Alignment;
				var centred = align == DataGridViewContentAlignment.TopCenter || align == DataGridViewContentAlignment.MiddleCenter
					|| align == DataGridViewContentAlignment.BottomCenter;
				var atRight = align == DataGridViewContentAlignment.TopRight || align == DataGridViewContentAlignment.MiddleRight
					|| align == DataGridViewContentAlignment.BottomRight;
				var box = row >= 0 && first is DataGridViewCheckBoxColumn;
				var wanted = box || !(centred || atRight) ? room : centred ? (room + grown) / 2.0 : grown;
				var line = string.Format("{0}: {1} of {2} ({3}, {4}) stands {5} px in when first, {6} px when second, {7} px wider; {8} px more wanted",
					name, row < 0 ? "the title" : "the first cell", first.Name, first.GetType().Name, align, asFirst, asSecond, grown, wanted);
				report.Add(line);
				if (Math.Abs(asFirst - asSecond - wanted) > (centred && !box ? 1 : 0))
					faults.Add(line);
			}
		}

		/// <summary>
		/// How far in from its cell's left edge the first mark drawn in a cell stands: text, a picture or a check box, unlike the
		/// colour the cell is filled with along its left edge; -1 when nothing is drawn there.
		/// </summary>
		static int Inset(DataGridView grid, int column, int row)
		{
			var cell = grid.GetCellDisplayRectangle(column, row, false);
			if (cell.Width <= 0 || cell.Height <= 0)
				return -1;
			using (var bitmap = new Bitmap(grid.Width, grid.Height))
			{
				grid.DrawToBitmap(bitmap, new Rectangle(Point.Empty, grid.Size));
				// The lines between rows run along the bottom of a cell, and are left out.
				var bottom = Math.Min(bitmap.Height, cell.Bottom - 1);
				for (var x = cell.Left + 1; x < Math.Min(bitmap.Width, cell.Right); x++)
					for (var y = cell.Top + 1; y < bottom; y++)
					{
						var fill = bitmap.GetPixel(cell.Left, y);
						var c = bitmap.GetPixel(x, y);
						if (Math.Abs(c.R - fill.R) + Math.Abs(c.G - fill.G) + Math.Abs(c.B - fill.B) > 90)
							return x - cell.Left;
					}
			}
			return -1;
		}

		static int Pixels(int value)
		{
			return (int)Math.Round(value * ControlsHelper.DpiScale);
		}
	}
}
