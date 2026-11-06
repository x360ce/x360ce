// @under-test: App.v4/Controls/PadControl.Designer.cs, App.v4/Controls/AxisMapUserControl.Designer.cs, App.v4/Controls/AxisToButtonUserControl.Designer.cs, App.v4/Controls/MouseStickUserControl.Designer.cs
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.Controls.Themes;
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
	/// The sliders on a controller's pages, as the WPF program draws them: each slider's name on its left, in the same row,
	/// no tick marks, and the names and sliders of one group each starting on one line.
	/// </summary>
	[TestClass]
	public class SliderRowLayoutTest
	{
		[TestMethod, TestCategory("ui")]
		[Description("Every slider on a controller's pages has no tick marks and its name on its left in the same row, and the names and sliders of a group each start on one line")]
		public void Every_slider_has_its_name_on_its_left_in_its_row()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				using (var page = new PadControl(MapTo.Controller1))
				{
					page.Dock = DockStyle.Fill;
					form.ClientSize = page.Size;
					form.Controls.Add(page);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					var problems = new List<string>();
					var seen = new HashSet<TrackBar>();
					var tabs = (TabControl)page.Controls.Find("PadTabControl", true).Single();
					// A page is laid out once it is shown, so each is opened before its sliders are measured.
					foreach (TabPage tab in tabs.TabPages)
					{
						tabs.SelectedTab = tab;
						Application.DoEvents();
						var sliders = All(tab).OfType<TrackBar>().Where(x => x.Visible).ToList();
						foreach (var slider in sliders)
						{
							seen.Add(slider);
							if (slider.TickStyle != TickStyle.None)
								problems.Add(Describe(tab, slider) + " has tick marks.");
							if (NameOf(slider) == null)
								problems.Add(Describe(tab, slider) + " has no name on its left in its row.");
						}
						// Sliders that share a grid share a column, and so do their names.
						foreach (var group in sliders.GroupBy(x => x.Parent))
						{
							if (group.Select(x => x.Left).Distinct().Count() > 1)
								problems.Add(tab.Text + ": the sliders in " + group.Key.Name + " start at " + string.Join(", ", group.Select(x => x.Name + " " + x.Left)) + ".");
							var names = group.Select(NameOf).Where(x => x != null).ToList();
							if (names.Select(x => x.Left).Distinct().Count() > 1)
								problems.Add(tab.Text + ": the names in " + group.Key.Name + " start at " + string.Join(", ", names.Select(x => x.Name + " " + x.Left)) + ".");
						}
					}
					var all = All(page).OfType<TrackBar>().ToList();
					Assert.IsTrue(all.Count > 0, "The controller's pages have no sliders.");
					var unseen = all.Where(x => !seen.Contains(x)).Select(x => x.Name).ToList();
					Assert.AreEqual(0, unseen.Count, "Sliders not shown on any page: " + string.Join(", ", unseen));
					Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
				}
			});
		}

		/// <summary>
		/// The label that names a slider: beside it in the same grid, ending before the slider starts, with the middle of its
		/// text inside the slider's height; the nearest such label, or null.
		/// </summary>
		static Label NameOf(TrackBar slider)
		{
			return slider.Parent.Controls.OfType<Label>()
				.Where(x => x.Visible && !string.IsNullOrEmpty(x.Text) && x.Right <= slider.Left)
				.Where(x => x.Top + x.Height / 2 >= slider.Top && x.Top + x.Height / 2 < slider.Bottom)
				.OrderByDescending(x => x.Right)
				.FirstOrDefault();
		}

		static string Describe(TabPage tab, Control control)
		{
			return tab.Text + ": " + (control.Parent == null ? "" : control.Parent.Parent?.Name + "/") + control.Name;
		}

		static IEnumerable<Control> All(Control root)
		{
			foreach (Control child in root.Controls)
			{
				yield return child;
				foreach (var deeper in All(child))
					yield return deeper;
			}
		}
	}
}
