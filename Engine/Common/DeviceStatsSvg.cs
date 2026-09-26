using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace x360ce.Engine
{
	/// <summary>
	/// Draws the "Controllers in Database" picture for the website as inline SVG.
	/// </summary>
	/// <remarks>
	/// The input is what <c>x360ce_GetNewDeviceStats</c> answers: one row per month with the
	/// running total of controllers in <c>NewDevicesSum</c>. The output is text the page carries
	/// itself, so there is no charting assembly, no image handler and no folder of temporary
	/// files; it scales with the page through its viewBox. Every number is written with the
	/// invariant culture, because a decimal comma inside an attribute breaks the picture.
	/// </remarks>
	public static class DeviceStatsSvg
	{
		const int Width = 800;
		const int Height = 300;
		const int Left = 60;
		const int Right = 784;
		const int Top = 40;
		const int Bottom = 270;
		const string Title = "Controllers in Database";

		/// <summary>The picture up to the current month, or a titled empty frame when the table has no rows.</summary>
		public static string Render(DataTable table)
		{
			return Render(table, DateTime.Today);
		}

		/// <summary>
		/// The picture from the first month in the table to <paramref name="today"/>'s month. The
		/// total is a running one, so a month with no row shows the total of the month before it.
		/// </summary>
		public static string Render(DataTable table, DateTime today)
		{
			var byMonth = new SortedDictionary<DateTime, long>();
			if (table != null)
			{
				foreach (DataRow row in table.Rows)
				{
					var date = (DateTime)row["Date"];
					byMonth[new DateTime(date.Year, date.Month, 1)] = Convert.ToInt64(row["NewDevicesSum"], CultureInfo.InvariantCulture);
				}
			}
			var last = new DateTime(today.Year, today.Month, 1);
			var totals = new List<long>();
			DateTime first = last;
			if (byMonth.Count > 0)
			{
				foreach (var pair in byMonth)
				{
					first = pair.Key;
					break;
				}
				if (last < first)
					last = first;
				long carried = 0;
				for (var month = first; month <= last; month = month.AddMonths(1))
				{
					long total;
					if (byMonth.TryGetValue(month, out total))
						carried = total;
					totals.Add(carried);
				}
			}
			var title = totals.Count == 0
				? Title + " • no data yet"
				: string.Format(CultureInfo.InvariantCulture, "{0} • Monthly, {1:yyyy-MM} - {2:yyyy-MM}", Title, first, last);

			var sb = new StringBuilder();
			sb.AppendFormat(CultureInfo.InvariantCulture,
				"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {0} {1}\" width=\"100%\" role=\"img\" aria-label=\"{2}\" " +
				"style=\"display:block;max-width:{0}px;margin:0 auto;font-family:'Trebuchet MS','Segoe UI',sans-serif;font-size:12px;fill:#111\">",
				Width, Height, Escape(title));
			sb.Append("<title>").Append(Escape(title)).Append("</title>");
			sb.AppendFormat(CultureInfo.InvariantCulture, "<text x=\"{0}\" y=\"20\" text-anchor=\"middle\" font-size=\"14\">{1}</text>", Width / 2, Escape(title));
			sb.AppendFormat(CultureInfo.InvariantCulture, "<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"none\" stroke=\"#111\" stroke-width=\"1\"/>",
				Left, Top, Right - Left, Bottom - Top);

			if (totals.Count > 0)
			{
				long max = 0;
				foreach (var total in totals)
					if (total > max)
						max = total;
				var step = NiceStep(max);
				var top = Math.Max(step, (long)Math.Ceiling(max / (double)step) * step);
				var plotHeight = Bottom - Top;

				// Grid lines and the totals they stand for.
				for (long value = step; value <= top; value += step)
				{
					var y = Bottom - plotHeight * (value / (double)top);
					sb.AppendFormat(CultureInfo.InvariantCulture,
						"<line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{1}\" stroke=\"#000\" stroke-opacity=\"0.25\"/>", Left, N(y), Right);
					sb.AppendFormat(CultureInfo.InvariantCulture,
						"<text x=\"{0}\" y=\"{1}\" text-anchor=\"end\" class=\"axis\">{2}</text>", Left - 6, N(y + 4), Short(value));
				}

				// One bar per month on a calendar axis; every January is labelled with its year.
				var slot = (Right - Left) / (double)totals.Count;
				var barWidth = Math.Max(0.5, slot * 0.7);
				for (int m = 0; m < totals.Count; m++)
				{
					var x = Left + m * slot + (slot - barWidth) / 2;
					var height = plotHeight * (totals[m] / (double)top);
					sb.AppendFormat(CultureInfo.InvariantCulture,
						"<rect class=\"bar\" x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"#2674ec\" fill-opacity=\"0.8\"/>",
						N(x), N(Bottom - height), N(barWidth), N(height));
					var month = first.AddMonths(m);
					if (month.Month == 1)
						sb.AppendFormat(CultureInfo.InvariantCulture,
							"<text x=\"{0}\" y=\"{1}\" text-anchor=\"middle\" class=\"axis\">{2}</text>",
							N(x + barWidth / 2), Bottom + 16, month.Year);
				}
			}
			sb.Append("</svg>");
			return sb.ToString();
		}

		/// <summary>A round grid step (1, 2, 2.5 or 5 times a power of ten) giving about four lines.</summary>
		static long NiceStep(long max)
		{
			if (max <= 4)
				return 1;
			var raw = max / 4.0;
			var magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
			foreach (var factor in new[] { 1, 2, 2.5, 5, 10 })
			{
				var step = factor * magnitude;
				if (step >= raw)
					return (long)step;
			}
			return (long)(10 * magnitude);
		}

		/// <summary>1500000 as "1.5M", 800000 as "800k", 250 as "250".</summary>
		static string Short(long value)
		{
			if (value >= 1000000)
				return (value / 1000000.0).ToString("0.#", CultureInfo.InvariantCulture) + "M";
			if (value >= 1000)
				return (value / 1000.0).ToString("0.#", CultureInfo.InvariantCulture) + "k";
			return value.ToString(CultureInfo.InvariantCulture);
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
