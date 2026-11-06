// @under-test: App.v4/Controls/OptionsUserControl.Designer.cs, Engine/JocysCom/IO/HardwareControl.Designer.cs, App.v4/Controls, App.v4/Forms
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.App.Forms;

namespace x360ce.Tests
{
	/// <summary>
	/// Text boxes, drop-down lists and number boxes that stand one under another on every page and window: at least as far
	/// apart as a layout table stands them, and clear of each other and of the text beside them.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The theme makes a box as tall as the WPF programs make it, from its font, which is taller than Windows Forms makes it.
	/// Boxes placed by position a row of Windows' boxes apart lose that much of the gap between them, and touch.
	/// </para>
	/// <para>
	/// Boxes in a layout table stand their margins apart, Windows Forms' 3 pixels a side, as the buttons and sliders in the
	/// same rows do: 6 pixels at 100 % and 10 at 150 %. A row with a button or a slider is taller than a row of boxes, so a
	/// stack is allowed to stand further apart, never closer.
	/// </para>
	/// </remarks>
	[TestClass]
	public class InputSpacingTest
	{
		public TestContext TestContext { get; set; }

		/// <summary>The pages and windows the picture test makes, with the dialogs and pages of their own that show a box.</summary>
		/// <remarks>The Cloud page, made only by the running program, shows no box.</remarks>
		static readonly Func<Control>[] Surfaces = ButtonIconTest.Surfaces.Concat(new Func<Control>[]
		{
			() => new AboutControl(),
			() => new MapDeviceToControllerForm(),
			() => new DeveloperToolsForm(),
			() => new PromptForm(),
			() => new DebugForm(),
			() => new HardwareForm(),
			() => new ErrorReportForm(),
		}).Select(make => (Func<Control>)(() => OnMainWindow(make()))).ToArray();

		/// <summary>
		/// A page that takes its scale from the window it is on, held as the main window holds it: by a control that scales
		/// what it holds by how much larger the font is than the one the main window was drawn with. Any other page or window
		/// scales itself, and is returned as it is.
		/// </summary>
		static Control OnMainWindow(Control made)
		{
			var page = made as ContainerControl;
			if (page == null || made is Form || page.AutoScaleMode != AutoScaleMode.Inherit)
				return made;
			var holder = new UserControl();
			holder.SuspendLayout();
			holder.Size = made.Size;
			made.Dock = DockStyle.Fill;
			holder.Controls.Add(made);
			holder.AutoScaleDimensions = new SizeF(6F, 13F);
			holder.AutoScaleMode = AutoScaleMode.Font;
			holder.ResumeLayout(false);
			return holder;
		}

		sealed class Pair
		{
			public string When;
			public string Page;
			public string Upper;
			public string Lower;
			public int Gap;
			/// <summary>In a layout table, the control other than a box that sets the height of the lower box's row, if any.</summary>
			public string RowSetBy;
		}

		[TestMethod, TestCategory("ui")]
		[Description("Boxes stacked one under another on every page and window are at least as far apart as a layout table stands them, and clear of each other and of the text beside them, at 100 % and 150 %, in both themes")]
		public void Stacked_boxes_stand_at_least_as_far_apart_as_a_layout_table_stands_them()
		{
			var pairs = new List<Pair>();
			var covered = new List<string>();
			var gaps = new Dictionary<string, int>();
			ButtonIconTest.OnEverySurface(Surfaces, (form, when) =>
			{
				if (!gaps.ContainsKey(when))
					gaps[when] = TableGap(form);
				OnEveryPage(form, container => Measure(container, when, pairs, covered));
			});
			var faults = new List<string>(covered);
			foreach (var group in pairs.GroupBy(x => x.When))
			{
				var least = gaps[group.Key];
				TestContext.WriteLine("{0}: a layout table stands boxes {1} px apart", group.Key, least);
				foreach (var pair in group)
				{
					var line = string.Format("{0} {1}: {2} over {3}, {4} px apart, a table's {5} px{6}",
						pair.When, pair.Page, pair.Upper, pair.Lower, pair.Gap, least,
						pair.RowSetBy == null ? "" : ", the row's height set by " + pair.RowSetBy);
					TestContext.WriteLine(line);
					if (pair.Gap < least)
						faults.Add(line);
				}
			}
			foreach (var line in covered)
				TestContext.WriteLine(line);
			Assert.AreEqual(0, faults.Count, faults.Count + " box(es) are off:" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		/// <summary>The left column of the General tab's boxes, by part, in its groups, each top to bottom.</summary>
		static readonly string[][] GeneralLeft =
		{
			new[] { "LeftTrigger", "LeftShoulder", "ButtonBack", "ButtonStart", "ButtonGuide", "DPad" },
			new[] { "LeftThumbAxisX", "LeftThumbAxisY", "LeftThumbButton" },
			new[] { "LeftThumbUp", "LeftThumbLeft", "LeftThumbRight", "LeftThumbDown" },
		};

		/// <summary>The right column, row for row beside the left one.</summary>
		static readonly string[][] GeneralRight =
		{
			new[] { "RightTrigger", "RightShoulder", "ButtonY", "ButtonX", "ButtonB", "ButtonA" },
			new[] { "RightThumbAxisX", "RightThumbAxisY", "RightThumbButton" },
			new[] { "RightThumbUp", "RightThumbLeft", "RightThumbRight", "RightThumbDown" },
		};

		/// <summary>The centre's D-Pad rows, under the controller picture, row for row beside each column's last group.</summary>
		static readonly string[][] GeneralCentre = { new[] { "DPadUp", "DPadLeft", "DPadRight", "DPadDown" } };

		[TestMethod, TestCategory("ui")]
		[Description("On the General tab the boxes of a group stand as far apart as a layout table stands them, the groups twice as far, and each row's boxes stand level across the left, centre and right columns, at 100 % and 150 %, in both themes")]
		public void General_tab_boxes_stand_a_table_gap_apart_and_level_across_the_columns()
		{
			var faults = new List<string>();
			ButtonIconTest.OnEverySurface(new Func<Control>[] { () => new PadControl(x360ce.Engine.MapTo.Controller1) }, (form, when) =>
			{
				var gap = TableGap(form);
				var page = form.Controls.Find("GeneralTabPage", true).Single();
				Func<string, Rectangle> at = part =>
				{
					var box = form.Controls.Find(part + "ComboBox", true).Single();
					return page.RectangleToClient(box.Parent.RectangleToScreen(box.Bounds));
				};
				TestContext.WriteLine("{0}: a layout table stands boxes {1} px apart, twice that is {2}", when, gap, gap * 2);
				foreach (var column in new[] { GeneralLeft, GeneralCentre, GeneralRight })
				{
					var parts = column.SelectMany(x => x).ToList();
					for (var i = 1; i < parts.Count; i++)
					{
						var between = column.Any(x => x[0] == parts[i]);
						var wanted = between ? gap * 2 : gap;
						var apart = at(parts[i]).Top - at(parts[i - 1]).Bottom;
						var line = string.Format("{0}: {1} over {2}, {3} px apart{4}, wanted {5}",
							when, parts[i - 1], parts[i], apart, between ? " between groups" : "", wanted);
						TestContext.WriteLine(line);
						if (apart != wanted)
							faults.Add(line);
					}
				}
				for (var g = 0; g < GeneralLeft.Length; g++)
					for (var i = 0; i < GeneralLeft[g].Length; i++)
					{
						var row = new List<string> { GeneralLeft[g][i], GeneralRight[g][i] };
						if (g == GeneralLeft.Length - 1)
							row.Insert(1, GeneralCentre[0][i]);
						var tops = row.Select(x => at(x).Top).ToList();
						if (tops.Distinct().Count() > 1)
							faults.Add(string.Format("{0}: the row of {1} is not level: its boxes stand at {2}.",
								when, string.Join(", ", row), string.Join(", ", tops)));
					}
			});
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("The Tab key moves through the General tab as the eye reads it: Remap All, the map name, the left column top to bottom, the centre's D-Pad rows, then the right column")]
		public void General_tab_tab_order_follows_the_eye()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				using (var pad = new PadControl(x360ce.Engine.MapTo.Controller1) { Dock = DockStyle.Fill })
				{
					form.ClientSize = pad.Size;
					form.Controls.Add(pad);
					form.Show();
					Application.DoEvents();
					var page = pad.Controls.Find("GeneralTabPage", true).Single();
					var stops = new List<string>();
					for (var c = page.GetNextControl(page, true); c != null; c = page.GetNextControl(c, true))
						if (c.TabStop && (c is ButtonBase || c is ComboBox))
							stops.Add(c.Name);
					var expected = new[] { "RemapAllButton", "MapNameComboBox" }
						.Concat(new[] { GeneralLeft, GeneralCentre, GeneralRight }.SelectMany(x => x).SelectMany(x => x).Select(x => x + "ComboBox"))
						.ToList();
					CollectionAssert.AreEqual(expected, stops, "The Tab key goes: " + string.Join(", ", stops));
				}
			});
		}

		/// <summary>
		/// How far apart a layout table stands two boxes in its rows, with Windows Forms' margins, on a page scaled as the main
		/// window scales one, in the theme in use, on the monitor <paramref name="beside"/> is drawn on.
		/// </summary>
		/// <param name="beside">A window shown; the table's window is shown where it is and owned by it, so it has its scale.</param>
		static int TableGap(Form beside)
		{
			using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = beside.Location, ShowInTaskbar = false, Owner = beside })
			{
				var table = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, RowCount = 2 };
				var upper = new ComboBox();
				var lower = new ComboBox();
				table.Controls.Add(upper, 0, 0);
				table.Controls.Add(lower, 0, 1);
				var page = new UserControl();
				page.SuspendLayout();
				page.Controls.Add(table);
				page.AutoScaleDimensions = new SizeF(6F, 13F);
				page.AutoScaleMode = AutoScaleMode.Font;
				page.ResumeLayout(false);
				form.Controls.Add(page);
				JocysCom.ClassLibrary.Controls.Themes.FormsTheme.Apply(form);
				form.Show();
				Application.DoEvents();
				return lower.Top - upper.Bottom;
			}
		}

		/// <summary>
		/// Hands every container on a window to <paramref name="measure"/> with its pages shown, one page of each tab strip at a
		/// time: a box on a page that is not in front is not laid out and reads as hidden.
		/// </summary>
		static void OnEveryPage(Control root, Action<Control> measure)
		{
			var tabs = new List<TabControl>();
			foreach (var container in Containers(root, tabs))
				measure(container);
			foreach (var strip in tabs)
				foreach (TabPage page in strip.TabPages)
				{
					strip.SelectedTab = page;
					Application.DoEvents();
					OnEveryPage(page, measure);
				}
		}

		/// <summary>A control and every control under it, stopping at each tab strip, which is added to <paramref name="tabs"/>.</summary>
		static IEnumerable<Control> Containers(Control root, List<TabControl> tabs)
		{
			yield return root;
			foreach (Control child in root.Controls)
			{
				if (child is TabControl strip)
				{
					tabs.Add(strip);
					continue;
				}
				foreach (var deeper in Containers(child, tabs))
					yield return deeper;
			}
		}

		/// <summary>Whether a control is a box a person types in or picks from: a text box, drop-down list, number box or date box.</summary>
		/// <remarks>The text box inside a number box is part of it, and a rich text box shows a document, not a value.</remarks>
		static bool IsBox(Control control)
		{
			return (control is TextBoxBase && !(control is RichTextBox) && !(control.Parent is UpDownBase))
				|| control is ComboBox || control is UpDownBase || control is DateTimePicker;
		}

		/// <summary>
		/// Records each pair of boxes in a container that stand one under the other, with room for no other row between them,
		/// and each box that covers another box or the text of a label or check box beside it.
		/// </summary>
		static void Measure(Control container, string when, List<Pair> pairs, List<string> covered)
		{
			var shown = container.Controls.Cast<Control>().Where(x => x.Visible && x.Width > 0 && x.Height > 0).ToList();
			var boxes = shown.Where(IsBox).ToList();
			if (boxes.Count == 0)
				return;
			var page = Page(container);
			foreach (var upper in boxes)
				foreach (var lower in boxes)
				{
					if (lower == upper || lower.Top <= upper.Top)
						continue;
					var across = Math.Min(upper.Right, lower.Right) - Math.Max(upper.Left, lower.Left);
					var gap = lower.Top - upper.Bottom;
					if (across <= 0 || gap >= Math.Min(upper.Height, lower.Height))
						continue;
					// Two boxes with a caption or another control between them are two rows apart, not a stack.
					var between = Rectangle.FromLTRB(Math.Min(upper.Left, lower.Left), upper.Bottom, Math.Max(upper.Right, lower.Right), lower.Top);
					if (gap > 0 && shown.Any(x => x != upper && x != lower && x.Bounds.IntersectsWith(between)))
						continue;
					pairs.Add(new Pair
					{
						When = when,
						Page = page,
						Upper = upper.Name,
						Lower = lower.Name,
						Gap = gap,
						RowSetBy = RowSetBy(container as TableLayoutPanel, lower),
					});
				}
			for (var i = 0; i < boxes.Count; i++)
				foreach (var other in shown)
				{
					var box = boxes[i];
					// Each pair of boxes once.
					if (other == box || boxes.IndexOf(other) > i)
						continue;
					var text = IsBox(other) ? other.Bounds : TextBounds(other);
					var over = Rectangle.Intersect(box.Bounds, text);
					if (over.Width > 0 && over.Height > 0)
						covered.Add(string.Format("{0} {1}: {2} {3} covers {4} {5} by {6}x{7} px",
							when, page, box.Name, box.Bounds, other.Name, text, over.Width, over.Height));
				}
		}

		/// <summary>
		/// The control, other than a box, that a layout table's row holding a box is as tall as, with its margins; null when a
		/// box sets it, or the box is not in a table.
		/// </summary>
		static string RowSetBy(TableLayoutPanel table, Control box)
		{
			if (table == null)
				return null;
			var row = table.GetPositionFromControl(box).Row;
			var tallest = table.Controls.Cast<Control>()
				.Where(x => x.Visible && table.GetPositionFromControl(x).Row == row && table.GetRowSpan(x) == 1)
				.OrderByDescending(x => x.Height + x.Margin.Vertical).FirstOrDefault();
			return tallest == null || IsBox(tallest) || tallest.Height + tallest.Margin.Vertical <= box.Height + box.Margin.Vertical
				? null
				: string.Format("{0} {1} with margins {2}", tallest.Name, tallest.Size, tallest.Margin);
		}

		/// <summary>
		/// Where the text of a label, or the box and text of a check box or option button, is drawn; empty for any other control
		/// and for one without text.
		/// </summary>
		static Rectangle TextBounds(Control control)
		{
			if (string.IsNullOrEmpty(control.Text) || !(control is Label || control is CheckBox || control is RadioButton))
				return Rectangle.Empty;
			var inside = new Rectangle(control.Left + control.Padding.Left, control.Top + control.Padding.Top,
				control.Width - control.Padding.Horizontal, control.Height - control.Padding.Vertical);
			// A label that sizes itself is as wide as its text and taller than its line; another wraps its text to its width.
			var size = !(control is Label label) ? control.GetPreferredSize(Size.Empty) - control.Padding.Size
				: label.AutoSize ? new Size(label.PreferredWidth - label.Padding.Horizontal, TextRenderer.MeasureText(label.Text, label.Font).Height)
				: TextRenderer.MeasureText(label.Text, label.Font, new Size(inside.Width, int.MaxValue), TextFormatFlags.WordBreak);
			size = new Size(Math.Min(size.Width, inside.Width), Math.Min(size.Height, inside.Height));
			var align = control is Label l ? l.TextAlign : ((ButtonBase)control).TextAlign;
			var right = align == ContentAlignment.TopRight || align == ContentAlignment.MiddleRight || align == ContentAlignment.BottomRight;
			var centre = align == ContentAlignment.TopCenter || align == ContentAlignment.MiddleCenter || align == ContentAlignment.BottomCenter;
			var middle = align == ContentAlignment.MiddleLeft || align == ContentAlignment.MiddleCenter || align == ContentAlignment.MiddleRight;
			var bottom = align == ContentAlignment.BottomLeft || align == ContentAlignment.BottomCenter || align == ContentAlignment.BottomRight;
			// A check box's own box stands at its left, before the text, whichever way the text is aligned.
			if (!(control is Label))
				right = centre = false;
			var x = right ? inside.Right - size.Width : centre ? inside.Left + (inside.Width - size.Width) / 2 : inside.Left;
			var y = bottom ? inside.Bottom - size.Height : middle ? inside.Top + (inside.Height - size.Height) / 2 : inside.Top;
			return new Rectangle(x, y, size.Width, size.Height);
		}

		/// <summary>The page or window a container is on, and the tabs chosen to show it, such as "PadControl > General".</summary>
		static string Page(Control container)
		{
			var names = new List<string>();
			var top = container;
			for (var c = container; c != null; c = c.Parent)
			{
				if (c is TabPage tab)
					names.Insert(0, tab.Text);
				// A page is shown in a plain window of its own, some held by a plain control; a window is the one made.
				if (c.GetType() != typeof(Form) && c.GetType() != typeof(UserControl))
					top = c;
			}
			names.Insert(0, top.GetType().Name);
			var path = ButtonIconTest.Path(container);
			return string.Join(" > ", names) + (string.IsNullOrEmpty(path) ? "" : " (" + path + ")");
		}
	}
}
