using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using JocysCom.ClassLibrary.Collections;

namespace JocysCom.ClassLibrary.Text
{

	public class Helper
	{

		static object providerLock = new object();

		static Regex tagRx = new Regex("{((?<prefix>[0-9A-Z]+)[.])?(?<property>[0-9A-Z]+)(:(?<format>[^{}]+))?}", RegexOptions.IgnoreCase);

		/// <summary>
		/// Replace {type.property}/{customPrefix.property} to its value.
		/// </summary>
		public static string Replace<T>(string s, T o, bool usePrefix = true, string customPrefix = null)
		{
			if (string.IsNullOrEmpty(s)) return s;
			if (o == null) return s;
			var t = typeof(T);
			var properties = t.GetProperties();
			var prefix = string.IsNullOrEmpty(customPrefix) ? t.Name : customPrefix;
			var matches = tagRx.Matches(s);
			foreach (var p in properties)
			{
				foreach (Match m in matches)
				{
					if (usePrefix && string.Compare(prefix, m.Groups["prefix"].Value, true) != 0) continue;
					if (string.Compare(p.Name, m.Groups["property"].Value, true) != 0) continue;
					var format = m.Groups["format"].Value;
					var value = p.GetValue(o, null);
					var text = string.IsNullOrEmpty(format)
						? string.Format("{0}", value)
						: string.Format("{0:" + format + "}", value);
					s = Replace(s, m.Value, text, StringComparison.InvariantCultureIgnoreCase);
				}
			}
			return s;
		}

		/// <summary>
		/// Allows case insensitive replace.
		/// </summary>
		public static string Replace(string s, string oldValue, string newValue, StringComparison comparison)
		{
			StringBuilder sb = new StringBuilder();
			int previousIndex = 0;
			int index = s.IndexOf(oldValue, comparison);
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
		public static int IndexOf(byte[] input, byte[] value, int startIndex = 0)
		{
			for (int i = startIndex; i < input.Length; i++)
			{
				// If remaining searchable data is smaller than value then...
				if (value.Length > (input.Length - i))
				{
					return -1;
				}
				for (int v = 0; v < value.Length; v++)
				{
					if (input[i + v] != value[v])
					{
						break;
					}
					else if (v + 1 == value.Length)
					{
						return i;
					}
				}
			}
			return -1;
		}

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
			string s = "";
			if (useWords)
			{
				List<string> list = new List<string>();
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
				bool showSeconds = !includeMilliseconds || ts.Seconds != 0;
				if ((!precision.HasValue || list.Count < precision.Value))
				{
					s = string.Format("{0} {1}", ts.Seconds, useShortWords ? "sec" : (ts.Seconds == 1 ? "second" : "seconds"));
					list.Add(s);
				}
				bool showMilliseconds = includeMilliseconds && (ts.Milliseconds != 0 || list.Count == 0);
				if (showMilliseconds && (!precision.HasValue || list.Count < precision.Value))
				{
					s = string.Format("{0} {1}", ts.Milliseconds, useShortWords ? "ms" : (ts.Milliseconds == 1 ? "millisecond" : "milliseconds"));
					list.Add(s);
				}
				s = string.Join(" ", list);
			}
			else
			{
				if (ts.Days != 0) s += ts.Days.ToString("0") + ".";
				if (s.Length != 0 || ts.Hours > 0) s += ts.Days.ToString("00") + ":";
				if (s.Length != 0 || ts.Minutes > 0) s += ts.Minutes.ToString("00") + ":";
				// Seconds will be always included.
				s += ts.Seconds.ToString("00");
				if (includeMilliseconds) s += "." + ts.Milliseconds.ToString("000");
			}
			return s;
		}

		/// <summary>
		/// Convert string value to an escaped C# string literal.
		/// </summary>
		public static string ToLiteral(string input, string language = "JScript")
		{
			using (var writer = new StringWriter())
			{
				using (var provider = CodeDomProvider.CreateProvider(language))
				{
					var exp = new CodePrimitiveExpression(input);
					CodeGeneratorOptions options = null;
					provider.GenerateCodeFromExpression(exp, writer, options);
					var literal = writer.ToString();
					Regex rxLines = new Regex("\"\\s*[+]\\s*[\r\n]\"", RegexOptions.Multiline);
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
			// Lucidity check
			if (width < 1) return text;

			if (!useSpaces)
			{
				var sb2 = new StringBuilder(string.Empty);
				var i = 0;
				foreach (var c in text)
				{
					if (i > 0 && i % width == 0) sb2.Append(Environment.NewLine);
					sb2.Append(c);
					i++;
				}
				return sb2.ToString().Trim('\r', '\n');
			}
			int pos, next;
			StringBuilder sb = new StringBuilder();
			// Parse each line of text
			for (pos = 0; pos < text.Length; pos = next)
			{
				// Find end of line
				int eol = text.IndexOf(Environment.NewLine, pos);
				if (eol == -1) next = eol = text.Length;
				else next = eol + Environment.NewLine.Length;
				// Copy this line of text, breaking into smaller lines as needed
				if (eol > pos)
				{
					do
					{
						int len = eol - pos;
						if (len > width) len = BreakLine(text, pos, width);
						sb.Append(text, pos, len);
						sb.Append(Environment.NewLine);

						// Trim white space following break
						pos += len;
						while (pos < eol && Char.IsWhiteSpace(text[pos])) pos++;
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
			int i = max;
			while (i >= 0 && !Char.IsWhiteSpace(text[pos + i])) i--;
			// If no white space found, break at maximum length
			if (i < 0) return max;
			// Find start of white space
			while (i >= 0 && Char.IsWhiteSpace(text[pos + i])) i--;
			// Return length of text before white space
			return i + 1;
		}

		#endregion

		public static string IdentText(int tabs, string s, char ident = '\t')
		{
			if (tabs == 0) return s;
			if (s == null) s = string.Empty;
			var sb = new StringBuilder();
			var tr = new StringReader(s);
			string prefix = string.Empty;
			for (int i = 0; i < tabs; i++) prefix += ident;
			string line;
			while ((line = tr.ReadLine()) != null)
			{
				if (sb.Length > 0) sb.AppendLine();
				if (tabs > 0) sb.Append(prefix);
				sb.Append(line);
			}
			return sb.ToString();
		}

		public static string BytesToStringBlock(string s, bool addIndex, bool addHex, bool addText)
		{
			var bytes = Encoding.ASCII.GetBytes(s);
			return BytesToStringBlock(bytes, addIndex, addHex, addText);
		}

		public static string BytesToStringBlock(byte[] bytes, bool addIndex, bool addHex, bool addText, int offset = 0, int size = -1, int? maxDisplayLines = null)
		{
			var builder = new StringBuilder();
			var hx = new StringBuilder();
			var ch = new StringBuilder();
			var lineIndex = 0;
			List<string> lines = new List<string>();
			var length = size == -1 ? bytes.Length : size;
			for (int i = 1; i <= length; i++)
			{
				var modulus = i % 16;
				hx.Append(bytes[i - 1 + offset].ToString("X2")).Append(" ");
				var c = (char)bytes[i - 1 + offset];
				if (System.Char.IsControl(c)) ch.Append(".");
				else ch.Append(c);
				// If line ended.
				if ((modulus == 0 && i > 1) || (i == length))
				{
					if (addIndex)
					{
						builder.Append((lineIndex * 16).ToString("X8"));
						if (addHex || addText) builder.Append(": ");
					}
					if (addHex)
					{
						if (hx.Length < 50) hx.Append(' ', 50 - hx.Length);
						builder.Append(hx.ToString());
						if (addText) builder.Append(" | ");
					}
					if (addText)
					{
						builder.Append(ch.ToString());
						if (!maxDisplayLines.HasValue || lines.Count < maxDisplayLines.Value)
						{
							lines.Add(builder.ToString());
						}
						builder.Clear();
					}
					hx.Clear();
					ch.Clear();
					lineIndex++;
				}
			}
			if (lineIndex > lines.Count)
			{
				lines[lines.Count - 1] = string.Format("... {0} more lines.", lineIndex - lines.Count + 1);
			}
			return string.Join(Environment.NewLine, lines);
		}

		public static string CropLines(string s, int maxLines = 8)
		{
			if (string.IsNullOrEmpty(s)) return s;
			var lines = s.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			if (lines.Length <= maxLines) return s;
			var sb = new StringBuilder();
			for (int i = 0; i < maxLines - 1; i++)
			{
				sb.AppendLine(lines[i]);
			}
			sb.AppendFormat("... {0} more lines.", lines.Length - maxLines + 1);
			return sb.ToString();
		}

		static KeyValue[] SplitAndKeep(string s, string[] separators)
		{
			var list = new List<KeyValue>();
			var sepIndex = new List<int>();
			var sepValue = new List<string>();
			var prevSepIndex = 0;
			var prevSepValue = "";
			// Loop trough every character of the string.
			for (int i = 0; i < s.Length; i++)
			{
				// Loop trough separators.
				for (int j = 0; j < separators.Length; j++)
				{
					var sep = separators[j];
					// If separator is empty then continue.
					if (string.IsNullOrEmpty(sep)) continue;
					int sepLen = sep.Length;
					// If separator is one char length and chars match or
					// separator is in the bounds of the string and string match then...
					if ((sepLen == 1 && s[i] == sep[0]) || (sepLen <= (s.Length - i) && string.CompareOrdinal(s, i, sep, 0, sepLen) == 0))
					{
						// Find string value from last separator.
						var prevIndex = prevSepIndex + prevSepValue.Length;
						var prevValue = s.Substring(prevIndex, i - prevIndex);
						var item = new KeyValue(prevSepValue, prevValue);
						list.Add(item);
						prevSepIndex = i;
						prevSepValue = sep;
						sepIndex.Add(i);
						sepValue.Add(sep);
						i += sepLen - 1;
						break;
					}
				}
			}
			// If no split were done then add complete string.
			if (list.Count == 0)
			{
				list.Add(new KeyValue("", s));
			}
			else
			{
				// Add value for last separator
				var prevI = prevSepIndex + prevSepValue.Length;
				var value = s.Substring(prevI, s.Length - prevI);
				list.Add(new KeyValue(prevSepValue, value));
			}
			return list.ToArray();
		}

		/// <summary>
		/// Convert .NFO file content to Unicode so they can be displayed properly between 'pre' tags on web.
		/// </summary>
		public static string IBM437ToUTF8(byte[] bytes)
		{
			var src = Encoding.GetEncoding("IBM437");
			var dst = Encoding.UTF8;
			var uBytes = Encoding.Convert(src, dst, bytes);
			return dst.GetString(uBytes);
		}

		public static string IBM437ToUTF8(string s)
		{
			var src = Encoding.GetEncoding("IBM437");
			return IBM437ToUTF8(src.GetBytes(s));
		}

		public static string IBM437ToUnicode(byte[] bytes)
		{
			var sb = new StringBuilder();
			foreach (var b in bytes)
			{
				sb.Append((Char)Oem437U[b]);
			}
			return sb.ToString();
		}

		public static string IBM437ToUnicode(string s)
		{
			var src = Encoding.GetEncoding("IBM437");
			return IBM437ToUnicode(src.GetBytes(s));
		}

		private static ushort[] _oem437u;
		public static ushort[] Oem437U
		{
			get
			{
				if (_oem437u != null) return _oem437u;
				var n = new ushort[256];
				// Technical Reference  > Code Pages > OEM Code Pages >  OEM 437
				// http://msdn.microsoft.com/en-gb/goglobal/cc305156.aspx
				n[0x00] = 0x0000; // NULL
				n[0x01] = 0x0001; // START OF HEADING
				n[0x02] = 0x0002; // START OF TEXT
				n[0x03] = 0x0003; // END OF TEXT
				n[0x04] = 0x0004; // END OF TRANSMISSION
				n[0x05] = 0x0005; // ENQUIRY
				n[0x06] = 0x0006; // ACKNOWLEDGE
				n[0x07] = 0x0007; // BELL
				n[0x08] = 0x0008; // BACKSPACE
				n[0x09] = 0x0009; // HORIZONTAL TABULATION
				n[0x0A] = 0x000A; // LINE FEED
				n[0x0B] = 0x000B; // VERTICAL TABULATION
				n[0x0C] = 0x000C; // FORM FEED
				n[0x0D] = 0x000D; // CARRIAGE RETURN
				n[0x0E] = 0x000E; // SHIFT OUT
				n[0x0F] = 0x000F; // SHIFT IN
				n[0x10] = 0x0010; // DATA LINK ESCAPE
				n[0x11] = 0x0011; // DEVICE CONTROL ONE
				n[0x12] = 0x0012; // DEVICE CONTROL TWO
				n[0x13] = 0x0013; // DEVICE CONTROL THREE
				n[0x14] = 0x0014; // DEVICE CONTROL FOUR
				n[0x15] = 0x0015; // NEGATIVE ACKNOWLEDGE
				n[0x16] = 0x0016; // SYNCHRONOUS IDLE
				n[0x17] = 0x0017; // END OF TRANSMISSION BLOCK
				n[0x18] = 0x0018; // CANCEL
				n[0x19] = 0x0019; // END OF MEDIUM
				n[0x1A] = 0x001A; // SUBSTITUTE
				n[0x1B] = 0x001B; // ESCAPE
				n[0x1C] = 0x001C; // FILE SEPARATOR
				n[0x1D] = 0x001D; // GROUP SEPARATOR
				n[0x1E] = 0x001E; // RECORD SEPARATOR
				n[0x1F] = 0x001F; // UNIT SEPARATOR
				n[0x20] = 0x0020; // SPACE
				n[0x21] = 0x0021; // EXCLAMATION MARK
				n[0x22] = 0x0022; // QUOTATION MARK
				n[0x23] = 0x0023; // NUMBER SIGN
				n[0x24] = 0x0024; // DOLLAR SIGN
				n[0x25] = 0x0025; // PERCENT SIGN
				n[0x26] = 0x0026; // AMPERSAND
				n[0x27] = 0x0027; // APOSTROPHE
				n[0x28] = 0x0028; // LEFT PARENTHESIS
				n[0x29] = 0x0029; // RIGHT PARENTHESIS
				n[0x2A] = 0x002A; // ASTERISK
				n[0x2B] = 0x002B; // PLUS SIGN
				n[0x2C] = 0x002C; // COMMA
				n[0x2D] = 0x002D; // HYPHEN-MINUS
				n[0x2E] = 0x002E; // FULL STOP
				n[0x2F] = 0x002F; // SOLIDUS
				n[0x30] = 0x0030; // DIGIT ZERO
				n[0x31] = 0x0031; // DIGIT ONE
				n[0x32] = 0x0032; // DIGIT TWO
				n[0x33] = 0x0033; // DIGIT THREE
				n[0x34] = 0x0034; // DIGIT FOUR
				n[0x35] = 0x0035; // DIGIT FIVE
				n[0x36] = 0x0036; // DIGIT SIX
				n[0x37] = 0x0037; // DIGIT SEVEN
				n[0x38] = 0x0038; // DIGIT EIGHT
				n[0x39] = 0x0039; // DIGIT NINE
				n[0x3A] = 0x003A; // COLON
				n[0x3B] = 0x003B; // SEMICOLON
				n[0x3C] = 0x003C; // LESS-THAN SIGN
				n[0x3D] = 0x003D; // EQUALS SIGN
				n[0x3E] = 0x003E; // GREATER-THAN SIGN
				n[0x3F] = 0x003F; // QUESTION MARK
				n[0x40] = 0x0040; // COMMERCIAL AT
				n[0x41] = 0x0041; // LATIN CAPITAL LETTER A
				n[0x42] = 0x0042; // LATIN CAPITAL LETTER B
				n[0x43] = 0x0043; // LATIN CAPITAL LETTER C
				n[0x44] = 0x0044; // LATIN CAPITAL LETTER D
				n[0x45] = 0x0045; // LATIN CAPITAL LETTER E
				n[0x46] = 0x0046; // LATIN CAPITAL LETTER F
				n[0x47] = 0x0047; // LATIN CAPITAL LETTER G
				n[0x48] = 0x0048; // LATIN CAPITAL LETTER H
				n[0x49] = 0x0049; // LATIN CAPITAL LETTER I
				n[0x4A] = 0x004A; // LATIN CAPITAL LETTER J
				n[0x4B] = 0x004B; // LATIN CAPITAL LETTER K
				n[0x4C] = 0x004C; // LATIN CAPITAL LETTER L
				n[0x4D] = 0x004D; // LATIN CAPITAL LETTER M
				n[0x4E] = 0x004E; // LATIN CAPITAL LETTER N
				n[0x4F] = 0x004F; // LATIN CAPITAL LETTER O
				n[0x50] = 0x0050; // LATIN CAPITAL LETTER P
				n[0x51] = 0x0051; // LATIN CAPITAL LETTER Q
				n[0x52] = 0x0052; // LATIN CAPITAL LETTER R
				n[0x53] = 0x0053; // LATIN CAPITAL LETTER S
				n[0x54] = 0x0054; // LATIN CAPITAL LETTER T
				n[0x55] = 0x0055; // LATIN CAPITAL LETTER U
				n[0x56] = 0x0056; // LATIN CAPITAL LETTER V
				n[0x57] = 0x0057; // LATIN CAPITAL LETTER W
				n[0x58] = 0x0058; // LATIN CAPITAL LETTER X
				n[0x59] = 0x0059; // LATIN CAPITAL LETTER Y
				n[0x5A] = 0x005A; // LATIN CAPITAL LETTER Z
				n[0x5B] = 0x005B; // LEFT SQUARE BRACKET
				n[0x5C] = 0x005C; // REVERSE SOLIDUS
				n[0x5D] = 0x005D; // RIGHT SQUARE BRACKET
				n[0x5E] = 0x005E; // CIRCUMFLEX ACCENT
				n[0x5F] = 0x005F; // LOW LINE
				n[0x60] = 0x0060; // GRAVE ACCENT
				n[0x61] = 0x0061; // LATIN SMALL LETTER A
				n[0x62] = 0x0062; // LATIN SMALL LETTER B
				n[0x63] = 0x0063; // LATIN SMALL LETTER C
				n[0x64] = 0x0064; // LATIN SMALL LETTER D
				n[0x65] = 0x0065; // LATIN SMALL LETTER E
				n[0x66] = 0x0066; // LATIN SMALL LETTER F
				n[0x67] = 0x0067; // LATIN SMALL LETTER G
				n[0x68] = 0x0068; // LATIN SMALL LETTER H
				n[0x69] = 0x0069; // LATIN SMALL LETTER I
				n[0x6A] = 0x006A; // LATIN SMALL LETTER J
				n[0x6B] = 0x006B; // LATIN SMALL LETTER K
				n[0x6C] = 0x006C; // LATIN SMALL LETTER L
				n[0x6D] = 0x006D; // LATIN SMALL LETTER M
				n[0x6E] = 0x006E; // LATIN SMALL LETTER N
				n[0x6F] = 0x006F; // LATIN SMALL LETTER O
				n[0x70] = 0x0070; // LATIN SMALL LETTER P
				n[0x71] = 0x0071; // LATIN SMALL LETTER Q
				n[0x72] = 0x0072; // LATIN SMALL LETTER R
				n[0x73] = 0x0073; // LATIN SMALL LETTER S
				n[0x74] = 0x0074; // LATIN SMALL LETTER T
				n[0x75] = 0x0075; // LATIN SMALL LETTER U
				n[0x76] = 0x0076; // LATIN SMALL LETTER V
				n[0x77] = 0x0077; // LATIN SMALL LETTER W
				n[0x78] = 0x0078; // LATIN SMALL LETTER X
				n[0x79] = 0x0079; // LATIN SMALL LETTER Y
				n[0x7A] = 0x007A; // LATIN SMALL LETTER Z
				n[0x7B] = 0x007B; // LEFT CURLY BRACKET
				n[0x7C] = 0x007C; // VERTICAL LINE
				n[0x7D] = 0x007D; // RIGHT CURLY BRACKET
				n[0x7E] = 0x007E; // TILDE
				n[0x7F] = 0x007F; // DELETE
				n[0x80] = 0x00C7; // LATIN CAPITAL LETTER C WITH CEDILLA
				n[0x81] = 0x00FC; // LATIN SMALL LETTER U WITH DIAERESIS
				n[0x82] = 0x00E9; // LATIN SMALL LETTER E WITH ACUTE
				n[0x83] = 0x00E2; // LATIN SMALL LETTER A WITH CIRCUMFLEX
				n[0x84] = 0x00E4; // LATIN SMALL LETTER A WITH DIAERESIS
				n[0x85] = 0x00E0; // LATIN SMALL LETTER A WITH GRAVE
				n[0x86] = 0x00E5; // LATIN SMALL LETTER A WITH RING ABOVE
				n[0x87] = 0x00E7; // LATIN SMALL LETTER C WITH CEDILLA
				n[0x88] = 0x00EA; // LATIN SMALL LETTER E WITH CIRCUMFLEX
				n[0x89] = 0x00EB; // LATIN SMALL LETTER E WITH DIAERESIS
				n[0x8A] = 0x00E8; // LATIN SMALL LETTER E WITH GRAVE
				n[0x8B] = 0x00EF; // LATIN SMALL LETTER I WITH DIAERESIS
				n[0x8C] = 0x00EE; // LATIN SMALL LETTER I WITH CIRCUMFLEX
				n[0x8D] = 0x00EC; // LATIN SMALL LETTER I WITH GRAVE
				n[0x8E] = 0x00C4; // LATIN CAPITAL LETTER A WITH DIAERESIS
				n[0x8F] = 0x00C5; // LATIN CAPITAL LETTER A WITH RING ABOVE
				n[0x90] = 0x00C9; // LATIN CAPITAL LETTER E WITH ACUTE
				n[0x91] = 0x00E6; // LATIN SMALL LETTER AE
				n[0x92] = 0x00C6; // LATIN CAPITAL LETTER AE
				n[0x93] = 0x00F4; // LATIN SMALL LETTER O WITH CIRCUMFLEX
				n[0x94] = 0x00F6; // LATIN SMALL LETTER O WITH DIAERESIS
				n[0x95] = 0x00F2; // LATIN SMALL LETTER O WITH GRAVE
				n[0x96] = 0x00FB; // LATIN SMALL LETTER U WITH CIRCUMFLEX
				n[0x97] = 0x00F9; // LATIN SMALL LETTER U WITH GRAVE
				n[0x98] = 0x00FF; // LATIN SMALL LETTER Y WITH DIAERESIS
				n[0x99] = 0x00D6; // LATIN CAPITAL LETTER O WITH DIAERESIS
				n[0x9A] = 0x00DC; // LATIN CAPITAL LETTER U WITH DIAERESIS
				n[0x9B] = 0x00A2; // CENT SIGN
				n[0x9C] = 0x00A3; // POUND SIGN
				n[0x9D] = 0x00A5; // YEN SIGN
				n[0x9E] = 0x20A7; // PESETA SIGN
				n[0x9F] = 0x0192; // LATIN SMALL LETTER F WITH HOOK
				n[0xA0] = 0x00E1; // LATIN SMALL LETTER A WITH ACUTE
				n[0xA1] = 0x00ED; // LATIN SMALL LETTER I WITH ACUTE
				n[0xA2] = 0x00F3; // LATIN SMALL LETTER O WITH ACUTE
				n[0xA3] = 0x00FA; // LATIN SMALL LETTER U WITH ACUTE
				n[0xA4] = 0x00F1; // LATIN SMALL LETTER N WITH TILDE
				n[0xA5] = 0x00D1; // LATIN CAPITAL LETTER N WITH TILDE
				n[0xA6] = 0x00AA; // FEMININE ORDINAL INDICATOR
				n[0xA7] = 0x00BA; // MASCULINE ORDINAL INDICATOR
				n[0xA8] = 0x00BF; // INVERTED QUESTION MARK
				n[0xA9] = 0x2310; // REVERSED NOT SIGN
				n[0xAA] = 0x00AC; // NOT SIGN
				n[0xAB] = 0x00BD; // VULGAR FRACTION ONE HALF
				n[0xAC] = 0x00BC; // VULGAR FRACTION ONE QUARTER
				n[0xAD] = 0x00A1; // INVERTED EXCLAMATION MARK
				n[0xAE] = 0x00AB; // LEFT-POINTING DOUBLE ANGLE QUOTATION MARK
				n[0xAF] = 0x00BB; // RIGHT-POINTING DOUBLE ANGLE QUOTATION MARK
				n[0xB0] = 0x2591; // LIGHT SHADE
				n[0xB1] = 0x2592; // MEDIUM SHADE
				n[0xB2] = 0x2593; // DARK SHADE
				n[0xB3] = 0x2502; // BOX DRAWINGS LIGHT VERTICAL
				n[0xB4] = 0x2524; // BOX DRAWINGS LIGHT VERTICAL AND LEFT
				n[0xB5] = 0x2561; // BOX DRAWINGS VERTICAL SINGLE AND LEFT DOUBLE
				n[0xB6] = 0x2562; // BOX DRAWINGS VERTICAL DOUBLE AND LEFT SINGLE
				n[0xB7] = 0x2556; // BOX DRAWINGS DOWN DOUBLE AND LEFT SINGLE
				n[0xB8] = 0x2555; // BOX DRAWINGS DOWN SINGLE AND LEFT DOUBLE
				n[0xB9] = 0x2563; // BOX DRAWINGS DOUBLE VERTICAL AND LEFT
				n[0xBA] = 0x2551; // BOX DRAWINGS DOUBLE VERTICAL
				n[0xBB] = 0x2557; // BOX DRAWINGS DOUBLE DOWN AND LEFT
				n[0xBC] = 0x255D; // BOX DRAWINGS DOUBLE UP AND LEFT
				n[0xBD] = 0x255C; // BOX DRAWINGS UP DOUBLE AND LEFT SINGLE
				n[0xBE] = 0x255B; // BOX DRAWINGS UP SINGLE AND LEFT DOUBLE
				n[0xBF] = 0x2510; // BOX DRAWINGS LIGHT DOWN AND LEFT
				n[0xC0] = 0x2514; // BOX DRAWINGS LIGHT UP AND RIGHT
				n[0xC1] = 0x2534; // BOX DRAWINGS LIGHT UP AND HORIZONTAL
				n[0xC2] = 0x252C; // BOX DRAWINGS LIGHT DOWN AND HORIZONTAL
				n[0xC3] = 0x251C; // BOX DRAWINGS LIGHT VERTICAL AND RIGHT
				n[0xC4] = 0x2500; // BOX DRAWINGS LIGHT HORIZONTAL
				n[0xC5] = 0x253C; // BOX DRAWINGS LIGHT VERTICAL AND HORIZONTAL
				n[0xC6] = 0x255E; // BOX DRAWINGS VERTICAL SINGLE AND RIGHT DOUBLE
				n[0xC7] = 0x255F; // BOX DRAWINGS VERTICAL DOUBLE AND RIGHT SINGLE
				n[0xC8] = 0x255A; // BOX DRAWINGS DOUBLE UP AND RIGHT
				n[0xC9] = 0x2554; // BOX DRAWINGS DOUBLE DOWN AND RIGHT
				n[0xCA] = 0x2569; // BOX DRAWINGS DOUBLE UP AND HORIZONTAL
				n[0xCB] = 0x2566; // BOX DRAWINGS DOUBLE DOWN AND HORIZONTAL
				n[0xCC] = 0x2560; // BOX DRAWINGS DOUBLE VERTICAL AND RIGHT
				n[0xCD] = 0x2550; // BOX DRAWINGS DOUBLE HORIZONTAL
				n[0xCE] = 0x256C; // BOX DRAWINGS DOUBLE VERTICAL AND HORIZONTAL
				n[0xCF] = 0x2567; // BOX DRAWINGS UP SINGLE AND HORIZONTAL DOUBLE
				n[0xD0] = 0x2568; // BOX DRAWINGS UP DOUBLE AND HORIZONTAL SINGLE
				n[0xD1] = 0x2564; // BOX DRAWINGS DOWN SINGLE AND HORIZONTAL DOUBLE
				n[0xD2] = 0x2565; // BOX DRAWINGS DOWN DOUBLE AND HORIZONTAL SINGLE
				n[0xD3] = 0x2559; // BOX DRAWINGS UP DOUBLE AND RIGHT SINGLE
				n[0xD4] = 0x2558; // BOX DRAWINGS UP SINGLE AND RIGHT DOUBLE
				n[0xD5] = 0x2552; // BOX DRAWINGS DOWN SINGLE AND RIGHT DOUBLE
				n[0xD6] = 0x2553; // BOX DRAWINGS DOWN DOUBLE AND RIGHT SINGLE
				n[0xD7] = 0x256B; // BOX DRAWINGS VERTICAL DOUBLE AND HORIZONTAL SINGLE
				n[0xD8] = 0x256A; // BOX DRAWINGS VERTICAL SINGLE AND HORIZONTAL DOUBLE
				n[0xD9] = 0x2518; // BOX DRAWINGS LIGHT UP AND LEFT
				n[0xDA] = 0x250C; // BOX DRAWINGS LIGHT DOWN AND RIGHT
				n[0xDB] = 0x2588; // FULL BLOCK
				n[0xDC] = 0x2584; // LOWER HALF BLOCK
				n[0xDD] = 0x258C; // LEFT HALF BLOCK
				n[0xDE] = 0x2590; // RIGHT HALF BLOCK
				n[0xDF] = 0x2580; // UPPER HALF BLOCK
				n[0xE0] = 0x03B1; // GREEK SMALL LETTER ALPHA
				n[0xE1] = 0x00DF; // LATIN SMALL LETTER SHARP S
				n[0xE2] = 0x0393; // GREEK CAPITAL LETTER GAMMA
				n[0xE3] = 0x03C0; // GREEK SMALL LETTER PI
				n[0xE4] = 0x03A3; // GREEK CAPITAL LETTER SIGMA
				n[0xE5] = 0x03C3; // GREEK SMALL LETTER SIGMA
				n[0xE6] = 0x00B5; // MICRO SIGN
				n[0xE7] = 0x03C4; // GREEK SMALL LETTER TAU
				n[0xE8] = 0x03A6; // GREEK CAPITAL LETTER PHI
				n[0xE9] = 0x0398; // GREEK CAPITAL LETTER THETA
				n[0xEA] = 0x03A9; // GREEK CAPITAL LETTER OMEGA
				n[0xEB] = 0x03B4; // GREEK SMALL LETTER DELTA
				n[0xEC] = 0x221E; // INFINITY
				n[0xED] = 0x03C6; // GREEK SMALL LETTER PHI
				n[0xEE] = 0x03B5; // GREEK SMALL LETTER EPSILON
				n[0xEF] = 0x2229; // INTERSECTION
				n[0xF0] = 0x2261; // IDENTICAL TO
				n[0xF1] = 0x00B1; // PLUS-MINUS SIGN
				n[0xF2] = 0x2265; // GREATER-THAN OR EQUAL TO
				n[0xF3] = 0x2264; // LESS-THAN OR EQUAL TO
				n[0xF4] = 0x2320; // TOP HALF INTEGRAL
				n[0xF5] = 0x2321; // BOTTOM HALF INTEGRAL
				n[0xF6] = 0x00F7; // DIVISION SIGN
				n[0xF7] = 0x2248; // ALMOST EQUAL TO
				n[0xF8] = 0x00B0; // DEGREE SIGN
				n[0xF9] = 0x2219; // BULLET OPERATOR
				n[0xFA] = 0x00B7; // MIDDLE DOT
				n[0xFB] = 0x221A; // SQUARE ROOT
				n[0xFC] = 0x207F; // SUPERSCRIPT LATIN SMALL LETTER N
				n[0xFD] = 0x00B2; // SUPERSCRIPT TWO
				n[0xFE] = 0x25A0; // BLACK SQUARE
				n[0xFF] = 0x00A0; // NO-BREAK SPACE
				_oem437u = n;
				return _oem437u;
			}
			set { _oem437u = value; }
		}



	}
}
