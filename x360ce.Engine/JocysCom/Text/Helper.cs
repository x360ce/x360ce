using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JocysCom.ClassLibrary.Text
{

	public static class Helper
	{
		private static readonly Regex tagRx = new Regex("{((?<prefix>[\\w]+)[.])?(?<property>[\\w]+)(:(?<format>[^{}]+))?}", RegexOptions.IgnoreCase);

		/// <summary>
		/// Replace {TypeName.PropertyName[:format]} or {customPrefix.propertyName[:format]} pattern with the property value of the object.
		/// </summary>
		/// <remarks>
		/// Example 1: Supply current date. Use {customPrefix.propertyName[:format]}.
		///	var template = "file_{date.Now:yyyyMMdd}.txt";
		/// var fileName = JocysCom.ClassLibrary.Text.Helper.Replace(template, DateTime.Now, true, "date");
		///
		/// Example 2: Supply profile object. Use {TypeName.PropertyName[:format]}.
		///	var template = "Profile full name: {Profile.first_name} {Profile.last_name}";
		/// var fileName = JocysCom.ClassLibrary.Text.Helper.Replace(template, profile);
		/// </remarks>
		/// <param name="s">String template</param>
		/// <param name="o">Object values.</param>
		/// <param name="usePrefix">If true then type name or custom prefix, separated by dot, will be used.</param>
		/// <param name="customPrefix">Custom prefix string.</param>
		public static string Replace<T>(string s, T o, bool usePrefix = true, string customPrefix = null)
		{
			if (string.IsNullOrEmpty(s))
				return s;
			if (o == null)
				return s;
			var t = typeof(T);
			var properties = t.GetProperties();
			var prefix = string.IsNullOrEmpty(customPrefix) ? t.Name : customPrefix;
			var matches = tagRx.Matches(s);
			foreach (var p in properties)
			{
				foreach (Match m in matches)
				{
					if (usePrefix && string.Compare(prefix, m.Groups["prefix"].Value, true) != 0)
						continue;
					if (string.Compare(p.Name, m.Groups["property"].Value, true) != 0)
						continue;
					var format = m.Groups["format"].Value;
					var value = p.GetValue(o, null);
					var text = string.IsNullOrEmpty(format)
						? string.Format("{0}", value)
						: string.Format("{0:" + format + "}", value);
					s = Replace(s, m.Value, text, StringComparison.OrdinalIgnoreCase);
				}
			}
			return s;
		}

		/// <summary>
		/// Replace {TypeName.PropertyName[:format]} or {customPrefix.propertyName[:format]} pattern with the property value of the object.
		/// </summary>
		/// <remarks>
		/// Example 1: Supply current date. Use {customPrefix.propertyName[:format]}.
		///	var template = "file_{date.Now:yyyyMMdd}.txt";
		/// var fileName = JocysCom.ClassLibrary.Text.Helper.Replace(template, DateTime.Now, true, "date");
		///
		/// Example 2: Supply profile object. Use {TypeName.PropertyName[:format]}.
		///	var template = "Profile full name: {Profile.first_name} {Profile.last_name}";
		/// var fileName = JocysCom.ClassLibrary.Text.Helper.Replace(template, profile);
		/// </remarks>
		/// <param name="s">String template</param>
		/// <param name="o">Object values.</param>
		public static string ReplaceDictionary(string s, Dictionary<string, object> o, bool usePrefix = true, string customPrefix = null)
		{
			if (string.IsNullOrEmpty(s))
				return s;
			if (o == null)
				return s;
			var prefix = customPrefix;
			var matches = tagRx.Matches(s);
			foreach (var key in o.Keys)
			{
				foreach (Match m in matches)
				{
					if (usePrefix && string.Compare(prefix, m.Groups["prefix"].Value, true) != 0)
						continue;
					if (string.Compare(key, m.Groups["property"].Value, true) != 0)
						continue;
					var format = m.Groups["format"].Value;
					var value = o[key];
					var text = string.IsNullOrEmpty(format)
						? string.Format("{0}", value)
						: string.Format("{0:" + format + "}", value);
					s = Replace(s, m.Value, text, StringComparison.OrdinalIgnoreCase);
				}
			}
			return s;
		}

		public static List<string> GetReplaceMacros<T>(bool usePrefix = true, string customPrefix = null)
		{
			var list = new List<string>();
			var t = typeof(T);
			var properties = t.GetProperties();
			var prefix = string.IsNullOrEmpty(customPrefix) ? t.Name : customPrefix;
			foreach (var p in properties)
			{
				var macro = usePrefix
					? prefix + "." + p.Name
					: p.Name;
				list.Add(macro);
			}
			return list;
		}

		/// <summary>
		/// Used to parametrize path. For example:
		/// Convert "C:\Program Files\JocysCom\Focus Logger" to
		/// "C:\Program Files\{Company}\{Product}"
		/// </summary>
		public static string Replace<T>(T o, string s, bool usePrefix = true, string customPrefix = null)
		{
			if (string.IsNullOrEmpty(s))
				return s;
			if (o == null)
				return s;
			var t = typeof(T);
			var properties = t.GetProperties();
			var prefix = string.IsNullOrEmpty(customPrefix) ? t.Name : customPrefix;
			var replacement = new List<(string Param, string Value)>();
			foreach (var p in properties)
			{
				var value = $"{p.GetValue(o, null)}";
				if (string.IsNullOrEmpty(value))
					continue;
				var param = "{";
				if (usePrefix && !string.IsNullOrEmpty(prefix))
					param += prefix;
				param += p.Name + "}";
				replacement.Add((param, value));
			}
			replacement = replacement
				.OrderByDescending(x => x.Value.Length)
				.ThenBy(x => x.Param.Length)
				.ToList();
			foreach (var item in replacement)
				s = Replace(s, item.Value, item.Param, StringComparison.OrdinalIgnoreCase);
			return s;
		}

		/// <summary>Case insensitive replace.</summary>
		public static string Replace(string s, string oldValue, string newValue, StringComparison comparison)
		{
			if (string.IsNullOrEmpty(s))
				return s;
			if (string.IsNullOrEmpty(oldValue))
				throw new ArgumentNullException(nameof(oldValue));
			var sb = new StringBuilder();
			var previousIndex = 0;
			var index = s.IndexOf(oldValue, comparison);
			while (index != -1)
			{
				sb.Append(s.Substring(previousIndex, index - previousIndex));
				sb.Append(newValue);
				index += oldValue.Length;
				previousIndex = index;
				index = s.IndexOf(oldValue, index, comparison);
			}
			sb.Append(s.Substring(previousIndex));
			return sb.ToString();
		}

		/// <summary>
		/// Reports the zero-based index of the first occurrence of the specified value
		/// in input. The search starts at a specified byte position.
		/// </summary>
		/// <param name="input">input bytes.</param>
		/// <param name="value">The bytes to seek.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <returns>
		/// The zero-based index position of value if that byte value is found, or -1 if it is not.
		/// </returns>
		/// <remarks>This is very fast search.</remarks>
		public static int IndexOf(byte[] input, byte[] value, int startIndex = 0)
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input));
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			var endIndex = input.Length - value.Length;
			int v;
			for (var i = startIndex; i <= endIndex; i++)
			{
				// Check sequence against pattern.
				for (v = 0; v < value.Length; v++)
				{
					// Break if byte doesn't match.
					if (input[i + v] != value[v])
						break;
					// If last byte matched then return.
					else if (v + 1 == value.Length)
						return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Get value from text [name]:\s*[value].
		/// </summary>
		/// <param name="name">Prefix name.</param>
		/// <param name="s">String to get value from.</param>
		/// <param name="defaultValue">Override default value.</param>
		public static string GetValue(string name, string s, string defaultValue = "")
		{
			var pattern = string.Format(@"{0}:\s*(?<Value>[^\s]+)", name);
			var rx = new Regex(pattern, RegexOptions.IgnoreCase);
			var m = rx.Match(s ?? "");
			if (!m.Success)
				return defaultValue;
			var v = m.Groups["Value"].Value;
			return v;
		}

		/// <summary>
		/// Convert string value to an escaped C# string literal.
		/// </summary>
		public static string ToLiteral(string input, string language = "JScript")
		{
			using (var writer = new StringWriter())
			{
				using (var provider = System.CodeDom.Compiler.CodeDomProvider.CreateProvider(language))
				{
					var exp = new System.CodeDom.CodePrimitiveExpression(input);
					System.CodeDom.Compiler.CodeGeneratorOptions options = null;
					provider.GenerateCodeFromExpression(exp, writer, options);
					var literal = writer.ToString();
					var rxLines = new Regex("\"\\s*[+]\\s*[\r\n]\"", RegexOptions.Multiline);
					literal = rxLines.Replace(literal, "");
					//literal = literal.Replace(string.Format("\" +{0}\t\"", Environment.NewLine), "");
					//literal = literal.Replace("\\r\\n", "\\r\\n\"+\r\n\"");
					return literal;
				}
			}
		}

		#region Word Wrap

		// http://www.codeproject.com/Articles/51488/Implementing-Word-Wrap-in-C


		/// <summary>
		/// Word wraps the given text to fit within the specified width.
		/// </summary>
		/// <param name="text">Text to be word wrapped</param>
		/// <param name="width">Width, in characters, to which the text
		/// should be word wrapped</param>
		/// <returns>The modified text</returns>
		public static string WrapText(string text, int width, bool useSpaces = false)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));
			// Lucidity check
			if (width < 1)
				return text;
			if (!useSpaces)
			{
				var sb2 = new StringBuilder(string.Empty);
				var i = 0;
				foreach (var c in text)
				{
					if (i > 0 && i % width == 0)
						sb2.Append(Environment.NewLine);
					sb2.Append(c);
					i++;
				}
				return sb2.ToString().Trim('\r', '\n');
			}
			int pos, next;
			var sb = new StringBuilder();
			// Parse each line of text
			for (pos = 0; pos < text.Length; pos = next)
			{
				// Find end of line
				var eol = text.IndexOf(Environment.NewLine, pos);
				if (eol == -1)
					next = eol = text.Length;
				else
					next = eol + Environment.NewLine.Length;
				// Copy this line of text, breaking into smaller lines as needed
				if (eol > pos)
				{
					do
					{
						var len = eol - pos;
						if (len > width)
							len = BreakLine(text, pos, width);
						sb.Append(text, pos, len);
						sb.Append(Environment.NewLine);
						// Trim white space following break
						pos += len;
						while (pos < eol && char.IsWhiteSpace(text[pos]))
							pos++;
					} while (eol > pos);
				}
				else sb.Append(Environment.NewLine); // Empty line
			}
			return sb.ToString();
		}

		/// <summary>
		/// Locates position to break the given line so as to avoid
		/// breaking words.
		/// </summary>
		/// <param name="text">String that contains line of text</param>
		/// <param name="pos">Index where line of text starts</param>
		/// <param name="max">Maximum line length</param>
		/// <returns>The modified line length</returns>
		private static int BreakLine(string text, int pos, int max)
		{
			// Find last white space in line
			var i = max;
			while (i >= 0 && !char.IsWhiteSpace(text[pos + i]))
				i--;
			// If no white space found, break at maximum length
			if (i < 0)
				return max;
			// Find start of white space
			while (i >= 0 && char.IsWhiteSpace(text[pos + i]))
				i--;
			// Return length of text before white space
			return i + 1;
		}

		#endregion

		/// <summary>
		/// Add or remove ident.
		/// </summary>
		/// <param name="s">String to ident.</param>
		/// <param name="tabs">Positive - add ident, negative - remove ident.</param>
		/// <param name="ident">Ident character</param>
		public static string IdentText(string s, int tabs = 1, string ident = "\t")
		{
			if (tabs == 0)
				return s;
			if (string.IsNullOrEmpty(s))
				return s;
			var sb = new StringBuilder();
			var tr = new StringReader(s);
			var prefix = string.Concat(Enumerable.Repeat(ident, tabs));
			string line;
			var lines = s.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
			for (int i = 0; i < lines.Length; i++)
			{
				line = lines[i];
				if (line != "")
				{
					// If add idents then...
					if (tabs > 0)
						sb.Append(prefix);
					// If remove idents then...
					else if (tabs < 0)
					{
						var count = 0;
						// Count how much idents could be removed
						while (line.Substring(count * ident.Length, ident.Length) == ident && count < tabs)
							count++;
						line = line.Substring(count * ident.Length);
					}
				}
				if (i < lines.Length - 1)
					sb.AppendLine(line);
				else
					sb.Append(line);
			}
			tr.Dispose();
			return sb.ToString();
		}

		public static string RemoveIdent(string s)
		{
			s = s.Trim('\n', '\r', ' ', '\t').Replace("\r\n", "\n");
			var lines = s.Split('\n');
			var checkLines = lines
				// Ignore first trimmed line.
				.Where((x, i) => i > 0 && !string.IsNullOrWhiteSpace(x)).ToArray();
			if (checkLines.Length == 0)
				return s;
			var minIndent = checkLines.Min(x => x.Length - x.TrimStart(' ', '\t').Length);
			for (var i = 0; i < lines.Length; i++)
			{
				if (lines[i].Length > minIndent)
					// Don't trim first line.
					lines[i] = lines[i].Substring(i == 0 ? 0 : minIndent);
				else if (string.IsNullOrWhiteSpace(lines[i]))
					lines[i] = "";
			}
			return string.Join(Environment.NewLine, lines);
		}

		public static string BytesToStringBlock(string s, bool addIndex, bool addHex, bool addText)
		{
			var bytes = Encoding.ASCII.GetBytes(s);
			return BytesToStringBlock(bytes, addIndex, addHex, addText);
		}

		public static string BytesToStringBlock(byte[] bytes, bool addIndex, bool addHex, bool addText, int offset = 0, int size = -1, int? maxDisplayLines = null)
		{
			if (bytes is null)
				throw new ArgumentNullException(nameof(bytes));
			var builder = new StringBuilder();
			var hx = new StringBuilder();
			var ch = new StringBuilder();
			var lineIndex = 0;
			var lines = new List<string>();
			var length = size == -1 ? bytes.Length : size;
			for (var i = 1; i <= length; i++)
			{
				var modulus = i % 16;
				hx.Append(bytes[i - 1 + offset].ToString("X2")).Append(" ");
				var c = (char)bytes[i - 1 + offset];
				if (char.IsControl(c))
					ch.Append(".");
				else
					ch.Append(c);
				// If line ended.
				if ((modulus == 0 && i > 1) || (i == length))
				{
					if (addIndex)
					{
						builder.Append((lineIndex * 16).ToString("X8"));
						if (addHex || addText)
							builder.Append(": ");
					}
					if (addHex)
					{
						if (hx.Length < 50) hx.Append(' ', 50 - hx.Length);
						builder.Append(hx.ToString());
						if (addText)
							builder.Append(" | ");
					}
					if (addText)
					{
						builder.Append(ch.ToString());
						if (!maxDisplayLines.HasValue || lines.Count < maxDisplayLines.Value)
							lines.Add(builder.ToString());
						builder.Clear();
					}
					hx.Clear();
					ch.Clear();
					lineIndex++;
				}
			}
			if (lineIndex > lines.Count)
				lines[lines.Count - 1] = string.Format("... {0} more lines.", lineIndex - lines.Count + 1);
			return string.Join(Environment.NewLine, lines);
		}

		public static string CropLines(string s, int maxLines = 8)
		{
			if (string.IsNullOrEmpty(s))
				return s;
			var lines = s.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			if (lines.Length <= maxLines)
				return s;
			var sb = new StringBuilder();
			for (var i = 0; i < maxLines - 1; i++)
				sb.AppendLine(lines[i]);
			sb.AppendFormat("... {0} more lines.", lines.Length - maxLines + 1);
			return sb.ToString();
		}

		/// <summary>
		/// Convert .NFO file content to Unicode so they can be displayed properly between 'pre' tags on web.
		/// </summary>
		public static string IBM437ToUTF8(byte[] bytes)
		{
			var enc = Encoding.GetEncoding("IBM437");
			return enc.GetString(bytes);
		}

		/// <summary>
		/// Convert .NFO file content to Unicode so they can be displayed properly between 'pre' tags on web.
		/// </summary>
		public static string IBM437ToUTF8(string text)
		{
			var bytes = Encoding.ASCII.GetBytes(text);
			var enc = Encoding.GetEncoding("IBM437");
			return enc.GetString(bytes);
		}

		#region TimeSpan

		/// <summary>
		/// Convert timespan to string.
		/// </summary>
		/// <param name="ts">TimeSpan value to convert.</param>
		/// <param name="includeMilliseconds">include milliseconds.</param>
		/// <param name="useWords">Use words instead of ':' and '.' separator.</param>
		/// <param name="useShortWords">Use short words. Applied when useWords = true.</param>
		/// <param name="precision">Precision. Applied when useWords = true.</param>
		/// <returns></returns>
		public static string TimeSpanToString(TimeSpan ts, bool includeMilliseconds = false, bool useWords = false, bool useShortWords = false, int? precision = null)
		{
			var s = "";
			if (useWords)
			{
				var list = new List<string>();
				if (ts.Days != 0)
				{
					s = string.Format("{0} {1}", ts.Days, ts.Days == 1 ? "day" : "days");
					list.Add(s);
				}
				if (ts.Hours != 0 && (!precision.HasValue || list.Count < precision.Value))
				{
					s = string.Format("{0} {1}", ts.Hours, ts.Hours == 1 ? "hour" : "hours");
					list.Add(s);
				}
				if (ts.Minutes != 0 && (!precision.HasValue || list.Count < precision.Value))
				{
					s = string.Format("{0} {1}", ts.Minutes, useShortWords ? "min" : (ts.Minutes == 1 ? "minute" : "minutes"));
					list.Add(s);
				}
				// Force to show seconds if milliseconds will not be visible.
				if (!precision.HasValue || list.Count < precision.Value)
				{
					s = string.Format("{0} {1}", ts.Seconds, useShortWords ? "sec" : (ts.Seconds == 1 ? "second" : "seconds"));
					list.Add(s);
				}
				var showMilliseconds = includeMilliseconds && (ts.Milliseconds != 0 || list.Count == 0);
				if (showMilliseconds && (!precision.HasValue || list.Count < precision.Value))
				{
					s = string.Format("{0} {1}", ts.Milliseconds, useShortWords ? "ms" : (ts.Milliseconds == 1 ? "millisecond" : "milliseconds"));
					list.Add(s);
				}
				s = string.Join(" ", list);
			}
			else
			{
				if (ts.Days != 0)
					s += ts.Days.ToString("0") + ".";
				if (s.Length != 0 || ts.Hours > 0)
					s += ts.Days.ToString("00") + ":";
				if (s.Length != 0 || ts.Minutes > 0)
					s += ts.Minutes.ToString("00") + ":";
				// Seconds will be always included.
				s += ts.Seconds.ToString("00");
				if (includeMilliseconds)
					s += "." + ts.Milliseconds.ToString("000");
			}
			return s;
		}

		/// <summary>Time Span Standard regular expression.</summary>
		/// <remarks>
		/// Minutes are mandatory with required colon from left or right.
		/// Pattern: [-][[dd.]HH:](:mm|mm:)[:ss[.fffffff]]
		/// </remarks>
		public const string TimeSpanStandard =
			@"(?:(?<ne>-))?" +
			@"(?:(?:(?<dd>0*[0-9]+)[.])?(?:(?<HH>0*[2][0-3]|0*[1][0-9]|0*[0-9])[:]))?" +
			@"(?<mm>(?<=:)0*[0-5]?[0-9]|0*[5-9]?[0-9](?=[:]))" +
			@"(?:[:](?<ss>0*[0-5]?[0-9](?:[.][0-9]{0,7})?))?";

		/// <summary>
		/// Convert JSON TimeSpan format...
		///		From Standard: [-][d.]HH:mm[:ss.fffffff]
		///		To   ISO8601:  P(n)Y(n)M(n)DT(n)H(n)M(n)S
		/// </summary>
		public static string ConvertTimeSpanStandardToISO8601(string jsonString)
		{
			var spanRx = new Regex("\"" + TimeSpanStandard + "\"");
			var me = new MatchEvaluator((Match m) =>
			{
				var standard = m.Value.Trim('"');
				var span = TimeSpan.Parse(standard);
				var iso8601 = System.Xml.XmlConvert.ToString(span);
				return string.Format(@"""{0}""", iso8601);
			});
			return spanRx.Replace(jsonString, me);
		}

		/// <summary>Time Span ISO8601 regular expression</summary>
		public const string TimeSpanISO8601 =
			@"(?<V>(P(?=\d+[YMWD])?(\d+Y)?(\d+M)?(\d+W)?(\d+D)?)(T(?=\d+[HMS])(\d+H)?(\d+M)?(\d+S)?))";

		/// <summary>
		/// Convert JSON TimeSpan format...
		///		From ISO8601:  P(n)Y(n)M(n)DT(n)H(n)M(n)S
		///		To   Standard: [-][d.]HH:mm[:ss.fffffff]
		/// </summary>
		public static string ConvertTimeSpanISO8601ToStandard(string jsonString)
		{
			var spanRx = new Regex("\"" + TimeSpanISO8601 + "\"");
			var me = new MatchEvaluator((Match m) =>
			{
				var iso8601 = m.Groups["V"].Value.Trim('"');
				var span = System.Xml.XmlConvert.ToTimeSpan(iso8601);
				var standard = span.ToString();
				return string.Format(@"""{0}""", standard);
			});
			return spanRx.Replace(jsonString, me);
		}

		#endregion

	}
}
