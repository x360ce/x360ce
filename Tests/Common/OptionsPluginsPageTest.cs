// @under-test: App.v4/Controls/OptionsPluginsUserControl.cs, App.v4/Controls/OptionsPluginsUserControl.Designer.cs
// @area: plugins   @layer: unit
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
		[Description("A click on the Enabled box of a built-in plugin, where the box is drawn, disables and unloads it and keeps the choice; another click restores it")]
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
						FormsTheme.SetTheme(ThemeType.Dark);
						using (var form = new Form { Width = 1000, Height = 600 })
						using (var page = new OptionsPluginsUserControl { Dock = DockStyle.Fill })
						{
							form.Controls.Add(page);
							// Drawn by the theme, as in the program: the box at the left of its cell.
							FormsTheme.Apply(form);
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
							Click(grid, index);
							Assert.IsFalse(x360ce.App.SettingsManager.Options.IsPluginEnabled("WheelTelemetry"), "Disabled state must be persisted in Options.");
							Assert.AreEqual(0, manager.TelemetryTypes.Length, "Disabling WheelTelemetry must unload its active provider.");
							rows = (OptionsPluginsUserControl.PluginRow[])grid.DataSource;
							index = Array.FindIndex(rows, r => r.Name == "WheelTelemetry");
							Assert.IsFalse(rows[index].Enabled);
							Assert.AreEqual("Disabled", rows[index].State);

							// Re-tick Enabled on the grid.
							Click(grid, index);
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
				Ui.OnUiThread(() => FormsTheme.SetTheme(ThemeType.Light));
				x360ce.App.SettingsManager.Options.SetPluginEnabled("WheelTelemetry", true);
				PluginManager.Current = before;
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui"), TestCategory("critical")]
		[Description("A click on the Enabled box of a plugin off by default, as a person makes it, switches the plugin on and keeps it on")]
		public void A_click_on_Enabled_switches_on_a_plugin_off_by_default()
		{
			const string name = "OffByDefaultPlugin";
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.OptionsPluginsPageTest." + Guid.NewGuid().ToString("N"));
			var before = PluginManager.Current;
			var options = x360ce.App.SettingsManager.Options;
			try
			{
				WriteOffByDefaultPlugin(folder, name);
				options.SetPluginEnabled(name, false);
				options.DisabledPlugins.Remove(name);
				using (var manager = new PluginManager(folder, null, () => true, null, options.IsPluginEnabled))
				{
					// As the program has it: a change updates the AI skill's plugin documents and the plugins' MCP doors.
					manager.Changed += (s, e) =>
					{
						PluginDocs.UpdateReferences(manager);
						PluginMcpDoors.Apply();
					};
					manager.Scan();
					PluginManager.Current = manager;
					Ui.OnUiThread(() =>
					{
						FormsTheme.SetTheme(ThemeType.Dark);
						using (var form = new Form { Width = 1000, Height = 600 })
						using (var page = new OptionsPluginsUserControl { Dock = DockStyle.Fill })
						{
							form.Controls.Add(page);
							FormsTheme.Apply(form);
							form.Show();
							Application.DoEvents();
							var grid = page.PluginsDataGridView;
							var rows = (OptionsPluginsUserControl.PluginRow[])grid.DataSource;
							var index = Array.FindIndex(rows, r => r.Name == name);
							Assert.IsTrue(index >= 0, name + " is not listed.");
							Assert.IsFalse(rows[index].Enabled, "The plugin starts ticked.");
							Click(grid, index);
							Assert.IsTrue(options.IsPluginEnabled(name), "The tick was not kept in the options.");
							var file = manager.Files.Single(x => x.Name == name);
							Assert.AreEqual(PluginFileState.Loaded, file.State, file.Reason);
							Assert.IsTrue(manager.DeviceTypes.Any(x => x.File == file), "Its device is not offered.");
							rows = (OptionsPluginsUserControl.PluginRow[])grid.DataSource;
							index = Array.FindIndex(rows, r => r.Name == name);
							Assert.IsTrue(rows[index].Enabled, "The box shows the plugin unticked.");
							Assert.IsTrue((bool)grid.Rows[index].Cells[0].FormattedValue, "The box is drawn unticked.");
						}
					});
				}
			}
			finally
			{
				Ui.OnUiThread(() => FormsTheme.SetTheme(ThemeType.Light));
				options.SetPluginEnabled(name, false);
				options.DisabledPlugins.Remove(name);
				PluginManager.Current = before;
				if (Directory.Exists(folder))
					Directory.Delete(folder, true);
			}
		}

		/// <summary>Writes a plugin off by default into the plugins folder, in a folder of its own: one kind of device, compiled here so no built-in plugin has to be off by default.</summary>
		static void WriteOffByDefaultPlugin(string folder, string name)
		{
			var sub = Path.Combine(folder, name);
			Directory.CreateDirectory(sub);
			var source = "[assembly: x360ce.Plugins.PluginOffByDefault]\n"
				+ "public class OffByDefaultPluginDevice : x360ce.Plugins.IPluginDevice\n{\n"
				+ "	public string Name { get { return \"Off By Default Plugin Device\"; } }\n"
				+ "	public System.Guid ProductGuid { get { return new System.Guid(\"0e4f5a2b-7c1d-4b8e-9f3a-6d2c1b0a9e87\"); } }\n"
				+ "	public int AxisCount { get { return 2; } }\n"
				+ "	public int SliderCount { get { return 0; } }\n"
				+ "	public int PovCount { get { return 0; } }\n"
				+ "	public int ButtonCount { get { return 1; } }\n"
				+ "	public void Read(x360ce.Plugins.PluginState state) { }\n}\n";
			using (var compiler = new Microsoft.CSharp.CSharpCodeProvider())
			{
				var parameters = new System.CodeDom.Compiler.CompilerParameters(new[] { "System.dll", typeof(x360ce.Plugins.IPluginDevice).Assembly.Location })
				{
					OutputAssembly = Path.Combine(sub, name + ".dll"),
				};
				var results = compiler.CompileAssemblyFromSource(parameters, source);
				Assert.IsFalse(results.Errors.HasErrors, "The plugin off by default does not compile: " + string.Join("; ", results.Errors.Cast<System.CodeDom.Compiler.CompilerError>().Select(x => x.ErrorText)));
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui"), TestCategory("critical")]
		[Description("The Plugins page, in a window minimized and restored, shows its list, every row in it, and the AutoHotkey scripts group below it, as tall as its few lines")]
		public void The_plugin_list_shows_after_the_window_is_minimized()
		{
			var before = PluginManager.Current;
			try
			{
				using (var manager = new PluginManager(null, null, () => false, typeof(PluginManager).Assembly,
					x360ce.App.SettingsManager.Options.IsPluginEnabled))
				{
					manager.LoadEmbedded();
					PluginManager.Current = manager;
					// The zooms Windows offers: the screen's zoom reaches the window as a larger font.
					foreach (var zoom in new[] { 1f, 1.25f, 1.5f, 2f })
					Ui.OnUiThread(() =>
					{
						FormsTheme.SetTheme(ThemeType.Light);
						using (var form = new Form { Font = new Font(Control.DefaultFont.FontFamily, Control.DefaultFont.Size * zoom) })
						using (var main = new TabControl { Dock = DockStyle.Fill })
						using (var options = new OptionsUserControl { Dock = DockStyle.Fill })
						{
							// As the program has it: the window opens on a controller's tab, the Options tab behind it.
							var first = new TabPage("Controller 1");
							var optionsTab = new TabPage("Options");
							optionsTab.Controls.Add(options);
							main.TabPages.Add(first);
							main.TabPages.Add(optionsTab);
							form.Controls.Add(main);
							FormsTheme.Apply(form);
							// Opened in the tray, or restored from it, the window is laid out with no room before it has its size.
							form.ClientSize = new Size(40, 40);
							form.Show();
							Application.DoEvents();
							form.ClientSize = new Size((int)(1000 * zoom), (int)(900 * zoom));
							Application.DoEvents();
							main.SelectedTab = optionsTab;
							Application.DoEvents();
							var tabs = (TabControl)Descendants(options).Single(x => x.Name == "MainTabControl");
							var page = Descendants(options).OfType<OptionsPluginsUserControl>().Single();
							tabs.SelectedTab = (TabPage)page.Parent;
							Application.DoEvents();
							// Minimized and restored, as a window sent to the taskbar or the tray and brought back.
							form.WindowState = FormWindowState.Minimized;
							Application.DoEvents();
							form.WindowState = FormWindowState.Normal;
							Application.DoEvents();
							var grid = page.PluginsDataGridView;
							var rows = grid.Rows.Count;
							Assert.IsTrue(rows > 0, "No plugin is listed.");
							var needed = grid.ColumnHeadersHeight + grid.Rows.GetRowsHeight(DataGridViewElementStates.Visible);
							Assert.IsTrue(grid.DisplayRectangle.Height >= needed, string.Format(
								"At {0:P0} the list is {1} px tall and its {2} rows need {3}.", zoom, grid.Height, rows, needed));
							var group = page.AutoHotkeyGroupBox;
							Assert.IsTrue(group.Height < grid.Height, string.Format(
								"At {0:P0} the AutoHotkey scripts group is {1} px tall, taller than the list above it.", zoom, group.Height));
							Assert.IsTrue(group.Top >= 0 && group.Bottom <= page.ClientSize.Height, string.Format(
								"At {0:P0} the AutoHotkey scripts group runs from {1} to {2}, off the page's {3}.", zoom, group.Top, group.Bottom, page.ClientSize.Height));
						}
					});
				}
			}
			finally
			{
				PluginManager.Current = before;
			}
		}

		[DllImport("user32.dll")]
		static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		/// <summary>Presses and releases the left mouse button over the Enabled box of a row where it is drawn, as a person's click reaches the list.</summary>
		/// <remarks>
		/// The box is found in the picture of the cell: the first mark from the left across its middle. The theme draws it at the
		/// left of the cell, and Windows Forms takes a box's click only in the middle, so a click there proves nothing.
		/// </remarks>
		static void Click(DataGridView grid, int rowIndex)
		{
			var rect = grid.GetCellDisplayRectangle(0, rowIndex, false);
			var y = rect.Top + rect.Height / 2;
			int x;
			using (var bitmap = new Bitmap(grid.Width, grid.Height))
			{
				grid.DrawToBitmap(bitmap, new Rectangle(Point.Empty, grid.Size));
				var behind = bitmap.GetPixel(rect.Left + 1, y).ToArgb();
				var left = Enumerable.Range(rect.Left + 1, rect.Width - 2).FirstOrDefault(i => bitmap.GetPixel(i, y).ToArgb() != behind);
				Assert.IsTrue(left > 0, "No box is drawn in row " + rowIndex + ".");
				// The box's far edge: the last mark before the cell's colour comes back for good, a box's width at most.
				var right = Enumerable.Range(left, rect.Height).Where(i => i < rect.Right && bitmap.GetPixel(i, y).ToArgb() != behind).Max();
				x = (left + right) / 2;
				// Where the cell takes the click: Windows Forms ticks the box only there.
				var takes = grid.Rows[rowIndex].Cells[0].ContentBounds;
				takes.Offset(rect.Location);
				Assert.IsTrue(Math.Abs((takes.Left + takes.Right) / 2 - x) <= 1, string.Format(
					"Row {0}: the box is drawn from x {1} to {2}, and the cell takes its click from {3} to {4}.", rowIndex, left, right, takes.Left, takes.Right));
			}
			Assert.IsTrue(x < rect.Left + rect.Width / 2 - 4, "The box is drawn in the middle of its cell, so the click would not test the box at the left.");
			var point = new IntPtr((y << 16) | (x & 0xFFFF));
			const int WM_MOUSEMOVE = 0x0200, WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202, MK_LBUTTON = 1;
			// The pointer comes to the box first, as a mouse brings it: a check box cell learns it is over its box only as the
			// pointer moves, and keeps that in one place for every grid.
			SendMessage(grid.Handle, WM_MOUSEMOVE, IntPtr.Zero, point);
			SendMessage(grid.Handle, WM_LBUTTONDOWN, new IntPtr(MK_LBUTTON), point);
			SendMessage(grid.Handle, WM_LBUTTONUP, IntPtr.Zero, point);
			Application.DoEvents();
			Application.DoEvents();
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
