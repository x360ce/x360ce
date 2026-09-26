// @under-test: App.v4/Controls/AxisToButtonUserControl.Designer.cs, App.v4/Controls/AxisToButtonUserControl.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
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
			page.Font = new Font(page.Font.FontFamily, page.Font.Size * zoom);
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

		[TestMethod, TestCategory("ui")]
		[Description("Rows with different button names start their sliders on the same line")]
		public void Sliders_line_up_across_rows()
		{
			Ui.OnUiThread(() =>
			{
				using (var a = new AxisToButtonUserControl { GamepadButton = SharpDX.XInput.GamepadButtonFlags.A })
				using (var b = new AxisToButtonUserControl { GamepadButton = SharpDX.XInput.GamepadButtonFlags.RightThumb })
				{
					a.CreateControl();
					b.CreateControl();
					a.Width = b.Width = 554;
					a.PerformLayout();
					b.PerformLayout();
					var sliderA = Part(a, "DeadZoneTrackBar");
					var sliderB = Part(b, "DeadZoneTrackBar");
					Assert.AreEqual(sliderA.Left, sliderB.Left, "'A Button' and 'Right Stick Button' rows start their sliders at different places.");
				}
			});
		}

		static void AssertLaidOut(AxisToButtonUserControl row, string when)
		{
			var parts = Parts(row);
			var outside = parts.Where(p => !row.ClientRectangle.Contains(row.RectangleToClient(p.RectangleToScreen(p.ClientRectangle)))).ToList();
			Assert.AreEqual(0, outside.Count, string.Format("Row {0}: {1} part(s) reach outside it: {2}",
				when, outside.Count, string.Join(", ", outside.Select(p => p.Name))));
			var covered = ControlOverlapTest.Covered(row).ToList();
			Assert.AreEqual(0, covered.Count, string.Format("Row {0}:{1}{2}", when, Environment.NewLine, string.Join(Environment.NewLine, covered)));
			var value = Part(row, "DeadZoneNumericUpDown");
			var right = row.RectangleToClient(value.RectangleToScreen(value.ClientRectangle)).Right;
			Assert.IsTrue(row.ClientSize.Width - right <= 4, string.Format("Row {0}: the value box ends {1} px short of the right edge.", when, row.ClientSize.Width - right));
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
