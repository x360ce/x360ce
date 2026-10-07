using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace x360ce.App
{
	/// <summary>
	/// The x360ce skill for AI agents: the copy built into the program, written into the folders agents read skills
	/// from, into a ZIP for the Claude app, or printed by the /Skill switch, with the program's help and a description
	/// of its interface beside it.
	/// </summary>
	/// <remarks>
	/// Installed from the program's own copy only: a copy from the internet could describe tools this build does not
	/// have. A newer skill arrives with a newer program. Two folders and one ZIP reach the common agents on Windows:
	/// Claude Code reads its own folder, and Codex, GitHub Copilot, Gemini CLI, Cursor, OpenCode and Windsurf read
	/// the shared one. An agent that meets the program with none of them reads the skill from the program itself.
	/// </remarks>
	public static class AiSkill
	{
		/// <summary>The skill's name, which is also its folder's name.</summary>
		public const string Name = "x360ce";

		/// <summary>The embedded skills/x360ce/SKILL.md.</summary>
		public const string Resource = "Documents.x360ce.SKILL.md";

		/// <summary>The embedded docs/Help.v3.md, the help version 3 shows.</summary>
		public const string HelpV3Resource = "Documents.Help.v3.md";

		/// <summary>The references beside SKILL.md, by their path in the skill's folder, and the embedded document each one is.</summary>
		/// <remarks>Both programs, each named for its version as they were in docs: the skill teaches version 4, and an agent may meet version 3.</remarks>
		public static readonly Dictionary<string, string> References = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "references/help-v4.md", AppHelper.HelpV4Resource },
			{ "references/ui-tree-v4.md", "Documents.x360ce.ui-tree-v4.md" },
			{ "references/ui-tree-v4.json", "Documents.x360ce.ui-tree-v4.json" },
			{ "references/help-v3.md", HelpV3Resource },
			{ "references/ui-tree-v3.md", "Documents.x360ce.ui-tree-v3.md" },
			{ "references/ui-tree-v3.json", "Documents.x360ce.ui-tree-v3.json" },
		};

		public enum State
		{
			NotInstalled,
			UpToDate,
			/// <summary>A copy older than the program, or one that carries no version.</summary>
			Older,
			/// <summary>A copy a newer program wrote; it is left alone.</summary>
			Newer,
		}

		static readonly Regex VersionLine = new Regex(@"(?m)^(\s*version:\s*)""?([^""\r\n]*)""?[ \t]*\r?$");

		static string UserProfile { get { return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); } }

		/// <summary>Where Claude Code reads skills: the skills folder of CLAUDE_CONFIG_DIR when it is set, otherwise of .claude in the user's folder.</summary>
		public static string ClaudeFolder
		{
			get
			{
				var config = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
				return Path.Combine(string.IsNullOrEmpty(config) ? Path.Combine(UserProfile, ".claude") : config, "skills");
			}
		}

		/// <summary>Where Codex, GitHub Copilot, Gemini CLI, Cursor, OpenCode and Windsurf read skills.</summary>
		public static string AgentsFolder { get { return Path.Combine(UserProfile, ".agents", "skills"); } }

		/// <summary>The version of the program that carries the skill, which an installed copy carries so a later program can tell it is out of date.</summary>
		public static Version ProgramVersion { get { return typeof(AiSkill).Assembly.GetName().Version; } }

		/// <summary>The day the program was built, as its interface description names it.</summary>
		public static string ProgramBuilt
		{
			get { return new JocysCom.ClassLibrary.Configuration.AssemblyInfo(typeof(AiSkill).Assembly).BuildDateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture); }
		}

		/// <summary>The start of this program's own interface description among the references, which an install stamps with its version and build day.</summary>
		const string OwnTree = "references/ui-tree-v4";

		/// <summary>The skill as the program ships it, its version set to the program's.</summary>
		/// <remarks>Written with LF line endings whatever the checkout it was built from, so every install is the same.</remarks>
		public static string Text(Version version)
		{
			var text = AppHelper.ReadHelp(Resource).Replace("\r\n", "\n");
			if (string.IsNullOrEmpty(text))
				throw new InvalidOperationException("The program carries no skill (" + Resource + ").");
			return VersionLine.Replace(text, "${1}\"" + version + "\"", 1);
		}

		/// <summary>The skill's files by their path in its folder: SKILL.md with the version set, and its references.</summary>
		public static Dictionary<string, string> Files(Version version)
		{
			var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "SKILL.md", Text(version) } };
			foreach (var reference in References)
			{
				var text = AppHelper.ReadHelp(reference.Value);
				// The description was written by the build that last exported it; the copy names the build that installs it,
				// so an agent can tell from the file alone whether it is current. Version 3's keeps its own.
				if (reference.Key.StartsWith(OwnTree, StringComparison.OrdinalIgnoreCase))
					text = Engine.UiTree.UiTreeExporter.Restamp(text, version.ToString(), ProgramBuilt);
				files.Add(reference.Key, text);
			}
			return files;
		}

		/// <summary>The version a SKILL.md carries, or null when it carries none.</summary>
		public static Version VersionOf(string text)
		{
			var match = VersionLine.Match(text ?? "");
			Version version;
			return match.Success && Version.TryParse(match.Groups[2].Value, out version) ? version : null;
		}

		/// <summary>Whether the folder holds the skill, and whether it is the program's own, older or newer. Installed is the version it carries.</summary>
		public static State Check(string skillsFolder, Version version, out Version installed)
		{
			installed = null;
			var file = Path.Combine(skillsFolder, Name, "SKILL.md");
			if (!File.Exists(file))
				return State.NotInstalled;
			installed = VersionOf(File.ReadAllText(file));
			if (installed == null || installed < version)
				return State.Older;
			return installed > version ? State.Newer : State.UpToDate;
		}

		/// <summary>Writes the skill's folder into a skills folder, with the program's version unless another is given.</summary>
		/// <remarks>
		/// Only the x360ce folder is touched. It ends as the program ships it: each file written where it differs, and
		/// anything else in it removed, which is what an older copy left behind. A file that is already right keeps its
		/// time, so the build that writes the skill into the repository, where these files are also its sources, does
		/// not start another build.
		/// </remarks>
		public static void Install(string skillsFolder, Version version = null)
		{
			var folder = Path.GetFullPath(Path.Combine(skillsFolder, Name));
			var utf8 = new UTF8Encoding(false);
			var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var file in Files(version ?? ProgramVersion))
			{
				var path = Path.Combine(folder, file.Key.Replace('/', Path.DirectorySeparatorChar));
				written.Add(path);
				var bytes = utf8.GetBytes(file.Value);
				if (File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(bytes))
					continue;
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				File.WriteAllBytes(path, bytes);
			}
			foreach (var path in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
				if (!written.Contains(path))
					File.Delete(path);
			// Deepest first, so a folder emptied by its subfolders' removal goes too.
			foreach (var path in Directory.GetDirectories(folder, "*", SearchOption.AllDirectories).OrderByDescending(x => x.Length))
				if (!Directory.EnumerateFileSystemEntries(path).Any())
					Directory.Delete(path);
		}

		/// <summary>The skill as a ZIP the Claude app takes under Customize, Skills: the x360ce folder with its files.</summary>
		public static void SaveZip(string zipPath, Version version = null)
		{
			var temp = Path.Combine(Path.GetTempPath(), "x360ce-skill-" + Guid.NewGuid().ToString("N"));
			try
			{
				Install(temp, version);
				if (File.Exists(zipPath))
					File.Delete(zipPath);
				JocysCom.ClassLibrary.Files.Zip.ZipFiles(temp, zipPath);
			}
			finally
			{
				if (Directory.Exists(temp))
					Directory.Delete(temp, true);
			}
		}

		/// <summary>What the /Skill switch does: alone it prints SKILL.md, with help or ui-tree it prints that reference, and with a folder that exists it installs the skill there. Returns the exit code.</summary>
		/// <param name="value">The switch's value.</param>
		/// <param name="callerFolder">The folder the command was given in, which a relative folder is taken from.</param>
		/// <param name="output">Where the skill is printed.</param>
		/// <param name="error">Where a failure is said.</param>
		public static int RunSwitch(string value, string callerFolder, TextWriter output, TextWriter error)
		{
			value = (value ?? "").Trim().Trim('"');
			var file = FileOf(value);
			if (file != null)
			{
				output.Write(Files(ProgramVersion)[file]);
				return 0;
			}
			// Only into a folder that is there: a mistyped name, or an argument PowerShell split at its dot, makes no folders.
			var folder = FolderOf(callerFolder, value);
			if (folder == null)
			{
				error.WriteLine("No skill file and no folder " + value + ". -Skill prints the skill, -Skill=help the help, " +
					"-Skill=ui-tree every element of the interface, -Skill=<file> any other of " + string.Join(", ", References.Keys) +
					", and -Skill=<folder> writes the skill into a skills folder that exists.");
				return 1;
			}
			try
			{
				Install(folder);
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{
				error.WriteLine("The skill was not written to " + folder + ": " + ex.Message);
				return 1;
			}
			output.WriteLine("Wrote the skill to " + Path.Combine(folder, Name) + ".");
			return 0;
		}

		/// <summary>The skill's file a /Skill value names, or null.</summary>
		/// <remarks>
		/// Named without its folder and its .md too, because PowerShell splits an unquoted -Skill=references/help-v4.md
		/// at the dot. Help and ui-tree alone are this program's, version 4.
		/// </remarks>
		static string FileOf(string value)
		{
			var name = value.Replace('\\', '/');
			if (name.StartsWith("references/", StringComparison.OrdinalIgnoreCase))
				name = name.Substring("references/".Length);
			if (name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
				name = name.Substring(0, name.Length - ".md".Length);
			if (name.Length == 0 || string.Equals(name, "skill", StringComparison.OrdinalIgnoreCase))
				return "SKILL.md";
			if (string.Equals(name, "help", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ui-tree", StringComparison.OrdinalIgnoreCase))
				name += "-v4";
			var path = "references/" + name;
			if (References.ContainsKey(path))
				return path;
			return References.ContainsKey(path + ".md") ? path + ".md" : null;
		}

		/// <summary>The full path of a folder that exists, taken from the caller's folder when relative, or null.</summary>
		static string FolderOf(string callerFolder, string value)
		{
			string folder;
			try
			{
				folder = Path.GetFullPath(Path.Combine(callerFolder, value));
			}
			catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
			{
				return null;
			}
			return Directory.Exists(folder) ? folder : null;
		}
	}
}
