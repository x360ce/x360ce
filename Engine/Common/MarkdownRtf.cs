using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace x360ce.Engine
{

	/// <summary>
	/// How a rendered document looks. One place decides it, for every document.
	/// </summary>
	/// <remarks>
	/// The look used to belong to each document, because each was a finished file somebody had
	/// formatted by hand. They drifted: two documents shown by the same program in the same box
	/// disagreed about their font size and their spacing. Deciding it here means a change applies
	/// everywhere at once and no document can be the odd one out.
	/// </remarks>
	public class RtfStyle
	{
		/// <summary>Body font.</summary>
		public string FontName = "Segoe UI";

		/// <summary>Font for code, where the letters have to line up.</summary>
		public string CodeFontName = "Consolas";

		/// <summary>Body size in points.</summary>
		public float FontSize = 9f;

		/// <summary>Colour of a literal value: a file name, a formula, a setting.</summary>
		public Color Literal = new Color(0, 176, 80);

		/// <summary>Colour of something on screen the reader is told to click.</summary>
		public Color Control = new Color(79, 129, 189);

		/// <summary>Colour of a warning.</summary>
		public Color Warning = new Color(255, 0, 0);

		/// <summary>Colour of a link.</summary>
		public Color Link = new Color(0, 0, 255);

		/// <summary>A colour, as RTF counts them.</summary>
		public struct Color
		{
			public Color(int r, int g, int b) { R = r; G = g; B = b; }
			public readonly int R, G, B;
		}
	}

	/// <summary>
	/// Turns the documents this program ships into what a RichTextBox can show.
	/// </summary>
	/// <remarks>
	/// The documents are written in Markdown and that is the only copy of them. This renders one
	/// when it is opened, so there is no second file to generate, to commit, or to find out of date
	/// later. The same call would serve a build step if the finished text were ever wanted on disk;
	/// nothing here depends on when it runs.
	///
	/// Only the constructs these documents use are understood. Anything else is written out as the
	/// characters it is made of, so a document that reaches past the subset looks wrong on screen
	/// rather than losing a sentence without saying so.
	///
	/// Colour carries meaning rather than decoration, which is what lets plain Markdown produce a
	/// coloured document: a code span is a literal value, a code span in square brackets is
	/// something to click, and bold is a warning. Headings are written as headings, so bold is free
	/// to mean that.
	/// </remarks>
	public static class MarkdownRtf
	{

		/// <summary>Renders a Markdown document as RTF.</summary>
		/// <param name="markdown">The document.</param>
		/// <param name="style">How it should look, or null for the standard look.</param>
		public static string ToRtf(string markdown, RtfStyle style = null)
		{
			if (markdown == null)
				return string.Empty;
			if (style == null)
				style = new RtfStyle();
			var body = new StringBuilder();
			var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
			var half = (int)Math.Round(style.FontSize * 2);
			var inCode = false;
			// The left indent of the list item last written, so a line belonging to that item is put
			// inside it instead of back at the margin. Zero when the last thing written was not one.
			var listIndent = 0;
			for (int i = 0; i < lines.Length; i++)
			{
				var line = lines[i];
				// A fenced block is copied out as it stands, which is the whole point of one.
				if (line.TrimStart().StartsWith("```"))
				{
					inCode = !inCode;
					continue;
				}
				if (inCode)
				{
					body.Append(Pard(400)).Append(@"\sa0\f1 ").Append(Escape(line)).Append(@"\par").Append("\r\n");
					continue;
				}
				var trimmed = line.Trim();
				// A blank line does not end a list item. An item often has a second paragraph under
				// it — a worked example, an address — separated by a blank line and written indented
				// underneath, and that paragraph belongs to the item. What ends the item is the next
				// thing written back at the margin.
				if (trimmed.Length == 0)
					continue;
				// A rule.
				if (trimmed == "---" || trimmed == "***" || trimmed == "___")
				{
					body.Append(Pard(0)).Append(@"\brdrb\brdrs\brdrw10\brsp20\sa120\par").Append("\r\n");
					listIndent = 0;
					continue;
				}
				// A heading. Deeper headings are smaller, down to the body size.
				var heading = Regex.Match(trimmed, @"^(#{1,6})\s+(.*)$");
				if (heading.Success)
				{
					var level = heading.Groups[1].Value.Length;
					// Restrained. This is a help pane a few hundred pixels wide, not a page: a heading
					// three sizes above the body wraps onto a second line and shouts at the reader.
					var size = Math.Max(half, half + (4 - level) * 2);
					body.Append(Pard(0)).Append(@"\sb240\sa80\keepn\b\fs").Append(size).Append(' ')
						.Append(Inline(heading.Groups[2].Value, style))
						.Append(@"\b0\fs").Append(half).Append(@"\par").Append("\r\n");
					listIndent = 0;
					continue;
				}
				// A table row is shown as its cells, separated. Tables are rare here and a real RTF
				// table would be a large amount of machinery for the two that exist.
				if (trimmed.StartsWith("|") && trimmed.EndsWith("|"))
				{
					if (Regex.IsMatch(trimmed, @"^\|[\s:\-\|]+\|$"))
						continue;
					var cells = trimmed.Trim('|').Split('|');
					for (int c = 0; c < cells.Length; c++)
						cells[c] = cells[c].Trim();
					body.Append(Pard(200)).Append(@"\sa40 ")
						.Append(Inline(string.Join("   -   ", cells), style))
						.Append(@"\par").Append("\r\n");
					continue;
				}
				// A bullet.
				var bullet = Regex.Match(line, @"^(\s*)[-*+]\s+(.*)$");
				if (bullet.Success)
				{
					body.Append(ListParagraph(bullet.Groups[1].Value)).Append(@"\'b7\tab ")
						.Append(Inline(bullet.Groups[2].Value, style))
						.Append(@"\par").Append("\r\n");
					listIndent = ListTextIndent(bullet.Groups[1].Value);
					continue;
				}
				// A numbered step. The number written is the one in the document, so a document that
				// numbers its steps 1, 2, 4 shows exactly that rather than being quietly corrected.
				var step = Regex.Match(line, @"^(\s*)(\d+)[.)]\s+(.*)$");
				if (step.Success)
				{
					listIndent = ListTextIndent(step.Groups[1].Value);
					body.Append(ListParagraph(step.Groups[1].Value))
						.Append(Escape(step.Groups[2].Value)).Append(@".\tab ")
						.Append(Inline(step.Groups[3].Value, style))
						.Append(@"\par").Append("\r\n");
					continue;
				}
				// A line written underneath a list item and indented under it belongs to that item, so
				// it keeps that item's indent. As an ordinary paragraph it would start again at the
				// margin, outside the list it is part of.
				if (listIndent > 0 && line.Length > 0 && char.IsWhiteSpace(line[0]))
				{
					body.Append(Pard(listIndent)).Append(@"\sa40 ")
						.Append(Inline(trimmed, style))
						.Append(@"\par").Append("\r\n");
					continue;
				}
				// An ordinary paragraph. A line that follows another without a blank between them
				// continues it, which is how Markdown reads and how these documents are written.
				var continues = i > 0 && lines[i - 1].Trim().Length > 0
					&& !Regex.IsMatch(lines[i - 1].Trim(), @"^(#{1,6}\s|[-*+]\s|\d+[.)]\s|\||```)");
				if (continues)
					body.Length -= (@"\par" + "\r\n").Length;
				body.Append(continues ? " " : Pard(0) + @"\sa120\sl276\slmult1 ")
					.Append(Inline(trimmed, style))
					.Append(@"\par").Append("\r\n");
				listIndent = 0;
			}
			return Header(style) + body + "}";
		}

		/// <summary>How far in a list moves, in twips: one step for the marker, one more for its text.</summary>
		const int ListStep = 300;

		/// <summary>Space kept between the text and the edge of the box, in twips.</summary>
		const int Margin = 120;

		/// <summary>Starts a paragraph indented by the given amount, inside the document's margins.</summary>
		/// <remarks>
		/// The margins belong to the document rather than to the box showing it. A box can only set
		/// one left indent for everything it holds, so setting the margin there flattens the indents
		/// that make a list a list: the bullets and numbers end up back against the edge with only
		/// their text moved in. Written here, each paragraph keeps the indent it asked for.
		/// </remarks>
		static string Pard(int leftIndent)
		{
			return @"\pard\li" + (Margin + leftIndent).ToString(CultureInfo.InvariantCulture)
				+ @"\ri" + Margin.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>Where the words of a list item start, from the spaces written before its marker.</summary>
		static int ListTextIndent(string leadingSpaces)
		{
			return ListStep + leadingSpaces.Length / 2 * ListStep + ListStep;
		}

		/// <summary>The paragraph settings of one list item, from the spaces written before its marker.</summary>
		/// <remarks>
		/// The step is taken before the marker, not after it. A bullet or number left against the
		/// margin sits in the same column as the paragraph above it and reads as a sentence that
		/// happens to start with a digit, so the list is only visible by reading it. Moving the
		/// marker in makes the list a shape on the page. The text keeps its own step so a line that
		/// wraps lands under the words rather than under the marker.
		/// </remarks>
		static string ListParagraph(string leadingSpaces)
		{
			return Pard(ListTextIndent(leadingSpaces))
				+ @"\fi-" + ListStep.ToString(CultureInfo.InvariantCulture) + @"\sa40 ";
		}

		/// <summary>The document's opening: which fonts and colours the rest may name.</summary>
		static string Header(RtfStyle style)
		{
			var half = (int)Math.Round(style.FontSize * 2);
			var sb = new StringBuilder();
			sb.Append(@"{\rtf1\ansi\ansicpg1252\deff0\nouicompat");
			sb.Append(@"{\fonttbl{\f0\fswiss\fcharset0 ").Append(style.FontName).Append(@";}");
			sb.Append(@"{\f1\fmodern\fcharset0 ").Append(style.CodeFontName).Append(@";}}");
			sb.Append(@"{\colortbl ;");
			foreach (var c in new[] { style.Literal, style.Control, style.Warning, style.Link })
				sb.Append(@"\red").Append(c.R).Append(@"\green").Append(c.G).Append(@"\blue").Append(c.B).Append(';');
			sb.Append('}');
			sb.Append(@"\viewkind4\uc1\f0\fs").Append(half).Append(' ');
			return sb.ToString();
		}

		// Colour numbers, in the order Header writes them.
		const int Literal = 1;
		const int Control = 2;
		const int Warning = 3;
		const int LinkColour = 4;

		/// <summary>Renders the marks that appear inside a line.</summary>
		/// <remarks>
		/// Code spans are taken out first and put back last, so a formula containing an asterisk is
		/// not mistaken for emphasis. That is the whole reason a formula like =a1*abs(a1) is written
		/// in a code span in the first place.
		/// </remarks>
		static string Inline(string text, RtfStyle style)
		{
			var spans = new List<string>();
			// Code spans, held aside behind a marker no document can contain.
			text = Regex.Replace(text, "`([^`]*)`", m =>
			{
				var inner = m.Groups[1].Value;
				// A span written in square brackets names something on screen, so it is coloured as
				// one. Everything else in a code span is a literal value.
				var colour = inner.StartsWith("[") && inner.EndsWith("]") ? Control : Literal;
				spans.Add(@"{\cf" + colour + " " + Escape(inner) + @"\cf0 }");
				return "\u0001" + (spans.Count - 1) + "\u0002";
			});
			// Links, before emphasis, so a link's text may be emphasised but its address is not read.
			text = Regex.Replace(text, @"!?\[([^\]]*)\]\(([^)\s]+)(?:\s+""[^""]*"")?\)", m =>
			{
				var label = m.Groups[1].Value;
				var url = m.Groups[2].Value;
				// A picture cannot be shown in this box, so it is named instead of silently vanishing.
				if (m.Value.StartsWith("!"))
				{
					spans.Add(@"{\i " + Escape("[picture: " + (label.Length > 0 ? label : url) + "]") + @"\i0 }");
					return "\u0001" + (spans.Count - 1) + "\u0002";
				}
				spans.Add(Hyperlink(url, label.Length > 0 ? label : url));
				return "\u0001" + (spans.Count - 1) + "\u0002";
			});
			// A bare address in angle brackets, which is how these documents write most of theirs.
			text = Regex.Replace(text, @"<((?:https?|ftp)://[^>\s]+)>", m =>
			{
				spans.Add(Hyperlink(m.Groups[1].Value, m.Groups[1].Value));
				return "\u0001" + (spans.Count - 1) + "\u0002";
			});
			text = Escape(text);
			// Bold is a warning. Headings are written as headings, so nothing else needs bold.
			text = Regex.Replace(text, @"\*\*(.+?)\*\*", @"{\b\cf" + Warning + " $1" + @"\cf0\b0 }");
			text = Regex.Replace(text, @"__(.+?)__", @"{\b\cf" + Warning + " $1" + @"\cf0\b0 }");
			text = Regex.Replace(text, @"(?<![\*\w])\*(?!\s)(.+?)(?<!\s)\*(?![\*\w])", @"{\i $1\i0 }");
			text = Regex.Replace(text, @"(?<![_\w])_(?!\s)(.+?)(?<!\s)_(?![_\w])", @"{\i $1\i0 }");
			// The held-aside spans go back exactly as they were.
			text = Regex.Replace(text, "\u0001(\\d+)\u0002",
				m => spans[int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)]);
			return text;
		}

		/// <summary>A clickable address.</summary>
		static string Hyperlink(string url, string label)
		{
			// Shaped the way the documents this replaces shape it. A RichTextBox reads fields only
			// partially, and a result nested inside a group of its own came out with that group's
			// boundaries drawn on screen, either side of every link.
			return @"{\field{\*\fldinst HYPERLINK " + Escape(url) + @" }{\fldrslt \cf"
				+ LinkColour + @"\ul " + Escape(label) + @"\ulnone\cf0 }}";
		}

		/// <summary>
		/// Makes text safe to put in an RTF document.
		/// </summary>
		/// <remarks>
		/// A backslash and both braces are what RTF is written in, so text containing them has to say
		/// so or the document stops making sense from that point on. Every Windows path in these
		/// documents contains backslashes, so this is the common case rather than the odd one.
		///
		/// Anything outside plain ASCII is written as its number. The degree sign and the arrow both
		/// appear in these documents and neither survives being written directly.
		/// </remarks>
		static string Escape(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;
			var sb = new StringBuilder(text.Length + 16);
			foreach (var c in text)
			{
				if (c == '\\' || c == '{' || c == '}')
					sb.Append('\\').Append(c);
				else if (c == '\t')
					sb.Append(@"\tab ");
				else if (c < 128)
					sb.Append(c);
				else
					sb.Append(@"\u").Append((int)c).Append('?');
			}
			return sb.ToString();
		}

	}
}
