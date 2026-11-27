#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;

namespace JocysCom.ClassLibrary.Controls.UiTree
{
	/// <summary>The interface in the language the person chose, and English wherever a translation is missing.</summary>
	/// <remarks>
	/// English stays where the designer and the code wrote it, and is what every translation is made from. A language is
	/// one document, <c>{culture}.json</c>, holding for each text its key, the English it was translated from and the
	/// translation. A text whose English has changed since it was translated shows in English until it is translated again,
	/// so a stale translation never says something the program no longer says.
	///
	/// Keys are "OwningType.FieldName.Property" for what a designer made, the pair <see cref="UiText"/> keys its names
	/// by, and a key the program chooses for a text it builds in code, which it passes with the English to
	/// <see cref="T"/>. Controls are recorded and translated in UiLanguage.Forms.cs.
	/// </remarks>
	public static partial class UiLanguage
	{
		/// <summary>The language every text is written in first.</summary>
		public const string SourceCulture = "en";

		/// <summary>The language shown now, such as "de", or <see cref="SourceCulture"/>.</summary>
		public static string Culture { get; private set; } = SourceCulture;

		/// <summary>Raised on the thread that changed the language, after every recorded control shows it.</summary>
		public static event EventHandler Changed;

		static Dictionary<string, UiLanguageEntry> _Items = new Dictionary<string, UiLanguageEntry>(StringComparer.Ordinal);

		/// <summary>Shows a language: its document, or null for English. Recorded controls change at once.</summary>
		public static void Use(UiLanguageDocument document)
		{
			var items = new Dictionary<string, UiLanguageEntry>(StringComparer.Ordinal);
			if (document != null && document.Items != null)
				foreach (var item in document.Items)
					if (!string.IsNullOrEmpty(item.Key) && !string.IsNullOrEmpty(item.Text))
						items[item.Key] = item;
			// Replaced whole, so a reader on another thread sees the old language or the new one, never half of each.
			_Items = items;
			Culture = document == null || string.IsNullOrEmpty(document.Culture) ? SourceCulture : document.Culture;
			Reapply();
			Changed?.Invoke(null, EventArgs.Empty);
		}

		/// <summary>The words for a text the program builds in code: the translation of <paramref name="english"/> under <paramref name="key"/>, or the English itself.</summary>
		/// <remarks>Give both as literals: the sync finds every call by them in the compiled program, so a call with a key made at run time is never translated.</remarks>
		public static string T(string key, string english)
		{
			return Find(key, english) ?? english;
		}

		/// <summary>The translation under a key, or null when there is none or it was made from other English.</summary>
		public static string Find(string key, string english)
		{
			UiLanguageEntry item;
			if (key == null || _InSource || !_Items.TryGetValue(key, out item))
				return null;
			return string.Equals(item.Source, english, StringComparison.Ordinal) ? item.Text : null;
		}

		/// <summary>True on a thread while <see cref="InSource{TResult}"/> reads the English.</summary>
		[ThreadStatic]
		static bool _InSource;

		/// <summary>
		/// What a reader gets in English whatever language is shown, such as an item of a list as the list words it through
		/// <see cref="T"/>: for a reader that must not depend on the language, such as an AI assistant setting a list by text.
		/// </summary>
		public static TResult InSource<TResult>(Func<TResult> read)
		{
			var before = _InSource;
			_InSource = true;
			try
			{
				return read();
			}
			finally
			{
				_InSource = before;
			}
		}

		/// <summary>Puts the language shown now on every recorded control; written in UiLanguage.Forms.cs.</summary>
		static partial void Reapply();

		#region Sync

		/// <summary>What a language document lacks against the English: texts to add, texts whose English changed, and keys no longer used.</summary>
		public static UiLanguageReport Compare(IEnumerable<UiLanguageEntry> sources, UiLanguageDocument document)
		{
			var report = new UiLanguageReport { Culture = document == null ? null : document.Culture };
			var have = new Dictionary<string, UiLanguageEntry>(StringComparer.Ordinal);
			if (document != null && document.Items != null)
				foreach (var item in document.Items)
					if (!string.IsNullOrEmpty(item.Key))
						have[item.Key] = item;
			var keys = new HashSet<string>(StringComparer.Ordinal);
			foreach (var source in sources.OrderBy(x => x.Key, StringComparer.Ordinal))
			{
				if (!keys.Add(source.Key))
					continue;
				report.Total++;
				UiLanguageEntry item;
				if (!have.TryGetValue(source.Key, out item) || string.IsNullOrEmpty(item.Text))
					report.Add.Add(Copy(source));
				else if (!string.Equals(item.Source, source.Source, StringComparison.Ordinal))
				{
					var update = Copy(source);
					update.Was = item.Source;
					update.Text = item.Text;
					report.Update.Add(update);
				}
				else
					report.Translated++;
			}
			foreach (var key in have.Keys.OrderBy(x => x, StringComparer.Ordinal))
				if (!keys.Contains(key))
					report.Remove.Add(key);
			return report;
		}

		static UiLanguageEntry Copy(UiLanguageEntry source)
		{
			return new UiLanguageEntry { Key = source.Key, Source = source.Source, Role = source.Role, Purpose = source.Purpose };
		}

		#endregion

		#region Calls in code

		/// <summary>
		/// Every call to <paramref name="method"/> in an assembly whose first two arguments are literals, as a key and its
		/// English: the texts a program builds in code with <see cref="T"/>, or with a method of the same shape.
		/// </summary>
		/// <param name="prefix">Put before each key, such as the name of the plugin the assembly is.</param>
		/// <remarks>
		/// Read from the compiled program, so what is found is what runs, and there is no second list of texts to keep in
		/// step with the code. The two literals are the two strings loaded just before the call.
		/// </remarks>
		public static List<UiLanguageEntry> FindCalls(Assembly assembly, MethodInfo method, string prefix = null)
		{
			var found = new List<UiLanguageEntry>();
			const BindingFlags all = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			foreach (var type in TypesOf(assembly))
			{
				var bodies = type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all));
				foreach (var body in bodies)
					FindCalls(body, method, prefix, found);
			}
			return found;
		}

		static Type[] TypesOf(Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			// A type that needs an assembly which is not there is left out; the others are still read.
			catch (ReflectionTypeLoadException ex)
			{
				return ex.Types.Where(x => x != null).ToArray();
			}
		}

		static void FindCalls(MethodBase body, MethodInfo method, string prefix, List<UiLanguageEntry> found)
		{
			var il = body.GetMethodBody()?.GetILAsByteArray();
			if (il == null)
				return;
			var module = body.Module;
			string key = null, english = null;
			var i = 0;
			while (i < il.Length)
			{
				var code = il[i] == 0xFE && i + 1 < il.Length ? OpCodeTable.TwoByte[il[++i]] : OpCodeTable.OneByte[il[i]];
				i++;
				var operand = i;
				i += OperandSize(code.OperandType, il, operand);
				if (code.Value == OpCodes.Ldstr.Value)
				{
					key = english;
					english = module.ResolveString(BitConverter.ToInt32(il, operand));
					continue;
				}
				if ((code.Value == OpCodes.Call.Value || code.Value == OpCodes.Callvirt.Value) && key != null
					&& IsMethod(module, BitConverter.ToInt32(il, operand), body, method))
					found.Add(new UiLanguageEntry { Key = prefix + key, Source = english });
				key = null;
				english = null;
			}
		}

		static bool IsMethod(Module module, int token, MethodBase body, MethodInfo method)
		{
			var types = body.DeclaringType != null && body.DeclaringType.IsGenericType ? body.DeclaringType.GetGenericArguments() : null;
			var arguments = body.IsGenericMethod ? body.GetGenericArguments() : null;
			try
			{
				return module.ResolveMethod(token, types, arguments) == method;
			}
			// A call into an assembly that is not there names nothing that can be the method looked for.
			catch (Exception ex) when (ex is ArgumentException || ex is TypeLoadException || ex is System.IO.FileNotFoundException || ex is MissingMemberException)
			{
				return false;
			}
		}

		static int OperandSize(OperandType type, byte[] il, int at)
		{
			switch (type)
			{
				case OperandType.InlineNone:
					return 0;
				case OperandType.ShortInlineBrTarget:
				case OperandType.ShortInlineI:
				case OperandType.ShortInlineVar:
					return 1;
				case OperandType.InlineVar:
					return 2;
				case OperandType.InlineI8:
				case OperandType.InlineR:
					return 8;
				case OperandType.InlineSwitch:
					return 4 + 4 * BitConverter.ToInt32(il, at);
				default:
					return 4;
			}
		}

		/// <summary>Every instruction of the intermediate language, by its one or two bytes.</summary>
		static class OpCodeTable
		{
			internal static readonly OpCode[] OneByte = new OpCode[0x100];
			internal static readonly OpCode[] TwoByte = new OpCode[0x100];

			static OpCodeTable()
			{
				foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
				{
					var code = (OpCode)field.GetValue(null);
					var value = (ushort)code.Value;
					if (code.Size == 1)
						OneByte[value] = code;
					else
						TwoByte[value & 0xFF] = code;
				}
			}
		}

		#endregion

		#region Documents

		/// <summary>Reads a language document.</summary>
		public static UiLanguageDocument Read(string json)
		{
			return Runtime.Serializer.DeserializeFromJson<UiLanguageDocument>(json);
		}

		/// <summary>
		/// A language document as JSON, laid out for a small diff: one text to a line, in the order of its keys, letters
		/// as they are and only what JSON needs escaped.
		/// </summary>
		public static string Write(UiLanguageDocument document)
		{
			var sb = new StringBuilder();
			sb.Append("{\r\n");
			Property(sb, "Culture", document.Culture, true);
			if (!string.IsNullOrEmpty(document.Name))
				Property(sb, "Name", document.Name, true);
			if (document.Glossary != null && document.Glossary.Count > 0)
				List(sb, "Glossary", document.Glossary, true);
			List(sb, "Items", (document.Items ?? new List<UiLanguageEntry>()).OrderBy(x => x.Key, StringComparer.Ordinal), false);
			sb.Append("}\r\n");
			return sb.ToString();
		}

		/// <summary>
		/// What a translator does for one language after a change of the English, as JSON: the glossary, the texts to add
		/// and to update, each with what it is and what it is for, and the keys to remove.
		/// </summary>
		/// <param name="rules">The program's own rule for its words, put after the steps, such as where its terms come from; or null.</param>
		public static string WriteTodo(UiLanguageReport report, string file, IList<UiLanguageEntry> glossary, string rules = null)
		{
			var sb = new StringBuilder();
			sb.Append("{\r\n");
			Property(sb, "Culture", report.Culture, true);
			Property(sb, "File", file, true);
			var instructions = "In " + file + ": add each entry under Add with its Key, its Source and your Text; under Update put the new Source and a new Text in place of the entry's own; delete each key under Remove. Keep {0}-style placeholders, the & of an access key and the names of products as they are. Use the glossary's word for each term, and add a new term to the glossary with its Note.";
			Property(sb, "Instructions", string.IsNullOrEmpty(rules) ? instructions : instructions + " " + rules, true);
			if (glossary != null && glossary.Count > 0)
				List(sb, "Glossary", glossary, true);
			List(sb, "Add", report.Add, true);
			List(sb, "Update", report.Update, true);
			sb.Append("\t\"Remove\": [");
			for (var i = 0; i < report.Remove.Count; i++)
				sb.Append(i == 0 ? "\r\n\t\t" : ",\r\n\t\t").Append(Quote(report.Remove[i]));
			sb.Append(report.Remove.Count == 0 ? "]\r\n" : "\r\n\t]\r\n");
			sb.Append("}\r\n");
			return sb.ToString();
		}

		static void Property(StringBuilder sb, string name, string value, bool more)
		{
			sb.Append('\t').Append(Quote(name)).Append(": ").Append(Quote(value)).Append(more ? ",\r\n" : "\r\n");
		}

		static void List(StringBuilder sb, string name, IEnumerable<UiLanguageEntry> items, bool more)
		{
			sb.Append('\t').Append(Quote(name)).Append(": [");
			var first = true;
			foreach (var item in items)
			{
				sb.Append(first ? "\r\n\t\t" : ",\r\n\t\t");
				first = false;
				sb.Append("{ ");
				var fields = new List<string>();
				Field(fields, "Key", item.Key);
				Field(fields, "Source", item.Source);
				Field(fields, "Was", item.Was);
				Field(fields, "Text", item.Text);
				Field(fields, "Role", item.Role);
				Field(fields, "Purpose", item.Purpose);
				Field(fields, "Note", item.Note);
				sb.Append(string.Join(", ", fields.ToArray())).Append(" }");
			}
			sb.Append(first ? "]" : "\r\n\t]").Append(more ? ",\r\n" : "\r\n");
		}

		static void Field(List<string> fields, string name, string value)
		{
			if (value != null)
				fields.Add(Quote(name) + ": " + Quote(value));
		}

		/// <summary>A JSON string: quotes, backslashes and control characters escaped, every letter as it is.</summary>
		static string Quote(string value)
		{
			if (value == null)
				return "null";
			var sb = new StringBuilder(value.Length + 2);
			sb.Append('"');
			foreach (var c in value)
			{
				switch (c)
				{
					case '"': sb.Append("\\\""); break;
					case '\\': sb.Append("\\\\"); break;
					case '\r': sb.Append("\\r"); break;
					case '\n': sb.Append("\\n"); break;
					case '\t': sb.Append("\\t"); break;
					default:
						if (c < ' ')
							sb.Append("\\u").Append(((int)c).ToString("x4"));
						else
							sb.Append(c);
						break;
				}
			}
			sb.Append('"');
			return sb.ToString();
		}

		#endregion
	}

	/// <summary>One text of the interface: its key, the English it is made from, and its translation.</summary>
	[DataContract]
	public class UiLanguageEntry
	{
		/// <summary>"OwningType.FieldName.Property" for what a designer made, or the key the code gave.</summary>
		[DataMember(Order = 1)]
		public string Key { get; set; }

		/// <summary>The English the translation was made from.</summary>
		[DataMember(Order = 2)]
		public string Source { get; set; }

		/// <summary>The English before it changed, in a to-do list's updates.</summary>
		[DataMember(Order = 3, EmitDefaultValue = false)]
		public string Was { get; set; }

		/// <summary>The translation.</summary>
		[DataMember(Order = 4, EmitDefaultValue = false)]
		public string Text { get; set; }

		/// <summary>What kind of element shows the text, for the translator.</summary>
		[DataMember(Order = 5, EmitDefaultValue = false)]
		public string Role { get; set; }

		/// <summary>What the element is for, for the translator.</summary>
		[DataMember(Order = 6, EmitDefaultValue = false)]
		public string Purpose { get; set; }

		/// <summary>Where a glossary's term comes from, such as the Microsoft Terminology collection, so the next translator keeps it.</summary>
		[DataMember(Order = 7, EmitDefaultValue = false)]
		public string Note { get; set; }
	}

	/// <summary>One language: its culture, its name in itself, the words it uses for the program's terms, and its texts.</summary>
	[DataContract]
	public class UiLanguageDocument
	{
		/// <summary>The culture, such as "de" or "zh-Hans".</summary>
		[DataMember(Order = 1)]
		public string Culture { get; set; }

		/// <summary>The language's name in itself, such as Deutsch.</summary>
		[DataMember(Order = 2, EmitDefaultValue = false)]
		public string Name { get; set; }

		/// <summary>The program's terms in this language, Source and Text, so that one term is translated the same everywhere.</summary>
		[DataMember(Order = 3, EmitDefaultValue = false)]
		public List<UiLanguageEntry> Glossary { get; set; }

		/// <summary>The texts.</summary>
		[DataMember(Order = 4)]
		public List<UiLanguageEntry> Items { get; set; }
	}

	/// <summary>What one language document lacks against the English.</summary>
	public class UiLanguageReport
	{
		public string Culture;

		/// <summary>Texts the document has no translation of.</summary>
		public List<UiLanguageEntry> Add = new List<UiLanguageEntry>();

		/// <summary>Texts whose English changed since they were translated: the new Source, the old one as Was, and the old Text.</summary>
		public List<UiLanguageEntry> Update = new List<UiLanguageEntry>();

		/// <summary>Keys the program no longer has.</summary>
		public List<string> Remove = new List<string>();

		/// <summary>Texts translated from the English the program has now.</summary>
		public int Translated;

		/// <summary>Texts the program has.</summary>
		public int Total;

		/// <summary>True when there is nothing to do.</summary>
		public bool IsComplete { get { return Add.Count == 0 && Update.Count == 0 && Remove.Count == 0; } }
	}
}
