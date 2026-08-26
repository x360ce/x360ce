// @under-test: App.v4/app.config, App.v4/x360ce.App.v4.csproj
// @area: diagnostics   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace x360ce.Tests
{
	/// <summary>
	/// The program ships as one file, so symbols are carried inside each binary rather than in
	/// files beside them. Two things have to hold for a crash report to name a line, and each
	/// fails silently on its own:
	///
	/// The symbols have to be built into the binary, and the runtime has to be told to read
	/// them. It ignores symbols in that form for a program built against a framework older than
	/// 4.7.2 unless the application asks, which is done in app.config. Reports named no line for
	/// years because of that one missing switch, and nothing anywhere reported a problem.
	/// </summary>
	[TestClass]
	public class EmbeddedSymbolsTest
	{
		[TestMethod, TestCategory("diagnostics"), TestCategory("smoke")]
		[Description("A library carried in memory is given the symbols that name its lines")]
		public void Libraries_loaded_from_memory_are_given_their_symbols()
		{
			var dll = Path.Combine(Ui.RepoRoot.FullName, "Engine", "bin", "Debug", "x360ce.Engine.dll");
			var pdb = Path.ChangeExtension(dll, ".pdb");
			if (!File.Exists(dll) || !File.Exists(pdb))
				Assert.Inconclusive("Build the engine in Debug before running this test.");

			// A library the program carries has no file on disk, so the runtime has nowhere to
			// look for symbols beside it and cannot use symbols built into it either. They have
			// to be handed over with the bytes.
			var given = LineOfFailure(Assembly.Load(File.ReadAllBytes(dll), File.ReadAllBytes(pdb)));
			var withheld = LineOfFailure(Assembly.Load(File.ReadAllBytes(dll)));

			Assert.IsTrue(given > 0,
				"A failure inside a library loaded with its symbols was still reported with no "
				+ "line number, so crash reports from the engine cannot be acted on.");
			Assert.AreEqual(0, withheld,
				"A library loaded without symbols reported a line anyway, so this test is not "
				+ "measuring what it claims and proves nothing.");
		}

		[TestMethod, TestCategory("diagnostics")]
		[Description("Every program asks the runtime for what it needs, from code rather than a file")]
		public void Programs_ask_for_what_they_need_in_code()
		{
			// These were once set in app.config, which put them in a file the build writes beside
			// the program. The programs ship as one file, so that file never reaches anyone who
			// downloads them, and every switch silently did nothing. Nothing failed: the tests ran
			// against a build folder, where the file is present, and passed while the shipped
			// program had no symbols and no accessibility at all.
			var required = new[]
			{
				"Switch.System.Diagnostics.IgnorePortablePDBsInStackTraces",
				"Switch.UseLegacyAccessibilityFeatures",
			};
			foreach (var program in new[] { @"App.v4\Program.cs", @"App.v3\Program.cs" })
			{
				var path = Path.Combine(Ui.RepoRoot.FullName, program);
				Assert.IsTrue(File.Exists(path), program + " is missing.");
				var code = File.ReadAllText(path);
				foreach (var name in required)
				{
					// Checked with Contains rather than StringAssert, which prints the whole file
					// when it fails and buries the one line that matters.
					Assert.IsTrue(code.Contains("AppContext.SetSwitch(\"" + name + "\", false)"),
						program + " does not ask for " + name + " at start-up, so it will do nothing "
						+ "for anyone running the shipped single file.");
				}
			}

			// One place only. A switch in both a file and the code is two answers to one question,
			// and the file is the one that will be believed while being absent where it matters.
			foreach (var config in new[] { @"App.v4\app.config", @"App.v3\app.config" })
			{
				var path = Path.Combine(Ui.RepoRoot.FullName, config);
				if (!File.Exists(path))
					continue;
				Assert.IsFalse(File.ReadAllText(path).Contains("AppContextSwitchOverrides"),
					config + " still sets switches. They belong in code, because this file does not "
					+ "travel with the program.");
			}
		}

		[TestMethod, TestCategory("diagnostics")]
		[Description("The program carries the libraries it needs")]
		public void Libraries_are_carried_inside_the_program()
		{
			var names = Assembly.ReflectionOnlyLoadFrom(Ui.FindApp("App.v4")).GetManifestResourceNames();
			Assert.IsTrue(names.Any(x => x.EndsWith("x360ce.Engine.dll", StringComparison.Ordinal)),
				"The engine is not carried inside the program, so it would have to ship beside it.");
		}

		/// <summary>
		/// Provokes a failure inside the given copy of the engine and returns the line it is
		/// reported at, or 0 when no line is reported. An interval of zero is rejected by the
		/// timer, which makes it a dependable place to fail inside the library itself.
		/// </summary>
		static int LineOfFailure(Assembly engine)
		{
			var type = engine.GetType("JocysCom.ClassLibrary.HiResTimer", throwOnError: true);
			var timer = Activator.CreateInstance(type, new object[] { 100, "symbol probe" });
			try
			{
				type.GetProperty("Interval").SetValue(timer, 0, null);
			}
			catch (TargetInvocationException ex)
			{
				var trace = ex.InnerException.StackTrace ?? "";
				var match = System.Text.RegularExpressions.Regex.Match(trace, @"HiResTimer\.cs:line (\d+)");
				return match.Success ? int.Parse(match.Groups[1].Value) : 0;
			}
			finally
			{
				(timer as IDisposable)?.Dispose();
			}
			Assert.Fail("The timer accepted an interval of zero, so this test has no failure to measure.");
			return 0;
		}
	}
}
