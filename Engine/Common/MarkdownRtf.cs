using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

		/// <summary>Colour of a link: blue, or the theme's link colour when the theme is dark.</summary>
		public Color Link = new Color(JocysCom.ClassLibrary.Controls.Themes.FormsTheme.GetColor("ColorBrand", System.Drawing.Color.FromArgb(0, 0, 255)));

		/// <summary>Colour of the text: the window text colour, so it follows the theme.</summary>
		public Color Text = new Color(System.Drawing.SystemColors.WindowText);

		/// <summary>A colour, as RTF counts them.</summary>
		public struct Color
		{
			public Color(int r, int g, int b) { R = r; G = g; B = b; }
			public Color(System.Drawing.Color color) : this(color.R, color.G, color.B) { }
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

		#region Links between documents

		/// <summary>Where a page of the docs folder is published: the project wiki, made from that folder.</summary>
		public const string WikiUrl = "https://github.com/x360ce/x360ce/wiki/";

		/// <summary>Where any other file of the docs folder is shown, such as a preset to download.</summary>
		public const string DocsUrl = "https://github.com/x360ce/x360ce/blob/master/docs/";

		/// <summary>Where a picture of the docs folder is served as the picture itself.</summary>
		public const string DocsRawUrl = "https://raw.githubusercontent.com/x360ce/x360ce/master/docs/";

		/// <summary>A link or a picture: its text or description, and its target.</summary>
		static readonly Regex Link = new Regex(@"!?\[([^\]]*)\]\(([^)\s]+)(?:\s+""[^""]*"")?\)");

		/// <summary>A link target as written in a document, made into an address that opens wherever the document is shown.</summary>
		/// <param name="target">The target, such as <c>Help.HidGuardian.md#how-to</c>, <c>.attachments/preset.xml</c> or an address.</param>
		/// <param name="picture">True for a picture, which is served as itself rather than shown in a page.</param>
		/// <remarks>
		/// The documents link to each other and to their files by paths relative to the docs folder, which work where
		/// GitHub shows the folder. Inside the program, or copied beside the AI skill, there is no folder to be relative
		/// to, so a page goes to its wiki page and a file to the repository. An address, or an anchor in the same page,
		/// is left as it is.
		/// </remarks>
		public static string ResolveLink(string target, bool picture = false)
		{
			if (string.IsNullOrEmpty(target) || target.StartsWith("#", StringComparison.Ordinal) || Regex.IsMatch(target, @"^[A-Za-z][A-Za-z0-9+.-]*:"))
				return target;
			var hash = target.IndexOf('#');
			var path = hash < 0 ? target : target.Substring(0, hash);
			if (picture)
				return DocsRawUrl + path;
			if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) && path.IndexOf('/') < 0)
				return WikiUrl + path.Substring(0, path.Length - 3) + (hash < 0 ? "" : target.Substring(hash));
			return DocsUrl + target;
		}

		/// <summary>The document with every link and picture target made into an address: see <see cref="ResolveLink"/>.</summary>
		public static string ResolveLinks(string markdown)
		{
			if (string.IsNullOrEmpty(markdown))
				return markdown;
			return Link.Replace(markdown, m =>
			{
				var target = m.Groups[2];
				var resolved = ResolveLink(target.Value, m.Value.StartsWith("!", StringComparison.Ordinal));
				return m.Value.Substring(0, target.Index - m.Index) + resolved + m.Value.Substring(target.Index - m.Index + target.Length);
			});
		}

		#endregion

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
					body.Append(Pard(400)).Append(@"\sa0{\f1 ").Append(Escape(line)).Append(@"}\par").Append("\r\n");
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
				// A table: its rows in the fixed font with the columns padded to line up, the header
				// row bold. A real RTF table would be a large amount of machinery for the few that
				// exist, and padded columns read the same in every viewer.
				if (IsTableRow(trimmed))
				{
					var rows = new List<string[]>();
					var end = i;
					for (; end < lines.Length && IsTableRow(lines[end].Trim()); end++)
					{
						var row = lines[end].Trim();
						if (Regex.IsMatch(row, @"^\|[\s:\-\|]+\|$"))
							continue;
						var cells = row.Trim('|').Split('|');
						for (int c = 0; c < cells.Length; c++)
							cells[c] = cells[c].Trim();
						rows.Add(cells);
					}
					AppendTable(body, rows, style);
					i = end - 1;
					listIndent = 0;
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

		static bool IsTableRow(string trimmed)
		{
			return trimmed.StartsWith("|") && trimmed.EndsWith("|");
		}

		/// <summary>Writes the rows of a table, each cell padded to its column's width so they line up in the fixed font.</summary>
		static void AppendTable(StringBuilder body, List<string[]> rows, RtfStyle style)
		{
			var columns = rows.Count == 0 ? 0 : rows.Max(r => r.Length);
			var widths = new int[columns];
			// A column of numbers lines up on the right, as numbers do; every other column on the left.
			var numeric = new bool[columns];
			for (int c = 0; c < columns; c++)
				numeric[c] = rows.Skip(1).Any(r => c < r.Length && r[c].Length > 0)
					&& rows.Skip(1).All(r => c >= r.Length || r[c].Length == 0 || char.IsDigit(Visible(r[c])[0]));
			foreach (var row in rows)
				for (int c = 0; c < row.Length; c++)
					widths[c] = Math.Max(widths[c], Visible(row[c]).Length);
			for (int r = 0; r < rows.Count; r++)
			{
				var row = rows[r];
				body.Append(Pard(400)).Append(r == 0 ? @"\sa0{\f1\b " : @"\sa0{\f1 ");
				for (int c = 0; c < row.Length; c++)
				{
					var pad = widths[c] - Visible(row[c]).Length;
					if (numeric[c])
						body.Append(' ', pad);
					body.Append(Inline(row[c], style));
					if (c < row.Length - 1)
						body.Append(' ', (numeric[c] ? 0 : pad) + 2);
				}
				body.Append(@"}\par").Append("\r\n");
			}
			// Room under the table, as a paragraph leaves under itself.
			if (rows.Count > 0)
				body.Length -= (@"\par" + "\r\n").Length;
			body.Append(@"\sa120\par").Append("\r\n");
		}

		/// <summary>The text of a cell as it will be seen: the marks that become formatting removed.</summary>
		static string Visible(string cell)
		{
			return Regex.Replace(cell, @"[`*_]", "");
		}

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
			foreach (var c in new[] { style.Literal, style.Control, style.Warning, style.Link, style.Text })
				sb.Append(@"\red").Append(c.R).Append(@"\green").Append(c.G).Append(@"\blue").Append(c.B).Append(';');
			sb.Append('}');
			// The text is given its colour by number: the automatic colour, number 0, is drawn black
			// whatever the box behind it.
			sb.Append(@"\viewkind4\uc1\f0\cf").Append(TextColour).Append(@"\fs").Append(half).Append(' ');
			return sb.ToString();
		}

		// Colour numbers, in the order Header writes them.
		const int Literal = 1;
		const int Control = 2;
		const int Warning = 3;
		const int LinkColour = 4;
		const int TextColour = 5;

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
				spans.Add(@"{\cf" + colour + " " + Escape(inner) + @"\cf" + TextColour + " }");
				return "\u0001" + (spans.Count - 1) + "\u0002";
			});
			// Links, before emphasis, so a link's text may be emphasised but its address is not read.
			text = Link.Replace(text, m =>
			{
				var label = m.Groups[1].Value;
				var url = m.Groups[2].Value;
				// A picture cannot be shown in this box, so it is named instead of silently vanishing.
				if (m.Value.StartsWith("!"))
				{
					spans.Add(@"{\i " + Escape("[picture: " + (label.Length > 0 ? label : url) + "]") + @"\i0 }");
					return "\u0001" + (spans.Count - 1) + "\u0002";
				}
				spans.Add(Hyperlink(ResolveLink(url), label.Length > 0 ? label : url));
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
			text = Regex.Replace(text, @"\*\*(.+?)\*\*", @"{\b\cf" + Warning + " $1" + @"\cf" + TextColour + @"\b0 }");
			text = Regex.Replace(text, @"__(.+?)__", @"{\b\cf" + Warning + " $1" + @"\cf" + TextColour + @"\b0 }");
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
				+ LinkColour + @"\ul " + Escape(label) + @"\ulnone\cf" + TextColour + " }}";
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
