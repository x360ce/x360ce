// @under-test: Engine/Common/DeviceStatsSvg.cs
// @area: website   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The "Controllers in Database" picture on the website, drawn as inline SVG from the cached
	/// monthly counts.
	/// </summary>
	/// <remarks>
	/// The picture used to be a PNG made by a charting assembly through its own HTTP handler and
	/// a temp folder of image files. Now it is text the page carries, so what these check is the
	/// text: one bar per month, the title naming the first and last month, an empty table saying
	/// so instead of showing year 0001, and numbers written the same on every server whatever
	/// culture it runs under, because a decimal comma inside an SVG attribute breaks the picture.
	/// </remarks>
	[TestClass]
	public class DeviceStatsSvgTest
	{
		static DataTable Table(params int[] monthlyNew)
		{
			var table = new DataTable();
			table.Columns.Add("Date", typeof(DateTime));
			table.Columns.Add("NewDevices", typeof(int));
			table.Columns.Add("Created", typeof(DateTime));
			table.Columns.Add("NewDevicesSum", typeof(int));
			var sum = 0;
			for (int i = 0; i < monthlyNew.Length; i++)
			{
				sum += monthlyNew[i];
				table.Rows.Add(new DateTime(2011, 9, 1).AddMonths(i), monthlyNew[i], DateTime.UtcNow, sum);
			}
			return table;
		}

		static XElement Parse(string svg)
		{
			var doc = XDocument.Parse(svg);
			return doc.Root;
		}

		static XName Svg(string name) => XName.Get(name, "http://www.w3.org/2000/svg");

		[TestMethod, TestCategory("website"), TestCategory("critical")]
		[Description("One bar per month, the last one the tallest, and the title names the range")]
		public void One_bar_per_month_and_the_title_names_the_range()
		{
			var svg = Parse(DeviceStatsSvg.Render(Table(1, 2195, 2809, 3214), new DateTime(2011, 12, 20)));
			var bars = svg.Descendants(Svg("rect")).Where(x => (string)x.Attribute("class") == "bar").ToList();
			Assert.AreEqual(4, bars.Count, "bars");
			var heights = bars.Select(x => double.Parse((string)x.Attribute("height"), CultureInfo.InvariantCulture)).ToList();
			Assert.AreEqual(heights.Max(), heights.Last(), "The running total is highest in the last month");
			Assert.IsTrue(heights.First() < heights.Last(), "The first month is a sliver next to the last");
			var title = (string)svg.Element(Svg("title"));
			StringAssert.Contains(title, "2011-09 - 2011-12");
		}

		[TestMethod, TestCategory("website"), TestCategory("critical")]
		[Description("An empty table says there is no data yet; it never shows year 0001")]
		public void An_empty_table_says_no_data_yet()
		{
			var text = DeviceStatsSvg.Render(Table());
			var svg = Parse(text);
			Assert.AreEqual(0, svg.Descendants(Svg("rect")).Count(x => (string)x.Attribute("class") == "bar"));
			StringAssert.Contains(text, "no data yet");
			Assert.IsFalse(text.Contains("0001"), text);
		}

		[TestMethod, TestCategory("website"), TestCategory("critical")]
		[Description("Numbers are written with a decimal point whatever culture the server runs under")]
		public void Numbers_are_culture_invariant()
		{
			var culture = Thread.CurrentThread.CurrentCulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = new CultureInfo("lt-LT");
				var text = DeviceStatsSvg.Render(Table(3, 5, 7));
				Parse(text);
				Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(text, @"=""[^""]*\d,\d[^""]*"""),
					"A decimal comma inside an attribute: " + text);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = culture;
			}
		}

		[TestMethod, TestCategory("website"), TestCategory("critical")]
		[Description("Drawn without a date it runs to the current month")]
		public void Without_a_date_it_runs_to_the_current_month()
		{
			var svg = Parse(DeviceStatsSvg.Render(Table(10, 20)));
			StringAssert.Contains((string)svg.Element(Svg("title")), "2011-09 - " + DateTime.Today.ToString("yyyy-MM", CultureInfo.InvariantCulture));
		}

		[TestMethod, TestCategory("website")]
		[Description("The picture scales with the page: a viewBox and a relative width, no fixed pixels")]
		public void The_picture_scales_with_the_page()
		{
			var svg = Parse(DeviceStatsSvg.Render(Table(10, 20)));
			Assert.IsNotNull(svg.Attribute("viewBox"), "viewBox");
			Assert.AreEqual("100%", (string)svg.Attribute("width"));
			Assert.AreEqual("img", (string)svg.Attribute("role"), "Read as one picture by a screen reader");
		}

		[TestMethod, TestCategory("website"), TestCategory("critical")]
		[Description("A month with no row carries the running total forward; the total never drops to nothing")]
		public void Missing_months_carry_the_running_total()
		{
			// 2011-09, 2011-10, then nothing until 2012-09: the eleven months between show the
			// 2011-10 total, and the picture runs to the month it is drawn in.
			var table = Table(10, 20);
			table.Rows.Add(new DateTime(2012, 9, 1), 5, DateTime.UtcNow, 35);
			var today = new DateTime(2012, 12, 15);
			var svg = Parse(DeviceStatsSvg.Render(table, today));
			var bars = svg.Descendants(Svg("rect")).Where(x => (string)x.Attribute("class") == "bar").ToList();
			Assert.AreEqual(16, bars.Count, "2011-09 to 2012-12 is sixteen months, every one drawn");
			var heights = bars.Select(x => double.Parse((string)x.Attribute("height"), CultureInfo.InvariantCulture)).ToList();
			Assert.AreEqual(heights[1], heights[2], 0.001, "November carries October's total");
			Assert.AreEqual(heights[1], heights[11], 0.001, "August carries it still");
			Assert.IsTrue(heights[12] > heights[11], "September adds its own");
			Assert.AreEqual(heights[12], heights[15], 0.001, "The months after the last row carry the last total");
			StringAssert.Contains((string)svg.Element(Svg("title")), "2011-09 - 2012-12", "The range ends at today, not at the last row");
			StringAssert.Contains(svg.ToString(), ">2012<", "The January inside the gap is labelled");
		}

		[TestMethod, TestCategory("website")]
		[Description("Axis labels: a year at every January, totals in thousands or millions")]
		public void Axis_labels_are_years_and_short_totals()
		{
			var months = Enumerable.Repeat(50000, 16).ToArray(); // 2011-09 .. 2012-12, 800 000 total
			var text = DeviceStatsSvg.Render(Table(months), new DateTime(2012, 12, 1));
			StringAssert.Contains(text, ">2012<", "January 2012 is labelled");
			Assert.IsFalse(text.Contains(">2011<"), "2011 has no January in the range");
			StringAssert.Contains(text, "800k", "The top of the axis in thousands");
		}
	}
}
