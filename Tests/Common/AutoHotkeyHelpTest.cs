// @under-test: docs/Help.AutoHotkey.md, docs/Help.v4.md, AutoHotkey/x360ce.ahk
// @area: documents   @layer: integration
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace x360ce.Tests
{
	/// <summary>Every AutoHotkey script the help shows loads with the library x360ce writes, so no example names a call that does not exist.</summary>
	/// <remarks>AutoHotkey's <c>/validate</c> loads a script, reports what it cannot load and runs nothing. Inconclusive where AutoHotkey v2 is not installed.</remarks>
	[TestClass]
	public class AutoHotkeyHelpTest
	{
		[TestMethod, TestCategory("documents")]
		[Description("Every AutoHotkey script in the help loads with x360ce.ahk")]
		public void Every_script_in_the_help_loads()
		{
			var ahk = AutoHotkeyRun.FindAutoHotkey();
			if (ahk == null)
				Assert.Inconclusive("AutoHotkey v2 is not installed.");
			var root = Ui.RepoRoot.FullName;
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-autohotkey-help-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			try
			{
				File.Copy(Path.Combine(root, "AutoHotkey", "x360ce.ahk"), Path.Combine(folder, "x360ce.ahk"));
				var count = 0;
				foreach (var page in new[] { "Help.AutoHotkey.md", "Help.v4.md" })
				{
					var text = File.ReadAllText(Path.Combine(root, "docs", page)).Replace("\r", "");
					var blocks = Regex.Matches(text, "```autohotkey\n(.*?)```", RegexOptions.Singleline)
						.Cast<Match>()
						.Select(m => m.Groups[1].Value)
						.Where(x => x.Contains("#Include <x360ce>"))
						.ToArray();
					Assert.IsTrue(blocks.Length > 0, page + " shows no script.");
					for (var i = 0; i < blocks.Length; i++)
					{
						var script = Path.Combine(folder, page + "." + (i + 1) + ".ahk");
						File.WriteAllText(script, blocks[i].Replace("#Include <x360ce>", "#Include %A_ScriptDir%\\x360ce.ahk"), new UTF8Encoding(false));
						var info = new ProcessStartInfo(ahk, "/ErrorStdOut=UTF-8 /validate \"" + script + "\"")
						{
							UseShellExecute = false,
							RedirectStandardError = true,
							StandardErrorEncoding = Encoding.UTF8,
						};
						using (var process = Process.Start(info))
						{
							var error = process.StandardError.ReadToEnd();
							process.WaitForExit();
							Assert.AreEqual(0, process.ExitCode, "Script " + (i + 1) + " of " + page + " does not load: " + error);
						}
						count++;
					}
				}
				Console.WriteLine(count + " scripts in the help load.");
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}
	}
}
