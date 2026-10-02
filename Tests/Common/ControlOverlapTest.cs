// @under-test: App.v4/Controls/PadTabPages/DirectInputControl.Designer.cs, App.v4/Controls/PadControl.Designer.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// A page laid out by fixed positions looks right at the width it was drawn at and nowhere
	/// else. Boxes told to keep both edges grow with the window; the things beside them, told to
	/// keep only their right edge, merely slide. At any width past the drawn one the growing box
	/// covers its neighbours, and the fields underneath cannot be read or reached at all.
	///
	/// Reported against 4.18.63.0 as identifiers and counts missing from the Direct Input page.
	/// Checked here at several widths, because the drawn width is the one width that hides it.
	/// </summary>
	[TestClass]
	public class ControlOverlapTest
	{
		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("No two controls on the Direct Input page cover each other, at any width")]
		public void Direct_input_page_keeps_every_control_clear_of_the_others()
		{
			Ui.OnUiThread(() =>
			{
				using (var page = new DirectInputUserControl())
				{
					page.CreateControl();
					var drawn = page.Width;
					// The width it was drawn at, narrower, and the widths a real window reaches.
					foreach (var width in new[] { drawn, (int)(drawn * 0.75), (int)(drawn * 1.5), drawn * 2 })
					{
						page.Width = width;
						page.PerformLayout();
						var covered = Covered(page).ToList();
						Assert.AreEqual(0, covered.Count, string.Format(
							"At {0} pixels wide, {1} control(s) are covered by another:{2}{3}",
							width, covered.Count, Environment.NewLine,
							string.Join(Environment.NewLine, covered)));
					}
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("Every mapping box on the General tab is the same width, wide enough for the longest mapping, and clear of its label")]
		public void General_tab_mapping_boxes_show_the_longest_mapping()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var page = new PadControl(MapTo.Controller1))
				{
					page.Dock = DockStyle.Fill;
					form.ClientSize = page.Size;
					form.Controls.Add(page);
					form.Show();
					Application.DoEvents();
					var panels = new[] { "GeneralLeftPanel", "GeneralCenterPanel", "GeneralRightPanel" }
						.Select(name => page.Controls.Find(name, true).Single()).ToList();
					var boxes = panels.SelectMany(x => x.Controls.OfType<ComboBox>())
						.Where(x => x.Name != "MapNameComboBox").ToList();
					Assert.IsTrue(boxes.Count > 0, "The General tab has no mapping boxes.");
					var widest = MappingTexts()
						.OrderByDescending(x => TextRenderer.MeasureText(x, boxes[0].Font).Width).First();
					// The same text is wider in another font or at another text size, so the box keeps a
					// seventh of the text's width to spare, beside the drop-down arrow and the box's edges.
					var textWidth = TextRenderer.MeasureText(widest, boxes[0].Font).Width;
					var needed = textWidth + textWidth / 7 + SystemInformation.VerticalScrollBarWidth + 6;
					var tooNarrow = boxes.Where(x => x.Width < needed)
						.Select(x => string.Format("{0} is {1} px", x.Name, x.Width)).ToList();
					Assert.AreEqual(0, tooNarrow.Count, string.Format(
						"'{0}' needs {1} px, but {2} box(es) are narrower: {3}",
						widest, needed, tooNarrow.Count, string.Join("; ", tooNarrow)));
					Assert.AreEqual(1, boxes.Select(x => x.Width).Distinct().Count(),
						"The mapping boxes must all be the same width.");
					var outside = boxes.Where(x => x.Right > x.Parent.ClientSize.Width)
						.Select(x => x.Name).ToList();
					Assert.AreEqual(0, outside.Count, "Past the edge of its panel: " + string.Join(", ", outside));
					var covered = panels.SelectMany(x => Covered(x)).ToList();
					Assert.AreEqual(0, covered.Count, string.Format("{0} control(s) are covered by another:{1}{2}",
						covered.Count, Environment.NewLine, string.Join(Environment.NewLine, covered)));
				}
			});
		}

		/// <summary>Every text a mapping box can hold: a button, an axis or slider (plain, inverted or half), a POV and a POV direction.</summary>
		static IEnumerable<string> MappingTexts()
		{
			for (var i = 1; i <= 128; i++)
				foreach (var type in new[] { MapType.Button, MapType.IButton })
					yield return SettingsConverter.ToTextValue(type, i);
			for (var i = 1; i <= 24; i++)
				foreach (var type in new[] { MapType.Axis, MapType.IAxis, MapType.HAxis, MapType.IHAxis })
					yield return SettingsConverter.ToTextValue(type, i);
			for (var i = 1; i <= 8; i++)
				foreach (var type in new[] { MapType.Slider, MapType.ISlider, MapType.HSlider, MapType.IHSlider })
					yield return SettingsConverter.ToTextValue(type, i);
			for (var i = 1; i <= 4; i++)
				yield return SettingsConverter.ToTextValue(MapType.POV, i);
			for (var i = 1; i <= 16; i++)
				yield return SettingsConverter.ToTextValue(MapType.DPOVButton, i);
		}

		/// <summary>Describes every control that another control is painted over.</summary>
		/// <remarks>
		/// Only controls with the same parent share a place to be. Windows paints them back to
		/// front in reverse order, so of an overlapping pair the earlier one is on top.
		/// </remarks>
		internal static IEnumerable<string> Covered(Control root)
		{
			foreach (var parent in Containers(root))
			{
				var children = parent.Controls.Cast<Control>()
					.Where(x => x.Visible && x.Width > 0 && x.Height > 0).ToList();
				for (var i = 0; i < children.Count; i++)
				{
					for (var j = i + 1; j < children.Count; j++)
					{
						var a = children[i].Bounds;
						var b = children[j].Bounds;
						var over = Rectangle.Intersect(a, b);
						if (over.Width <= 0 || over.Height <= 0)
							continue;
						yield return string.Format("  '{0}' covers '{1}' by {2}x{3}",
							Describe(children[i]), Describe(children[j]), over.Width, over.Height);
					}
				}
			}
		}

		static IEnumerable<Control> Containers(Control root)
		{
			yield return root;
			foreach (Control child in root.Controls)
				foreach (var deeper in Containers(child))
					yield return deeper;
		}

		static string Describe(Control control)
		{
			return string.IsNullOrEmpty(control.Name) ? control.GetType().Name : control.Name;
		}
	}
}
