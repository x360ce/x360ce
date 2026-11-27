using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace x360ce.Engine
{
	/// <summary>
	/// The parts x360ce carries from other projects, as Resources/Licenses/README.md lists them, and their licences, which
	/// each program's About page shows after its own.
	/// </summary>
	/// <remarks>
	/// A program embeds the list and each licence file under the file's path in the repository (a Link), so each resource's
	/// name ends with that path, its folders joined by dots.
	/// </remarks>
	public static class ThirdPartyLicences
	{
		/// <summary>The list's path in the repository; its links to licence files are relative to its folder.</summary>
		public const string ListPath = "Resources/Licenses/README.md";

		/// <summary>One row of the list.</summary>
		public sealed class Part
		{
			/// <summary>What is carried, such as "SharpDX".</summary>
			public string Name;
			/// <summary>What x360ce uses it for.</summary>
			public string Use;
			/// <summary>The programs that carry it: "x360ce 4", "x360ce 3", "XInput library".</summary>
			public string[] Programs;
			/// <summary>Where the project lives.</summary>
			public string Address;
			/// <summary>The licence's name.</summary>
			public string Licence;
			/// <summary>The licence file's path in the repository, with forward slashes.</summary>
			public string File;
			/// <summary>Where the licence file was taken from, at which version, when, and its SHA-256.</summary>
			public string Provenance;
		}

		/// <summary>The rows of the list's first table, in its order.</summary>
		public static List<Part> Parse(string markdown)
		{
			var parts = new List<Part>();
			var lines = markdown.Replace("\r\n", "\n").Split('\n').Select(x => x.Trim()).ToArray();
			var start = Array.FindIndex(lines, IsTableRow);
			if (start < 0)
				return parts;
			var header = Cells(lines[start]);
			Func<string[], string, string> cell = (cells, column) =>
			{
				var i = Array.IndexOf(header, column);
				return i >= 0 && i < cells.Length ? cells[i] : "";
			};
			// The row under the header only aligns the columns.
			for (var i = start + 2; i < lines.Length && IsTableRow(lines[i]); i++)
			{
				var cells = Cells(lines[i]);
				var link = Link.Match(cell(cells, "Licence file"));
				parts.Add(new Part
				{
					Name = cell(cells, "Part"),
					Use = cell(cells, "Used for"),
					Programs = cell(cells, "Programs").Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray(),
					Address = cell(cells, "Address").Trim('<', '>'),
					Licence = cell(cells, "Licence"),
					File = link.Success ? FromList(link.Groups[1].Value) : "",
					Provenance = cell(cells, "Provenance"),
				});
			}
			return parts;
		}

		/// <summary>The name the list gives a program: "x360ce" and its major version.</summary>
		public static string ProgramName(Assembly program)
		{
			return "x360ce " + program.GetName().Version.Major;
		}

		/// <summary>A text file the program embeds under its path in the repository, or null when it does not carry it.</summary>
		/// <param name="program">The program's assembly.</param>
		/// <param name="path">The file's path in the repository, with forward slashes.</param>
		public static string ReadFile(Assembly program, string path)
		{
			var name = ResourceName(program, path);
			if (name == null)
				return null;
			using (var stream = program.GetManifestResourceStream(name))
			using (var reader = new StreamReader(stream, Encoding.UTF8))
				return reader.ReadToEnd();
		}

		/// <summary>The name of the resource the program embeds a file of the repository under, or null when it does not carry it.</summary>
		public static string ResourceName(Assembly program, string path)
		{
			var suffix = "." + path.Replace('/', '.');
			return program.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(suffix, StringComparison.Ordinal));
		}

		/// <summary>
		/// The licences of the parts the list says the program carries, in the list's order, to put under the program's own:
		/// each under a rule, its parts' names and addresses, and the licence's name.
		/// </summary>
		/// <remarks>Parts next to each other in the list that share one licence file are named together above it.</remarks>
		public static string Text(Assembly program)
		{
			var list = ReadFile(program, ListPath);
			if (string.IsNullOrEmpty(list))
				return "";
			var name = ProgramName(program);
			var parts = Parse(list).Where(x => x.Programs.Contains(name)).ToList();
			var text = new StringBuilder();
			for (var i = 0; i < parts.Count;)
			{
				var next = i + 1;
				while (next < parts.Count && parts[next].File == parts[i].File && parts[next].Licence == parts[i].Licence)
					next++;
				var licence = ReadFile(program, parts[i].File);
				if (!string.IsNullOrEmpty(licence))
				{
					var names = parts.Skip(i).Take(next - i).Select(x => x.Name + " (" + x.Address + ")").ToArray();
					var named = names.Length == 1 ? names[0] : string.Join(", ", names, 0, names.Length - 1) + " and " + names[names.Length - 1];
					text.Append("\r\n\r\n").Append('-', 72).Append("\r\n\r\n")
						.Append(named).Append(", ").Append(parts[i].Licence).Append(":\r\n\r\n").Append(licence);
				}
				i = next;
			}
			return text.ToString();
		}

		/// <summary>A link written as [text](target).</summary>
		static readonly Regex Link = new Regex(@"\[[^\]]*\]\(([^)\s]+)\)");

		static bool IsTableRow(string line)
		{
			return line.Length > 1 && line.StartsWith("|", StringComparison.Ordinal) && line.EndsWith("|", StringComparison.Ordinal);
		}

		static string[] Cells(string row)
		{
			return row.Substring(1, row.Length - 2).Split('|').Select(x => x.Trim()).ToArray();
		}

		/// <summary>A link of the list made into a path in the repository.</summary>
		static string FromList(string link)
		{
			var path = new List<string>(ListPath.Split('/'));
			path.RemoveAt(path.Count - 1);
			foreach (var segment in link.Split('/'))
			{
				if (segment == "..")
					path.RemoveAt(path.Count - 1);
				else if (segment != ".")
					path.Add(segment);
			}
			return string.Join("/", path);
		}
	}
}
