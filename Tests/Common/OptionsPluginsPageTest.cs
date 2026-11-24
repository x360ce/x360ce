// @under-test: App.v4/Controls/OptionsPluginsUserControl.cs, App.v4/Controls/OptionsPluginsUserControl.Designer.cs
// @area: plugins   @layer: unit
using JocysCom.ClassLibrary.Controls.Themes;
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
							var grid = (DataGridView)page.Controls.Find(nameof(OptionsPluginsUserControl.PluginsDataGridView), true).Single();
							var rows = (OptionsPluginsUserControl.PluginRow[])grid.DataSource;
							var row = rows.SingleOrDefault(x => x.Name == "SamplePlugin");
							Assert.IsNotNull(row, "The plugin found before the page was shown is not on the list.");
							Assert.IsTrue(row.Enabled, "A loaded plugin starts enabled.");
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

		[TestMethod, TestCategory("plugins"), TestCategory("ui"), TestCategory("critical")]
		[Description("Unticking the Enabled checkbox on Options > Plugins disables/unloads the plugin and persists the choice; re-ticking restores it")]
		public void Enabled_checkbox_toggles_plugin_state_and_persists_in_options()
		{
			var before = PluginManager.Current;
			try
			{
				x360ce.App.SettingsManager.Options.SetPluginEnabled("WheelTelemetry", true);
				using (var manager = new PluginManager(null, null, () => false, typeof(PluginManager).Assembly,
					name => x360ce.App.SettingsManager.Options.IsPluginEnabled(name)))
				{
					manager.LoadEmbedded();
					PluginManager.Current = manager;
					Ui.OnUiThread(() =>
					{
						using (var form = new Form())
						using (var page = new OptionsPluginsUserControl { Dock = DockStyle.Fill })
						{
							form.Controls.Add(page);
							form.Show();
							Application.DoEvents();
							var grid = page.PluginsDataGridView;
							Assert.IsTrue(grid.Columns[0] is DataGridViewCheckBoxColumn, "First column should be the Enabled checkbox.");
							var rows = (OptionsPluginsUserControl.PluginRow[])grid.DataSource;
							var index = Array.FindIndex(rows, r => r.Name == "WheelTelemetry");
							Assert.IsTrue(index >= 0, "WheelTelemetry built-in plugin should be listed.");
							Assert.IsTrue(rows[index].Enabled, "Built-in plugin must start enabled.");
							Assert.AreEqual("Loaded", rows[index].State);

							// Untick Enabled on the grid.
							grid.Rows[index].Cells[0].Value = false;
							Application.DoEvents();
							Assert.IsFalse(x360ce.App.SettingsManager.Options.IsPluginEnabled("WheelTelemetry"), "Disabled state must be persisted in Options.");
							Assert.AreEqual(0, manager.TelemetryTypes.Length, "Disabling WheelTelemetry must unload its active provider.");
							rows = (OptionsPluginsUserControl.PluginRow[])grid.DataSource;
							index = Array.FindIndex(rows, r => r.Name == "WheelTelemetry");
							Assert.IsFalse(rows[index].Enabled);
							Assert.AreEqual("Disabled", rows[index].State);

							// Re-tick Enabled on the grid.
							grid.Rows[index].Cells[0].Value = true;
							Application.DoEvents();
							Assert.IsTrue(x360ce.App.SettingsManager.Options.IsPluginEnabled("WheelTelemetry"));
							Assert.IsTrue(manager.TelemetryTypes.Any(t => t.File.Name == "WheelTelemetry"));
							rows = (OptionsPluginsUserControl.PluginRow[])grid.DataSource;
							index = Array.FindIndex(rows, r => r.Name == "WheelTelemetry");
							Assert.IsTrue(rows[index].Enabled);
							Assert.AreEqual("Loaded", rows[index].State);
						}
					});
				}
			}
			finally
			{
				x360ce.App.SettingsManager.Options.SetPluginEnabled("WheelTelemetry", true);
				PluginManager.Current = before;
			}
		}

		[TestMethod, TestCategory("ui")]
		[Description("Every list on the Options tab is drawn alike: the window colour, no frame, and headers in the program's colours rather than the Windows style")]
		public void Every_list_on_the_options_tab_is_drawn_alike()
		{
			Ui.OnUiThread(() =>
			{
				// The theme is chosen before the page is made and gives it its look, as the program does.
				FormsTheme.SetTheme(ThemeType.Light);
				using (var page = new OptionsUserControl())
				{
					FormsTheme.Apply(page);
					var grids = Descendants(page).OfType<DataGridView>().ToList();
					CollectionAssert.Contains(grids.Select(x => x.Name).ToArray(), nameof(OptionsPluginsUserControl.PluginsDataGridView));
					foreach (var grid in grids)
					{
						Assert.AreEqual(SystemColors.Window.ToArgb(), grid.BackgroundColor.ToArgb(), grid.Name + " is not on the window colour.");
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
