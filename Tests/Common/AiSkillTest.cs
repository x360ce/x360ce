// @under-test: App.v4/Common/AiSkill.cs, App.v4/Issues/AiSkillIssue.cs
// @area: mcp   @layer: unit
using JocysCom.ClassLibrary.Controls.IssuesControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using x360ce.App;
using x360ce.App.Issues;

namespace x360ce.Tests
{
	/// <summary>
	/// Installing the skill writes into folders other programs own, so what it replaces, what it stamps and how it
	/// tells its own copy from an older or a newer one are checked here, in a temporary folder.
	/// </summary>
	[TestClass]
	public class AiSkillTest
	{
		string _folder;

		[TestInitialize]
		public void Init()
		{
			_folder = Path.Combine(Path.GetTempPath(), "x360ce-AiSkillTest-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_folder);
		}

		[TestCleanup]
		public void Cleanup()
		{
			if (Directory.Exists(_folder))
				Directory.Delete(_folder, true);
		}

		static readonly Version Program = new Version(4, 25, 31, 0);

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("A folder without the skill, with this version, an older one, a newer one, and one without a version")]
		public void Check_tells_each_state()
		{
			Version installed;
			Assert.AreEqual(AiSkill.State.NotInstalled, AiSkill.Check(_folder, Program, out installed));
			Assert.IsNull(installed);
			AiSkill.Install(_folder, Program);
			Assert.AreEqual(AiSkill.State.UpToDate, AiSkill.Check(_folder, Program, out installed));
			Assert.AreEqual(Program, installed);
			Assert.AreEqual(AiSkill.State.Older, AiSkill.Check(_folder, new Version(4, 26, 0, 0), out installed));
			Assert.AreEqual(AiSkill.State.Newer, AiSkill.Check(_folder, new Version(4, 25, 30, 0), out installed));
			File.WriteAllText(Path.Combine(_folder, AiSkill.Name, "SKILL.md"), "---\nname: x360ce\n---\n");
			Assert.AreEqual(AiSkill.State.Older, AiSkill.Check(_folder, Program, out installed),
				"A copy that carries no version is older: only an older program wrote one like that.");
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Installing writes the skill with the help and interface the program carries, and replaces only the x360ce folder")]
		public void Install_replaces_only_its_own_folder()
		{
			var other = Directory.CreateDirectory(Path.Combine(_folder, "other-skill")).FullName;
			File.WriteAllText(Path.Combine(other, "SKILL.md"), "another skill");
			var own = Directory.CreateDirectory(Path.Combine(_folder, AiSkill.Name)).FullName;
			File.WriteAllText(Path.Combine(own, "left-over.txt"), "from an older install");
			AiSkill.Install(_folder, Program);
			Assert.AreEqual("another skill", File.ReadAllText(Path.Combine(other, "SKILL.md")), "Another skill was touched.");
			Assert.IsFalse(File.Exists(Path.Combine(own, "left-over.txt")), "A file of the older install was kept.");
			Assert.AreEqual(Program, AiSkill.VersionOf(File.ReadAllText(Path.Combine(own, "SKILL.md"))));
			foreach (var reference in AiSkill.References)
			{
				var embedded = AppHelper.ReadHelp(reference.Value);
				Assert.IsTrue(embedded.Length > 0, "The program does not carry " + reference.Key + ".");
				var written = File.ReadAllText(Path.Combine(own, reference.Key.Replace('/', Path.DirectorySeparatorChar)));
				Assert.AreEqual(Unstamped(embedded), Unstamped(written), reference.Key);
			}
			// Version 4's own description names the build that installed it; version 3's keeps its own.
			StringAssert.Contains(File.ReadAllText(Path.Combine(own, "references", "ui-tree-v4.md")), "Version " + Program + ", built ");
			StringAssert.Contains(File.ReadAllText(Path.Combine(own, "references", "ui-tree-v4.json")), "\"Version\":\"" + Program + "\"");
			Assert.AreEqual(AppHelper.ReadHelp(AiSkill.References["references/ui-tree-v3.md"]), File.ReadAllText(Path.Combine(own, "references", "ui-tree-v3.md")));
			var bytes = File.ReadAllBytes(Path.Combine(own, "SKILL.md"));
			Assert.IsFalse(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
				"SKILL.md starts with a byte order mark, which hides the --- that opens its frontmatter from some agents.");
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Installing again leaves a file that is already right as it was, time and all")]
		public void Install_leaves_a_file_already_right_untouched()
		{
			// The build writes the skill into the repository, where these files are also its sources; a file written
			// again with the same bytes would look changed and start another build.
			AiSkill.Install(_folder, Program);
			var help = Path.Combine(_folder, AiSkill.Name, "references", "help-v4.md");
			var then = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			File.SetLastWriteTimeUtc(help, then);
			AiSkill.Install(_folder, Program);
			Assert.AreEqual(then, File.GetLastWriteTimeUtc(help), "A file with the right content was written again.");
		}

		static string Unstamped(string text)
		{
			return x360ce.Engine.UiTree.UiTreeExporter.Restamp(text, "0", "0");
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("The ZIP for the Claude app holds the x360ce folder with its files")]
		public void Zip_holds_the_skill_folder()
		{
			var zip = Path.Combine(_folder, "x360ce-skill.zip");
			AiSkill.SaveZip(zip, Program);
			using (var storer = ZipStorer.Open(zip, FileAccess.Read))
			{
				var names = storer.ReadCentralDir().Select(e => e.FilenameInZip).OrderBy(x => x, StringComparer.Ordinal).ToArray();
				var expected = new[] { "x360ce/SKILL.md" }.Concat(AiSkill.References.Keys.Select(x => "x360ce/" + x)).OrderBy(x => x, StringComparer.Ordinal).ToArray();
				CollectionAssert.AreEqual(expected, names);
			}
			Assert.AreEqual(1, Directory.GetFileSystemEntries(_folder).Length, "The folder the ZIP was built in was left behind.");
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("The Issues tab offers an update for an older copy only, and says nothing when none is installed")]
		public void Issue_offers_an_update_only_for_an_older_copy()
		{
			Version installed;
			if (AiSkill.Check(AiSkill.AgentsFolder, AiSkill.ProgramVersion, out installed) != AiSkill.State.NotInstalled)
				Assert.Inconclusive("The skill is installed for other agents on this computer, and this test would read it.");
			var saved = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
			try
			{
				Environment.SetEnvironmentVariable("CLAUDE_CONFIG_DIR", _folder);
				var issue = new AiSkillIssue();
				issue.CheckTask();
				Assert.AreEqual(IssueSeverity.None, issue.Severity, "Nothing is installed, so nothing should be said.");
				AiSkill.Install(AiSkill.ClaudeFolder, new Version(1, 0, 0, 0));
				issue.CheckTask();
				Assert.AreEqual(IssueSeverity.Low, issue.Severity, "An older copy should be offered an update.");
				StringAssert.Contains(issue.Description, "Claude Code");
				AiSkill.Install(AiSkill.ClaudeFolder, new Version(AiSkill.ProgramVersion.Major + 1, 0, 0, 0));
				issue.CheckTask();
				Assert.AreEqual(IssueSeverity.None, issue.Severity, "A copy from a newer program is left alone.");
			}
			finally
			{
				Environment.SetEnvironmentVariable("CLAUDE_CONFIG_DIR", saved);
			}
		}

		/// <summary>Runs /Skill with a value and hands back the exit code, what was printed and what was said wrong.</summary>
		int Skill(string value, out string printed, out string failed)
		{
			var output = new StringWriter();
			var error = new StringWriter();
			var code = AiSkill.RunSwitch(value, _folder, output, error);
			printed = output.ToString();
			failed = error.ToString();
			return code;
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("/Skill prints the skill or a reference by its short name or its path, and installs only into a folder that exists, taken from where the command was typed")]
		public void Skill_switch_prints_or_installs()
		{
			string printed, failed;
			Assert.AreEqual(0, Skill("", out printed, out failed));
			Assert.AreEqual(AiSkill.Text(AiSkill.ProgramVersion), printed, "Alone, the switch prints the skill as the program would install it.");
			var help = AppHelper.ReadHelp(AppHelper.HelpV4Resource);
			// PowerShell splits an unquoted -Skill=references/help.md at the dot and passes references/help.
			foreach (var name in new[] { "help", "HELP.md", "help-v4", "references\\help-v4.md", "references/help-v4" })
			{
				Assert.AreEqual(0, Skill(name, out printed, out failed), name);
				Assert.AreEqual(help, printed, name);
			}
			Assert.AreEqual(0, Skill("ui-tree", out printed, out failed));
			Assert.AreEqual(AiSkill.Files(AiSkill.ProgramVersion)["references/ui-tree-v4.md"], printed);
			Assert.AreEqual(0, Skill("references/ui-tree-v3.json", out printed, out failed));
			Assert.AreEqual(AppHelper.ReadHelp(AiSkill.References["references/ui-tree-v3.json"]), printed);
			Assert.AreEqual(0, Skill("help-v3", out printed, out failed));
			Assert.AreEqual(AppHelper.ReadHelp(AiSkill.HelpV3Resource), printed);
			// A name that is neither a file nor a folder that exists makes no folder, and says what the switch takes.
			foreach (var name in new[] { "references/manual.md", "skills", "<folder>" })
			{
				Assert.AreEqual(1, Skill(name, out printed, out failed), name);
				StringAssert.Contains(failed, "-Skill=help", name);
			}
			Assert.AreEqual(0, Directory.GetFileSystemEntries(_folder).Length, "A folder was made for a name that was not one.");
			// The program moves to its own folder as it starts, so a relative folder is taken from where the command was typed.
			Directory.CreateDirectory(Path.Combine(_folder, "skills"));
			Assert.AreEqual(0, Skill("skills", out printed, out failed), failed);
			Assert.AreEqual(AiSkill.State.UpToDate, AiSkill.Check(Path.Combine(_folder, "skills"), AiSkill.ProgramVersion, out _));
			StringAssert.Contains(printed, Path.Combine(_folder, "skills", AiSkill.Name));
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("/?, -h and --help print what the program is and its switches, from the help, and point an AI agent at /Skill")]
		public void Help_switch_prints_the_switches_from_the_help()
		{
			var usage = AppHelper.Usage();
			StringAssert.StartsWith(usage, "X360CE " + AiSkill.ProgramVersion);
			StringAssert.Contains(usage, "## Command-line switches");
			StringAssert.Contains(usage, "/Skill");
			StringAssert.Contains(usage, "--help");
			Assert.AreEqual(1, Regex.Matches(usage, "(?m)^## ").Count, "Only the switches section is printed, not the sections after it.");
		}
	}
}
