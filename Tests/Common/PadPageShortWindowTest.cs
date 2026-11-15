// @under-test: App.v4/Controls/PadControl.cs, App.v4/Controls/PadControl.Designer.cs, App.v4/Controls/PadTabPages/General/InputUserControl.cs, App.v4/MainForm.Designer.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// The controller page in the main window: its bottom buttons stay in view, and what can grow scrolls inside itself.
	/// </summary>
	/// <remarks>
	/// The page measured its tabs and grew them to their content, and scrolled as a whole. A keyboard's long list of inputs, a
	/// long SDL list or a tab the page had not laid out grew the tabs, and the buttons under them slid below the window with a
	/// long scroll bar beside the page. The page is a table: its tool bar, device list and buttons as tall as they are, the
	/// tabs in the rest, and a list that is longer than its room scrolls inside itself.
	/// </remarks>
	[TestClass]
	public class PadPageShortWindowTest
	{
		public TestContext TestContext { get; set; }

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("Through a keyboard, a pad and no device, every tab, a long SDL list and the window at its smallest size and larger, the Controller page's bottom buttons stay in view, the page never scrolls, and a list or panel scrolls only when its own content is longer than its room, in both themes")]
		public void The_bottom_buttons_stay_in_view_and_lists_scroll_inside_themselves()
		{
			var faults = new List<string>();
			var report = new List<string>();
			ButtonIconTest.OnEverySurface(new Func<Control>[] { () => OptionsLayoutTest.MainWindowAtDefaultSize(new PadControl(MapTo.Controller1), "Controller 1") }, (form, when) =>
			{
				var window = (Form)form;
				window.MinimumSize = MinimumWindowSize();
				var page = Descendants(window).OfType<PadControl>().Single();
				var input = page.InputPanel;
				var inputTable = input.Controls.Cast<Control>().Single();
				var sdl = (DataGridView)page.Controls.Find("PositionsDataGridView", true).Single();
				var keyboard = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Keyboard", CapButtonCount = 128, CapType = (int)SharpDX.DirectInput.DeviceType.Keyboard };
				var pad = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Pad", CapButtonCount = 12, DiAxeMask = 0x0F, CapPovCount = 1, CapType = (int)SharpDX.DirectInput.DeviceType.Gamepad };
				var smallest = window.MinimumSize;
				var steps = new List<KeyValuePair<string, Action>>
				{
					Step("the smallest window", () => window.Size = smallest),
					Step("a keyboard", () => input.UpdateFrom(keyboard)),
					Step("a pad", () => input.UpdateFrom(pad)),
					Step("no device", () => input.UpdateFrom(null)),
					Step("a keyboard again", () => input.UpdateFrom(keyboard)),
					Step("a long SDL list", () => sdl.DataSource = Enumerable.Range(1, 200).Select(x => new SdlPositionRow { Field = "b" + x, Position = "Place " + x }).ToList()),
				};
				foreach (TabPage tab in page.PadTabControl.TabPages)
					steps.Add(Step("the " + tab.Text + " tab", () => page.PadTabControl.SelectedTab = tab));
				steps.Add(Step("the General tab", () => page.PadTabControl.SelectedTab = page.GeneralTabPage));
				steps.Add(Step("a larger window", () => window.Size = new Size(smallest.Width + 400, smallest.Height + 400)));
				steps.Add(Step("a pad in a larger window", () => input.UpdateFrom(pad)));
				steps.Add(Step("the smallest window again", () => window.Size = smallest));
				steps.Add(Step("the SDL tab", () => page.PadTabControl.SelectedTab = page.SdlTabPage));
				foreach (var step in steps)
				{
					step.Value();
					Application.DoEvents();
					var pageAt = window.RectangleToClient(page.Parent.RectangleToScreen(page.Bounds));
					var buttons = window.RectangleToClient(page.ButtonsPanel.Parent.RectangleToScreen(page.ButtonsPanel.Bounds));
					var line = string.Format("{0}, {1}: window {2}, page {3}, buttons {4}, tabs {5} tall, inputs {6} in {7}",
						when, step.Key, window.Size, pageAt, buttons, page.PadTabControl.Height, inputTable.Height, input.ClientSize.Height);
					report.Add(line);
					if (buttons.Bottom > pageAt.Bottom || buttons.Top < pageAt.Top)
						faults.Add(line + ": the bottom buttons are out of view.");
					// Only a panel whose own content is longer than its room scrolls, and never the page or what holds it.
					foreach (var scrolled in Descendants(window).OfType<ScrollableControl>().Where(x => x.Visible && x.VerticalScroll.Visible))
						if (scrolled == page || scrolled.Contains(page) || scrolled.DisplayRectangle.Height <= scrolled.ClientSize.Height)
							faults.Add(line + ": " + scrolled.Name + " scrolls.");
					var inputScrolls = input.VerticalScroll.Visible;
					var inputLonger = inputTable.Height > input.ClientSize.Height;
					if (input.Visible && inputScrolls != inputLonger)
						faults.Add(line + ": the inputs " + (inputScrolls ? "show a scroll bar they do not need." : "are longer than their room and do not scroll."));
					if (page.PadTabControl.SelectedTab == page.SdlTabPage)
					{
						var sdlAt = page.SdlTabPage.RectangleToClient(sdl.Parent.RectangleToScreen(sdl.Bounds));
						if (sdlAt.Bottom > page.SdlTabPage.ClientSize.Height)
							faults.Add(line + ": the SDL list runs past the bottom of its tab, at " + sdlAt + ".");
						if (sdl.RowCount > 0 && sdl.DisplayedRowCount(false) >= sdl.RowCount)
							faults.Add(line + ": the SDL list shows all " + sdl.RowCount + " rows rather than scrolling inside itself.");
					}
				}
			});
			foreach (var line in report)
				TestContext.WriteLine(line);
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults.Distinct()));
		}

		[TestMethod, TestCategory("ui")]
		[Description("In the main window at its smallest size the Controller page keeps room for two device rows in its list above the General tab, at 100 % and 150 %, in both themes")]
		public void The_device_list_keeps_two_rows_above_the_general_tab_at_the_smallest_window_size()
		{
			var faults = new List<string>();
			ButtonIconTest.OnEverySurface(new Func<Control>[] { () => OptionsLayoutTest.MainWindowAtDefaultSize(new PadControl(MapTo.Controller2), "Controller 2") }, (form, when) =>
			{
				var window = (Form)form;
				window.MinimumSize = MinimumWindowSize();
				window.Size = window.MinimumSize;
				Application.DoEvents();
				var page = Descendants(window).OfType<PadControl>().Single();
				var grid = page.MappedDevicesDataGridView;
				var rows = (grid.ClientSize.Height - grid.ColumnHeadersHeight) / (double)grid.RowTemplate.Height;
				var line = string.Format("{0}: the page is {1}, the tabs {2} px tall, the General tab {3}, the device list {4} px with room for {5:0.0} rows of {6} px",
					when, page.ClientSize, page.PadTabControl.Height, page.GeneralTabPage.ClientSize, grid.Height, rows, grid.RowTemplate.Height);
				TestContext.WriteLine(line);
				if (rows < 2)
					faults.Add(line);
			});
			Assert.AreEqual(0, faults.Count, Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		static KeyValuePair<string, Action> Step(string name, Action action)
		{
			return new KeyValuePair<string, Action>(name, action);
		}

		/// <summary>The main window's smallest size, as its designer file sets it, at the screen's scale.</summary>
		internal static Size MinimumWindowSize()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "MainForm.Designer.cs"));
			var match = Regex.Match(source, @"this\.MinimumSize = new System\.Drawing\.Size\((\d+), (\d+)\);");
			Assert.IsTrue(match.Success, "The main window's smallest size was not found in its designer file.");
			var scale = JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale;
			return new Size((int)Math.Round(int.Parse(match.Groups[1].Value) * scale), (int)Math.Round(int.Parse(match.Groups[2].Value) * scale));
		}

		internal static IEnumerable<Control> Descendants(Control parent)
		{
			yield return parent;
			foreach (Control child in parent.Controls)
				foreach (var deeper in Descendants(child))
					yield return deeper;
		}
	}
}
