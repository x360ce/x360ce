// @under-test: App.v4/Controls/PadTabPages/DirectInputControl.Designer.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;

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

		/// <summary>Describes every control that another control is painted over.</summary>
		/// <remarks>
		/// Only controls with the same parent share a place to be. Windows paints them back to
		/// front in reverse order, so of an overlapping pair the earlier one is on top.
		/// </remarks>
		static IEnumerable<string> Covered(Control root)
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
