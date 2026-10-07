// @under-test: skills/x360ce/SKILL.md
// @area: mcp   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using x360ce.App;
using x360ce.Engine.Mcp;

namespace x360ce.Tests
{
	/// <summary>
	/// The skill is what an AI agent reads before it touches the program, so a tool or argument it names that the
	/// program does not have sends the agent to a refusal it cannot explain. The checks here are the ones the skill
	/// format and the program's own catalogue make cheap: the frontmatter agents parse, the size they load, and every
	/// tool and argument the text names.
	/// </summary>
	[TestClass]
	public class SkillTest
	{
		static readonly string SkillFile = Path.Combine(Ui.RepoRoot.FullName, "skills", AiSkill.Name, "SKILL.md");

		/// <summary>The program's own switches the skill shows, which are not arguments of a tool.</summary>
		static readonly string[] Switches = { "Ai", "Skill" };

		/// <summary>The classes the program catalogues its tools from, as App.v4's McpTools.Register sets them.</summary>
		static readonly Type[] ToolSources = { typeof(McpUiTools), typeof(App.Mcp.McpTools) };

		static IEnumerable<MethodInfo> ToolMethods()
		{
			return ToolSources.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
				.Where(m => m.GetCustomAttribute<McpToolAttribute>() != null);
		}

		static string Text()
		{
			return Normalise(File.ReadAllText(SkillFile));
		}

		/// <summary>The frontmatter between the two --- lines.</summary>
		static string Frontmatter(string text)
		{
			var match = Regex.Match(text, @"\A---\n(.*?)\n---\n", RegexOptions.Singleline);
			Assert.IsTrue(match.Success, "SKILL.md does not open with a --- frontmatter block, so agents cannot read its name or description.");
			return match.Groups[1].Value;
		}

		/// <summary>The text inside backticks and fenced code, where the skill names tools and arguments.</summary>
		static IEnumerable<string> Code(string text)
		{
			foreach (Match m in Regex.Matches(text, @"```[^\n]*\n(.*?)```", RegexOptions.Singleline))
				yield return m.Groups[1].Value;
			foreach (Match m in Regex.Matches(Regex.Replace(text, @"```.*?```", "", RegexOptions.Singleline), "`([^`\n]+)`"))
				yield return m.Groups[1].Value;
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("The frontmatter has the name agents look it up by, a description within the limit, and a version line the program stamps")]
		public void Frontmatter_names_the_skill_and_carries_a_version()
		{
			var front = Frontmatter(Text());
			Assert.IsTrue(Regex.IsMatch(front, @"(?m)^name: " + AiSkill.Name + "$"),
				"The name must be the folder's name, which agents look the skill up by.");
			var description = Regex.Match(front, @"(?ms)^description: >-\n(.*?)(?=^\S)").Groups[1].Value;
			var words = Regex.Replace(description, @"\s+", " ").Trim();
			Assert.IsTrue(words.Length > 0, "The description is what agents decide by whether to load the skill; it is empty.");
			Assert.IsTrue(words.Length <= 1024, "The description is " + words.Length + " characters; agents read at most 1024.");
			Assert.IsNotNull(AiSkill.VersionOf(front),
				"SKILL.md carries no version line; the program writes its own version there whenever it installs the skill.");
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("SKILL.md stays under 500 lines, the size agents load whole")]
		public void Skill_is_short_enough_to_load_whole()
		{
			var lines = Text().Split('\n').Length;
			Assert.IsTrue(lines < 500, "SKILL.md has " + lines + " lines; move detail into the references the program writes beside it.");
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Every tool and argument the skill names is one the program has")]
		public void Skill_names_only_real_tools_and_arguments()
		{
			var tools = new HashSet<string>(ToolMethods().Select(m => McpCatalog.ToolName(m.Name)));
			var arguments = new HashSet<string>(ToolMethods().SelectMany(m => m.GetParameters()).Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
			var named = 0;
			foreach (var code in Code(Text()))
			{
				foreach (Match m in Regex.Matches(code, @"(?<![\w.$-])([a-z]+(?:_[a-z]+)+)(?![\w.])"))
				{
					named++;
					Assert.IsTrue(tools.Contains(m.Groups[1].Value), "The skill names tool " + m.Groups[1].Value + ", which the program does not have. Tools: " + string.Join(", ", tools.OrderBy(x => x)));
				}
				foreach (Match m in Regex.Matches(code, @"(?<![\w-])-Ai=([a-z_]+)"))
					Assert.IsTrue(tools.Contains(m.Groups[1].Value), "The skill calls -Ai=" + m.Groups[1].Value + ", which the program does not have.");
				foreach (Match m in Regex.Matches(code, @"(?<![\w-])-([a-zA-Z]+)="))
				{
					if (Switches.Contains(m.Groups[1].Value))
						continue;
					Assert.IsTrue(arguments.Contains(m.Groups[1].Value), "The skill passes -" + m.Groups[1].Value + "=, which no tool takes.");
				}
			}
			Assert.IsTrue(named >= 10, "Only " + named + " tool names were found in the skill, so this test may no longer read it.");
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("The program carries the repository's SKILL.md, and installs it with its own version")]
		public void Program_carries_the_skill_and_stamps_its_version()
		{
			var embedded = WithoutVersion(AppHelper.ReadHelp(AiSkill.Resource));
			Assert.AreEqual(WithoutVersion(Text()), embedded, "The program's copy differs from skills/x360ce/SKILL.md; build again.");
			var version = new Version(4, 25, 31, 0);
			var stamped = AiSkill.Text(version);
			Assert.AreEqual(version, AiSkill.VersionOf(stamped));
			Assert.AreEqual(embedded, WithoutVersion(stamped), "Stamping changed more than the version.");
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("The program carries the repository's help and interface description as the skill's references")]
		public void Program_carries_the_references()
		{
			var pairs = new[]
			{
				new[] { AppHelper.HelpV4Resource, "docs/Help.v4.md" },
				new[] { AiSkill.HelpV3Resource, "docs/Help.v3.md" },
				new[] { AiSkill.References["references/ui-tree-v4.md"], "skills/x360ce/references/ui-tree-v4.md" },
				new[] { AiSkill.References["references/ui-tree-v4.json"], "skills/x360ce/references/ui-tree-v4.json" },
				new[] { AiSkill.References["references/ui-tree-v3.md"], "skills/x360ce/references/ui-tree-v3.md" },
				new[] { AiSkill.References["references/ui-tree-v3.json"], "skills/x360ce/references/ui-tree-v3.json" },
			};
			foreach (var pair in pairs)
			{
				// The build writes its own version and day into v4's description in the repository.
				var embedded = Unstamped(AppHelper.ReadHelp(pair[0]));
				var file = Unstamped(File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, pair[1])));
				Assert.IsTrue(embedded.Length > 0, "The program does not carry " + pair[1] + ".");
				Assert.AreEqual(file, embedded, "The program's copy of " + pair[1] + " differs; build again.");
			}
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("The repository's skills/x360ce is whole: the files the program installs, as it installs them")]
		public void Repository_skill_is_the_one_the_program_installs()
		{
			// Skill sites read the repository, not the program, so what they publish is only as whole as this folder.
			var folder = Path.Combine(Ui.RepoRoot.FullName, "skills", AiSkill.Name);
			var files = AiSkill.Files(AiSkill.ProgramVersion);
			var present = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
				.Select(x => x.Substring(folder.Length + 1).Replace('\\', '/')).OrderBy(x => x, StringComparer.Ordinal).ToArray();
			CollectionAssert.AreEqual(files.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray(), present,
				"skills/x360ce holds other files than the program installs; build App.v4, which writes the folder.");
			foreach (var file in files)
				Assert.AreEqual(Unstamped(WithoutVersion(file.Value)), Unstamped(WithoutVersion(File.ReadAllText(Path.Combine(folder, file.Key)))),
					file.Key + " differs from the program's copy; build App.v4, which writes the folder.");
		}

		/// <summary>Compared on content: line endings and a byte order mark depend on the checkout, not on the skill.</summary>
		static string Normalise(string text)
		{
			return text.Replace("\r\n", "\n").TrimStart('\uFEFF');
		}

		/// <summary>An interface description without its version and build day, which every build writes anew.</summary>
		static string Unstamped(string text)
		{
			return x360ce.Engine.UiTree.UiTreeExporter.Restamp(Normalise(text), "0", "0");
		}

		/// <summary>The text with its version line blank: the build writes the program's version into the repository's copy.</summary>
		static string WithoutVersion(string text)
		{
			return Regex.Replace(Normalise(text), @"(?m)^(\s*version:\s*).*$", "$1");
		}
	}
}
