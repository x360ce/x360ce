// @under-test: App.v4/Controls/*.Designer.cs, App.v4/Forms/*.Designer.cs, Engine/JocysCom/Controls/*.Designer.cs, Engine/JocysCom/Controls/Themes/Default.Forms.cs
// @area: interface   @layer: ui
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.Forms;

namespace x360ce.Tests
{
	/// <summary>
	/// Every window and page keeps one gap round what it shows, measured as it is seen: every item has the gap at its left and
	/// above it, and every container at its right and below it, so items stand one gap in from the inside of their
	/// container's frame and one gap apart. Every message box keeps the gap at the screen's scale, and every window at its
	/// default size shows its texts whole.
	/// </summary>
	[TestClass]
	public class SpacingGridTest
	{
		public TestContext TestContext { get; set; }

		/// <summary>The one gap of every window, in pixels at 100 %.</summary>
		const int Gap = 8;

		/// <summary>The pages and windows walked, each made as the program makes it.</summary>
		static readonly Func<Control>[] Surfaces = InputSpacingTest.Surfaces.Concat(new Func<Control>[]
		{
			() => new HelpForm(),
			() => new JocysCom.ClassLibrary.Controls.MessageBoxForm(),
			// The information window, as the program opens it for a window: about that window.
			() => new JocysCom.ClassLibrary.Controls.InfoForm { SelectedControl = new Form { Name = "ExampleForm" } },
			() => new WebBrowserForm(),
			// The main window's pages, the controller page on the first Controller tab as the program builds it, as large as
			// they are in the window at its smallest: less its frame across, and less its title bar, frame, header with a tip
			// of two lines and status bar down, 16 and 132 at 100 %.
			() =>
			{
				var main = MainWindowPages();
				main.BuildPad();
				// Its lists filled before the window is shown, as the main window fills them as it loads.
				main.OptionsPanel.UpdateAppearanceMap();
				var smallest = PadPageShortWindowTest.MinimumWindowSize();
				main.Size = new Size(smallest.Width - (int)Math.Round(16 * ControlsHelper.DpiScale), smallest.Height - (int)Math.Round(132 * ControlsHelper.DpiScale));
				return main;
			},
		}).ToArray();

		/// <summary>The main window's tool bar and pages, made after the cloud client the program starts before them.</summary>
		internal static MainControl MainWindowPages()
		{
			if (Global.CloudClient == null)
				Global.InitializeCloudClient();
			return new MainControl();
		}

		#region One gap

		[TestMethod, TestCategory("ui")]
		[Description("Every window and page, in both themes, keeps one gap, 8 pixels at 100 %, as it is seen: inside every window, tab page, group box and panel with a frame or a colour of its own, the items at a side stand one gap from its inside edge and the first ones one gap in from its left and top, a tool bar's buttons as items of the container round the bar; and items side by side or one above the other stand one gap apart")]
		public void Every_item_stands_one_gap_from_its_container_and_its_neighbours()
		{
			var faults = new SortedDictionary<string, SortedSet<string>>();
			ButtonIconTest.OnEverySurface(Surfaces, (form, when) =>
			{
				// A page is looked at in the window that shows it, at that window's size: alone it has the size it was drawn at.
				if (form.GetType() == typeof(Form) && !form.Controls.OfType<MainControl>().Any())
					return;
				var gap = (int)Math.Round(Gap * ControlsHelper.DpiScale);
				MeasureContainers(form, null, when, gap, faults);
				// A page is looked at as it is seen: its tab, and the tab of every page it stands on, selected.
				foreach (var page in ButtonIconTest.All(form).OfType<TabPage>().ToList())
				{
					for (var c = (Control)page; c != null; c = c.Parent)
						if (c is TabPage shown && shown.Parent is TabControl tabs && tabs.SelectedTab != shown)
							tabs.SelectedTab = shown;
					Application.DoEvents();
					MeasureContainers(form, page, when, gap, faults);
				}
			});
			var lines = faults.Select(x => x.Key + " (" + string.Join(", ", x.Value) + ")").ToList();
			foreach (var line in lines)
				TestContext.WriteLine(line);
			Assert.AreEqual(0, lines.Count, lines.Count + " place(s) not one gap from their container or their neighbours:" + Environment.NewLine + string.Join(Environment.NewLine, lines));
		}

		/// <summary>
		/// An item a person sees, where it stands on the screen, whether a panel it stands in scrolls it, and for a tool bar's
		/// item the side of the bar it keeps to: the room between the two sides is the bar's to share, not a gap.
		/// </summary>
		sealed class Item
		{
			public string Name;
			/// <summary>Where it stands in the layout: a control centred in a row or a tool bar's row as high as the row.</summary>
			public Rectangle Bounds;
			/// <summary>Where it is seen: the control itself, or the box a tool bar holds.</summary>
			public Rectangle Seen;
			public bool ScrolledDown;
			public bool ScrolledAcross;
			public ToolStripItemAlignment? Alignment;
			public bool Button;
			/// <summary>Whether the layout stretches it to the container's right or bottom: its room there is a gap, not free room.</summary>
			public bool ReachesRight;
			public bool ReachesBottom;
		}

		static readonly string[] SideNames = { "left", "top", "right", "bottom" };

		/// <summary>Measures every container shown on a page, or every one outside the window's pages when none is given.</summary>
		static void MeasureContainers(Form form, TabPage page, string when, int gap, IDictionary<string, SortedSet<string>> faults)
		{
			var root = (Control)page ?? form;
			foreach (var container in new[] { root }.Concat(ButtonIconTest.All(root)))
				if (container.Visible && IsContainer(container) && PageOf(container) == page && !InsideBox(container))
					Measure(form, container, when, gap, faults);
		}

		/// <summary>
		/// Adds a fault for each item of a container that does not stand one gap from the inside edge it reaches, or stands
		/// closer than one gap to an edge, and for each pair of neighbours not one gap apart.
		/// </summary>
		static void Measure(Form form, Control container, string when, int gap, IDictionary<string, SortedSet<string>> faults)
		{
			var items = new List<Item>();
			var scrolls = container as ScrollableControl;
			Collect(form, container, items, scrolls != null && scrolls.AutoScroll && scrolls.VerticalScroll.Visible,
				scrolls != null && scrolls.AutoScroll && scrolls.HorizontalScroll.Visible, true, true);
			if (items.Count == 0)
				return;
			var inside = Inside(container);
			var name = container == form ? form.GetType().Name : Path(form, container);
			Action<string> fault = text => Add(faults, name + ": " + text, when);
			// An item near the left or the top stands one gap from it. An item the layout stretches to the right or the bottom
			// stands one gap from it, and any other at least one gap: the rest is free room. What is seen of it keeps at least one
			// gap from every side, whatever row it is centred in. The side a panel scrolls to has no
			// edge to measure from, the side of a panel scrolled down is its scroll bar's, and a tool bar's item faces the bar's
			// free room on the side it does not keep to.
			foreach (var item in items)
			{
				var sides = new[] { item.Bounds.Left - inside.Left, item.Bounds.Top - inside.Top, inside.Right - item.Bounds.Right, inside.Bottom - item.Bounds.Bottom };
				var seen = new[] { item.Seen.Left - inside.Left, item.Seen.Top - inside.Top, inside.Right - item.Seen.Right, inside.Bottom - item.Seen.Bottom };
				for (var i = 0; i < sides.Length; i++)
				{
					if ((i == 2 && (item.ScrolledAcross || item.ScrolledDown || item.Alignment == ToolStripItemAlignment.Left))
						|| (i == 3 && item.ScrolledDown) || (i == 0 && item.Alignment == ToolStripItemAlignment.Right))
						continue;
					var stretched = i == 2 ? item.ReachesRight : i == 3 ? item.ReachesBottom : sides[i] < 2 * gap;
					if (stretched ? Math.Abs(sides[i] - gap) > 1 : sides[i] < gap - 1)
						fault(string.Format("{0} stands {1} from the {2}, not {3}", item.Name, sides[i], SideNames[i], gap));
					else if (seen[i] < gap - 1)
						fault(string.Format("{0} is seen {1} from the {2}, closer than {3}", item.Name, seen[i], SideNames[i], gap));
				}
			}
			Neighbours(items, gap, true, fault, false);
			Neighbours(items, gap, false, fault, false);
			Neighbours(items, gap, true, fault, true);
			Neighbours(items, gap, false, fault, true);
			// A splitter between two panes is their gap: it is one gap wide, and the panes keep no room of their own beside it.
			foreach (var split in ButtonIconTest.All(container).OfType<SplitContainer>())
				if (split.Visible && ContainerOf(split) == container && Math.Abs(split.SplitterWidth - gap) > 1)
					fault(string.Format("{0}'s splitter is {1} wide, not {2}", Path(form, split), split.SplitterWidth, gap));
		}

		/// <summary>The container a person sees round a control: the nearest that is not a table, flow or plain panel.</summary>
		static Control ContainerOf(Control control)
		{
			for (var c = control.Parent; c != null; c = c.Parent)
				if (IsContainer(c))
					return c;
			return null;
		}

		/// <summary>
		/// Adds a fault for each item closer than one gap to the nearest item before it, side by side or one above the other,
		/// and, by where the layout puts them, for each line items start on whose nearest pair is near but not one gap apart:
		/// items in a column of a table start on one line, and the widest or tallest before them sets the gap. Measured by
		/// what is seen, only a pair closer than one gap is a fault: a control centred in a taller row stands further off.
		/// </summary>
		static void Neighbours(List<Item> items, int gap, bool across, Action<string> fault, bool seen)
		{
			Func<Item, Rectangle> at = x => seen ? x.Seen : x.Bounds;
			var pairs = new List<Tuple<Item, Item, int>>();
			foreach (var b in items)
			{
				Item nearest = null;
				foreach (var a in items)
				{
					// The room between a tool bar's two sides is the bar's to share.
					if (a == b || (across && a.Alignment.HasValue && b.Alignment.HasValue && a.Alignment != b.Alignment))
						continue;
					var before = across ? at(a).Right <= at(b).Left + 1 : at(a).Bottom <= at(b).Top + 1;
					var facing = across ? Overlap(at(a).Top, at(a).Bottom, at(b).Top, at(b).Bottom)
						: Overlap(at(a).Left, at(a).Right, at(b).Left, at(b).Right);
					if (before && facing && (nearest == null || (across ? at(a).Right > at(nearest).Right : at(a).Bottom > at(nearest).Bottom)))
						nearest = a;
				}
				if (nearest != null)
					pairs.Add(Tuple.Create(nearest, b, across ? at(b).Left - at(nearest).Right : at(b).Top - at(nearest).Bottom));
			}
			var how = (across ? "side by side" : "one above the other") + (seen ? " as seen" : "");
			foreach (var pair in pairs.Where(x => x.Item3 < gap - 1))
				fault(string.Format("{0} and {1} stand {2} apart {3}, closer than {4}", pair.Item1.Name, pair.Item2.Name, pair.Item3, how, gap));
			if (seen)
				return;
			foreach (var line in pairs.Where(x => x.Item3 >= gap - 1).GroupBy(x => across ? x.Item2.Bounds.Left : x.Item2.Bounds.Top))
			{
				var nearest = line.OrderBy(x => x.Item3).First();
				if (nearest.Item3 < 2 * gap && Math.Abs(nearest.Item3 - gap) > 1)
					fault(string.Format("{0} and {1} stand {2} apart {3}, not {4}", nearest.Item1.Name, nearest.Item2.Name, nearest.Item3, how, gap));
			}
		}

		static bool Overlap(int start1, int end1, int start2, int end2)
		{
			return Math.Min(end1, end2) - Math.Max(start1, start2) > 0;
		}

		/// <summary>
		/// Adds the items a container shows: each control a person sees, a tool bar's buttons rather than the bar, and what the
		/// tables, flows and plain panels that only place them hold. A container inside it is one of its items.
		/// </summary>
		static void Collect(Form form, Control parent, List<Item> items, bool scrolledDown, bool scrolledAcross, bool right, bool bottom)
		{
			foreach (Control child in parent.Controls)
			{
				if (!child.Visible || child is StatusStrip || child is MenuStrip || IsBand(child))
					continue;
				var reachesRight = right && Reaches(child, true);
				var reachesBottom = bottom && Reaches(child, false);
				if (child is ToolStrip strip)
				{
					// Each item stands as high as the bar's row less its margins: a shorter one is centred in it, as a label in
					// a table's row.
					foreach (ToolStripItem item in strip.Items)
						if (item.Available && item.Placement == ToolStripItemPlacement.Main && !(item is ToolStripSeparator))
							items.Add(new Item
							{
								Name = Path(form, strip) + "/" + (string.IsNullOrEmpty(item.Name) ? item.Text : item.Name),
								Bounds = strip.RectangleToScreen(Rectangle.FromLTRB(item.Bounds.Left, strip.DisplayRectangle.Top + item.Margin.Top,
									item.Bounds.Right, strip.DisplayRectangle.Bottom - item.Margin.Bottom)),
								Seen = item is ToolStripControlHost host && host.Control != null
									? host.Control.Parent.RectangleToScreen(host.Control.Bounds) : strip.RectangleToScreen(item.Bounds),
								ScrolledDown = scrolledDown,
								ScrolledAcross = scrolledAcross,
								Alignment = item.Alignment,
								ReachesRight = reachesRight && Math.Abs(item.Bounds.Right + item.Margin.Right - strip.DisplayRectangle.Right) <= 1,
								ReachesBottom = reachesBottom,
							});
					continue;
				}
				if (IsContainer(child) || child is TabControl || IsBox(child) || child.Controls.Count == 0)
				{
					var bounds = Slot(child);
					var seen = child.Parent.RectangleToScreen(child.Bounds);
					// Two buttons side by side with nothing between them, as tall as each other, are one split button: the
					// second opens the first's choices.
					var joined = child is Button ? items.FirstOrDefault(x => x.Button && (x.Bounds.Right == bounds.Left || bounds.Right == x.Bounds.Left)
						&& x.Bounds.Top == bounds.Top && x.Bounds.Bottom == bounds.Bottom) : null;
					if (joined != null)
					{
						joined.Bounds = Rectangle.Union(joined.Bounds, bounds);
						joined.Seen = Rectangle.Union(joined.Seen, seen);
						joined.ReachesRight |= reachesRight;
						joined.ReachesBottom |= reachesBottom;
						continue;
					}
					items.Add(new Item
					{
						Name = Path(form, child),
						Bounds = bounds,
						Seen = seen,
						ScrolledDown = scrolledDown,
						ScrolledAcross = scrolledAcross,
						Button = child is Button,
						ReachesRight = reachesRight,
						ReachesBottom = reachesBottom,
					});
					continue;
				}
				var scrolls = child as ScrollableControl;
				Collect(form, child, items, scrolledDown || (scrolls != null && scrolls.AutoScroll && scrolls.VerticalScroll.Visible),
					scrolledAcross || (scrolls != null && scrolls.AutoScroll && scrolls.HorizontalScroll.Visible), reachesRight, reachesBottom);
			}
		}

		/// <summary>
		/// Whether the layout puts a control's right edge, or its bottom, at its parent's content edge: with the margin a
		/// table or flow keeps round it, or without one where it is docked. A control that ends short of the edge, as its own
		/// size or a table's free room leaves it, faces free room there.
		/// </summary>
		static bool Reaches(Control control, bool right)
		{
			var parent = control.Parent;
			var display = parent.DisplayRectangle;
			var margin = parent is TableLayoutPanel || parent is FlowLayoutPanel ? control.Margin : Padding.Empty;
			return right ? Math.Abs(control.Right + margin.Right - display.Right) <= 1 : Math.Abs(control.Bottom + margin.Bottom - display.Bottom) <= 1;
		}

		/// <summary>
		/// Where an item stands on the screen: in a table, one centred in its row stands as high as the row less its margins,
		/// beside the item that sets the row's height, and one centred in its column as wide as the column; in a flow across,
		/// one centred in its line as high as the line.
		/// </summary>
		static Rectangle Slot(Control control)
		{
			var bounds = control.Bounds;
			var flow = control.Parent as FlowLayoutPanel;
			if (flow != null && (flow.FlowDirection == FlowDirection.LeftToRight || flow.FlowDirection == FlowDirection.RightToLeft)
				&& (control.Anchor & (AnchorStyles.Top | AnchorStyles.Bottom)) == AnchorStyles.None)
			{
				var line = flow.Controls.Cast<Control>().Where(x => x.Visible && Overlap(x.Top, x.Bottom, control.Top, control.Bottom)).ToList();
				bounds = Rectangle.FromLTRB(bounds.Left, line.Min(x => x.Top - x.Margin.Top) + control.Margin.Top,
					bounds.Right, line.Max(x => x.Bottom + x.Margin.Bottom) - control.Margin.Bottom);
			}
			var table = control.Parent as TableLayoutPanel;
			if (table != null && control.Dock == DockStyle.None)
			{
				var place = table.GetPositionFromControl(control);
				var display = table.DisplayRectangle;
				var margin = control.Margin;
				if (place.Row >= 0 && (control.Anchor & (AnchorStyles.Top | AnchorStyles.Bottom)) == AnchorStyles.None)
				{
					var heights = table.GetRowHeights();
					var top = display.Top + heights.Take(place.Row).Sum();
					var height = heights.Skip(place.Row).Take(table.GetRowSpan(control)).Sum();
					bounds = Rectangle.FromLTRB(bounds.Left, top + margin.Top, bounds.Right, top + height - margin.Bottom);
				}
				if (place.Column >= 0 && (control.Anchor & (AnchorStyles.Left | AnchorStyles.Right)) == AnchorStyles.None)
				{
					var widths = table.GetColumnWidths();
					var left = display.Left + widths.Take(place.Column).Sum();
					var width = widths.Skip(place.Column).Take(table.GetColumnSpan(control)).Sum();
					bounds = Rectangle.FromLTRB(left + margin.Left, bounds.Top, left + width - margin.Right, bounds.Bottom);
				}
			}
			return control.Parent.RectangleToScreen(bounds);
		}

		/// <summary>
		/// The inside of a container's frame on the screen: a group box's inside its lines and below its caption, any other's
		/// its client area, a window's below and above the bands across it.
		/// </summary>
		static Rectangle Inside(Control container)
		{
			var inside = container is GroupBox box ? FormsTheme.GetGroupBoxInside(box) : container.ClientRectangle;
			if (container is Form)
				foreach (Control child in container.Controls)
					if (child.Visible && IsBand(child))
						inside = child.Top <= 0
							? Rectangle.FromLTRB(inside.Left, Math.Max(inside.Top, child.Bottom), inside.Right, inside.Bottom)
							: Rectangle.FromLTRB(inside.Left, inside.Top, inside.Right, Math.Min(inside.Bottom, child.Top));
			return container.RectangleToScreen(inside);
		}

		/// <summary>
		/// Whether a control is a container a person sees: a window, a tab page, a group box, or a panel with a frame or a
		/// colour of its own. Tables, flows and plain panels only place what they hold.
		/// </summary>
		static bool IsContainer(Control control)
		{
			if (control is Form || control is TabPage || control is GroupBox)
				return true;
			if (control.Parent == null || !(control is Panel || control is UserControl))
				return false;
			var border = control is Panel panel ? panel.BorderStyle : ((UserControl)control).BorderStyle;
			return border != BorderStyle.None || (control.BackColor.A == 255 && control.BackColor != control.Parent.BackColor);
		}

		/// <summary>
		/// Whether a control is a band across a window, a header, a message's own colour, or a menu or status bar: one that
		/// spans the window's width at its top or bottom, part of the window's frame.
		/// </summary>
		static bool IsBand(Control control)
		{
			var form = control.Parent as Form;
			return form != null && (control is StatusStrip || control is MenuStrip || IsContainer(control))
				&& control.Left <= 0 && control.Right >= form.ClientSize.Width
				&& (control.Top <= 0 || control.Bottom >= form.ClientSize.Height);
		}

		/// <summary>Whether a control is a box, list or grid that places its own parts.</summary>
		static bool IsBox(Control control)
		{
			return control is UpDownBase || control is DataGridView || control is TextBoxBase || control is ComboBox
				|| control is ListControl || control is PropertyGrid || control is WebBrowser;
		}

		/// <summary>Whether a control is a part of a box, list or grid.</summary>
		static bool InsideBox(Control control)
		{
			for (var c = control.Parent; c != null; c = c.Parent)
				if (IsBox(c))
					return true;
			return false;
		}

		/// <summary>The page a control stands on, itself when it is one; none outside the window's pages.</summary>
		static TabPage PageOf(Control control)
		{
			for (var c = control; c != null; c = c.Parent)
				if (c is TabPage page)
					return page;
			return null;
		}

		static void Add(IDictionary<string, SortedSet<string>> faults, string fault, string when)
		{
			SortedSet<string> whens;
			if (!faults.TryGetValue(fault, out whens))
				faults[fault] = whens = new SortedSet<string>();
			whens.Add(when);
		}

		#endregion

		[TestMethod, TestCategory("ui")]
		[Description("The message box lays its buttons out at the screen's scale: 8 from the window's edges and 8 apart at 100 %, 12 and 12 at 150 %, each at least as wide as a dialog's button, and the text 8 beside the icon")]
		public void Message_box_gaps_scale_with_the_screen()
		{
			var faults = new List<string>();
			var measured = false;
			Ui.OnUiThread(() =>
			{
				using (var form = new JocysCom.ClassLibrary.Controls.MessageBoxForm())
				{
					form.StartPosition = FormStartPosition.Manual;
					form.Location = new Point(-5000, -5000);
					form.ShowInTaskbar = false;
					Func<int, int> scaled = x => (int)Math.Round(x * ControlsHelper.DpiScale);
					// Measured once it is shown as the program shows it, then answered.
					form.Shown += (sender, e) =>
					{
						var ok = form.Controls.Find("Button1", true).Single();
						var cancel = form.Controls.Find("Button2", true).Single();
						var icon = form.Controls.Find("IconPictureBox", true).Single();
						var text = form.Controls.Find("TextLabel", true).Single();
						Action<string, int, int, int> check = (what, actual, expected, within) =>
						{
							if (Math.Abs(actual - expected) > within)
								faults.Add(string.Format("{0} is {1}, not {2}.", what, actual, expected));
						};
						check("Cancel's right from the window's right edge", form.ClientSize.Width - cancel.Right, scaled(8), 0);
						check("The gap between OK and Cancel", cancel.Left - ok.Right, scaled(8), 0);
						// Places the window was drawn with, scaled by Windows Forms, round on their own.
						check("Cancel's bottom from the window's bottom edge", form.ClientSize.Height - cancel.Bottom, scaled(8), 1);
						check("The text beside the icon", text.Left - icon.Right, scaled(8), 1);
						foreach (var button in new[] { ok, cancel })
							if (button.Width < button.MinimumSize.Width || button.MinimumSize.Width < scaled(75) - 1)
								faults.Add(string.Format("{0} is {1} wide, less than a dialog's button, {2}.", button.Name, button.Width, scaled(75)));
						measured = true;
						form.DialogResult = DialogResult.Cancel;
					};
					form.ShowForm("Reset current settings to default?", "Reset", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
				}
			});
			Assert.IsTrue(measured, "The message box was not shown.");
			Assert.AreEqual(0, faults.Count, string.Format("At {0:P0}:", ControlsHelper.DpiScale) + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("Every window at its default size, the main window's pages in it, in both themes, shows its texts whole: every drop-down list at least as wide as its widest item needs as the theme draws it, every grid column at least as wide as its header, every check box, radio button and label as large as its text as the theme draws it, and every drop-down list, on a tool bar or not, as tall as the others in its font")]
		public void Every_window_shows_its_texts_whole()
		{
			var faults = new SortedSet<string>();
			ButtonIconTest.OnEverySurface(Surfaces, (form, when) =>
			{
				// A page is looked at in the window that shows it, at that window's size: alone it has the size it was drawn at.
				if (form.GetType() == typeof(Form) && !form.Controls.OfType<MainControl>().Any())
					return;
				var heights = new Dictionary<ComboBox, int>();
				// A property list draws its own parts.
				Action<Control> check = root =>
				{
					foreach (var control in ButtonIconTest.All(root).Where(x => x.Visible && !(x.Parent is PropertyGrid)))
						CheckWhole(form, control, when, faults, heights);
				};
				check(form);
				// A page is looked at as it is seen: its tab, and the tab of every page it stands on, selected.
				foreach (var page in ButtonIconTest.All(form).OfType<TabPage>().ToList())
				{
					for (var c = (Control)page; c != null; c = c.Parent)
						if (c is TabPage shown && shown.Parent is TabControl tabs && tabs.SelectedTab != shown)
							tabs.SelectedTab = shown;
					Application.DoEvents();
					check(page);
				}
				foreach (var font in heights.GroupBy(x => x.Key.Font.Name + " " + x.Key.Font.Size + " " + x.Key.Font.Style))
				{
					var common = font.GroupBy(x => x.Value).OrderByDescending(x => x.Count()).First().Key;
					foreach (var pair in font.Where(x => x.Value != common))
						faults.Add(string.Format("{0} {1}: {2} tall, the other drop-down lists in its font {3}", when, Path(form, pair.Key), pair.Value, common));
				}
			});
			foreach (var line in faults)
				TestContext.WriteLine(line);
			Assert.AreEqual(0, faults.Count, faults.Count + " text(s) not shown whole:" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		/// <summary>
		/// Adds a fault when a control shown cuts what it shows: a drop-down list narrower than its widest item needs, a grid
		/// column narrower than its header, or a check box, radio button or label smaller than its text as the theme draws it,
		/// a label narrower than its text on one line by its text on the lines it wraps to. A label that ends a cut text with
		/// "…" by design is left out; a drop-down list's height is kept to compare with the others.
		/// </summary>
		static void CheckWhole(Form form, Control control, string when, ISet<string> faults, Dictionary<ComboBox, int> heights)
		{
			var path = when + " " + Path(form, control);
			if (control is ComboBox combo)
			{
				heights[combo] = combo.Height;
				var needed = FormsTheme.GetComboBoxWidth(combo);
				if (combo.Items.Count > 0 && combo.Width < needed)
					faults.Add(string.Format("{0}: {1} wide, its widest item needs {2}", path, combo.Width, needed));
			}
			else if (control is DataGridView grid)
			{
				foreach (DataGridViewColumn column in grid.Columns)
				{
					if (!column.Visible || string.IsNullOrEmpty(column.HeaderText))
						continue;
					var needed = column.GetPreferredWidth(DataGridViewAutoSizeColumnMode.ColumnHeader, true);
					if (column.Width < needed)
						faults.Add(string.Format("{0}/{1}: {2} wide, its header needs {3}", path, column.Name, column.Width, needed));
				}
			}
			else if ((control is CheckBox || control is RadioButton) && !string.IsNullOrEmpty(control.Text))
			{
				var button = (ButtonBase)control;
				var text = Drawn(button.Text, button.Font, TextFlags(button.UseMnemonic));
				var needed = new Size(FormsTheme.GetCheckBoxTextLeft(button) + text.Width + button.Padding.Right, text.Height);
				if (button.Width < needed.Width || button.Height < needed.Height)
					faults.Add(string.Format("{0}: {1}, its text needs {2}", path, button.Size, needed));
			}
			else if (control is Label label && !label.AutoEllipsis && !string.IsNullOrEmpty(label.Text))
			{
				var line = FormsTheme.GetLabelWidth(label);
				var width = label.ClientSize.Width - label.Padding.Horizontal;
				var text = label.Width < line
					? Drawn(label.Text, label.Font, TextFlags(label.UseMnemonic) | TextFormatFlags.WordBreak, width)
					: Drawn(label.Text, label.Font, TextFlags(label.UseMnemonic));
				if ((label.Width < line && text.Width > width) || text.Height > label.ClientSize.Height)
					faults.Add(string.Format("{0}: {1}, its text needs {2} on one line, {3} as it is laid out", path, label.Size, line, text));
			}
		}

		/// <summary>The flags the theme measures a control's text with: without Windows' room at its sides.</summary>
		static TextFormatFlags TextFlags(bool useMnemonic)
		{
			return TextFormatFlags.NoPadding | (useMnemonic ? TextFormatFlags.Default : TextFormatFlags.NoPrefix);
		}

		/// <summary>
		/// The size of a text as the theme measures it to draw it: on a device context, each letter as wide as it is drawn, its
		/// lines as tall as its font, without the pixel Windows measures below them.
		/// </summary>
		static Size Drawn(string text, Font font, TextFormatFlags flags, int width = int.MaxValue)
		{
			using (var bitmap = new Bitmap(1, 1))
			using (var g = Graphics.FromImage(bitmap))
			{
				var size = TextRenderer.MeasureText(g, text, font, new Size(width, int.MaxValue), flags);
				var line = TextRenderer.MeasureText(g, " ", font, new Size(int.MaxValue, int.MaxValue), flags | TextFormatFlags.SingleLine);
				return new Size(size.Width, size.Height - Math.Max(0, line.Height - font.Height));
			}
		}

		static string Path(Control root, Control control)
		{
			var names = new List<string>();
			for (var c = control; c != null && c != root; c = c.Parent)
				names.Insert(0, string.IsNullOrEmpty(c.Name) ? c.GetType().Name : c.Name);
			return string.Join("/", names);
		}
	}
}
