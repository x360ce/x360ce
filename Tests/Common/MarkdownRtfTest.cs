// @under-test: Engine/Common/MarkdownRtf.cs
// @area: documents   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// What the Markdown the documents are written in promises to become.
	/// </summary>
	/// <remarks>
	/// The documents are held as Markdown and rendered when opened, so this rendering is the only
	/// thing standing between what is written and what a person reads. There is no second file to
	/// compare against and no proof-reading step: if a construct renders wrongly, it is wrong on
	/// screen and nowhere else.
	/// </remarks>
	[TestClass]
	public class MarkdownRtfTest
	{

		static string Rtf(string markdown)
		{
			return MarkdownRtf.ToRtf(markdown);
		}

		[TestMethod, TestCategory("documents"), TestCategory("critical")]
		[Description("A backslash, a brace and an accent survive")]
		public void A_backslash_a_brace_and_an_accent_survive()
		{
			// Every Windows path in these documents contains backslashes, and RTF is written in
			// backslashes, so this is the common case rather than the odd one. Getting it wrong does
			// not lose one character: the document stops making sense from that point onwards.
			var rtf = Rtf(@"C:\Program Files\x360ce");
			StringAssert.Contains(rtf, @"C:\\Program Files\\x360ce",
				"A path lost its backslashes, so everything after it is read as instructions.");

			StringAssert.Contains(Rtf("a {b} c"), @"a \{b\} c",
				"Braces open and close a group in RTF and have to be spelled out.");

			// The degree sign appears in the steering wheel section and the arrow in every set of
			// instructions. Neither survives being written directly.
			StringAssert.Contains(Rtf("1080\u00B0"), @"\u176?");
			StringAssert.Contains(Rtf("a \u2192 b"), @"\u8594?");
		}

		[TestMethod, TestCategory("documents"), TestCategory("critical")]
		[Description("A link is clickable and carries its address")]
		public void A_link_is_clickable_and_carries_its_address()
		{
			// Half the value of the help is the addresses in it. A link that renders as plain text
			// still reads, but nobody can follow it.
			var rtf = Rtf("See [the site](https://www.x360ce.com) for help.");
			StringAssert.Contains(rtf, "HYPERLINK", "The link is not a link.");
			StringAssert.Contains(rtf, "https://www.x360ce.com");
			StringAssert.Contains(rtf, "the site", "The words of the link went missing.");

			// The documents mostly write bare addresses in angle brackets.
			StringAssert.Contains(Rtf("<https://www.x360ce.com>"), "HYPERLINK");
		}

		[TestMethod, TestCategory("documents"), TestCategory("critical")]
		[Description("A formula in a code span is not read as emphasis")]
		public void A_formula_in_a_code_span_is_not_read_as_emphasis()
		{
			// This is why formulas are written in code spans. An asterisk means multiply here, and
			// mistaking a pair of them for emphasis would eat the middle of the formula.
			var rtf = Rtf("Use `=a1*abs(a1)*2` for aiming.");
			StringAssert.Contains(rtf, "=a1*abs(a1)*2",
				"The formula was changed on its way to the screen, which makes the help wrong " +
				"rather than merely ugly.");
		}

		[TestMethod, TestCategory("documents"), TestCategory("critical")]
		[Description("Colour follows meaning, not decoration")]
		public void Colour_follows_meaning_not_decoration()
		{
			// The three colours in the original documents each mean one thing. Plain Markdown can
			// carry that because the meanings line up with constructs it already has.
			StringAssert.Contains(Rtf("Run `x360ce.exe`."), @"\cf1 ",
				"A literal value is not coloured as one.");
			StringAssert.Contains(Rtf("Click `[Install]`."), @"\cf2 ",
				"Something to click is not coloured as one.");
			StringAssert.Contains(Rtf("**DO NOT** do that."), @"\cf3 ",
				"A warning is not coloured as one, which is the one case where colour is doing " +
				"real work.");
		}

		[TestMethod, TestCategory("documents")]
		[Description("Headings, lists and rules each become themselves")]
		public void Headings_lists_and_rules_each_become_themselves()
		{
			var heading = Rtf("## Install and Use");
			// Bold and larger. Control words run together in RTF, so the size follows the
			// bold marker with nothing between them.
			StringAssert.Contains(heading, @"\b\fs", "A heading is not bold and larger.");
			StringAssert.Contains(heading, @"\b0", "A heading never stops being bold.");
			StringAssert.Contains(heading, "Install and Use");
			Assert.IsFalse(heading.Contains("##"), "The hashes reached the screen.");

			StringAssert.Contains(Rtf("- first"), @"\'b7", "A bullet has no bullet.");
			StringAssert.Contains(Rtf("3. third"), "3.",
				"The number written in the document is the number shown, so a document that " +
				"skips a step still reads as it was written.");
			StringAssert.Contains(Rtf("---"), "brdrb", "A rule drew nothing.");
		}

		[TestMethod, TestCategory("documents")]
		[Description("A list is indented before its marker, not after it")]
		public void A_list_is_indented_before_its_marker_not_after_it()
		{
			// The step has to be taken before the bullet or number. With it taken after, the marker
			// stands in the same column as the paragraph above and the list stops looking like one.
			// Read the two numbers out of the paragraph and compare them: the marker starts at the
			// left indent less the first-line indent, and that has to be off the margin.
			foreach (var item in new[] { "- a bullet", "1. a step" })
			{
				var rtf = Rtf(item);
				var hanging = Number(rtf, @"\\fi-(\d+)", "first-line indent", item);
				var left = Number(rtf, @"\\li(\d+)", "left indent", item);
				Assert.IsTrue(left - hanging > 0,
					"The marker of '" + item + "' sits on the margin, so the indent lands after it " +
					"rather than before it.");
				Assert.IsTrue(left > left - hanging,
					"The words of '" + item + "' do not start after the marker.");
			}
		}

		[TestMethod, TestCategory("documents")]
		[Description("A line written under a list item stays inside it")]
		public void A_line_written_under_a_list_item_stays_inside_it()
		{
			// Half the addresses in the documents are written on the line below the item they belong
			// to. Rendered at the margin they read as a new point rather than part of that one.
			var rtf = Rtf(string.Join(System.Environment.NewLine, new[] {
				"- Redistributable Package (x86):",
				"  <https://example.com/download>",
			}));
			var left = Number(rtf, @"\\li(\d+)", "left indent", "the list item");
			var paragraphs = System.Text.RegularExpressions.Regex.Matches(rtf, @"\\li(\d+)");
			Assert.AreEqual(2, paragraphs.Count, "The item and the line under it are not two paragraphs.");
			Assert.AreEqual(left.ToString(), paragraphs[1].Groups[1].Value,
				"The address under the item fell back to the margin, outside the list.");
		}

		/// <summary>The first number a pattern finds in the rendered document.</summary>
		static int Number(string rtf, string pattern, string what, string source)
		{
			var m = System.Text.RegularExpressions.Regex.Match(rtf, pattern);
			Assert.IsTrue(m.Success, "'" + source + "' rendered without a " + what + ".");
			return int.Parse(m.Groups[1].Value);
		}

		[TestMethod, TestCategory("documents")]
		[Description("A picture is named rather than silently dropped")]
		public void A_picture_is_named_rather_than_silently_dropped()
		{
			// The box cannot show one. Saying so leaves the reader knowing something is missing,
			// instead of a sentence that refers to a picture which is not there.
			var rtf = Rtf("![Site Bindings dialog](.HowToBuild/iis-site-bindings.png)");
			StringAssert.Contains(rtf, "picture");
			StringAssert.Contains(rtf, "Site Bindings dialog");
		}

		[TestMethod, TestCategory("documents")]
		[Description("The result is a document a reader can open")]
		public void The_result_is_a_document_a_reader_can_open()
		{
			// Braces have to balance or the whole document is refused rather than shown imperfectly.
			var rtf = Rtf(string.Join(System.Environment.NewLine, new[] {
				"# Title",
				"",
				"Text with `code`, **a warning**, a [link](https://example.com) and C:\\a\\b.",
				"",
				"- a bullet",
			}));
			Assert.IsTrue(rtf.StartsWith(@"{\rtf1"), "Not an RTF document at all.");
			var depth = 0;
			for (var i = 0; i < rtf.Length; i++)
			{
				if (i > 0 && rtf[i - 1] == '\\')
					continue;
				if (rtf[i] == '{') depth++;
				if (rtf[i] == '}') depth--;
				Assert.IsTrue(depth >= 0, "A group closed that was never opened, at " + i + ".");
			}
			Assert.AreEqual(0, depth, "Groups do not balance, so the document will not open.");
		}

	}
}
