// @under-test: App.v4/Program.cs, App.v4/Common/AppLanguages.cs, App.v4/x360ce.App.v4.csproj
// @area: languages   @layer: integration
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace x360ce.Tests
{
	/// <summary>
	/// The language sync the build runs, <c>x360ce.exe -Languages=&lt;folder&gt;</c>, as the build runs it: it builds every page
	/// without showing a window, survives the pages that need the services a full start gives (the Cloud page among them),
	/// writes only into the folders it is given, and says what each language lacks as a build warning.
	/// </summary>
	[TestClass]
	public class LanguageSyncRunTest
	{
		[TestMethod, TestCategory("languages")]
		[Description("The language sync shows no window, finishes, and writes en.json and the warnings")]
		public void Sync_shows_no_window_and_writes_the_english()
		{
			var exe = Ui.FindApp("App.v4");
			Assert.IsNotNull(exe, "x360ce 4 is not built.");
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-languages-" + Guid.NewGuid().ToString("N"));
			var profile = Path.Combine(folder, "Profile");
			var languages = Path.Combine(folder, "Languages");
			Directory.CreateDirectory(languages);
			// One language lacking everything, so the run has something to say.
			File.WriteAllText(Path.Combine(languages, "de.json"), "{ \"Culture\": \"de\", \"Items\": [] }", new UTF8Encoding(false));
			var machine = MachineFiles();
			try
			{
				var info = new ProcessStartInfo(exe, "\"-Languages=" + languages + "\" \"-Profile=" + profile + "\"")
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					StandardOutputEncoding = Encoding.UTF8,
					CreateNoWindow = true,
				};
				var started = DateTime.Now;
				var windows = new List<string>();
				string output;
				int exitCode;
				using (var process = Process.Start(info))
				{
					var read = process.StandardOutput.ReadToEndAsync();
					// Every window the process shows while it runs, looked for as often as it can be.
					while (!process.WaitForExit(20))
						windows.AddRange(VisibleWindowsOf(process.Id));
					if (!process.HasExited)
						process.Kill();
					output = read.Result;
					exitCode = process.ExitCode;
					CrashLog.AssertExitedCleanly(process.Id, started, null, "x360ce -Languages");
				}
				Console.WriteLine(output);
				Assert.AreEqual(0, windows.Count, "The sync showed windows: " + string.Join(", ", windows.Distinct()));
				StringAssert.DoesNotMatch(output, new System.Text.RegularExpressions.Regex("XLANG000"), "The sync stopped.");
				Assert.AreEqual(1, exitCode, "A language lacking texts ends the sync with 1.");
				StringAssert.Contains(output, Path.Combine(languages, "de.json") + "(1,1): warning XLANG001:", "The warning is in the form a build shows.");
				Assert.IsTrue(File.Exists(Path.Combine(languages, "en.json")), "en.json was not written.");
				Assert.IsTrue(File.Exists(Path.Combine(languages, "de.todo.json")), "de.todo.json was not written.");
				var english = File.ReadAllText(Path.Combine(languages, "en.json"));
				// A page of each kind: the Game row, the controller page, the Cloud page, the Options pages, and a text built in code.
				foreach (var key in new[] { "MainControl.SaveAllButton.Text", "PadControl.", "CloudUserControl.", "OptionsUserControl.", "MainControl.LanguageButton.Text" })
					StringAssert.Contains(english, "\"Key\": \"" + key, "en.json lacks " + key);
				AssertMachineFilesKept(machine, "The sync");
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("languages")]
		[Description("The skill the build writes, x360ce.exe -Skill=<folder> with its own -Profile, leaves the settings in ProgramData as they were")]
		public void Skill_step_writes_nothing_into_the_machine_settings()
		{
			var exe = Ui.FindApp("App.v4");
			Assert.IsNotNull(exe, "x360ce 4 is not built.");
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-skill-" + Guid.NewGuid().ToString("N"));
			var skills = Path.Combine(folder, "skills");
			Directory.CreateDirectory(skills);
			var machine = MachineFiles();
			try
			{
				// The command the WriteSkill target of x360ce.App.v4.csproj runs.
				var info = new ProcessStartInfo(exe, "\"-Skill=" + skills + "\" \"-Profile=" + Path.Combine(folder, "Skill.Profile") + "\"")
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true,
				};
				using (var process = Process.Start(info))
				{
					var output = process.StandardOutput.ReadToEnd();
					Assert.IsTrue(process.WaitForExit(60000), "The skill step did not finish.");
					Assert.AreEqual(0, process.ExitCode, output);
				}
				Assert.IsTrue(Directory.EnumerateFiles(skills, "SKILL.md", SearchOption.AllDirectories).Any(), "The skill was not written.");
				AssertMachineFilesKept(machine, "The skill step");
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}

		/// <summary>The settings folder every copy shares in ProgramData: each file there and when it was last written.</summary>
		static Dictionary<string, DateTime> MachineFiles()
		{
			var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "X360CE");
			return Directory.Exists(root)
				? Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).ToDictionary(x => x, File.GetLastWriteTimeUtc, StringComparer.OrdinalIgnoreCase)
				: new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>Fails when a file in ProgramData's settings folder was written or added since <paramref name="before"/>.</summary>
		static void AssertMachineFilesKept(Dictionary<string, DateTime> before, string what)
		{
			var after = MachineFiles();
			var changed = after.Where(x => !before.ContainsKey(x.Key) || before[x.Key] != x.Value).Select(x => x.Key).ToList();
			Assert.AreEqual(0, changed.Count, what + " wrote into ProgramData: " + string.Join(", ", changed));
		}

		static List<string> VisibleWindowsOf(int processId)
		{
			var list = new List<string>();
			EnumWindows((window, parameter) =>
			{
				int owner;
				GetWindowThreadProcessId(window, out owner);
				if (owner == processId && IsWindowVisible(window))
				{
					var text = new StringBuilder(256);
					GetWindowText(window, text, text.Capacity);
					list.Add("\"" + text + "\"");
				}
				return true;
			}, IntPtr.Zero);
			return list;
		}

		delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

		[DllImport("user32.dll")]
		static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

		[DllImport("user32.dll")]
		static extern int GetWindowThreadProcessId(IntPtr window, out int processId);

		[DllImport("user32.dll")]
		static extern bool IsWindowVisible(IntPtr window);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
	}
}
