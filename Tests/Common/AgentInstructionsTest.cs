// @under-test: .ai/repository-analysis.instructions.md, AGENTS.md
// @area: build   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace x360ce.Tests
{
	/// <summary>
	/// The files under `.ai/` are the first thing an agent session reads, and `AGENTS.md` is a
	/// verbatim copy of them, so a claim that has fallen behind the code is not a cosmetic
	/// documentation defect: it is the starting instruction for the next change. Two properties
	/// are cheap to check and are the two that have drifted before - the C++ toolset the prose
	/// names, and whether the generated copy still matches the source it is generated from.
	/// </summary>
	[TestClass]
	public class AgentInstructionsTest
	{

		static readonly XNamespace MsBuild = "http://schemas.microsoft.com/developer/msbuild/2003";

		/// <summary>Instruction files an agent is pointed at, source first.</summary>
		static readonly string[] InstructionFiles =
		{
			".ai/developer.instructions.md",
			".ai/repository-analysis.instructions.md",
			"AGENTS.md",
		};

		/// <summary>A platform toolset as MSBuild spells it: v141, v142, v140_xp.</summary>
		static readonly Regex ToolsetName = new Regex(@"\bv\d{3}(_xp)?\b", RegexOptions.CultureInvariant);

		[TestMethod, TestCategory("build"), TestCategory("smoke")]
		[Description("Every C++ toolset the agent instructions name is one a project actually asks for")]
		public void Agent_instructions_name_only_toolsets_the_projects_declare()
		{
			var declared = DeclaredToolsets();
			Assert.IsTrue(declared.Count > 0,
				"No PlatformToolset was found in Native/*/*.vcxproj, so this test proves nothing.");
			foreach (var relative in InstructionFiles)
			{
				var path = RepoFile(relative);
				Assert.IsTrue(File.Exists(path), relative + " not found; the test no longer covers it.");
				foreach (Match match in ToolsetName.Matches(File.ReadAllText(path)))
				{
					Assert.IsTrue(declared.Contains(match.Value),
						relative + " tells the reader about toolset " + match.Value +
						", which no C++ project declares. Declared: " +
						string.Join(", ", declared.OrderBy(x => x, StringComparer.Ordinal).ToArray()) +
						". Instructions that name a toolset nobody builds with send the next " +
						"session to install the wrong tools.");
				}
			}
		}

		[TestMethod, TestCategory("build"), TestCategory("smoke")]
		[Description("AGENTS.md still matches the .ai files it is generated from")]
		public void Generated_agent_file_matches_its_sources()
		{
			var generated = Normalise(File.ReadAllText(RepoFile("AGENTS.md")));
			foreach (var relative in InstructionFiles.Where(x => x.StartsWith(".ai/", StringComparison.Ordinal)))
			{
				var source = Normalise(File.ReadAllText(RepoFile(relative))).Trim();
				Assert.IsTrue(source.Length > 0, relative + " is empty.");
				Assert.IsTrue(generated.Contains(source),
					"AGENTS.md no longer carries " + relative + " verbatim. It is that file " +
					"concatenated, so the two have drifted: edit the .ai file and write AGENTS.md " +
					"again rather than editing one of the copies.");
			}
		}

		/// <summary>Every distinct PlatformToolset value across the native projects.</summary>
		static HashSet<string> DeclaredToolsets()
		{
			var native = new DirectoryInfo(Path.Combine(Ui.RepoRoot.FullName, "Native"));
			var found = new HashSet<string>(StringComparer.Ordinal);
			if (!native.Exists)
				return found;
			foreach (var project in native.GetFiles("*.vcxproj", SearchOption.AllDirectories))
				foreach (var element in XDocument.Load(project.FullName).Descendants(MsBuild + "PlatformToolset"))
					found.Add(element.Value.Trim());
			return found;
		}

		static string RepoFile(string relative)
		{
			return Path.Combine(Ui.RepoRoot.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
		}

		/// <summary>Compare on content, not on the line endings a copy happened to be written with.</summary>
		static string Normalise(string text)
		{
			return text.Replace("\r\n", "\n").Replace("\r", "\n").TrimStart('\uFEFF');
		}

	}
}
