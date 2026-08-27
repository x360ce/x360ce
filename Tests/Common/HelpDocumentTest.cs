// @under-test: App.v4/Documents/Help.rtf
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The help files shipped with each application are written by hand, so nothing stops them falling
	/// behind the parser. These tests are what stops it: every operator, function and control the parser
	/// accepts has to appear in both files, and each file has to stay loadable.
	/// </summary>
	[TestClass]
	public class HelpDocumentTest
	{

		/// <summary>
		/// The help files that must describe formulas.
		/// </summary>
		/// <remarks>
		/// Version 4 only. Version 3 shares the engine but has nothing that works a formula out while
		/// polling, so a row holding one would do nothing there. Describing it in version 3's help
		/// would be telling somebody about a feature their copy does not have.
		/// </remarks>
		private static readonly string[] HelpFiles = { "App.v4/Documents/Help.rtf" };

		private static string Read(string relativePath)
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, relativePath);
			Assert.IsTrue(File.Exists(path), "Help file is missing: " + relativePath);
			// Both files are single-byte encoded, and neither is read as text by the applications, so
			// reading them this way is enough to look for the words they must contain.
			return File.ReadAllText(path, Encoding.GetEncoding("iso-8859-1"));
		}

		private static void Mentions(string help, string what, string file, string kind)
		{
			Assert.IsTrue(help.IndexOf(what, System.StringComparison.Ordinal) >= 0,
				string.Format("{0} does not mention the {1} '{2}', which the parser accepts.", file, kind, what));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Both help files describe every function the parser accepts")]
		public void Help_describes_every_function()
		{
			foreach (var file in HelpFiles)
			{
				var help = Read(file);
				foreach (var pair in MapExpression.FunctionArity)
					Mentions(help, pair.Key, file, "function");
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Both help files describe every control an expression can read")]
		public void Help_describes_every_control_prefix()
		{
			// A player who does not know that a hat switch is 'p' cannot write an expression for it,
			// and has no way to find out other than this page.
			foreach (var file in HelpFiles)
			{
				var help = Read(file);
				foreach (var source in MapExpressionHelp.Sources)
				{
					Mentions(help, source.Example, file, "control");
					Mentions(help, source.Name, file, "control name");
					Mentions(help, source.Range, file, "value range for " + source.Letter);
				}
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Both help files list every operator, and name which are binary and which unary")]
		public void Help_lists_both_kinds_of_operator()
		{
			foreach (var file in HelpFiles)
			{
				var help = Read(file);
				Mentions(help, "combine two values", file, "heading for binary operators");
				Mentions(help, "act on one value", file, "heading for unary operators");
				foreach (var op in MapExpressionHelp.BinaryOperators)
				{
					Mentions(help, op.Name, file, "operator name");
					Mentions(help, op.Example.Substring(1), file, "operator example");
				}
				foreach (var op in MapExpressionHelp.UnaryOperators)
					Mentions(help, op.Name, file, "operator name");
			}
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Both help files show the button logic, since it replaces operators that do not exist")]
		public void Help_shows_how_buttons_do_logic()
		{
			// There are no and, or or not operators. If the page does not say how to get them, a player
			// concludes the program cannot do it.
			foreach (var file in HelpFiles)
			{
				var help = Read(file);
				foreach (var example in MapExpressionHelp.ButtonLogic)
					Mentions(help, example.Expression.Substring(1), file, "button logic example");
			}
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Both help files warn that older versions ignore an expression")]
		public void Help_warns_that_older_versions_ignore_expressions()
		{
			// Configurations are shared. Somebody who does not know this loses a mapping and blames the
			// program rather than the version that opened it.
			foreach (var file in HelpFiles)
				Mentions(Read(file), "Older versions", file, "warning");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The help files are still structurally sound after being edited")]
		public void Help_files_are_still_well_formed()
		{
			// Both are loaded by the applications at startup. A malformed file shows an empty Help tab,
			// which nobody notices until a user asks why the page is blank.
			var html = Read("App.v3/Documents/Help.htm");
			Assert.AreEqual(1, CountOf(html, "<body>"), "The v3 help must have exactly one body.");
			Assert.AreEqual(1, CountOf(html, "</body>"), "The v3 help body is not closed exactly once.");
			Assert.AreEqual(CountOf(html, "<table"), CountOf(html, "</table>"), "A table is left open in the v3 help.");
			Assert.AreEqual(CountOf(html, "<tr>"), CountOf(html, "</tr>"), "A table row is left open in the v3 help.");
			Assert.IsTrue(html.TrimEnd().EndsWith("</html>"), "The v3 help does not end as a document.");

			var rtf = Read("App.v4/Documents/Help.rtf");
			Assert.IsTrue(rtf.StartsWith(@"{\rtf1"), "The v4 help is no longer a rich text document.");
			Assert.AreEqual(CountOf(rtf, "{"), CountOf(rtf, "}"),
				"Braces are unbalanced in the v4 help, so it will not load.");
		}

		private static int CountOf(string text, string what)
		{
			var count = 0;
			var at = 0;
			while ((at = text.IndexOf(what, at, System.StringComparison.Ordinal)) >= 0)
			{
				count++;
				at += what.Length;
			}
			return count;
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Both help files describe the clock, which no letter would lead anyone to")]
		public void Help_describes_the_clock()
		{
			// Every other source is a letter and a number, so somebody reading the list of letters finds
			// them all. The clock is a word, so a reader who is not told it exists never discovers it.
			//
			// Looking for the bare word is not enough: "now" sits inside "known", which both files
			// already say, so that test passed while neither file mentioned the clock at all. What is
			// looked for is the sentence that explains it, which nothing else can accidentally contain.
			foreach (var file in HelpFiles)
			{
				var help = Read(file);
				Mentions(help, "Milliseconds since the program started", file, "explanation of " + MapExpression.TimeName);
				Mentions(help, MapExpression.TimeName + "/60000", file, "worked example using " + MapExpression.TimeName);
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Every formula shown in the README is one a person can actually type and save")]
		public void The_readme_only_shows_formulas_that_work()
		{
			// A worked example that will not parse, or is too long to store, teaches somebody that the
			// feature is broken. The README is the first thing they read.
			var readme = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "README.MD"));
			var found = 0;
			foreach (System.Text.RegularExpressions.Match m in
				System.Text.RegularExpressions.Regex.Matches(readme, @"(?m)^\s{4}(=\S+)"))
			{
				var text = m.Groups[1].Value;
				found++;
				Assert.IsTrue(text.Length <= MapExpression.MaxLength,
					"README shows '" + text + "', which is " + text.Length + " characters and cannot be saved.");
				MapExpression parsed;
				string error;
				int position;
				Assert.IsTrue(MapExpression.TryParse(text, out parsed, out error, out position),
					"README shows '" + text + "', which the program refuses: " + error);
			}
			Assert.IsTrue(found >= 4, "Only " + found + " formulas found in the README; the check may have stopped matching.");
		}

	}
}
