// @under-test: App.v4/Controls/PadControl.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The height a controller page needs, measured on the page shown and on the pages behind it.
	/// </summary>
	/// <remarks>
	/// The main window holds the four controller pages on one tab strip, and lays out the three not shown as well. A page
	/// grows its tabs to the rows of its General tab, and on a page not shown it read a General tab that its tab control had
	/// not laid out, so each measure grew the tabs again (721, 728, 735 px at 150 %, 4.29.37.0).
	/// </remarks>
	[TestClass]
	public class PadPageMeasureTest
	{
		public TestContext TestContext { get; set; }

		[TestMethod, TestCategory("ui")]
		[Description("Measuring a controller page not shown three times gives one height and leaves its tabs at one height, at 100 % and 150 %, in both themes")]
		public void Measuring_a_page_not_shown_again_changes_nothing()
		{
			var faults = new List<string>();
			var report = new List<string>();
			ButtonIconTest.OnEverySurface(new Func<Control>[] { MainWindowWithFourControllerPages }, (window, when) =>
			{
				var pads = Pads(window);
				var page = pads[1];
				var before = Tabs(page).Height;
				var heights = new List<int>();
				var needs = new List<int>();
				for (var i = 0; i < 3; i++)
				{
					needs.Add(page.NeededHeight(page.Width));
					heights.Add(Tabs(page).Height);
				}
				var line = string.Format("{0}: Controller 2's tabs {1} px, after each of three measures {2}, needing {3}; the shown page's tabs {4}",
					when, before, string.Join("/", heights), string.Join("/", needs), Tabs(pads[0]).Height);
				report.Add(line);
				if (heights.Distinct().Count() > 1 || needs.Distinct().Count() > 1)
					faults.Add(line + ": a measure changed the page.");
			});
			foreach (var line in report)
				TestContext.WriteLine(line);
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("A controller page measured while not shown needs, once shown, what the page shown at start needs, with tabs as tall, when both hold the same devices, at 100 % and 150 %, in both themes")]
		public void A_page_measured_while_not_shown_needs_once_shown_what_the_first_page_needs()
		{
			var faults = new List<string>();
			var report = new List<string>();
			ButtonIconTest.OnEverySurface(new Func<Control>[] { MainWindowWithFourControllerPages }, (window, when) =>
			{
				var pads = Pads(window);
				var width = pads[0].Width;
				// Measured behind the page shown, as anything that measures the window may.
				var hidden = pads.Skip(1).Select(x => x.NeededHeight(width)).ToList();
				var strip = (TabControl)pads[0].Parent.Parent;
				var needs = new List<int>();
				var heights = new List<int>();
				for (var i = 0; i < pads.Count; i++)
				{
					strip.SelectedIndex = i;
					Application.DoEvents();
					needs.Add(pads[i].NeededHeight(width));
					heights.Add(Tabs(pads[i]).Height);
				}
				strip.SelectedIndex = 0;
				var line = string.Format("{0}: Controller 2 / 3 / 4 measured behind need {1} px; once shown, Controller 1 / 2 / 3 / 4 need {2} px with tabs {3} px",
					when, string.Join("/", hidden), string.Join("/", needs), string.Join("/", heights));
				report.Add(line);
				if (needs.Distinct().Count() > 1 || heights.Distinct().Count() > 1)
					faults.Add(line + ".");
			});
			foreach (var line in report)
				TestContext.WriteLine(line);
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		/// <summary>The main window at its default size with the four controller pages on its tab strip, Controller 1 shown.</summary>
		static Control MainWindowWithFourControllerPages()
		{
			var window = OptionsLayoutTest.MainWindowAtDefaultSize(new PadControl(MapTo.Controller1), "Controller 1");
			var tabs = (TabControl)Pads(window)[0].Parent.Parent;
			foreach (var mapTo in new[] { MapTo.Controller2, MapTo.Controller3, MapTo.Controller4 })
			{
				var page = new TabPage("Controller " + (int)mapTo) { Padding = new Padding(3), ImageIndex = 0 };
				page.Controls.Add(new PadControl(mapTo) { Dock = DockStyle.Fill });
				tabs.TabPages.Add(page);
			}
			return window;
		}

		/// <summary>The controller pages in the order of their tabs.</summary>
		static List<PadControl> Pads(Control window)
		{
			return Descendants(window).OfType<PadControl>().ToList();
		}

		static Control Tabs(PadControl page)
		{
			return page.Controls.Find("PadTabControl", true).Single();
		}

		static IEnumerable<Control> Descendants(Control parent)
		{
			yield return parent;
			foreach (Control child in parent.Controls)
				foreach (var deeper in Descendants(child))
					yield return deeper;
		}
	}
}
