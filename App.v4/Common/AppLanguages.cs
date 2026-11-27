using JocysCom.ClassLibrary.Controls.UiTree;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App
{
	/// <summary>The languages the interface is offered in, the one shown, and the sync that says what each one lacks.</summary>
	/// <remarks>
	/// English is written in the designer files and in the code, and each other language is one document in Languages,
	/// <c>{culture}.json</c>, embedded in the program. <see cref="UiLanguage"/> records the English of every control as it
	/// is built and puts the language chosen on it; a text built in code is written as <c>UiLanguage.T("Key", "English")</c>.
	/// </remarks>
	public static class AppLanguages
	{
		/// <summary>One language: its culture, its name in itself, and the country code of its flag.</summary>
		public sealed class Language
		{
			public Language(string culture, string name, string flag)
			{
				Culture = culture;
				Name = name;
				Flag = flag;
			}

			/// <summary>The culture, such as "de" or "zh-Hans", which names its document.</summary>
			public readonly string Culture;

			/// <summary>The language's name in itself, one word, as the menu shows it.</summary>
			public readonly string Name;

			/// <summary>The ISO 3166 two-letter code of the flag shown beside it.</summary>
			public readonly string Flag;

			/// <summary>The flag, at twice its size at 100 %, from the program's pictures.</summary>
			public Image FlagImage
			{
				get { return Properties.Resources.ResourceManager.GetObject("flag_" + Flag + "_32x32") as Image; }
			}
		}

		/// <summary>Every language offered, in the menu's order.</summary>
		public static readonly Language[] All =
		{
			new Language("en", "English", "gb"),
			new Language("zh-Hans", "中文", "cn"),
			new Language("uk", "Українська", "ua"),
			new Language("es", "Español", "es"),
			new Language("pt-BR", "Português", "br"),
			new Language("de", "Deutsch", "de"),
			new Language("ja", "日本語", "jp"),
			new Language("fr", "Français", "fr"),
			new Language("pl", "Polski", "pl"),
			new Language("ko", "한국어", "kr"),
			new Language("tr", "Türkçe", "tr"),
		};

		/// <summary>The language shown now.</summary>
		public static Language Current { get; private set; } = All[0];

		/// <summary>Where the documents are embedded: the name of a document is this and its culture and ".json".</summary>
		public const string ResourcePrefix = "x360ce.App.Languages.";

		/// <summary>The language a saved choice names; with none, Windows' display language when it is offered, else English.</summary>
		public static Language For(string saved)
		{
			var language = All.FirstOrDefault(x => string.Equals(x.Culture, saved, StringComparison.OrdinalIgnoreCase));
			return language ?? For(CultureInfo.CurrentUICulture) ?? All[0];
		}

		/// <summary>The language offered for a culture, or null. Chinese is offered in Simplified characters only.</summary>
		public static Language For(CultureInfo culture)
		{
			for (var c = culture; c != null && !string.IsNullOrEmpty(c.Name); c = c.Parent)
			{
				var name = c.Name;
				if (name.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase) || name == "zh-TW" || name == "zh-HK" || name == "zh-MO")
					return null;
				if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
					return All.First(x => x.Culture == "zh-Hans");
				if (name.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
					return All.First(x => x.Culture == "pt-BR");
				var language = All.FirstOrDefault(x => string.Equals(x.Culture, name, StringComparison.OrdinalIgnoreCase));
				if (language != null)
					return language;
			}
			return null;
		}

		/// <summary>Shows a language at once: every control recorded, and the plugins' pages through <see cref="global::x360ce.Plugins.PluginText"/>.</summary>
		public static void Use(Language language)
		{
			global::x360ce.Plugins.PluginText.Translate = UiLanguage.T;
			Current = language;
			UiLanguage.Use(language.Culture == UiLanguage.SourceCulture ? null : ReadEmbedded(language.Culture));
			global::x360ce.Plugins.PluginText.OnChanged();
		}

		/// <summary>The document of a language embedded in the program, or null when there is none.</summary>
		public static UiLanguageDocument ReadEmbedded(string culture)
		{
			using (var stream = typeof(AppLanguages).Assembly.GetManifestResourceStream(ResourcePrefix + culture + ".json"))
			{
				if (stream == null)
					return null;
				using (var reader = new StreamReader(stream, Encoding.UTF8))
					return UiLanguage.Read(reader.ReadToEnd());
			}
		}

		#region Menu

		/// <summary>Fills the globe's menu: each language with its flag and its name in itself, the one shown ticked.</summary>
		public static void FillMenu(ToolStripDropDownItem button, Action<Language> choose)
		{
			button.DropDownItems.Clear();
			foreach (var language in All)
			{
				var item = new ToolStripMenuItem(language.Name, language.FlagImage)
				{
					Name = "Language_" + language.Culture.Replace("-", "_"),
					Tag = language,
					Checked = language == Current,
					// One of them is on, and the AI door reads each as a box that is ticked or not.
					CheckOnClick = true,
					// A screen reader announces the language by its name in itself, which is how a person looks for it.
					AccessibleName = language.Name,
					AccessibleDescription = "Shows the window in " + language.Name + ".",
				};
				// A language is named in itself in every language.
				UiLanguage.Keep(item);
				item.Click += (sender, e) => choose(language);
				button.DropDownItems.Add(item);
			}
		}

		/// <summary>Ticks the language shown in the globe's menu.</summary>
		public static void TickMenu(ToolStripDropDownItem button)
		{
			foreach (ToolStripItem item in button.DropDownItems)
			{
				var menu = item as ToolStripMenuItem;
				if (menu != null)
					menu.Checked = menu.Tag == Current;
			}
		}

		#endregion

		#region Sync

		/// <summary>The command-line switch that runs the sync: <c>x360ce.exe -Languages=&lt;folder&gt;</c>.</summary>
		public const string Switch = "Languages";

		/// <summary>The rule for every translation, in each to-do list: docs/Help.Languages.md says the same.</summary>
		public const string TranslationRule = "Translate with the words this language uses for programs and settings, as Windows (Microsoft's terminology), Steam, Xbox and Logitech G HUB say them, never everyday wording; research a term that is unclear, and record it in the glossary with its source as its Note. See docs/Help.Languages.md.";

		/// <summary>Warns that the sync itself stopped; the languages are then not checked.</summary>
		public const string WarningFailed = "XLANG000";

		/// <summary>Warns that a language document lacks texts; shown by the build.</summary>
		public const string WarningLacks = "XLANG001";

		/// <summary>Warns that a plugin's key does not start with the plugin's name.</summary>
		public const string WarningPluginKey = "XLANG002";

		/// <summary>Warns that a control or an issue shows an English sentence written in code without <see cref="UiLanguage.T"/>.</summary>
		public const string WarningUntranslated = "XLANG003";

		/// <summary>Whether a type shows its texts to a person: a control, or an issue on the Issues page and what words it.</summary>
		static bool Shows(Type type)
		{
			return typeof(Control).IsAssignableFrom(type)
				|| typeof(JocysCom.ClassLibrary.Controls.IssuesControl.IssueItem).IsAssignableFrom(type)
				|| (type.Namespace != null && type.Namespace.EndsWith(".Issues", StringComparison.Ordinal));
		}

		/// <summary>One warning for each English sentence a control or an issue shows that no language can translate.</summary>
		static void FindUntranslated(IEnumerable<Assembly> plugins, List<string> warnings)
		{
			var t = typeof(UiLanguage).GetMethod(nameof(UiLanguage.T));
			var pluginT = typeof(global::x360ce.Plugins.PluginText).GetMethod(nameof(global::x360ce.Plugins.PluginText.T));
			var assemblies = new[] { typeof(AppLanguages).Assembly, typeof(UiLanguage).Assembly }.Concat(plugins);
			foreach (var assembly in assemblies)
				foreach (var text in UiLanguage.FindUntranslated(assembly, Shows, t, pluginT))
					warnings.Add(string.Format("warning {0}: {1}: not translated, write it with UiLanguage.T(\"Key\", \"English\")",
						WarningUntranslated, assembly.GetName().Name + " " + text.Replace("\r", "\\r").Replace("\n", "\\n")));
		}

		/// <summary>
		/// Every English text of the interface, for the sync: the texts of the controls recorded, the names and purposes
		/// the window's help line shows, and the texts built in code here, in the engine and in each plugin.
		/// </summary>
		public static List<UiLanguageEntry> Sources(IEnumerable<Assembly> plugins, List<string> warnings)
		{
			var list = UiLanguage.RecordedTexts();
			foreach (var pair in UiTree.UiCatalog.Build())
			{
				var text = pair.Value;
				// A reading's name is kept for the document, and the help line never shows it.
				if (!text.DocumentOnly && !string.IsNullOrEmpty(text.Name))
					list.Add(new UiLanguageEntry { Key = pair.Key + ".AccessibleName", Source = text.Name, Role = "Help line name", Purpose = text.Purpose });
				if (!string.IsNullOrEmpty(text.Purpose))
					list.Add(new UiLanguageEntry { Key = pair.Key + ".AccessibleDescription", Source = text.Purpose, Role = "Help line", Purpose = text.Name });
			}
			var t = typeof(UiLanguage).GetMethod(nameof(UiLanguage.T));
			foreach (var assembly in new[] { typeof(AppLanguages).Assembly, typeof(UiLanguage).Assembly })
				foreach (var found in UiLanguage.FindCalls(assembly, t))
					list.Add(Code(found));
			var pluginT = typeof(global::x360ce.Plugins.PluginText).GetMethod(nameof(global::x360ce.Plugins.PluginText.T));
			foreach (var assembly in plugins)
			{
				var name = assembly.GetName().Name;
				foreach (var found in UiLanguage.FindCalls(assembly, pluginT))
				{
					if (!found.Key.StartsWith(name + ".", StringComparison.Ordinal))
						warnings.Add(string.Format("warning {0}: the key \"{1}\" in plugin {2} does not start with \"{2}.\"", WarningPluginKey, found.Key, name));
					list.Add(Code(found));
				}
			}
			// One entry per key: a key used twice with other English is listed by the sync as it was found first.
			return list.GroupBy(x => x.Key, StringComparer.Ordinal).Select(x => x.First()).OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
		}

		static UiLanguageEntry Code(UiLanguageEntry found)
		{
			found.Role = "Text built in code";
			return found;
		}

		/// <summary>
		/// Compares every language document in a folder with the English, writes <c>en.json</c> with every text and what it
		/// is for, and for each language that lacks something writes <c>{culture}.todo.json</c> and a build warning.
		/// </summary>
		/// <returns>
		/// 0 when every language has every text, 1 when one lacks some. The build warns and goes on: a text without a
		/// translation shows in English. It runs the sync again on its next build until the warnings are gone.
		/// </returns>
		public static int Sync(string folder, IEnumerable<Assembly> plugins, TextWriter output)
		{
			folder = Path.GetFullPath(folder);
			Directory.CreateDirectory(folder);
			var warnings = new List<string>();
			var sources = Sources(plugins, warnings);
			FindUntranslated(plugins, warnings);
			WriteIfChanged(Path.Combine(folder, UiLanguage.SourceCulture + ".json"), UiLanguage.Write(new UiLanguageDocument
			{
				Culture = UiLanguage.SourceCulture,
				Name = All[0].Name,
				Items = sources,
			}));
			foreach (var warning in warnings)
				output.WriteLine(Path.Combine(folder, UiLanguage.SourceCulture + ".json") + "(1,1): " + warning);
			var summary = new List<string>();
			var lacks = warnings.Count > 0;
			foreach (var language in All.Skip(1))
			{
				var file = Path.Combine(folder, language.Culture + ".json");
				var todo = Path.Combine(folder, language.Culture + ".todo.json");
				var document = File.Exists(file)
					? UiLanguage.Read(File.ReadAllText(file, Encoding.UTF8))
					: new UiLanguageDocument { Culture = language.Culture, Name = language.Name };
				var report = UiLanguage.Compare(sources, document);
				report.Culture = language.Culture;
				summary.Add(string.Format("{0} {1}/{2}", language.Culture, report.Translated, report.Total));
				if (report.IsComplete)
				{
					if (File.Exists(todo))
						File.Delete(todo);
					continue;
				}
				lacks = true;
				WriteIfChanged(todo, UiLanguage.WriteTodo(report, Path.GetFileName(file), document.Glossary, TranslationRule));
				output.WriteLine("{0}(1,1): warning {1}: {2} to add, {3} to update, {4} to remove - see {5}",
					file, WarningLacks, report.Add.Count, report.Update.Count, report.Remove.Count, todo);
			}
			output.WriteLine("Languages, translated of " + sources.Count + ": " + string.Join(", ", summary));
			return lacks ? 1 : 0;
		}

		/// <summary>Writes a file only when its text differs, so a file that is already right keeps its time.</summary>
		static void WriteIfChanged(string path, string text)
		{
			var utf8 = new UTF8Encoding(false);
			if (File.Exists(path) && File.ReadAllText(path, utf8) == text)
				return;
			File.WriteAllText(path, text, utf8);
		}

		#endregion
	}
}
