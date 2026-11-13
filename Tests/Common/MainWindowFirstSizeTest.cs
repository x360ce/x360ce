// @under-test: App.v4/MainForm.cs, App.v4/Controls/PadControl.cs, App.v4/Forms/BaseFormWithHeader.cs
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The size and place the main window first opens at when it has no saved place, the saved place it reopens at exactly, and
	/// its header's height, which its text never changes.
	/// </summary>
	/// <remarks>
	/// At its designed size the Controller page needed more height than it had (985 px of 893 at 150 %, 4.29.36.0), so the
	/// buttons under the tabs, [Reset] among them, were below the fold. The work areas are handed in, so the tests do not
	/// depend on the size of the screens of the machine they run on, and lie off every screen, so nothing appears while they
	/// run.
	/// </remarks>
	[TestClass]
	public class MainWindowFirstSizeTest
	{
		public TestContext TestContext { get; set; }

		/// <summary>The work area of a 3840 x 2160 screen with the task bar at the bottom, at 100 %; taller than the page needs.</summary>
		static readonly Size TallScreen = new Size(2560, 1392);

		/// <summary>The work area of a 1920 x 1200 screen at 150 % with the task bar at the bottom (1152 px tall), at 100 %.</summary>
		static readonly Size ShortScreen = new Size(1280, 768);

		[TestMethod, TestCategory("ui")]
		[Description("With no saved place, on a tall screen, the main window first opens as tall as the Controller page needs and centred in the work area, so the page does not scroll and [Reset] is in view, in every interface font, at 100 % and 150 %, in both themes")]
		public void With_no_saved_place_the_window_opens_as_tall_as_the_page_needs_centred_on_a_tall_screen()
		{
			AppFontsTest.LoadFonts();
			var faults = new List<string>();
			var report = new List<string>();
			var untestable = new List<string>();
			foreach (var choice in PadValueLabelsTest.FontChoices)
				ButtonIconTest.OnEverySurface(new Func<Control>[] { MainWindowWithControllerPage }, (window, when) =>
				{
					var page = Page(window);
					when += " " + page.Font.Name;
					var designed = window.Size;
					var work = WorkArea(TallScreen);
					OpenWithNoSavedPlace(window, page, work);
					var reset = page.Controls.Find(nameof(PadControl.ResetPresetButton), true).Single();
					var resetAt = page.RectangleToClient(reset.Parent.RectangleToScreen(reset.Bounds));
					var general = page.Controls.Find(nameof(PadControl.GeneralTabPage), true).Single();
					var table = page.Controls.Find(nameof(PadControl.GeneralTableLayoutPanel), true).Single();
					var tableAt = general.RectangleToClient(table.Parent.RectangleToScreen(table.Bounds));
					// As tall as the page needs, or as designed where that is taller; as wide as designed.
					var measured = new Size(designed.Width,
						Math.Max(designed.Height, window.Height - page.ClientSize.Height + page.AutoScrollMinSize.Height));
					var line = string.Format("{0}: designed {1}, opened at {2} in {3}; the page is {4} and needs {5} px{6}; [Reset] at {7}",
						when, designed, window.Bounds, work, page.ClientSize, page.AutoScrollMinSize.Height,
						page.VerticalScroll.Visible ? ", with a scroll bar" : "", resetAt);
					report.Add(line);
					// Windows makes no window taller than the largest screen attached allows, whatever work area it is given.
					if (measured.Height > SystemInformation.MaxWindowTrackSize.Height)
					{
						untestable.Add(line);
						return;
					}
					if (window.Size != measured)
						faults.Add(line + ": the window is not the measured " + measured + ".");
					if (!work.Contains(window.Bounds))
						faults.Add(line + ": the window is not inside the work area.");
					if (!IsCentred(window.Bounds, work))
						faults.Add(line + ": the window is not centred in the work area.");
					if (page.VerticalScroll.Visible || page.AutoScrollMinSize.Height > page.ClientSize.Height)
						faults.Add(line + ": the page scrolls.");
					if (!page.ClientRectangle.Contains(resetAt))
						faults.Add(line + ": [Reset] is out of view.");
					if (tableAt.Bottom > general.ClientSize.Height)
						faults.Add(line + string.Format(": the General tab's rows end at {0} on a tab {1} tall.", tableAt.Bottom, general.ClientSize.Height));
				}, choice);
			foreach (var line in report)
				TestContext.WriteLine(line);
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
			if (untestable.Count > 0)
				Assert.Inconclusive(string.Format("{0} of {1} case(s) need a window taller than Windows allows here ({2} px, from the largest screen attached); attach a taller screen to check them:{3}{4}",
					untestable.Count, report.Count, SystemInformation.MaxWindowTrackSize.Height, Environment.NewLine, string.Join(Environment.NewLine, untestable)));
		}

		[TestMethod, TestCategory("ui")]
		[Description("With no saved place, on a screen shorter than the page needs, the main window is as tall as the work area and inside it, and the page scrolls with its tab strip below the device list, at 100 % and 150 %, in both themes")]
		public void On_a_short_screen_the_window_takes_the_work_area_height_and_the_page_scrolls()
		{
			var faults = new List<string>();
			var report = new List<string>();
			ButtonIconTest.OnEverySurface(new Func<Control>[] { MainWindowWithControllerPage }, (window, when) =>
			{
				var page = Page(window);
				var work = WorkArea(ShortScreen);
				OpenWithNoSavedPlace(window, page, work);
				Func<string, Rectangle> at = name =>
				{
					var c = page.Controls.Find(name, true).Single();
					return page.RectangleToClient(c.Parent.RectangleToScreen(c.Bounds));
				};
				var grid = at(nameof(PadControl.MappedDevicesDataGridView));
				var tabs = (TabControl)page.Controls.Find(nameof(PadControl.PadTabControl), true).Single();
				var strip = at(nameof(PadControl.PadTabControl));
				var line = string.Format("{0}: opened at {1} in {2}; the page is {3} and needs {4} px{5}; device list {6}, tabs {7}",
					when, window.Bounds, work, page.ClientSize, page.AutoScrollMinSize.Height,
					page.VerticalScroll.Visible ? ", with a scroll bar" : "", grid, strip);
				report.Add(line);
				if (window.Height != work.Height)
					faults.Add(line + ": the window is not as tall as the work area.");
				if (!work.Contains(window.Bounds))
					faults.Add(line + ": the window is not inside the work area.");
				if (!page.VerticalScroll.Visible)
					faults.Add(line + ": the page has rows out of view and nothing to scroll them in with.");
				if (strip.Top < grid.Bottom || tabs.GetTabRect(0).Height <= 0)
					faults.Add(line + ": the tab strip is under the device list or has no height.");
				if (strip.Top + tabs.GetTabRect(0).Bottom > page.DisplayRectangle.Height)
					faults.Add(line + ": the tab strip is past the end of the page's scroll.");
			});
			foreach (var line in report)
				TestContext.WriteLine(line);
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("A saved place reopens exactly as it was saved, its place, width and height, shorter or taller than the page needs: the main window grows to its page only when no place is saved, in every interface font, in both themes")]
		public void A_saved_place_reopens_at_its_place_and_size()
		{
			AppFontsTest.LoadFonts();
			var faults = new List<string>();
			var report = new List<string>();
			foreach (var choice in PadValueLabelsTest.FontChoices)
				ButtonIconTest.OnEverySurface(new Func<Control>[] { MainWindowWithControllerPage }, (window, when) =>
				{
					var page = Page(window);
					when += " " + page.Font.Name;
					var work = WorkArea(TallScreen);
					var designed = window.Size;
					var gap = (int)Math.Round(30 * ControlsHelper.DpiScale);
					// Saved shorter than designed, near the top and near the bottom of the work area; and taller than the page needs.
					var shorter = new Size(designed.Width - 30, designed.Height - 100);
					var places = new[]
					{
						new { Name = "shorter", Place = new Rectangle(work.Left + gap, work.Top + gap, shorter.Width, shorter.Height) },
						new { Name = "shorter, low", Place = new Rectangle(work.Left + gap, work.Bottom - gap - shorter.Height, shorter.Width, shorter.Height) },
						new { Name = "taller", Place = new Rectangle(work.Left + gap, work.Top + gap, shorter.Width, work.Height - 2 * gap) },
					};
					foreach (var saved in places)
					{
						// As the main window opens at a saved place: put there, at that size, by the saved place, which Windows then
						// does not place, and given its first size before it is shown.
						window.StartPosition = FormStartPosition.Manual;
						window.Bounds = saved.Place;
						Application.DoEvents();
						MainForm.FitToPage(window, page, work);
						Application.DoEvents();
						var line = string.Format("{0}, saved {1} at {2}: opened at {3} in {4}; the page is {5} and needs {6} px",
							when, saved.Name, saved.Place, window.Bounds, work, page.ClientSize, page.AutoScrollMinSize.Height);
						report.Add(line);
						if (window.Bounds != saved.Place)
							faults.Add(line + ": not where and as large as it was saved.");
					}
				}, choice);
			foreach (var line in report)
				TestContext.WriteLine(line);
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("The header of the main window, Load Presets and Map Device is one line of its subject and two of its tip tall whatever they say, a longer tip ending with an ellipsis, and changing them or the status bar's text lays out nothing beside the header and moves nothing, in both themes, at the screen's scale")]
		public void The_header_stays_as_tall_whatever_it_says()
		{
			var faults = new List<string>();
			var report = new List<string>();
			var windows = new Func<Control>[] { MainWindowWithControllerPage, () => new LoadPresetsForm(), () => new MapDeviceToControllerForm() };
			var longTip = string.Join(" ", Enumerable.Repeat("A tip much longer than two lines of the header, so that it is cut and ends with an ellipsis.", 8));
			ButtonIconTest.OnEverySurface(windows, (form, when) =>
			{
				var window = (BaseFormWithHeader)form;
				var header = window.TopPanel;
				var tip = window.HelpBodyLabel;
				when += " " + window.GetType().Name;
				var height = header.Height;
				var others = window.Controls.Cast<Control>().Where(x => x != header && x.Visible).ToList();
				var places = others.ToDictionary(x => x, x => x.Bounds);
				var layouts = new List<string>();
				foreach (var control in new Control[] { header, window.HeaderTableLayoutPanel }.Concat(others.Where(x => !(x is ToolStrip))))
				{
					var name = control.Name.Length > 0 ? control.Name : control.GetType().Name;
					control.Layout += (s, e) => layouts.Add(name);
				}
				var status = others.OfType<StatusStrip>().SelectMany(x => x.Items.OfType<ToolStripStatusLabel>()).FirstOrDefault();
				var steps = new List<KeyValuePair<string, Action>>
				{
					new KeyValuePair<string, Action>("a long tip", () => window.SetHeaderBody(MessageBoxIcon.None, longTip)),
					new KeyValuePair<string, Action>("the help of a control", () => window.ShowHelp("A control's name", "What it does.")),
					new KeyValuePair<string, Action>("a long notice", () => window.SetHeaderInfo(longTip)),
					new KeyValuePair<string, Action>("an error", () => window.SetHeaderError("Something failed.")),
					new KeyValuePair<string, Action>("the help cleared", () => window.ClearHelp()),
				};
				if (status != null)
					steps.Add(new KeyValuePair<string, Action>("a status", () => status.Text = "A status message of another length, " + DateTime.Now.Ticks));
				foreach (var step in steps)
				{
					step.Value();
					Application.DoEvents();
					var line = string.Format("{0}, {1}: the header is {2} tall (was {3}), its tip {4} for two lines of {5} px",
						when, step.Key, header.Height, height, tip.Size, tip.Font.Height);
					report.Add(line);
					if (header.Height != height)
						faults.Add(line + ": the header's height changed.");
					if (tip.Height != BaseFormWithHeader.Lines(tip, 2) + tip.Padding.Vertical || tip.Height >= 3 * tip.Font.Height || !tip.AutoEllipsis)
						faults.Add(line + ": the tip is not two lines tall with an ellipsis.");
					foreach (var other in others.Where(x => x.Bounds != places[x]))
						faults.Add(line + ": " + other.Name + " moved from " + places[other] + " to " + other.Bounds + ".");
				}
				window.SetHeaderBody(MessageBoxIcon.None, longTip);
				Application.DoEvents();
				if (tip.GetPreferredSize(new Size(tip.Width, 0)).Height <= tip.Height)
					faults.Add(when + ": the long tip fits two lines, so its cut is not tried.");
				if (layouts.Count > 0)
					faults.Add(when + ": text changes laid out " + string.Join(", ", layouts.Distinct()) + ".");
			});
			foreach (var line in report)
				TestContext.WriteLine(line);
			Assert.IsTrue(report.Count >= 30, "Only " + report.Count + " changes were looked at.");
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults.Distinct()));
		}

		/// <summary>The main window at its default size holding a Controller page, as task 17's tests hold it.</summary>
		static Control MainWindowWithControllerPage()
		{
			return OptionsLayoutTest.MainWindowAtDefaultSize(new PadControl(MapTo.Controller2), "Controller 2");
		}

		static PadControl Page(Form window)
		{
			return Descendants(window).OfType<PadControl>().Single();
		}

		/// <summary>
		/// Opens the window as the main window opens with no saved place: where Windows puts it, then given its first size and
		/// place before it is shown.
		/// </summary>
		static void OpenWithNoSavedPlace(Form window, PadControl page, Rectangle work)
		{
			window.StartPosition = FormStartPosition.WindowsDefaultLocation;
			MainForm.FitToPage(window, page, work);
			Application.DoEvents();
		}

		/// <summary>A work area of <paramref name="logical"/> at the scale windows are made at, off every screen.</summary>
		static Rectangle WorkArea(Size logical)
		{
			var scale = ControlsHelper.DpiScale;
			return new Rectangle(-12000, -12000, (int)Math.Round(logical.Width * scale), (int)Math.Round(logical.Height * scale));
		}

		/// <summary>In the middle of the work area, to the pixel the halving rounds to.</summary>
		static bool IsCentred(Rectangle window, Rectangle work)
		{
			return Math.Abs(window.Left - work.Left - (work.Right - window.Right)) <= 1
				&& Math.Abs(window.Top - work.Top - (work.Bottom - window.Bottom)) <= 1;
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
