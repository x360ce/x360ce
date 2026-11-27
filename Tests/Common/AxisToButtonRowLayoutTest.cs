// @under-test: App.v4/Controls/AxisToButtonUserControl.Designer.cs, App.v4/Controls/AxisToButtonUserControl.cs, App.v4/Common/AppHelper.cs, App.v4/Controls/PadControl.Designer.cs
// @area: ui   @layer: unit
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

namespace x360ce.Tests
{
	/// <summary>
	/// One row of the Buttons page: mapped axis, arrow, button, name, slider, percent, value.
	/// </summary>
	/// <remarks>
	/// The page that holds the rows was drawn at 150 % and hands each row a size in those units;
	/// the row was drawn at 100 % and kept its slider and boxes by their right edge. Given the
	/// larger size the three moved right; scaled back down they stayed there, so on the Buttons
	/// tab every slider sat past its row's middle and the percent and value boxes were cut off
	/// entirely. The row now lays itself out as a grid, so whatever size it is given the slider
	/// takes what is left and the boxes end at the right edge.
	/// </remarks>
	[TestClass]
	public class AxisToButtonRowLayoutTest
	{
		public TestContext TestContext { get; set; }

		/// <summary>
		/// A page built the way the Buttons page's designer code builds it: drawn at 150 %, giving
		/// the row its size in those units, then brought to the running font by auto-scaling.
		/// </summary>
		static UserControl PageHolding(AxisToButtonUserControl row, float zoom)
		{
			var page = new UserControl();
			var group = new GroupBox();
			// The page suspends its own containers only; a row lays itself out as it is sized.
			page.SuspendLayout();
			// The screen's zoom reaches the controls as a larger font; that is what auto-scaling reads.
			// This screen's own zoom is taken out, so a zoom means the same on any screen.
			page.Font = new Font(page.Font.FontFamily, page.Font.Size * zoom / JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale);
			group.SuspendLayout();
			group.Controls.Add(row);
			row.Location = new Point(9, 29);
			row.Size = new Size(843, 43);
			group.Location = new Point(4, 5);
			group.Size = new Size(1115, 662);
			page.Controls.Add(group);
			page.AutoScaleDimensions = new SizeF(9F, 20F);
			page.AutoScaleMode = AutoScaleMode.Font;
			page.Size = new Size(1140, 714);
			group.ResumeLayout(false);
			page.ResumeLayout(false);
			return page;
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("Every part of a row is inside it and clear of the others, however the row is sized")]
		public void Every_part_of_the_row_stays_inside_it()
		{
			Ui.OnUiThread(() =>
			{
				// The zooms Windows offers, 100 % first; the page was drawn at 150 %.
				foreach (var zoom in new[] { 1f, 1.25f, 1.5f, 2f })
				{
					var row = new AxisToButtonUserControl();
					using (var page = PageHolding(row, zoom))
					{
						page.CreateControl();
						AssertLaidOut(row, string.Format("at {0:P0}, sized by the page ({1} wide)", zoom, row.Width));
						// The drawn width, and the widths a wider window brings, each at this zoom.
						foreach (var width in new[] { 554, 843, 1200 }.Select(w => (int)(w * zoom)))
						{
							row.Width = width;
							row.PerformLayout();
							AssertLaidOut(row, string.Format("at {0:P0}, {1} wide", zoom, width));
						}
					}
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("On a Controller tab's Buttons page, in the program's font, each row is as tall as its tallest content with a control's margin above and below it, so the rows stand a row's gap, 8 at 100 %, apart between their tallest content; the button's picture is drawn at its own size; all 14 rows are shown whole on the page below its note, the last one the page's room above its bottom; and a row made taller by what holds it goes back to that height")]
		public void Rows_stand_a_row_apart_between_their_tallest_content()
		{
			Ui.OnUiThread(() =>
			{
				try
				{
					AppFonts.Apply(new Options());
					FormsTheme.SetTheme(ThemeType.Light);
					var pad = new PadControl(x360ce.Engine.MapTo.Controller1) { Dock = DockStyle.Fill };
					using (var form = OptionsLayoutTest.ThemedWindow(pad))
					{
						pad.PadTabControl.SelectedTab = pad.ButtonsTabPage;
						Application.DoEvents();
						var rows = ButtonIconTest.All(pad.ButtonsTabPage).OfType<AxisToButtonUserControl>().OrderBy(x => x.Top).ToList();
						Assert.AreEqual(14, rows.Count, "The Buttons page has " + rows.Count + " rows, not 14.");
						var gap = (int)Math.Round(8 * ControlsHelper.DpiScale);
						var faults = new List<string>();
						foreach (var row in rows)
						{
							// What the row shows, but the slider, which stretches to the row.
							var tallest = Parts(row).Where(x => x.Name != nameof(AxisToButtonUserControl.DeadZoneTrackBar)).Max(x => x.Height);
							if (row.Height != tallest + gap)
								faults.Add(string.Format("{0} is {1} tall, not its tallest content, {2}, and a row's gap, {3}.", row.Name, row.Height, tallest, gap));
							var picture = row.ButtonImagePictureBox;
							var size = (int)Math.Round(24 * ControlsHelper.DpiScale);
							if (picture.Width != size || picture.Height != size)
								faults.Add(string.Format("{0}'s picture is {1}, not {2} square.", row.Name, picture.Size, size));
						}
						faults.AddRange(rows.Skip(1).Select((x, i) => new { Row = x, Gap = x.Top - rows[i].Bottom })
							.Where(x => x.Gap != 0).Select(x => x.Row.Name + " stands " + x.Gap + " px below the row above it."));
						var page = pad.ButtonsTabPage;
						var top = page.RectangleToClient(rows[0].RectangleToScreen(rows[0].ClientRectangle));
						var last = page.RectangleToClient(rows.Last().RectangleToScreen(rows.Last().ClientRectangle));
						var note = pad.AxisToButtonNoteLabel;
						var noteAt = page.RectangleToClient(note.Parent.RectangleToScreen(note.Bounds));
						// The page's room below the last row, as beside it.
						var below = (int)Math.Round(8 * ControlsHelper.DpiScale);
						if (top.Top < noteAt.Bottom || last.Bottom + below > page.ClientSize.Height)
							faults.Add(string.Format("The note ends at {0}, the rows start at {1} and end at {2}, the page at {3}: not every row is shown whole below the note, {4} above the page's bottom.",
								noteAt.Bottom, top.Top, last.Bottom, page.ClientSize.Height, below));
						var first = rows[0];
						var height = first.Height;
						first.Height = height * 3;
						first.PerformLayout();
						if (first.Height != height)
							faults.Add("Made taller, " + first.Name + " stays " + first.Height + " tall, not " + height + ".");
						Assert.AreEqual(0, faults.Count, string.Join(Environment.NewLine, faults));
					}
				}
				finally
				{
					FormsTheme.SetFonts(null, null);
				}
			});
		}

		[TestMethod, TestCategory("ui")]
		[Description("Rows with different button names start their sliders on the same line")]
		public void Sliders_line_up_across_rows()
		{
			Ui.OnUiThread(() =>
			{
				using (var a = new AxisToButtonUserControl { GamepadButton = SharpDX.XInput.GamepadButtonFlags.A })
				using (var b = new AxisToButtonUserControl { GamepadButton = SharpDX.XInput.GamepadButtonFlags.RightThumb })
				{
					// The names' column is sized by the names as they are drawn, which the program draws with GDI.
					OptionsLayoutTest.DrawTextAsTheProgram(a);
					OptionsLayoutTest.DrawTextAsTheProgram(b);
					a.CreateControl();
					b.CreateControl();
					a.Width = b.Width = 554;
					a.PerformLayout();
					b.PerformLayout();
					var sliderA = Part(a, nameof(AxisToButtonUserControl.DeadZoneTrackBar));
					var sliderB = Part(b, nameof(AxisToButtonUserControl.DeadZoneTrackBar));
					Assert.AreEqual(sliderA.Left, sliderB.Left, "'A Button' and 'Right Stick Button' rows start their sliders at different places.");
				}
			});
		}

		[TestMethod, TestCategory("ui")]
		[Description("The letter on the A, B, X and Y pictures stands out from the button's colour, at a contrast of 1.5 or more, in both themes, shown and greyed out, as the page draws them on its colour")]
		public void Face_button_letters_stand_out_from_their_colour()
		{
			var faults = new List<string>();
			var report = new List<string>();
			ButtonIconTest.OnEverySurface(new Func<Control>[] { () => new AxisToButtonUserControl { GamepadButton = SharpDX.XInput.GamepadButtonFlags.A } }, (window, when) =>
			{
				var page = window.BackColor;
				var pictures = new[] { Tuple.Create("A", App.Properties.Resources.Button_A), Tuple.Create("B", App.Properties.Resources.Button_B),
					Tuple.Create("X", App.Properties.Resources.Button_X), Tuple.Create("Y", App.Properties.Resources.Button_Y) };
				foreach (var picture in pictures)
					foreach (var disabled in new[] { false, true })
						using (var drawn = new Bitmap(picture.Item2.Width, picture.Item2.Height))
						{
							using (var g = Graphics.FromImage(drawn))
							{
								g.Clear(page);
								g.DrawImageUnscaled(disabled ? AppHelper.GetDisabledImage(picture.Item2) : picture.Item2, 0, 0);
							}
							var contrast = LetterContrast(drawn);
							var line = string.Format("{0}, {1} {2}: letter to colour {3:0.00}", when, picture.Item1, disabled ? "greyed out" : "shown", contrast);
							report.Add(line);
							if (contrast < 1.5)
								faults.Add(line);
						}
			});
			foreach (var line in report)
				TestContext.WriteLine(line);
			Assert.AreEqual(0, faults.Count, faults.Count + " letter(s) do not stand out:" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		/// <summary>
		/// The contrast between a face button's letter and its colour, as WCAG 2 counts it: inside the ring, the commonest grey,
		/// which is the letter, against the commonest colour that is not a grey, which is the button's.
		/// </summary>
		static double LetterContrast(Bitmap drawn)
		{
			var middle = drawn.Width / 2.0;
			var inside = middle * 0.6;
			var greys = new Dictionary<int, int>();
			var colours = new Dictionary<int, int>();
			for (var y = 0; y < drawn.Height; y++)
				for (var x = 0; x < drawn.Width; x++)
				{
					if (Math.Pow(x + 0.5 - middle, 2) + Math.Pow(y + 0.5 - middle, 2) > inside * inside)
						continue;
					var c = drawn.GetPixel(x, y);
					var chroma = Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B));
					var counts = chroma <= 8 ? greys : chroma >= 24 ? colours : null;
					if (counts == null)
						continue;
					int n;
					counts[c.ToArgb()] = counts.TryGetValue(c.ToArgb(), out n) ? n + 1 : 1;
				}
			Func<Dictionary<int, int>, Color> commonest = counts => Color.FromArgb(counts.OrderByDescending(x => x.Value).First().Key);
			Func<Color, double> luminance = c =>
			{
				Func<int, double> channel = v => { var s = v / 255.0; return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); };
				return 0.2126 * channel(c.R) + 0.7152 * channel(c.G) + 0.0722 * channel(c.B);
			};
			if (greys.Count == 0 || colours.Count == 0)
				return 0;
			var a = luminance(commonest(greys));
			var b = luminance(commonest(colours));
			return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
		}

		/// <summary>
		/// The arrow is a flat grey triangle. Brought down from its picture with a filter that sharpens, it gets a ring of
		/// lighter grey just inside its edge and darker grey outside, which reads as a border, and at 100 % a hole at its tip.
		/// </summary>
		[TestMethod, TestCategory("ui")]
		[Description("The arrow beside a button is its picture's grey with no ring at its edge: across each of its rows the colour only deepens from the page's to the arrow's and fades back, in both themes, at the screen's scale")]
		public void Arrow_has_no_ring_at_its_edge()
		{
			var faults = new List<string>();
			var rows = 0;
			ButtonIconTest.OnEverySurface(new Func<Control>[] { () => new AxisToButtonUserControl { GamepadButton = SharpDX.XInput.GamepadButtonFlags.A } }, (window, when) =>
			{
				var row = ButtonIconTest.All(window).OfType<AxisToButtonUserControl>().Single();
				var arrow = Part(row, nameof(AxisToButtonUserControl.ArrowPictureBox));
				// The arrow's own grey, from the middle of its picture.
				var picture = (Bitmap)row.ArrowPicture;
				var fill = picture.GetPixel(picture.Width * 2 / 5, picture.Height / 2);
				using (var bitmap = new Bitmap(arrow.Width, arrow.Height))
				{
					arrow.DrawToBitmap(bitmap, new Rectangle(Point.Empty, arrow.Size));
					var page = bitmap.GetPixel(0, 0);
					for (var y = 0; y < bitmap.Height; y++)
					{
						// How far each pixel of the row has gone from the page's grey towards the arrow's.
						var depths = Enumerable.Range(0, bitmap.Width).Select(x => Math.Abs(bitmap.GetPixel(x, y).R - page.R)).ToList();
						var deepest = depths.Max();
						if (deepest == 0)
							continue;
						rows++;
						var first = depths.IndexOf(deepest);
						var last = depths.LastIndexOf(deepest);
						var rises = Enumerable.Range(1, first).All(x => depths[x] >= depths[x - 1]);
						var flat = Enumerable.Range(first, last - first + 1).All(x => depths[x] == deepest);
						var falls = Enumerable.Range(last + 1, depths.Count - last - 1).All(x => depths[x] <= depths[x - 1]);
						if (!rises || !flat || !falls)
							faults.Add(string.Format("{0}: row {1} of the arrow has a ring: {2}", when, y, string.Join(" ", depths)));
					}
					var middle = bitmap.GetPixel(bitmap.Width * 2 / 5, bitmap.Height / 2);
					if (middle.ToArgb() != fill.ToArgb())
						faults.Add(string.Format("{0}: the arrow is {1}, not its picture's {2}.", when, ColorTranslator.ToHtml(middle), ColorTranslator.ToHtml(fill)));
				}
			});
			Assert.IsTrue(rows > 0, "No arrow is drawn.");
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		static void AssertLaidOut(AxisToButtonUserControl row, string when)
		{
			var parts = Parts(row);
			var outside = parts.Where(p => !row.ClientRectangle.Contains(row.RectangleToClient(p.RectangleToScreen(p.ClientRectangle)))).ToList();
			Assert.AreEqual(0, outside.Count, string.Format("Row {0}: {1} part(s) reach outside it: {2}",
				when, outside.Count, string.Join(", ", outside.Select(p => p.Name))));
			var covered = ControlOverlapTest.Covered(row).ToList();
			Assert.AreEqual(0, covered.Count, string.Format("Row {0}:{1}{2}", when, Environment.NewLine, string.Join(Environment.NewLine, covered)));
			// The value box ends the row: its own margin, a control's, from the row's right edge.
			var value = Part(row, nameof(AxisToButtonUserControl.DeadZoneNumericUpDown));
			var right = row.RectangleToClient(value.Parent.RectangleToScreen(value.Bounds)).Right;
			Assert.AreEqual(value.Margin.Right, row.ClientSize.Width - right, string.Format("Row {0}: the value box ends {1} px short of the right edge, not its margin.", when, row.ClientSize.Width - right));
		}

		static Control[] Parts(Control root)
		{
			// The grid is looked through; a value box's own inner pieces are not parts of the row.
			return root.Controls.Cast<Control>().SelectMany(c => c is TableLayoutPanel ? Parts(c) : new[] { c }).ToArray();
		}

		static Control Part(Control root, string name)
		{
			var part = Parts(root).FirstOrDefault(p => p.Name == name);
			Assert.IsNotNull(part, name + " is not in the row.");
			return part;
		}
	}
}
