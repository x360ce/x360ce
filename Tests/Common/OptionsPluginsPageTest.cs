// @under-test: App.v4/Controls/OptionsPluginsUserControl.cs, App.v4/Controls/OptionsPluginsUserControl.Designer.cs
// @area: plugins   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.App.Plugins;

namespace x360ce.Tests
{
	/// <summary>
	/// The Plugins page on the Options tab: the list follows the plugin manager, and it is drawn like every other list.
	/// </summary>
	[TestClass]
	public class OptionsPluginsPageTest
	{
		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("A plugin found before the page was ever shown is on the list when the page is shown, named by its place in the plugins folder")]
		public void A_plugin_found_before_the_page_is_shown_is_listed()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.OptionsPluginsPageTest." + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			var before = PluginManager.Current;
			try
			{
				using (var manager = new PluginManager(folder, null, () => true, null))
				{
					PluginManager.Current = manager;
					Ui.OnUiThread(() =>
					{
						using (var page = new OptionsPluginsUserControl())
						{
							// The person has not opened the page yet: it has no window when the plugin arrives.
							var sub = Path.Combine(folder, "SamplePlugin");
							Directory.CreateDirectory(sub);
							File.Copy(typeof(SamplePluginDevice).Assembly.Location, Path.Combine(sub, "SamplePlugin.dll"));
							manager.Scan();
							Assert.IsFalse(page.IsHandleCreated, "The page was shown before the plugin arrived.");
							// Opening the page.
							page.CreateControl();
							var grid = (DataGridView)page.Controls.Find("PluginsDataGridView", true).Single();
							var rows = (OptionsPluginsUserControl.PluginRow[])grid.DataSource;
							var row = rows.SingleOrDefault(x => x.Name == "SamplePlugin");
							Assert.IsNotNull(row, "The plugin found before the page was shown is not on the list.");
							Assert.AreEqual("Loaded", row.State);
							Assert.AreEqual(@"SamplePlugin\SamplePlugin.dll", row.Source, "The folder is shown above the list.");
						}
					});
				}
			}
			finally
			{
				PluginManager.Current = before;
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("ui")]
		[Description("Every list on the Options tab is drawn alike: the window colour, no frame, and headers in the program's colours rather than the Windows style")]
		public void Every_list_on_the_options_tab_is_drawn_alike()
		{
			Ui.OnUiThread(() =>
			{
				using (var page = new OptionsUserControl())
				{
					var grids = Descendants(page).OfType<DataGridView>().ToList();
					CollectionAssert.Contains(grids.Select(x => x.Name).ToArray(), "PluginsDataGridView");
					foreach (var grid in grids)
					{
						Assert.AreEqual(SystemColors.Window, grid.BackgroundColor, grid.Name + " is not on the window colour.");
						Assert.AreEqual(BorderStyle.None, grid.BorderStyle, grid.Name + " has a frame of its own.");
						Assert.IsFalse(grid.EnableHeadersVisualStyles, grid.Name + " draws its headers in the Windows style.");
					}
				}
			});
		}

		/// <summary>Every control below this one, at any depth.</summary>
		static System.Collections.Generic.IEnumerable<Control> Descendants(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				yield return child;
				foreach (var grandChild in Descendants(child))
					yield return grandChild;
			}
		}
	}
}
