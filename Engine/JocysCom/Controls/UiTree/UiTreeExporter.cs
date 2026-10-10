#nullable disable
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
#if !NETFRAMEWORK
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
#endif
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls.UiTree
{
	/// <summary>Writes what the program looks like to a pair of documents.</summary>
	/// <remarks>
	/// The program is the only accurate account of its own features, so it writes the account
	/// itself. Anyone - a person or a program helping one - reads the result instead of guessing
	/// from screenshots or from documentation that has drifted.
	/// </remarks>
	public static class UiTreeExporter
	{
		/// <summary>The documents' name without the extension, set by each program, such as ui-tree or ui-tree-v4.</summary>
		public static string BaseName = "ui-tree";

		public static string JsonFileName { get { return BaseName + ".json"; } }

		public static string MarkdownFileName { get { return BaseName + ".md"; } }

		static readonly Regex MarkdownStamp = new Regex(@"(?m)^Version \S+, built \S+\.(?=\r?$)");
		static readonly Regex JsonVersion = new Regex("(\"Version\":\\s*)\"[^\"]*\"");
		static readonly Regex JsonBuilt = new Regex("(\"Built\":\\s*)\"[^\"]*\"");

		/// <summary>A written document, Markdown or JSON, with another version and build day in place of its own.</summary>
		/// <remarks>
		/// Lets a copy installed by a later build of the same interface name the build that installed it. The space
		/// after each colon stays as the document has it, so the copy is laid out like the file it came from.
		/// </remarks>
		public static string Restamp(string document, string version, string built)
		{
			document = MarkdownStamp.Replace(document, UiTreeMarkdown.Stamp(version, built), 1);
			document = JsonVersion.Replace(document, "${1}\"" + version + "\"", 1);
			return JsonBuilt.Replace(document, "${1}\"" + built + "\"", 1);
		}

#if NETFRAMEWORK
		/// <summary>The tree as JSON: indented with tabs for the document, on one line for a tool's answer.</summary>
		/// <remarks>
		/// Written by the data contract serialiser .NET Framework carries, which leaves out what an element does not
		/// have as the contract says. It writes '/' as '\/', valid JSON and useless as a path, so '/' is put back.
		/// </remarks>
		public static string ToJson(UiNode node, bool indented)
		{
			var json = JocysCom.ClassLibrary.Runtime.Serializer.SerializeToJson(node).Replace("\\/", "/");
			return indented ? JocysCom.ClassLibrary.Runtime.Serializer.FormatJson(json) : json;
		}
#else
		static readonly JsonSerializerOptions Indented = Options(true);
		static readonly JsonSerializerOptions Compact = Options(false);

		static JsonSerializerOptions Options(bool indented)
		{
			return new JsonSerializerOptions
			{
				WriteIndented = indented,
				IndentCharacter = '\t',
				IndentSize = 1,
				// What an element does not have is left out, as the document has always done.
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
				// A name keeps its letters as written, and '/' stays '/', so a path can be pasted back.
				Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			};
		}

		/// <summary>The tree as JSON: indented with tabs for the document, on one line for a tool's answer.</summary>
		public static string ToJson(UiNode node, bool indented)
		{
			return JsonSerializer.Serialize(node, indented ? Indented : Compact);
		}
#endif

		/// <summary>Describes the whole program: its shared controls, its window, and its tray menu.</summary>
		/// <param name="window">The main window, already built: a form or a WPF window.</param>
		/// <param name="trayMenu">The menu behind the tray icon, or null when there is none.</param>
		/// <param name="raw">True keeps every arranging panel, for looking at what is there.</param>
		public static UiNode Read(object window, ContextMenuStrip trayMenu, bool raw = false)
		{
			// The version and build day of the program that owns the window, so a reader can tell whether this is current.
			var program = new JocysCom.ClassLibrary.Configuration.AssemblyInfo(window.GetType().Assembly);
			var root = new UiNode
			{
				Name = program.Product,
				Role = "Program",
				Version = program.Version.ToString(),
				Built = program.BuildDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
			};
			var app = UiTreeWalker.Read(window, raw, null);
			app.Name = "App";
			app.Role = "Section";
			app.Description = "The main window.";
			// The window is built, not shown, while the description is written, which says nothing about the program.
			app.Hidden = false;
			var shared = new UiNode
			{
				Name = "Controls",
				Role = "Section",
				Description = "Controls used in more than one place, described once here.",
			};
			foreach (var control in ExtractShared(app))
				shared.Add(control);
			if (shared.Items != null)
				root.Add(shared);
			root.Add(app);
			if (trayMenu != null)
			{
				var tray = UiTreeWalker.Read(trayMenu, raw);
				tray.Name = "Tray";
				tray.Role = "Section";
				tray.Description = "The menu behind the icon in the notification area.";
				// A menu shows only while it is open, which says nothing about the program either.
				tray.Hidden = false;
				root.Add(tray);
			}
			return root;
		}

		/// <summary>
		/// Moves a control used in more than one place into its own description, and leaves a
		/// reference behind at each place it appears. A controller panel exists four times; saying
		/// what is inside it four times would make the document four times as long and no clearer.
		/// </summary>
		static List<UiNode> ExtractShared(UiNode app)
		{
			var byType = new Dictionary<string, List<UiNode>>();
			Collect(app, byType);
			var shared = new List<UiNode>();
			foreach (var pair in byType.OrderBy(x => x.Key))
			{
				if (pair.Value.Count < 2)
					continue;
				// The instance that carries the most is the one worth describing: a panel built for
				// a device that is absent can be emptier than its siblings.
				var richest = pair.Value.OrderByDescending(Count).First();
				DropRangesThatDiffer(pair.Value, richest);
				var description = new UiNode
				{
					Name = pair.Key,
					Role = "Control",
					Description = richest.Description,
					Items = richest.Items,
				};
				shared.Add(description);
				foreach (var instance in pair.Value)
				{
					instance.Items = null;
					instance.SameAs = pair.Key;
				}
			}
			return shared;
		}

		/// <summary>
		/// Clears the range of any element whose instances disagree about it. The same control holds
		/// a trigger, which runs to 255, and a thumb axis, which runs to 32767; one description
		/// covers both, so it may only state a range where every instance states the same one.
		/// </summary>
		static void DropRangesThatDiffer(List<UiNode> instances, UiNode description)
		{
			var seen = new Dictionary<string, string>();
			var differs = new List<string>();
			foreach (var instance in instances)
				Ranges(instance, seen, differs);
			ClearRanges(description, differs);
		}

		static void Ranges(UiNode node, Dictionary<string, string> seen, List<string> differs)
		{
			if (!string.IsNullOrEmpty(node.Id) && node.Min.HasValue)
			{
				var range = node.Min + ".." + node.Max;
				string first;
				if (!seen.TryGetValue(node.Id, out first))
					seen[node.Id] = range;
				else if (first != range && !differs.Contains(node.Id))
					differs.Add(node.Id);
			}
			if (node.Items == null)
				return;
			foreach (var child in node.Items)
				Ranges(child, seen, differs);
		}

		static void ClearRanges(UiNode node, List<string> differs)
		{
			if (!string.IsNullOrEmpty(node.Id) && differs.Contains(node.Id))
			{
				node.Min = null;
				node.Max = null;
			}
			if (node.Items == null)
				return;
			foreach (var child in node.Items)
				ClearRanges(child, differs);
		}

		static void Collect(UiNode node, Dictionary<string, List<UiNode>> byType)
		{
			if (!string.IsNullOrEmpty(node.Type))
			{
				List<UiNode> list;
				if (!byType.TryGetValue(node.Type, out list))
					byType[node.Type] = list = new List<UiNode>();
				list.Add(node);
			}
			if (node.Items == null)
				return;
			foreach (var child in node.Items)
				Collect(child, byType);
		}

		static int Count(UiNode node)
		{
			if (node.Items == null)
				return 1;
			return 1 + node.Items.Sum(Count);
		}

		/// <summary>Writes both documents into a folder, and says what it wrote.</summary>
		public static string Write(UiNode root, string folder)
		{
			Directory.CreateDirectory(folder);
			var jsonPath = Path.Combine(folder, JsonFileName);
			var markdownPath = Path.Combine(folder, MarkdownFileName);
			// No byte order mark. It is not part of JSON, and a reader that follows the standard
			// strictly - Python's among them - refuses a document that starts with one. These files
			// exist to be read by other programs, so they are written the way those expect.
			var utf8 = new UTF8Encoding(false);
			File.WriteAllText(jsonPath, ToJson(root, true), utf8);
			File.WriteAllText(markdownPath, UiTreeMarkdown.Write(root), utf8);
			return jsonPath + "\r\n" + markdownPath;
		}
	}
}
