// @under-test: App.v4/Controls/*.Designer.cs, App.v4/Forms/*.Designer.cs, Engine/JocysCom/Controls/*.Designer.cs
// @area: interface   @layer: ui
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.Forms;

namespace x360ce.Tests
{
	/// <summary>
	/// Every page and window lays its controls out on the 4-pixel grid of the Options > General spacing: in a layout table or
	/// flow, every margin and padding a multiple of 4; placed by position, every place, and every size the program sets, a
	/// multiple of 4 from the corner of what holds it. Every page has the same room above its content as beside it. Every
	/// window at its default size shows its texts whole, and its drop-down lists at one height.
	/// </summary>
	/// <remarks>
	/// The values are read as the program sets them, after Windows Forms has scaled the window to the screen and before the
	/// theme gives its own room: a group box's or other control's padding that is still Windows Forms' default is the theme's
	/// to set, and is left out. The pages and windows of the steps not yet done are listed in <see cref="NotYetDone"/>; each
	/// step takes its own out of the list, and the last leaves it empty.
	/// </remarks>
	[TestClass]
	public class SpacingGridTest
	{
		public TestContext TestContext { get; set; }

		/// <summary>The pages and windows walked, each made as the program makes it.</summary>
		static readonly Func<Control>[] Surfaces = InputSpacingTest.Surfaces.Concat(new Func<Control>[]
		{
			() => new HelpForm(),
			() => new JocysCom.ClassLibrary.Controls.MessageBoxForm(),
			// The information window, as the program opens it for a window: about that window.
			() => new JocysCom.ClassLibrary.Controls.InfoForm { SelectedControl = new Form { Name = "ExampleForm" } },
			() => new WebBrowserForm(),
			() => MainWindowPages(),
		}).ToArray();

		/// <summary>The main window's tool bar and pages, made after the cloud client the program starts before them.</summary>
		internal static MainControl MainWindowPages()
		{
			if (Global.CloudClient == null)
				Global.InitializeCloudClient();
			return new MainControl();
		}

		/// <summary>The pages and windows of the steps still to come, by the type of control made, and the step that does them.</summary>
		static readonly Dictionary<string, string> NotYetDone = new Dictionary<string, string>
		{
		};

		static readonly PropertyInfo DefaultPaddingProperty = typeof(Control).GetProperty("DefaultPadding", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>Where the header a window shares with others stands in its tree.</summary>
		static readonly string HeaderPath = nameof(BaseFormWithHeader.TopPanel) + "/";

		[TestMethod, TestCategory("ui")]
		[Description("Every page and window of the steps done lays its controls out on the 4-pixel grid at 100 %: in a layout table or flow every margin and padding, placed by position every place and set size, a multiple of 4 in pixels at 100 %, at the screen's scale; the pages of the steps to come are listed, and a listed page already on the grid fails, so the list shrinks")]
		public void Every_page_lays_its_controls_out_on_the_4_pixel_grid()
		{
			var faults = new List<string>();
			var listed = new Dictionary<string, int>();
			OnEverySurface((name, form) =>
			{
				var found = new List<string>();
				Walk(form, form, found);
				// The header a window shares with others is listed by the window it comes from.
				foreach (var part in found.GroupBy(x => form is BaseFormWithHeader && x.StartsWith(HeaderPath) ? nameof(BaseFormWithHeader) : name))
				{
					if (NotYetDone.ContainsKey(part.Key))
						listed[part.Key] = (listed.TryGetValue(part.Key, out var count) ? count : 0) + part.Count();
					else
						faults.AddRange(part.Select(x => part.Key + ": " + x));
					foreach (var line in part)
						TestContext.WriteLine("{0}{1}: {2}", NotYetDone.ContainsKey(part.Key) ? "(step " + NotYetDone[part.Key] + ") " : "", part.Key, line);
				}
				if (form is BaseFormWithHeader && NotYetDone.ContainsKey(nameof(BaseFormWithHeader)) && !listed.ContainsKey(nameof(BaseFormWithHeader)))
					listed[nameof(BaseFormWithHeader)] = 0;
				if (NotYetDone.ContainsKey(name) && !listed.ContainsKey(name))
					listed[name] = 0;
			});
			foreach (var pair in listed.Where(x => x.Value == 0))
				faults.Add(pair.Key + " is on the grid: take it out of the list of pages still to do.");
			Assert.AreEqual(0, faults.Count, faults.Count + " value(s) off the 4-pixel grid:" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("Every page of the steps done has the same room above its content as beside it, measured to the content's layout box, where a group's top is its caption's line: its first content stands as far below the page's top as its content stands in from the page's left; the pages of the steps to come are listed")]
		public void Every_page_has_as_much_room_above_its_content_as_beside_it()
		{
			var faults = new List<string>();
			OnEverySurface((name, form) =>
			{
				foreach (var page in ButtonIconTest.All(form).OfType<TabPage>().ToList())
				{
					// A page is laid out as it is seen: its tab, and the tab of every page it stands on, selected.
					for (var c = (Control)page; c != null; c = c.Parent)
						if (c is TabPage shown && shown.Parent is TabControl tabs && tabs.SelectedTab != shown)
							tabs.SelectedTab = shown;
					Application.DoEvents();
					var start = OptionsLayoutTest.Start(page);
					if (start == null)
						continue;
					var line = string.Format("{0}/{1}: content {2} in from the left and {3} below the top", name, page.Name, start.Value.X, start.Value.Y);
					TestContext.WriteLine("{0}{1}", NotYetDone.ContainsKey(name) ? "(step " + NotYetDone[name] + ") " : "", line);
					// Within a pixel: a page scaled to the screen rounds its spacing to whole pixels its own way.
					if (!NotYetDone.ContainsKey(name) && Math.Abs(start.Value.X - start.Value.Y) > 1)
						faults.Add(line);
				}
			});
			Assert.AreEqual(0, faults.Count, faults.Count + " page(s) with other room above their content than beside it:" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("The message box lays its buttons out at the screen's scale: 12 from the window's right edge and 8 apart at 100 %, 18 and 12 at 150 %, each at least as wide as a dialog's button, and the text 8 beside the icon")]
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
						check("Cancel's right from the window's right edge", form.ClientSize.Width - cancel.Right, scaled(12), 0);
						check("The gap between OK and Cancel", cancel.Left - ok.Right, scaled(8), 0);
						// Places the window was drawn with, scaled by Windows Forms, round on their own.
						check("Cancel's bottom from the window's bottom edge", form.ClientSize.Height - cancel.Bottom, scaled(12), 1);
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

		/// <summary>
		/// Makes each page and window as the program makes it, shows it off the screen, and gives it to <paramref name="check"/>
		/// with the name it is listed by.
		/// </summary>
		static void OnEverySurface(Action<string, Form> check)
		{
			Ui.OnUiThread(() =>
			{
				foreach (var make in Surfaces)
				{
					var made = make();
					var name = Surface(made);
					var form = made as Form ?? new Form { ClientSize = made.Size };
					form.StartPosition = FormStartPosition.Manual;
					form.Location = new Point(-5000, -5000);
					form.ShowInTaskbar = false;
					try
					{
						if (form != made)
						{
							made.Dock = DockStyle.Fill;
							form.Controls.Add(made);
						}
						form.Show();
						Application.DoEvents();
						check(name, form);
					}
					finally
					{
						form.Dispose();
						made.Dispose();
					}
				}
			}, "Walking every page");
		}

		/// <summary>The name a page or window is listed by: the type of the control made, inside the holder that scales a page.</summary>
		static string Surface(Control made)
		{
			var inner = made.GetType() == typeof(UserControl) && made.Controls.Count == 1 ? made.Controls[0] : made;
			return inner.GetType().Name;
		}

		static void Walk(Control root, Control control, List<string> found)
		{
			foreach (Control child in control.Controls)
			{
				var path = Path(root, child);
				var parent = child.Parent;
				var scale = Scale(child);
				var laidOut = parent is TableLayoutPanel || parent is FlowLayoutPanel;
				if (laidOut)
				{
					var margin = child.Margin;
					// A note under a check box starts where the check box's text starts, after the box the theme draws at the
					// scale of its font (OptionsLayoutTest.Notes_under_a_check_box_start_where_its_text_starts).
					if (IsNoteUnderCheckBox(child))
						margin.Left = 0;
					Check(path, "Margin", margin, scale, found);
				}
				var standard = (Padding)(DefaultPaddingProperty?.GetValue(child, null) ?? Padding.Empty);
				if (!IsStandard(child.Padding, standard))
				{
					var padding = child.Padding;
					// A number under its name stands in from the name's edge as far as its digits need to line up with the name's
					// letters, the owner's item 4: 2 at 100 %.
					if (IsNumberUnderName(child))
						padding = Padding.Empty;
					// A table that scrolls keeps Windows' width of its scroll bar free at its right, so its rows never stay wider
					// than what is left once the bar appears.
					if (child is ScrollableControl scrolled && scrolled.AutoScroll && padding.Right == SystemInformation.VerticalScrollBarWidth)
						padding.Right = 0;
					Check(path, "Padding", padding, scale, found);
				}
				// Placed by position: not in a layout panel, not docked, and not a page of a tab strip or a split container's, nor a
				// box a tool bar holds, which the tool bar places as its items.
				if (!laidOut && child.Dock == DockStyle.None && !(child is TabPage) && !(parent is TabControl) && !(parent is SplitContainer)
					&& !(child is SplitterPanel) && !(parent is ToolStrip))
				{
					if (!OnGrid(child.Left, scale.Width) || !OnGrid(child.Top, scale.Height))
						found.Add(string.Format("{0} Location {1}", path, child.Location));
					var across = (child.Anchor & (AnchorStyles.Left | AnchorStyles.Right)) == (AnchorStyles.Left | AnchorStyles.Right);
					var down = (child.Anchor & (AnchorStyles.Top | AnchorStyles.Bottom)) == (AnchorStyles.Top | AnchorStyles.Bottom);
					if (!child.AutoSize && ((!across && !OnGrid(child.Width, scale.Width)) || (!down && !OnGrid(child.Height, scale.Height))))
						found.Add(string.Format("{0} Size {1}", path, child.Size));
				}
				// A box, list or grid places its own parts (an up-down's edit and buttons, a grid's scroll bars, a property list's
				// description and tool bar), and a tool bar its boxes.
				if (!(child is UpDownBase || child is DataGridView || child is TextBoxBase || child is ComboBox || child is ListControl
					|| child is PropertyGrid || child is ToolStrip))
					Walk(root, child, found);
			}
		}

		/// <summary>Whether a control is a number under its name: a label docked under a label docked above it in a panel of their own.</summary>
		static bool IsNumberUnderName(Control control)
		{
			return control is Label && control.Dock == DockStyle.Bottom && control.Parent?.GetType() == typeof(Panel)
				&& control.Parent.Controls.OfType<Label>().Any(x => x != control && x.Dock == DockStyle.Top);
		}

		/// <summary>Whether a control is a label in a table's cell right under a check box or radio button.</summary>
		static bool IsNoteUnderCheckBox(Control control)
		{
			var table = control.Parent as TableLayoutPanel;
			if (!(control is Label) || table == null)
				return false;
			var place = table.GetPositionFromControl(control);
			var above = place.Row > 0 ? table.GetControlFromPosition(place.Column, place.Row - 1) : null;
			return above is CheckBox || above is RadioButton;
		}

		static void Check(string path, string what, Padding value, SizeF scale, List<string> found)
		{
			if (!OnGrid(value.Left, scale.Width) || !OnGrid(value.Top, scale.Height) || !OnGrid(value.Right, scale.Width) || !OnGrid(value.Bottom, scale.Height))
				found.Add(string.Format("{0} {1} {2},{3},{4},{5}", path, what, value.Left, value.Top, value.Right, value.Bottom));
		}

		/// <summary>
		/// Whether a size in pixels on the screen is a multiple of 4 in pixels at 100 %: device pixels x 96 / the screen's DPI,
		/// or, along the axis given, divided by the factor Windows Forms scaled the control's window or page by. Windows Forms
		/// rounds each value it scales to a whole pixel, so within a pixel.
		/// </summary>
		static bool OnGrid(int pixels, float axis)
		{
			Func<double, bool> on = factor =>
			{
				var steps = Math.Round(pixels / (4 * factor));
				return Math.Abs(pixels - steps * 4 * factor) < 1;
			};
			return on(ControlsHelper.DpiScale) || on(axis);
		}

		/// <summary>
		/// The factors Windows Forms scaled a control by, across and down: a window or page scaled by its font is scaled by how
		/// much larger its font is than the one it was drawn in (6 x 13 pixels a character), which down is not quite the
		/// screen's scale; any other by the screen's.
		/// </summary>
		static SizeF Scale(Control control)
		{
			for (var c = control.Parent; c != null; c = c.Parent)
			{
				var scaled = c as ContainerControl;
				if (scaled == null || scaled.AutoScaleMode == AutoScaleMode.Inherit)
					continue;
				if (scaled.AutoScaleMode != AutoScaleMode.Font)
					break;
				var current = scaled.CurrentAutoScaleDimensions;
				return new SizeF(current.Width / 6F, current.Height / 13F);
			}
			return new SizeF(ControlsHelper.DpiScale, ControlsHelper.DpiScale);
		}

		/// <summary>
		/// Whether a padding is still Windows Forms' default for its control, as made or as Windows Forms scaled it to the screen,
		/// which it rounds up on some sides and down on others: the theme's to set.
		/// </summary>
		static bool IsStandard(Padding value, Padding standard)
		{
			var scale = ControlsHelper.DpiScale;
			Func<int, int, bool> scaled = (own, made) => own == made
				|| own == (int)Math.Floor(made * scale) || own == (int)Math.Ceiling(made * scale);
			return scaled(value.Left, standard.Left) && scaled(value.Top, standard.Top)
				&& scaled(value.Right, standard.Right) && scaled(value.Bottom, standard.Bottom);
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
