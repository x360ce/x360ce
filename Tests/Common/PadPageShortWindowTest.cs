// @under-test: App.v4/Controls/PadControl.cs, App.v4/Controls/PadControl.Designer.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The controller page in a window shorter than its rows.
	/// </summary>
	/// <remarks>
	/// The table gives the tabs all the height they ask for, and in a short window it found it by moving
	/// them up, under the tool bar and the device list. On a 1920 x 1080 screen at 150 % (4.27.53.0) the
	/// General, Advanced, Buttons ... strip was gone and no tab could be chosen. The page now scrolls instead.
	/// </remarks>
	[TestClass]
	public class PadPageShortWindowTest
	{
		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("In a short window the controller page scrolls and keeps its tabs below the device list")]
		public void A_short_window_scrolls_the_page_and_keeps_the_tabs_in_view()
		{
			Ui.OnUiThread(() =>
			{
				// The full height, the reporter's (about four fifths of it), and shorter still.
				foreach (var share in new[] { 1.0, 0.8, 0.6 })
				{
					using (var form = new Form())
					using (var page = new PadControl(MapTo.Controller1))
					{
						page.Dock = DockStyle.Fill;
						var natural = page.Size;
						form.ClientSize = new Size(natural.Width, (int)(natural.Height * share));
						form.Controls.Add(page);
						form.Show();
						Application.DoEvents();
						var rows = new[] { "GamesToolStrip", "MappedDevicesDataGridView", "PadTabControl", "ButtonsPanel" }
							.Select(name => page.Controls.Find(name, true).Single()).ToList();
						var at = string.Join(", ", rows.Select(x => string.Format("{0} {1}-{2}", x.Name, x.Top, x.Bottom)));
						for (var i = 1; i < rows.Count; i++)
							Assert.IsTrue(rows[i].Top >= rows[i - 1].Bottom, string.Format(
								"At {0:0%} of its height, {1} is drawn over {2}: {3}.", share, rows[i].Name, rows[i - 1].Name, at));
						Assert.IsTrue(rows[0].Top >= 0, "The page starts above its own top: " + at);
						var tabs = (TabControl)rows[2];
						Assert.IsTrue(tabs.GetTabRect(0).Height > 0, "The tab strip has no height.");
						if (share < 1)
							Assert.IsTrue(page.VerticalScroll.Visible,
								"At " + share.ToString("0%") + " of its height the page has rows out of view and nothing to scroll them in with.");
						form.Close();
					}
				}
			});
		}
	}
}
