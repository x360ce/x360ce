using System;
using System.Data;
using System.Globalization;
using System.Text;

namespace x360ce.Engine
{
	/// <summary>
	/// Draws a top list for the website as SVG: one line per item, its count right-aligned, then its name.
	/// </summary>
	/// <remarks>
	/// The input is what <c>x360ce_GetTopGames</c> or <c>x360ce_GetTopControllers</c> answers: one row per
	/// item with <c>InstanceCount</c> and a name column. The text takes the colour and font of the page that
	/// carries the picture inline (<c>currentColor</c>, an inherited font), and positions are in em, so the
	/// lines follow the page's font size. Each name's text element holds the plain name in <c>data-name</c>,
	/// so a page that carries the picture can link a name to its own content; the picture links nothing.
	/// </remarks>
	public static class TopListSvg
	{
		const double LineHeight = 1.5;
		const double CountRight = 5.5;
		const double NameLeft = 6;

		/// <summary>The list, or a line saying there is no data when the table has no rows.</summary>
		/// <param name="table">Rows with <c>InstanceCount</c> and <paramref name="nameColumn"/>.</param>
		/// <param name="nameColumn">The column holding each item's name.</param>
		/// <param name="title">What the list shows, read by screen readers.</param>
		public static string Render(DataTable table, string nameColumn, string title)
		{
			var rows = table == null ? 0 : table.Rows.Count;
			var sb = new StringBuilder();
			sb.AppendFormat(CultureInfo.InvariantCulture,
				"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100%\" height=\"{0}em\" aria-label=\"{1}\" " +
				"style=\"display:block;overflow:visible;font:inherit;fill:currentColor\">",
				N(Math.Max(1, rows) * LineHeight), Escape(title));
			if (rows == 0)
				sb.AppendFormat(CultureInfo.InvariantCulture, "<text x=\"0\" y=\"{0}em\">No data was returned.</text>", N(Baseline(0)));
			for (var i = 0; i < rows; i++)
			{
				var row = table.Rows[i];
				var count = Convert.ToInt64(row["InstanceCount"], CultureInfo.InvariantCulture).ToString("0,00", CultureInfo.InvariantCulture);
				var name = Convert.ToString(row[nameColumn], CultureInfo.InvariantCulture);
				sb.AppendFormat(CultureInfo.InvariantCulture,
					"<text x=\"{0}em\" y=\"{1}em\" text-anchor=\"end\">{2} -</text>", N(CountRight), N(Baseline(i)), count);
				sb.AppendFormat(CultureInfo.InvariantCulture,
					"<text x=\"{0}em\" y=\"{1}em\" data-name=\"{2}\">{2}</text>", N(NameLeft), N(Baseline(i)), Escape(name));
			}
			sb.Append("</svg>");
			return sb.ToString();
		}

		/// <summary>The baseline of a line, a little above its bottom.</summary>
		static double Baseline(int line)
		{
			return (line + 1) * LineHeight - 0.4;
		}

		static string N(double value)
		{
			return value.ToString("0.##", CultureInfo.InvariantCulture);
		}

		static string Escape(string text)
		{
			return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
		}
	}
}
