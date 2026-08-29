// @under-test: App.v4/Documents/ChangeLog.txt, App.v3/Documents/ChangeLog.txt
// @area: about   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace x360ce.Tests
{
	/// <summary>
	/// Entries inside a release are ordered by what a reader wants to know first.
	/// </summary>
	/// <remarks>
	/// Fixed, because the first question is whether the problem that brought them here is solved.
	/// Removed next, because losing something relied upon matters more than a change to it. Then
	/// Changed, then Added, then Known - the caveat, read last.
	///
	/// This is checked rather than remembered. The order was agreed, applied to two hundred
	/// releases, and then broken again within the hour by one entry appended to the end of a list
	/// instead of into its place. A rule nobody can break by accident is worth more than a rule
	/// everybody agrees with.
	/// </remarks>
	[TestClass]
	public class ChangeLogOrderTest
	{
		/// <summary>The only keywords, most important to a reader first.</summary>
		static readonly string[] Keywords = { "Fixed", "Removed", "Changed", "Added", "Known" };

		static readonly string[] Files =
		{
			"App.v4/Documents/ChangeLog.txt",
			"App.v3/Documents/ChangeLog.txt",
		};

		static readonly Regex Bullet = new Regex(@"^- ([A-Za-z]+):");

		[TestMethod, TestCategory("about"), TestCategory("critical")]
		[Description("Entries in a release run fixed, removed, changed, added, known")]
		public void Entries_are_ordered_by_what_matters_to_a_reader()
		{
			var wrong = new List<string>();
			foreach (var file in Files)
			{
				string release = null;
				var rank = -1;
				foreach (var line in Read(file))
				{
					if (line.StartsWith("## ["))
					{
						release = line;
						rank = -1;
						continue;
					}
					var word = Keyword(line);
					if (word == null)
						continue;
					var here = System.Array.IndexOf(Keywords, word);
					if (here < rank)
						wrong.Add(release + "  ->  " + line);
					rank = here;
				}
			}
			Assert.AreEqual(0, wrong.Count, string.Format(
				"{0} entr(ies) sit out of order. A reader scanning for the fix that brought them "
				+ "here should not meet an addition first. Expected order: {1}.\r\n{2}",
				wrong.Count, string.Join(", ", Keywords),
				string.Join("\r\n", wrong.Take(10).ToArray())));
		}

		[TestMethod, TestCategory("about"), TestCategory("critical")]
		[Description("Only the agreed keywords are used")]
		public void Only_the_agreed_keywords_are_used()
		{
			// One word per meaning. Fix and Fixed, Update and Updated and Changed, New and Added all
			// meant the same thing across the years, and a reader had to learn which was which.
			var strays = new List<string>();
			foreach (var file in Files)
			{
				foreach (var line in Read(file))
				{
					var m = Bullet.Match(line);
					if (!m.Success)
						continue;
					if (!Keywords.Contains(m.Groups[1].Value))
						strays.Add(file + ": " + line);
				}
			}
			Assert.AreEqual(0, strays.Count, string.Format(
				"{0} entr(ies) use a keyword outside {1}.\r\n{2}",
				strays.Count, string.Join(", ", Keywords),
				string.Join("\r\n", strays.Take(10).ToArray())));
		}

		static string Keyword(string line)
		{
			var m = Bullet.Match(line);
			return m.Success && Keywords.Contains(m.Groups[1].Value) ? m.Groups[1].Value : null;
		}

		static IEnumerable<string> Read(string relative)
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
			return File.ReadAllLines(path).Select(x => x.TrimStart('﻿'));
		}
	}
}
