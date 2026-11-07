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
	/// The lists (grids) of every page and window: edge to edge in the area that holds them, their column titles with 3 px
	/// more room above and below, and their first shown column with 3 px more room on the left, at 100 % and 150 %, in
	/// both themes.
	/// </summary>
	/// <remarks>
	/// The owner's report of 2026-11-04 (4.29): a list stood in from the sides of its page, its titles were cramped, and the
	/// text of its first column stood against its left edge.
	/// </remarks>
	[TestClass]
	public class ListLayoutTest
	{
		public TestContext TestContext { get; set; }

		/// <summary>The room the owner asked for, in pixels at 100 %: above and below a title, and before the first column.</summary>
		const int Room = 3;

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
		};

		/// <summary>Panels shown beside a list only at times: while games are scanned, and once no issue is left.</summary>
		static readonly string[] ShownAtTimes = { nameof(GamesGridUserControl.ScanProgressPanel), nameof(IssuesUserControl.NoIssuesPanel), nameof(IssuesUserControl.LinePanel) };

		[TestMethod, TestCategory("ui")]
		[Description("Every list stands edge to edge in the area that holds it, what stands above and below it keeps its place, its titles have 3 px more room above and below, and its first shown column 3 px more on the left, at 100 % and 150 %, in both themes")]
		public void Lists_stand_edge_to_edge_with_room_round_their_titles_and_first_column()
		{
			var report = new List<string>();
			var faults = new List<string>();
			var grids = 0;
			ButtonIconTest.OnEverySurface(Surfaces, (form, when) =>
			{
				foreach (var shown in ShownAtTimes)
					foreach (var panel in form.Controls.Find(shown, true))
						panel.Visible = true;
				// The pages of the main window and of the Add Device window keep their content in from their sides; the
				// other windows hold their lists and tool bars against the sides of their tabs.
				var inset = form.GetType() == typeof(BaseFormWithHeader) || form is MapDeviceToControllerForm ? Pixels(Room) : 0;
				foreach (var grid in ButtonIconTest.All(form).OfType<DataGridView>().ToList())
				{
					grids++;
					ShowWhereItStands(grid);
					var name = when + " " + ButtonIconTest.Path(grid);
					CheckEdges(grid, name, report, faults);
					CheckBeside(grid, name, inset, report, faults);
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
								using (var g = Graphics.FromImage(image))
									g.Clear(Color.Black);
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

		/// <summary>Selects the tab of every tab strip the grid stands on, so it is laid out and drawn as it is seen.</summary>
		static void ShowWhereItStands(Control control)
		{
			for (var c = control; c != null; c = c.Parent)
				if (c is TabPage page && page.Parent is TabControl tabs && tabs.SelectedTab != page)
					tabs.SelectedTab = page;
			Application.DoEvents();
		}

		/// <summary>The tab page or window a grid stands in.</summary>
		static Control AreaOf(Control control)
		{
			var c = control.Parent;
			while (c != null && !(c is TabPage) && !(c is Form))
				c = c.Parent;
			return c;
		}

		/// <summary>
		/// Each side of a list that nothing else stands beside meets the area that holds it: the client area of its tab page
		/// or window. Lists side by side keep the room between them.
		/// </summary>
		static void CheckEdges(DataGridView grid, string name, List<string> report, List<string> faults)
		{
			var area = AreaOf(grid);
			if (area == null || !grid.Visible)
				return;
			var at = area.RectangleToClient(grid.Parent.RectangleToScreen(grid.Bounds));
			var client = area.ClientRectangle;
			// Anything shown beside the list, level with some of its height, but what holds it or what it holds.
			var beside = ButtonIconTest.All(area).Where(x => x.Visible && x != grid && !x.Contains(grid) && !grid.Contains(x))
				.Select(x => area.RectangleToClient(x.Parent.RectangleToScreen(x.Bounds)))
				.Where(x => x.Width > 0 && x.Height > 0 && x.Top < at.Bottom && x.Bottom > at.Top).ToList();
			var left = beside.Any(x => x.Right <= at.Left);
			var right = beside.Any(x => x.Left >= at.Right);
			var bars = ScrollBarWidth(grid, area);
			var line = string.Format("{0}: in {1} {2} at {3}{4}{5}{6}; parents {7}", name, area.GetType().Name, client, at,
				left ? ", something to its left" : "", right ? ", something to its right" : "",
				bars > 0 ? ", " + bars + " px of scroll bar" : "", Parents(grid, area));
			report.Add(line);
			if (!left && at.Left != client.Left)
				faults.Add(line + ": its left edge is " + (at.Left - client.Left) + " px in from the area's.");
			if (!right && at.Right != client.Right - bars)
				faults.Add(line + ": its right edge is " + (client.Right - bars - at.Right) + " px in from the area's.");
		}

		/// <summary>The scroll bars of the pages between a control and its area that scroll: they stand at the area's right.</summary>
		static int ScrollBarWidth(Control control, Control area)
		{
			var bars = 0;
			for (var c = control.Parent; c != null && c != area; c = c.Parent)
				if (c is ScrollableControl scrolled && scrolled.AutoScroll && scrolled.VerticalScroll.Visible)
					bars += c.Width - c.ClientSize.Width;
			return bars;
		}

		/// <summary>
		/// What stands above or below a list, or beside it, keeps the room the area's padding gave it before the list stood edge
		/// to edge: its content, a tool bar's buttons among it, starts no nearer the area's sides than <paramref name="inset"/>.
		/// </summary>
		/// <remarks>Controls that hold a list of their own are left to that list's check.</remarks>
		static void CheckBeside(DataGridView grid, string name, int inset, List<string> report, List<string> faults)
		{
			var area = AreaOf(grid);
			if (area == null || !grid.Visible || inset == 0)
				return;
			var client = area.ClientRectangle;
			var right = client.Right - ScrollBarWidth(grid, area) - inset;
			for (Control chain = grid; chain != area; chain = chain.Parent)
				foreach (Control sibling in chain.Parent.Controls)
				{
					if (sibling == chain || !sibling.Visible || sibling.Width <= 0 || sibling.Height <= 0
						|| sibling is DataGridView || ButtonIconTest.All(sibling).OfType<DataGridView>().Any())
						continue;
					var content = area.RectangleToClient(Content(sibling));
					if (content.Width <= 0)
						continue;
					var line = string.Format("{0}: {1} beside it has its content at {2} in {3}, {4} px in from each side wanted",
						name, sibling.Name, content, client, inset);
					report.Add(line);
					if (content.Left < client.Left + inset || content.Right > right)
						faults.Add(line);
				}
		}

		/// <summary>
		/// Where what a control shows stands, on the screen: a tool bar's buttons, inside its padding; the text of a label;
		/// the frame of a framed control, or a control holding nothing, such as a line; otherwise what it holds.
		/// </summary>
		static Rectangle Content(Control control)
		{
			if (control is ToolStrip strip)
				return strip.RectangleToScreen(strip.DisplayRectangle);
			var held = control.Controls.Cast<Control>().Where(x => x.Visible && x.Width > 0 && x.Height > 0).ToList();
			var framed = control is Panel panel && panel.BorderStyle != BorderStyle.None;
			if (framed || held.Count == 0)
			{
				var own = control.Parent.RectangleToScreen(control.Bounds);
				return control is Label ? Rectangle.FromLTRB(own.Left + control.Padding.Left, own.Top, own.Right - control.Padding.Right, own.Bottom) : own;
			}
			var bounds = held.Select(x => control.RectangleToScreen(x.Bounds)).ToList();
			return Rectangle.FromLTRB(bounds.Min(x => x.Left), bounds.Min(x => x.Top), bounds.Max(x => x.Right), bounds.Max(x => x.Bottom));
		}

		/// <summary>The controls between a grid and its area, with their places, paddings and margins, for the report.</summary>
		static string Parents(Control control, Control area)
		{
			var parts = new List<string>();
			for (var c = control; c != null && c != area.Parent; c = c.Parent)
				parts.Add(string.Format("{0} {1} pad {2} margin {3}", c.GetType().Name, c.Bounds, Short(c.Padding), Short(c.Margin)));
			return string.Join(" < ", parts);
		}

		static string Short(Padding p)
		{
			return p.Left + "," + p.Top + "," + p.Right + "," + p.Bottom;
		}

		/// <summary>
		/// The title row is as tall as Windows Forms makes one for the titles' text, with the room added above and below: the
		/// height a plain grid in the same font and widths sizes its titles to, and twice the room.
		/// </summary>
		static void CheckTitles(DataGridView grid, string name, List<string> report, List<string> faults)
		{
			var columns = grid.Columns.Cast<DataGridViewColumn>().Where(x => x.Visible).ToList();
			if (!grid.ColumnHeadersVisible || columns.Count == 0)
				return;
			var plain = PlainTitleHeight(grid, columns);
			var wanted = plain + 2 * Pixels(Room);
			var line = string.Format("{0}: titles {1} px tall, a plain grid's {2} px, {3} px wanted ({4})",
				name, grid.ColumnHeadersHeight, plain, wanted, grid.ColumnHeadersHeightSizeMode);
			report.Add(line);
			if (grid.ColumnHeadersHeight != wanted)
				faults.Add(line);
		}

		/// <summary>
		/// The height Windows Forms sizes a grid's titles to by itself: the text, in the titles' font and the columns' widths,
		/// with what it keeps round it, in a grid without lines between its titles or a visual style.
		/// </summary>
		static int PlainTitleHeight(DataGridView grid, List<DataGridViewColumn> columns)
		{
			var style = grid.ColumnHeadersDefaultCellStyle;
			using (var plain = new DataGridView
			{
				ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
				ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
				EnableHeadersVisualStyles = false,
				RowHeadersVisible = false,
				Font = grid.Font,
			})
			{
				plain.ColumnHeadersDefaultCellStyle.Font = style.Font ?? grid.Font;
				plain.ColumnHeadersDefaultCellStyle.WrapMode = style.WrapMode;
				foreach (var column in columns)
					plain.Columns.Add(new DataGridViewTextBoxColumn
					{
						HeaderText = column.HeaderText,
						SortMode = column.SortMode,
						AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
						MinimumWidth = 2,
						Width = Math.Max(2, column.Width),
					});
				var handle = plain.Handle;
				plain.AutoResizeColumnHeadersHeight();
				return plain.ColumnHeadersHeight;
			}
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
