// @under-test: Engine/Common/TopListSvg.cs
// @area: website   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The website's top games and top controllers, drawn as SVG: one line per item, its count, then its
	/// name, with the plain name in data-name for a page that links names to its own content.
	/// </summary>
	[TestClass]
	public class TopListSvgTest
	{
		static DataTable Table(params (string Name, int Count)[] items)
		{
			var table = new DataTable();
			table.Columns.Add("RowNumber", typeof(long));
			table.Columns.Add("FileProductName", typeof(string));
			table.Columns.Add("InstanceCount", typeof(int));
			for (int i = 0; i < items.Length; i++)
				table.Rows.Add(i + 1, items[i].Name, items[i].Count);
			return table;
		}

		static XName Svg(string name) => XName.Get(name, "http://www.w3.org/2000/svg");

		[TestMethod, TestCategory("website"), TestCategory("critical")]
		[Description("One line per item: the count, then the name, which carries the plain name in data-name")]
		public void One_line_per_item_with_its_name_in_data_name()
		{
			var text = TopListSvg.Render(Table(("Grand Theft Auto V", 129312), ("FIFA 19", 104063)), "FileProductName", "Top Games");
			var svg = XDocument.Parse(text).Root;
			var names = svg.Descendants(Svg("text")).Where(x => x.Attribute("data-name") != null).ToList();
			CollectionAssert.AreEqual(new[] { "Grand Theft Auto V", "FIFA 19" }, names.Select(x => (string)x.Attribute("data-name")).ToArray());
			CollectionAssert.AreEqual(new[] { "Grand Theft Auto V", "FIFA 19" }, names.Select(x => x.Value).ToArray());
			StringAssert.Contains(text, ">129,312 -<");
			Assert.AreEqual("Top Games", (string)svg.Attribute("aria-label"));
			Assert.IsFalse(svg.Descendants(Svg("a")).Any(), "The picture links nothing; a page that carries it adds its own links");
		}

		[TestMethod, TestCategory("website"), TestCategory("critical")]
		[Description("A name with markup characters stays text and keeps the picture valid")]
		public void Names_are_escaped()
		{
			var text = TopListSvg.Render(Table(("Tom & Jerry <\"Deluxe\">", 7)), "FileProductName", "Top Games");
			var name = XDocument.Parse(text).Root.Descendants(Svg("text")).Single(x => x.Attribute("data-name") != null);
			Assert.AreEqual("Tom & Jerry <\"Deluxe\">", (string)name.Attribute("data-name"));
			Assert.AreEqual("Tom & Jerry <\"Deluxe\">", name.Value);
		}

		[TestMethod, TestCategory("website")]
		[Description("An empty table says there is no data, in a picture one line tall")]
		public void An_empty_table_says_no_data()
		{
			var text = TopListSvg.Render(Table(), "FileProductName", "Top Games");
			var svg = XDocument.Parse(text).Root;
			StringAssert.Contains(text, "No data was returned.");
			Assert.AreEqual("1.5em", (string)svg.Attribute("height"));
		}

		[TestMethod, TestCategory("website"), TestCategory("critical")]
		[Description("Numbers are written the same whatever culture the server runs under")]
		public void Numbers_are_culture_invariant()
		{
			var culture = Thread.CurrentThread.CurrentCulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = new CultureInfo("lt-LT");
				var text = TopListSvg.Render(Table(("A", 1234567), ("B", 5), ("C", 3)), "FileProductName", "Top Games");
				XDocument.Parse(text);
				StringAssert.Contains(text, ">1,234,567 -<");
				Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(text, @"=""[^""]*\d,\d[^""]*"""),
					"A decimal comma inside an attribute: " + text);
				Assert.AreEqual("4.5em", (string)XDocument.Parse(text).Root.Attribute("height"), "Three lines of 1.5em");
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = culture;
			}
		}
	}
}
