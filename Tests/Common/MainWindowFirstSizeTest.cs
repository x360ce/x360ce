// @under-test: App.v4/MainForm.cs, App.v4/MainForm.Designer.cs, App.v4/Controls/PadControl.cs, App.v4/Forms/BaseFormWithHeader.cs
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
	/// The main window's smallest size, which shows the Controller page's General tab and bottom buttons whole, and its header's
	/// height, which its text never changes.
	/// </summary>
	[TestClass]
	public class MainWindowFirstSizeTest
	{
		public TestContext TestContext { get; set; }

		[TestMethod, TestCategory("ui")]
		[Description("The main window's smallest size, in its designer file at 100 %, is a multiple of 4 the Controller page's pages, the tallest of them included, and bottom buttons show whole in, with the device list at its fewest rows, no smaller than measured here; at that size, at the screen's scale, in both themes, nothing on them is cut")]
		public void The_smallest_window_shows_the_general_tab_and_the_buttons_whole()
		{
			var faults = new List<string>();
			var report = new List<string>();
			// What the tallest page needs, measured on every page at the screen's scale.
			var measured = PadPageShortWindowTest.Measured(TestContext);
			ButtonIconTest.OnEverySurface(new Func<Control>[] { MainWindowWithControllerPage }, (form, when) =>
			{
				var window = (Form)form;
				window.MinimumSize = PadPageShortWindowTest.MinimumWindowSize();
				window.Size = window.MinimumSize;
				Application.DoEvents();
				var page = Page(window);
				var general = page.GeneralTabPage;
				var table = page.GeneralTableLayoutPanel;
				var columns = (TableLayoutPanel)table.Parent;
				var buttons = page.ButtonsPanel;
				var tallest = PadPageShortWindowTest.TallestNeed(measured, ControlsHelper.DpiScale, when.Split(' ').Last());
				// The narrowest and shortest window the pages are whole in: made smaller a pixel at a time until two of the General
				// tab's controls overlap, which they do once its columns are squeezed, or the tallest page needs more height than
				// the pages have.
				var smallest = window.MinimumSize;
				window.MinimumSize = Size.Empty;
				var scale = ControlsHelper.DpiScale;
				var needed = smallest;
				for (var w = smallest.Width + (int)(32 * scale); w > smallest.Width - (int)(96 * scale); w--)
				{
					window.Size = new Size(w, smallest.Height + (int)(200 * scale));
					Application.DoEvents();
					if (Overlap(table) != null)
						break;
					needed.Width = w;
				}
				for (var h = smallest.Height + (int)(32 * scale); h > smallest.Height - (int)(96 * scale); h--)
				{
					window.Size = new Size(smallest.Width + (int)(200 * scale), h);
					Application.DoEvents();
					var tableAtNow = general.RectangleToClient(table.Parent.RectangleToScreen(table.Bounds));
					if (tableAtNow.Top + table.GetPreferredSize(new Size(table.Width, 0)).Height > general.ClientSize.Height - general.Padding.Bottom
						|| tallest.Value > general.ClientSize.Height)
						break;
					needed.Height = h;
				}
				window.MinimumSize = smallest;
				window.Size = smallest;
				Application.DoEvents();
				var buttonsNeed = buttons.GetPreferredSize(Size.Empty).Width + buttons.Margin.Horizontal;
				var tableAt = general.RectangleToClient(table.Parent.RectangleToScreen(table.Bounds));
				var buttonsAt = page.RectangleToClient(buttons.Parent.RectangleToScreen(buttons.Bounds));
				// At 100 %, as the designer file holds it.
				var at100 = new Size((int)Math.Ceiling(needed.Width / scale), (int)Math.Ceiling(needed.Height / scale));
				var rounded = new Size((at100.Width + 3) / 4 * 4, (at100.Height + 3) / 4 * 4);
				var overlap = Overlap(table);
				var line = string.Format("{0}: the smallest window {1}; the pages are whole down to {2}, {3} at 100 %, {4} rounded up, the tallest, {5}, needing {6} px; the buttons need {7} px across, at {8}",
					when, window.Size, needed, at100, rounded, tallest.Key, tallest.Value, buttonsNeed, buttonsAt);
				report.Add(line);
				// Measured in the real program at 150 %, 1024 x 908 at 100 %, which the Buttons tab needs with three rows in the
				// device list: the controller picture 8 pixels from the names beside it and the last row 8 above the tab's frame.
				// This window, which stands in for the main one, needs no more, and no less than two steps of 4 under it.
				var designed = DesignerMinimumSize();
				if (designed.Width < rounded.Width || designed.Height < rounded.Height || designed.Height - rounded.Height > 8
					|| designed.Width % 4 != 0 || designed.Height % 4 != 0)
					faults.Add(line + ": the designer's smallest size " + designed + " is not the measured size rounded up to 4 px at 100 %.");
				if (overlap != null || tableAt.Top + table.GetPreferredSize(new Size(table.Width, 0)).Height > general.ClientSize.Height - general.Padding.Bottom)
					faults.Add(line + ": the General tab is cut" + (overlap == null ? "." : ": " + overlap + "."));
				if (tallest.Value > general.ClientSize.Height)
					faults.Add(line + ": " + tallest.Key + " is cut, in pages " + general.ClientSize.Height + " px tall.");
				if (buttonsAt.Right > page.ClientSize.Width || buttonsAt.Bottom > page.ClientSize.Height)
					faults.Add(line + ": the bottom buttons are cut.");
			});
			foreach (var line in report)
				TestContext.WriteLine(line);
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("The smallest size the main window takes stays within the work area of a screen smaller than it")]
		public void The_smallest_size_stays_within_a_small_screen()
		{
			Ui.OnUiThread(() =>
			{
				using (var window = new Form { MinimumSize = new Size(1056, 948) })
				{
					MainForm.KeepMinimumOnScreen(window, new Rectangle(0, 0, 1280, 720));
					Assert.AreEqual(new Size(1056, 720), window.MinimumSize, "The smallest size is larger than the screen's work area.");
					MainForm.KeepMinimumOnScreen(window, new Rectangle(0, 0, 2560, 1392));
					Assert.AreEqual(new Size(1056, 720), window.MinimumSize, "A larger screen changed the smallest size.");
				}
			});
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
		/// <summary>Two controls of a table that overlap, as its columns do once the table is narrower than they need; null for none.</summary>
		static string Overlap(TableLayoutPanel table)
		{
			var shown = table.Controls.Cast<Control>().Where(x => x.Visible).ToList();
			for (var i = 0; i < shown.Count; i++)
				for (var j = i + 1; j < shown.Count; j++)
					if (shown[i].Bounds.IntersectsWith(shown[j].Bounds))
						return shown[i].Name + " " + shown[i].Bounds + " over " + shown[j].Name + " " + shown[j].Bounds;
			return null;
		}

		/// <summary>The main window's smallest size as its designer file holds it, at 100 %.</summary>
		static Size DesignerMinimumSize()
		{
			var scale = ControlsHelper.DpiScale;
			var scaled = PadPageShortWindowTest.MinimumWindowSize();
			return new Size((int)Math.Round(scaled.Width / scale), (int)Math.Round(scaled.Height / scale));
		}

		static Control MainWindowWithControllerPage()
		{
			return OptionsLayoutTest.MainWindowAtDefaultSize(new PadControl(MapTo.Controller2), "Controller 2");
		}

		static PadControl Page(Form window)
		{
			return Descendants(window).OfType<PadControl>().Single();
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
