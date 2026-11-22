// @under-test: App.v4/Controls/PadControl.cs, App.v4/Controls/PadControl.Designer.cs, App.v4/Controls/PadTabPages/General/InputUserControl.cs, App.v4/MainForm.Designer.cs
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// The controller page in the main window: its bottom buttons stay in view, and what can grow scrolls inside itself.
	/// </summary>
	/// <remarks>
	/// The page measured its tabs and grew them to their content, and scrolled as a whole. A keyboard's long list of inputs, a
	/// long SDL list or a tab the page had not laid out grew the tabs, and the buttons under them slid below the window with a
	/// long scroll bar beside the page. The page is a table: its tool bar, device list and buttons as tall as they are, the
	/// tabs in the rest, and a list that is longer than its room scrolls inside itself.
	/// </remarks>
	[TestClass]
	public class PadPageShortWindowTest
	{
		public TestContext TestContext { get; set; }

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("Through a keyboard, a pad and no device, every tab, a long SDL list and the window at its smallest size and larger, the Controller page's bottom buttons stay in view, the page never scrolls, and a list or panel scrolls only when its own content is longer than its room, in both themes")]
		public void The_bottom_buttons_stay_in_view_and_lists_scroll_inside_themselves()
		{
			var faults = new List<string>();
			var report = new List<string>();
			ButtonIconTest.OnEverySurface(new Func<Control>[] { () => OptionsLayoutTest.MainWindowAtDefaultSize(new PadControl(MapTo.Controller1), "Controller 1") }, (form, when) =>
			{
				var window = (Form)form;
				window.MinimumSize = MinimumWindowSize();
				var page = Descendants(window).OfType<PadControl>().Single();
				var input = page.InputPanel;
				var inputTable = input.Controls.Cast<Control>().Single();
				var sdl = (DataGridView)page.Controls.Find("PositionsDataGridView", true).Single();
				var keyboard = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Keyboard", CapButtonCount = 128, CapType = (int)SharpDX.DirectInput.DeviceType.Keyboard };
				var pad = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Pad", CapButtonCount = 12, DiAxeMask = 0x0F, CapPovCount = 1, CapType = (int)SharpDX.DirectInput.DeviceType.Gamepad };
				var smallest = window.MinimumSize;
				var steps = new List<KeyValuePair<string, Action>>
				{
					Step("the smallest window", () => window.Size = smallest),
					Step("a keyboard", () => input.UpdateFrom(keyboard)),
					Step("a pad", () => input.UpdateFrom(pad)),
					Step("no device", () => input.UpdateFrom(null)),
					Step("a keyboard again", () => input.UpdateFrom(keyboard)),
					Step("a long SDL list", () => sdl.DataSource = Enumerable.Range(1, 200).Select(x => new SdlPositionRow { Field = "b" + x, Position = "Place " + x }).ToList()),
				};
				foreach (TabPage tab in page.PadTabControl.TabPages)
					steps.Add(Step("the " + tab.Text + " tab", () => page.PadTabControl.SelectedTab = tab));
				steps.Add(Step("the General tab", () => page.PadTabControl.SelectedTab = page.GeneralTabPage));
				steps.Add(Step("a larger window", () => window.Size = new Size(smallest.Width + 400, smallest.Height + 400)));
				steps.Add(Step("a pad in a larger window", () => input.UpdateFrom(pad)));
				steps.Add(Step("the smallest window again", () => window.Size = smallest));
				steps.Add(Step("the SDL tab", () => page.PadTabControl.SelectedTab = page.SdlTabPage));
				foreach (var step in steps)
				{
					step.Value();
					Application.DoEvents();
					var pageAt = window.RectangleToClient(page.Parent.RectangleToScreen(page.Bounds));
					var buttons = window.RectangleToClient(page.ButtonsPanel.Parent.RectangleToScreen(page.ButtonsPanel.Bounds));
					var line = string.Format("{0}, {1}: window {2}, page {3}, buttons {4}, tabs {5} tall, inputs {6} in {7}",
						when, step.Key, window.Size, pageAt, buttons, page.PadTabControl.Height, inputTable.Height, input.ClientSize.Height);
					report.Add(line);
					if (buttons.Bottom > pageAt.Bottom || buttons.Top < pageAt.Top)
						faults.Add(line + ": the bottom buttons are out of view.");
					// Only a panel whose own content is longer than its room scrolls, and never the page or what holds it.
					foreach (var scrolled in Descendants(window).OfType<ScrollableControl>().Where(x => x.Visible && x.VerticalScroll.Visible))
						if (scrolled == page || scrolled.Contains(page) || scrolled.DisplayRectangle.Height <= scrolled.ClientSize.Height)
							faults.Add(line + ": " + scrolled.Name + " scrolls.");
					var inputScrolls = input.VerticalScroll.Visible;
					var inputLonger = inputTable.Height > input.ClientSize.Height;
					if (input.Visible && inputScrolls != inputLonger)
						faults.Add(line + ": the inputs " + (inputScrolls ? "show a scroll bar they do not need." : "are longer than their room and do not scroll."));
					if (page.PadTabControl.SelectedTab == page.SdlTabPage)
					{
						var sdlAt = page.SdlTabPage.RectangleToClient(sdl.Parent.RectangleToScreen(sdl.Bounds));
						if (sdlAt.Bottom > page.SdlTabPage.ClientSize.Height)
							faults.Add(line + ": the SDL list runs past the bottom of its tab, at " + sdlAt + ".");
						if (sdl.RowCount > 0 && sdl.DisplayedRowCount(false) >= sdl.RowCount)
							faults.Add(line + ": the SDL list shows all " + sdl.RowCount + " rows rather than scrolling inside itself.");
					}
				}
			});
			foreach (var line in report)
				TestContext.WriteLine(line);
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults.Distinct()));
		}

		[TestMethod, TestCategory("ui")]
		[Description("In the main window at its smallest size, and in a window shorter still, the Controller page keeps room for three device rows in its list above the pages, at the screen's scale, in both themes: the list gives up its height before the pages")]
		public void The_device_list_keeps_three_rows_at_the_smallest_window_size_and_shorter()
		{
			var faults = new List<string>();
			ButtonIconTest.OnEverySurface(new Func<Control>[] { () => OptionsLayoutTest.MainWindowAtDefaultSize(new PadControl(MapTo.Controller2), "Controller 2") }, (form, when) =>
			{
				var window = (Form)form;
				var smallest = MinimumWindowSize();
				window.MinimumSize = Size.Empty;
				foreach (var size in new[] { smallest, new Size(smallest.Width, smallest.Height - (int)Math.Round(100 * ControlsHelper.DpiScale)) })
				{
					window.Size = size;
					Application.DoEvents();
					var page = Descendants(window).OfType<PadControl>().Single();
					var grid = page.MappedDevicesDataGridView;
					var rows = (grid.ClientSize.Height - grid.ColumnHeadersHeight) / (double)grid.RowTemplate.Height;
					var line = string.Format("{0}, window {1}: the page is {2}, the tabs {3} px tall, the General tab {4}, the device list {5} px with room for {6:0.0} rows of {7} px",
						when, window.Size, page.ClientSize, page.PadTabControl.Height, page.GeneralTabPage.ClientSize, grid.Height, rows, grid.RowTemplate.Height);
					TestContext.WriteLine(line);
					if (rows < 3)
						faults.Add(line);
				}
			});
			Assert.AreEqual(0, faults.Count, Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("At 100, 125 and 150 %, in both themes, in windows tall enough for them, and at the window's smallest size at the screen's own scale, the Controller page's pages are no taller than the tallest of them needs, by more than a step of the 4 px grid, and none needs more than it has")]
		public void The_pages_are_as_tall_as_the_tallest_needs()
		{
			var faults = new List<string>();
			var all = Measured(TestContext);
			foreach (var m in all.Where(x => x.Kind != WindowKind.Short && (x.Kind != WindowKind.Smallest || x.Zoom == ControlsHelper.DpiScale)))
			{
				var tallest = TallestNeed(all, m);
				var room = m.PageHeight - tallest.Value;
				if (room > m.Step)
					faults.Add(string.Format("{0}: the pages are {1} px tall and the tallest, {2}, needs {3}: {4} px to spare.",
						m.When, m.PageHeight, tallest.Key, tallest.Value, room));
				foreach (var need in m.Needs.Where(x => x.Value > m.PageHeight))
					faults.Add(string.Format("{0}: {1} needs {2} px and has {3}.", m.When, need.Key, need.Value, m.PageHeight));
			}
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("At 100, 125 and 150 %, in both themes, at the window's smallest size, the owner's 1302 by 940 and 1302 by 1200 at 100 %, the SDL page holding the list a pad shows, a row for each of its places, fills its page with the list down to its buttons, with no band between, and a list longer than its room scrolls until its last row shows whole; the Mapped devices list keeps three rows")]
		public void The_sdl_page_shows_its_whole_list()
		{
			var faults = new List<string>();
			var designed = MinimumWindowSize();
			foreach (var zoom in new[] { 1f, 1.25f, 1.5f })
				ButtonIconTest.OnEverySurface(new Func<Control>[] { () => OptionsLayoutTest.MainWindowAtDefaultSize(new PadControl(MapTo.Controller1), "Controller 1") }, (form, when) =>
				{
					var window = (Form)form;
					window.MinimumSize = Size.Empty;
					var page = Descendants(window).OfType<PadControl>().Single();
					var grid = page.MappedDevicesDataGridView;
					var tab = page.SdlTabPage;
					var sdl = (DataGridView)tab.Controls.Find("PositionsDataGridView", true).Single();
					var buttons = tab.Controls.Find("ButtonsFlowLayoutPanel", true).Single();
					var step = (int)Math.Round(4 * zoom);
					var smallest = new Size((int)Math.Round(designed.Width / ControlsHelper.DpiScale), (int)Math.Round(designed.Height / ControlsHelper.DpiScale));
					FillSdlPage(page);
					foreach (var size in new[] { smallest, new Size(1302, 940), new Size(1302, 1200) })
					{
						window.Size = new Size((int)Math.Round(size.Width * zoom), (int)Math.Round(size.Height * zoom));
						Application.DoEvents();
						var rows = (grid.ClientSize.Height - grid.ColumnHeadersHeight) / (double)grid.RowTemplate.Height;
						var listAt = tab.RectangleToClient(sdl.Parent.RectangleToScreen(sdl.Bounds));
						var buttonsAt = tab.RectangleToClient(buttons.Parent.RectangleToScreen(buttons.Bounds));
						var shown = sdl.DisplayedRowCount(false);
						var line = string.Format("{0}, window {1} at 100 %: the SDL list {2} shows {3} of {4} rows whole, its buttons {5} in a page of {6}, the Mapped devices list {7:0.0} rows",
							when, size, listAt, shown, sdl.RowCount, buttonsAt, tab.ClientSize, rows);
						TestContext.WriteLine(line);
						var between = buttonsAt.Top - buttons.Margin.Top - listAt.Bottom - sdl.Margin.Bottom;
						if (between > step)
							faults.Add(line + ": a band of " + between + " px between the SDL list and its buttons.");
						var below = tab.DisplayRectangle.Bottom - buttonsAt.Bottom - buttons.Margin.Bottom;
						if (below > step)
							faults.Add(line + ": a band of " + below + " px under the SDL page's buttons, which the list does not fill.");
						if (sdl.RowCount == 0)
							faults.Add(line + ": the SDL list is empty.");
						else if (shown == 0)
							faults.Add(line + ": the SDL list has no room for a row.");
						else if (shown < sdl.RowCount)
						{
							if (!sdl.Controls.OfType<VScrollBar>().Any(x => x.Visible && x.Enabled))
								faults.Add(line + ": the SDL list is cut and does not scroll.");
							var last = sdl.RowCount - 1;
							sdl.FirstDisplayedScrollingRowIndex = last;
							Application.DoEvents();
							if (sdl.GetRowDisplayRectangle(last, true).Height < sdl.Rows[last].Height)
								faults.Add(line + ": the SDL list's last row does not show whole when scrolled to the end.");
							sdl.FirstDisplayedScrollingRowIndex = 0;
						}
						if (rows < 3)
							faults.Add(line + ": the Mapped devices list has less than three rows.");
					}
				}, zoom: zoom);
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("At 100, 125 and 150 %, in both themes, in a tall window, with the SDL page holding its list of 19 places as it does once opened, the General page's last row ends its page's room above the page's bottom, within a step of the 4 px grid")]
		public void The_general_page_has_no_blank_band_in_a_tall_window()
		{
			var faults = new List<string>();
			foreach (var m in Measured(TestContext).Where(x => x.Kind == WindowKind.Tall))
			{
				var band = m.PageHeight - m.Needs["General"];
				if (band > m.Step)
					faults.Add(string.Format("{0}: the General page is {1} px tall and needs {2}: an empty band of {3} px below its last row.",
						m.When, m.PageHeight, m.Needs["General"], band));
			}
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("At 100, 125 and 150 %, in both themes, a taller window gives all the height it adds to the Mapped devices list, more rows of devices, and the pages under it stay as tall: from a tall window to a taller one, and from the smallest to a tall one at the screen's own scale")]
		public void A_taller_window_gives_its_height_to_the_device_list()
		{
			var faults = new List<string>();
			var all = Measured(TestContext);
			foreach (var taller in all.Where(x => x.Kind == WindowKind.Tall || x.Kind == WindowKind.Taller))
			{
				var shorter = all.Single(x => x.Zoom == taller.Zoom && x.Theme == taller.Theme
					&& x.Kind == (taller.Kind == WindowKind.Taller ? WindowKind.Tall : WindowKind.Smallest));
				if (shorter.Kind == WindowKind.Smallest && shorter.Zoom != ControlsHelper.DpiScale)
					continue;
				var added = taller.Window.Height - shorter.Window.Height;
				var grown = taller.GridHeight - shorter.GridHeight;
				if (taller.TabsHeight != shorter.TabsHeight)
					faults.Add(string.Format("{0}: {1} px more window than {2} made the pages {3} px tall, not {4}.",
						taller.When, added, shorter.Window, taller.TabsHeight, shorter.TabsHeight));
				if (grown != added)
					faults.Add(string.Format("{0}: {1} px more window than {2} made the device list {3} px taller ({4:0.0} rows, not {5:0.0}).",
						taller.When, added, shorter.Window, grown, taller.GridRows, shorter.GridRows));
			}
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		/// <summary>The windows the Controller page is measured in.</summary>
		internal enum WindowKind
		{
			/// <summary>Shorter than its smallest size, so the pages are given less than they need.</summary>
			Short,
			/// <summary>The window's smallest size.</summary>
			Smallest,
			/// <summary>1200 tall at 100 %.</summary>
			Tall,
			/// <summary>1400 tall at 100 %.</summary>
			Taller,
		}

		/// <summary>The Controller page measured at a zoom, in a theme, in a window of a size.</summary>
		internal class Measurement
		{
			public string When;
			public float Zoom;
			public string Theme;
			public WindowKind Kind;
			public Size Window;
			/// <summary>One step of the 4 px grid at the zoom, in pixels.</summary>
			public int Step;
			public int GridHeight;
			public double GridRows;
			public int TabsHeight;
			/// <summary>The height of the room a page has, the same for every page.</summary>
			public int PageHeight;
			/// <summary>Each page's text and how far below its top its content needs it to reach.</summary>
			public Dictionary<string, int> Needs = new Dictionary<string, int>();
		}

		/// <summary>
		/// The page that needs the most room at the zoom and theme of a measurement, and how much. A page whose content follows
		/// the room it is given, a list, a chart or a demo that fills what is left, needs it the least it was measured at, in the
		/// short window.
		/// </summary>
		static KeyValuePair<string, int> TallestNeed(List<Measurement> all, Measurement m)
		{
			return TallestNeed(all, m.Zoom, m.Theme);
		}

		/// <summary>The page that needs the most room at a zoom, in a theme, and how much, as <see cref="TallestNeed(List{Measurement}, Measurement)"/>.</summary>
		internal static KeyValuePair<string, int> TallestNeed(List<Measurement> all, float zoom, string theme)
		{
			var same = all.Where(x => x.Zoom == zoom && x.Theme == theme).ToList();
			return same[0].Needs.Keys.Select(x => new KeyValuePair<string, int>(x, same.Min(y => y.Needs[x])))
				.OrderByDescending(x => x.Value).First();
		}

		static List<Measurement> _Measured;

		/// <summary>
		/// The Controller page in the main window at 100, 125 and 150 %, in both themes, in a window 300 shorter than its smallest
		/// size at 100 %, at that size, and 1200 and 1400 tall at 100 %, all as wide: the device list, the tabs and each page's
		/// needs. Measured once for the run.
		/// </summary>
		internal static List<Measurement> Measured(TestContext context)
		{
			if (_Measured != null)
				return _Measured;
			var list = new List<Measurement>();
			var designed = MinimumWindowSize();
			var scale = ControlsHelper.DpiScale;
			foreach (var zoom in new[] { 1f, 1.25f, 1.5f, scale }.Distinct())
				ButtonIconTest.OnEverySurface(new Func<Control>[] { () => OptionsLayoutTest.MainWindowAtDefaultSize(new PadControl(MapTo.Controller1), "Controller 1") }, (form, when) =>
				{
					var window = (Form)form;
					var page = Descendants(window).OfType<PadControl>().Single();
					var smallest = new Size((int)Math.Round(designed.Width / scale * zoom), (int)Math.Round(designed.Height / scale * zoom));
					var sizes = new Dictionary<WindowKind, Size>
					{
						{ WindowKind.Short, new Size(smallest.Width, smallest.Height - (int)Math.Round(300 * zoom)) },
						{ WindowKind.Smallest, smallest },
						{ WindowKind.Tall, new Size(smallest.Width, Math.Max(smallest.Height, (int)Math.Round(1200 * zoom))) },
						{ WindowKind.Taller, new Size(smallest.Width, Math.Max(smallest.Height, (int)Math.Round(1400 * zoom))) },
					};
					window.MinimumSize = Size.Empty;
					foreach (var size in sizes)
					{
						window.Size = size.Value;
						Application.DoEvents();
						FillSdlPage(page);
						var grid = page.MappedDevicesDataGridView;
						var m = new Measurement
						{
							Zoom = zoom,
							Theme = when.Split(' ').Last(),
							Kind = size.Key,
							Window = window.Size,
							Step = (int)Math.Round(4 * zoom),
							GridHeight = grid.Height,
							GridRows = (grid.ClientSize.Height - grid.ColumnHeadersHeight) / (double)grid.RowTemplate.Height,
							TabsHeight = page.PadTabControl.Height,
						};
						m.When = string.Format("{0}, {1} window {2}", when, size.Key, m.Window);
						var lowest = new List<string>();
						// The SDL page first, as it is selected and filled: selected again, it would be drawn for no device.
						var tabs = page.PadTabControl.TabPages.Cast<TabPage>().ToList();
						foreach (var tab in tabs.Where(x => x == page.SdlTabPage).Concat(tabs.Where(x => x != page.SdlTabPage)))
						{
							page.PadTabControl.SelectedTab = tab;
							Application.DoEvents();
							m.PageHeight = tab.ClientSize.Height;
							Control at;
							m.Needs[tab.Text] = Needs(tab, out at);
							lowest.Add(string.Format("{0} {1} ({2})", tab.Text, m.Needs[tab.Text], at?.Name));
						}
						page.PadTabControl.SelectedTab = page.GeneralTabPage;
						Application.DoEvents();
						context.WriteLine("{0}: device list {1} px, {2:0.0} rows of {3}; tabs {4} px; pages {5} px; needs: {6}",
							m.When, m.GridHeight, m.GridRows, grid.RowTemplate.Height, m.TabsHeight, m.PageHeight, string.Join(", ", lowest));
						list.Add(m);
					}
				}, zoom: zoom);
			_Measured = list;
			return list;
		}

		/// <summary>SDL's and x360ce's controller databases as the program carries them.</summary>
		static SdlDatabase _SdlBuiltIn, _X360ceBuiltIn;

		/// <summary>
		/// Shows the SDL page as it is once opened on a Thrustmaster T.16000M, which both databases know: its list of 19 places, a
		/// D-Pad button mapped, under the databases' words. The page draws itself as its tab is selected, for the device then
		/// selected, none here, so it is filled after.
		/// </summary>
		internal static void FillSdlPage(PadControl page)
		{
			if (_SdlBuiltIn == null)
			{
				_SdlBuiltIn = SdlDatabase.Merge(SdlSource.Parse(SdlDatabase.ReadBuiltIn(), SdlSourceKind.BuiltIn));
				_X360ceBuiltIn = SdlDatabase.Merge(SdlSource.Parse(X360ceDatabase.ReadBuiltIn(), SdlSourceKind.BuiltIn));
			}
			page.PadTabControl.SelectedTab = page.SdlTabPage;
			Application.DoEvents();
			var stick = new UserDevice
			{
				InstanceGuid = Guid.NewGuid(),
				ProductName = "T.16000M",
				DevVendorId = 0x044F,
				DevProductId = 0xB10A,
				CapType = (int)SharpDX.DirectInput.DeviceType.Joystick,
				DiAxeMask = 0x23,
				DiSliderMask = 0x1,
				CapButtonCount = 16,
				CapPovCount = 1,
			};
			var sdl = (SdlUserControl)page.SdlTabPage.Controls[0];
			sdl.UpdateFrom(stick, new PadSetting { DPadUp = "1" }, _SdlBuiltIn, _X360ceBuiltIn);
			Application.DoEvents();
		}

		/// <summary>
		/// How far below the top of a page its content needs it to reach: the lowest bottom of what it shows, and the page's own
		/// room below.
		/// </summary>
		/// <remarks>
		/// What follows the page's height, docked to fill it or anchored to its top and bottom, a list, a drawing or a panel, is
		/// counted at its smallest height, and what it holds by where that stands. A plain panel that does not scroll cuts what it
		/// holds at its edges, as a General page name's panel cuts the empty rows of the number under it, so only what is inside
		/// it stands anywhere.
		/// </remarks>
		/// <param name="lowest">What reaches lowest.</param>
		static int Needs(TabPage page, out Control lowest)
		{
			var need = 0;
			lowest = null;
			foreach (var control in Content(page))
			{
				var at = page.RectangleToClient(control.Parent.RectangleToScreen(control.Bounds));
				var bottom = Follows(control)
					? at.Top + Math.Max(control.MinimumSize.Height, control.AutoSize ? control.GetPreferredSize(new Size(control.Width, 0)).Height : 0)
					: at.Bottom;
				var holder = control.Parent as Panel;
				if (holder != null && holder.GetType() == typeof(Panel) && !holder.AutoScroll)
					bottom = Math.Min(bottom, page.RectangleToClient(holder.RectangleToScreen(holder.ClientRectangle)).Bottom);
				if (bottom > need)
				{
					need = bottom;
					lowest = control;
				}
			}
			return need + page.Padding.Bottom;
		}

		/// <summary>Whether a control's height follows what holds it: docked to fill it or a side, or anchored to its top and bottom.</summary>
		static bool Follows(Control control)
		{
			const AnchorStyles topAndBottom = AnchorStyles.Top | AnchorStyles.Bottom;
			if (control is TabPage)
				return true;
			return control.Dock == DockStyle.None
				? (control.Anchor & topAndBottom) == topAndBottom
				: control.Dock != DockStyle.Top && control.Dock != DockStyle.Bottom;
		}

		/// <summary>What a page shows, at every depth: of a tab strip, its page shown; of a list, a box or a slider, not its own parts.</summary>
		static IEnumerable<Control> Content(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				if (!child.Visible)
					continue;
				yield return child;
				if (child is DataGridView || child is UpDownBase || child is ComboBox || child is TextBoxBase || child is TrackBar)
					continue;
				var tabs = child as TabControl;
				var inside = tabs == null ? child : tabs.SelectedTab;
				if (inside == null)
					continue;
				if (inside != child)
					yield return inside;
				foreach (var deeper in Content(inside))
					yield return deeper;
			}
		}

		static KeyValuePair<string, Action> Step(string name, Action action)
		{
			return new KeyValuePair<string, Action>(name, action);
		}

		/// <summary>The main window's smallest size, as its designer file sets it, at the screen's scale.</summary>
		internal static Size MinimumWindowSize()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "MainForm.Designer.cs"));
			var match = Regex.Match(source, @"this\.MinimumSize = new System\.Drawing\.Size\((\d+), (\d+)\);");
			Assert.IsTrue(match.Success, "The main window's smallest size was not found in its designer file.");
			var scale = JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale;
			return new Size((int)Math.Round(int.Parse(match.Groups[1].Value) * scale), (int)Math.Round(int.Parse(match.Groups[2].Value) * scale));
		}

		internal static IEnumerable<Control> Descendants(Control parent)
		{
			yield return parent;
			foreach (Control child in parent.Controls)
				foreach (var deeper in Descendants(child))
					yield return deeper;
		}
	}
}
