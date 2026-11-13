// @under-test: App.v4/Controls/AxisMapUserControl.cs, App.v4/Controls/AxisMapUserControl.Designer.cs, App.v4/Controls/PadTabPages/WheelChartUserControl.cs, App.v4/Controls/PadTabPages/WheelChartUserControl.Designer.cs, App.v4/Controls/PadControl.Designer.cs
// @area: ui   @layer: unit
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
	/// The charts on a controller's Triggers, Left Stick, Right Stick and Wheel tabs: square, whatever the size of the window,
	/// with what they show named beside or under them.
	/// </summary>
	/// <remarks>
	/// A chart drawn square in the designer is not square once scaled, as the font scales a width and a height by different
	/// amounts, and a chart that fills its column is as wide as the column, whatever its height.
	/// </remarks>
	[TestClass]
	public class ChartSquareTest
	{
		public TestContext TestContext { get; set; }

		[TestMethod, TestCategory("ui")]
		[Description("The charts on the Triggers, Left Stick, Right Stick and Wheel tabs are square, box and plotted area, in a window narrower, wider and taller than the main window's first size, in every interface font, at the screen's scale in both themes; DInput and XInput stand in the names' column with their values where the sliders start, above the sliders; the wheel's stops stand under its chart's edges and middle; nothing is cut off or covered")]
		public void Charts_are_square_at_every_window_size()
		{
			AppFontsTest.LoadFonts();
			var faults = new List<string>();
			var report = new List<string>();
			foreach (var choice in PadValueLabelsTest.FontChoices)
				ButtonIconTest.OnEverySurface(new Func<Control>[] { () => OptionsLayoutTest.MainWindowAtDefaultSize(new PadControl(MapTo.Controller1), "Controller 1") },
					(window, when) => CheckWindow(window, when, faults, report), choice);
			foreach (var line in report.Distinct())
				TestContext.WriteLine(line);
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		/// <summary>Each chart tab of the window's controller page, in a window of each size tried.</summary>
		static void CheckWindow(Form window, string when, List<string> faults, List<string> report)
		{
			var pad = ButtonIconTest.All(window).OfType<PadControl>().Single();
			when += " " + pad.Font.Name;
			var tabs = (TabControl)Find(pad, nameof(PadControl.PadTabControl));
			var first = window.ClientSize;
			var sizes = new[]
			{
				first,
				new Size(first.Width * 3 / 4, first.Height),
				new Size(first.Width * 3 / 5, first.Height),
				new Size(first.Width * 3 / 2, first.Height),
				new Size(first.Width, first.Height * 5 / 4),
			};
			foreach (var name in new[] { nameof(PadControl.TriggersTabPage), nameof(PadControl.LeftThumbTabPage), nameof(PadControl.RightThumbTabPage), nameof(PadControl.WheelTabPage) })
			{
				var tab = (TabPage)Find(pad, name);
				tabs.SelectedTab = tab;
				foreach (var size in sizes)
				{
					window.ClientSize = size;
					Application.DoEvents();
					var at = string.Format("{0}, {1} in a {2} x {3} window", when, tab.Text, size.Width, size.Height);
					foreach (var axis in ButtonIconTest.All(tab).OfType<AxisMapUserControl>())
						CheckAxis(axis, at + ", " + axis.Name, faults, report);
					foreach (var wheel in ButtonIconTest.All(tab).OfType<WheelChartUserControl>())
						CheckWheel(wheel, at, faults, report);
					foreach (var covered in ControlOverlapTest.Covered(tab))
						faults.Add(at + ": " + covered);
				}
			}
			tabs.SelectedIndex = 0;
		}

		/// <summary>A stick's or trigger's chart: square, as tall as its table, with the values in the rows of names and sliders.</summary>
		static void CheckAxis(AxisMapUserControl axis, string at, List<string> faults, List<string> report)
		{
			var chart = axis.MainPictureBox;
			var table = chart.Parent;
			CheckSquare(chart, at, faults, report);
			if (chart.Bottom + chart.Margin.Bottom != table.DisplayRectangle.Bottom)
				faults.Add(string.Format("{0}: the chart ends {1} px above its table's bottom.", at, table.DisplayRectangle.Bottom - chart.Bottom - chart.Margin.Bottom));
			// Every row beside the chart is inside the table.
			foreach (var cut in table.Controls.Cast<Control>().Where(x => x.Visible && x.Bottom > table.DisplayRectangle.Bottom))
				faults.Add(string.Format("{0}: {1} is cut off at the table's bottom.", at, cut.Name));
			var names = new[] { "DInputLabel", "XInputLabel", "DeadZoneLabel", "AntiDeadZoneLabel", "SensitivityLabel" }.Select(x => Find(axis, x)).ToList();
			if (names.Select(x => x.Left).Distinct().Count() > 1)
				faults.Add(at + ": the names start at " + string.Join(", ", names.Select(x => x.Name + " " + x.Left)) + ".");
			var values = new[] { "DInputValueLabel", "XInputValueLabel" }.Select(x => Find(axis, x)).ToList();
			var slider = Find(axis, nameof(AxisMapUserControl.DeadZoneTrackBar));
			foreach (var value in values)
			{
				if (value.Left != slider.Left)
					faults.Add(string.Format("{0}: {1} starts at {2}, the sliders at {3}.", at, value.Name, value.Left, slider.Left));
				if (value.Bottom > slider.Top)
					faults.Add(string.Format("{0}: {1} is not above the sliders.", at, value.Name));
			}
			if (names[0].Bottom > names[1].Top || names[1].Bottom > slider.Top)
				faults.Add(at + ": DInput is not above XInput, or XInput not above the sliders.");
		}

		/// <summary>The wheel's chart: square, inside its control, with the stops under its edges and middle.</summary>
		static void CheckWheel(WheelChartUserControl wheel, string at, List<string> faults, List<string> report)
		{
			var chart = wheel.MainPictureBox;
			CheckSquare(chart, at, faults, report);
			var table = chart.Parent;
			foreach (var cut in table.Controls.Cast<Control>().Where(x => x.Visible && x.Bottom > table.DisplayRectangle.Bottom))
				faults.Add(string.Format("{0}: {1} is cut off at the chart's bottom.", at, cut.Name));
			if (wheel.Bottom > wheel.Parent.DisplayRectangle.Bottom)
				faults.Add(at + ": the wheel's chart reaches past its page's bottom.");
			var left = Find(wheel, "LeftStopLabel");
			var centre = Find(wheel, "CentreLabel");
			var right = Find(wheel, "RightStopLabel");
			if (left.Left != chart.Left)
				faults.Add(string.Format("{0}: the left stop starts at {1}, the chart at {2}.", at, left.Left, chart.Left));
			if (right.Right != chart.Right)
				faults.Add(string.Format("{0}: the right stop ends at {1}, the chart at {2}.", at, right.Right, chart.Right));
			var middle = chart.Left + chart.Width / 2f;
			// The centre's name stands in the middle of three equal columns: the table gives the pixels a width does not
			// share out evenly to the last, up to two, and the name is placed on a whole pixel, so up to 2 px off.
			if (Math.Abs(centre.Left + centre.Width / 2f - middle) > 2f)
				faults.Add(string.Format("{0}: the centre's name is {1:0.#} px from the chart's middle.", at, centre.Left + centre.Width / 2f - middle));
		}

		/// <summary>A chart's box and the area plotted in it are each as wide as they are tall.</summary>
		static void CheckSquare(PictureBox chart, string at, List<string> faults, List<string> report)
		{
			var area = AppHelper.ChartBounds(chart);
			report.Add(string.Format("{0}: box {1} x {2}, plotted {3} x {4}", at, chart.Width, chart.Height, area.Width, area.Height));
			if (chart.Width != chart.Height || area.Width != area.Height)
				faults.Add(string.Format("{0}: the chart is {1} x {2}, plotted {3} x {4}.", at, chart.Width, chart.Height, area.Width, area.Height));
		}

		static Control Find(Control root, string name)
		{
			return root.Controls.Find(name, true).Single();
		}
	}
}
