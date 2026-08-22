// @under-test: Engine/JocysCom/Runtime/LogHelper.File.cs, Engine/JocysCom/Runtime/LogHelper.cs
// @area: diagnostics   @layer: unit
using JocysCom.ClassLibrary.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;

namespace x360ce.Tests
{
	/// <summary>
	/// A crash report is only useful if it names the line that threw. These tests hold that
	/// property in place, because it depends on build settings which are easy to change by
	/// accident: symbols must be produced in Release and the .pdb must ship beside the .exe.
	/// Reports arriving at support without a line number cost far more to act on.
	/// </summary>
	[TestClass]
	public class CrashReportTest
	{

		private string _logs;
		private string _originalOverride;
		private bool _originalLogToFile;

		[TestInitialize]
		public void Setup()
		{
			// Redirect reports into the test's own folder. Without this the run would write
			// into the real application data folder and mix test noise into genuine reports.
			_logs = Path.Combine(Path.GetTempPath(), "x360ce.Tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_logs);
			_originalOverride = LogHelper.Current.OverrideLogFolder;
			_originalLogToFile = LogHelper.Current.LogToFile;
			LogHelper.Current.OverrideLogFolder = _logs;
			LogHelper.Current.LogToFile = true;
		}

		[TestCleanup]
		public void Cleanup()
		{
			LogHelper.Current.OverrideLogFolder = _originalOverride;
			LogHelper.Current.LogToFile = _originalLogToFile;
			try { Directory.Delete(_logs, true); } catch (IOException) { }
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("smoke")]
		[Description("A crash writes a report naming the source file and the line that threw")]
		public void Crash_report_names_the_source_file_and_line()
		{
			// Given: the application's own reporting path, writing into a private folder.
			var thrownAtLine = 0;
			Exception caught = null;

			// When: a failure happens and is reported the way the application reports one.
			try
			{
				ThrowAtKnownLine(out thrownAtLine);
			}
			catch (Exception ex)
			{
				caught = ex;
				LogHelper.Current.WriteException(ex);
			}

			// Then: a report file exists.
			var report = WaitForReport();
			Assert.IsNotNull(report, "No crash report was written to " + _logs);

			var text = File.ReadAllText(report.FullName);

			// ... and it names this source file, so a reader knows where to look.
			StringAssert.Contains(text, "CrashReportTest.cs",
				"The report does not name the source file. Symbols are missing from the build.");

			// ... and it carries the line number, which is what makes a report actionable.
			// The report is HTML, so tags are stripped first: matching raw markup picks up
			// colour codes such as #808080 and reads them as line numbers.
			var plain = Regex.Replace(text, "<[^>]+>", " ");
			plain = Regex.Replace(plain, @"\s+", " ");
			var lines = Regex.Matches(plain, @"CrashReportTest\.cs\s*,?\s*:?\s*(?:line\s*)?(\d{1,5})")
				.Cast<Match>().Select(m => int.Parse(m.Groups[1].Value)).ToArray();
			Assert.IsTrue(lines.Length > 0,
				"The report names the file but carries no line number. The .pdb is not beside the assembly. " +
				"Report: " + report.FullName);
			Assert.IsTrue(lines.Any(l => Math.Abs(l - thrownAtLine) <= 2),
				$"The report points at line(s) {string.Join(", ", lines)} but the throw is at line {thrownAtLine}. " +
				"Symbols are stale relative to the binary.");

			// ... and it names the failure itself.
			StringAssert.Contains(text, "Deliberate test failure");
			Assert.IsNotNull(caught);
		}

		[TestMethod, TestCategory("diagnostics")]
		[Description("The report body that would be emailed contains the stack trace")]
		public void Crash_report_body_is_complete_enough_to_send()
		{
			// Given: a failure captured the way the application captures one.
			Exception caught = null;
			try { ThrowAtKnownLine(out _); }
			catch (Exception ex) { caught = ex; }

			// When: the report body is built - the same content the error report window sends.
			// Nothing is mailed here on purpose: a test must never post to real support.
			var body = LogHelper.Current.ExceptionInfo(caught, null, true);

			// Then: it carries what a maintainer needs to act on the report.
			Assert.IsFalse(string.IsNullOrWhiteSpace(body), "The report body is empty.");
			StringAssert.Contains(body, "InvalidOperationException");
			StringAssert.Contains(body, "Deliberate test failure");
			StringAssert.Contains(body, "CrashReportTest.cs",
				"The emailed body would not tell the maintainer which file failed.");
		}

		[TestMethod, TestCategory("diagnostics")]
		[Description("Reporting is off by default so nothing is written until the application enables it")]
		public void Reporting_is_off_until_enabled()
		{
			LogHelper.Current.LogToFile = false;
			try { throw new InvalidOperationException("Deliberate test failure"); }
			catch (Exception ex) { LogHelper.Current.WriteException(ex); }
			Assert.AreEqual(0, ReportFiles().Length,
				"A report was written while file logging was disabled.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("smoke")]
		[Description("Release builds ship the symbols a crash report needs to name a line")]
		public void Release_builds_ship_symbols()
		{
			// The tests above prove the reporting mechanism, running against this assembly.
			// They cannot prove the shipped build carries symbols - that depends on the
			// product's own Release settings and on the .pdb being deployed beside the binary.
			// Without it every field report arrives with a stack trace and no line numbers,
			// which is the difference between a fixable report and a guess.
			var missing = new List<string>();
			var checkedAny = false;

			foreach (var binary in new[]
			{
				Tuple.Create("App.v4", "x360ce.exe"),
				Tuple.Create("App.v3", "x360ce.exe"),
				Tuple.Create("Engine", "x360ce.Engine.dll"),
			})
			{
				var bin = new DirectoryInfo(Path.Combine(Ui.RepoRoot.FullName, binary.Item1, "bin"));
				if (!bin.Exists)
					continue;
				foreach (var dir in bin.GetDirectories("Release*"))
				{
					var file = new FileInfo(Path.Combine(dir.FullName, binary.Item2));
					if (!file.Exists)
						continue;
					checkedAny = true;
					var pdb = Path.ChangeExtension(file.FullName, ".pdb");
					if (!File.Exists(pdb))
						missing.Add(file.FullName.Substring(Ui.RepoRoot.FullName.Length + 1));
				}
			}

			if (!checkedAny)
				Assert.Inconclusive("No Release build found. Build Release before running this test.");
			Assert.AreEqual(0, missing.Count,
				"Release binaries without symbols beside them: " + string.Join(", ", missing));
		}

		// Separate method so the throw site has a stable, isolated line number.
		private static void ThrowAtKnownLine(out int line)
		{
			line = new StackFrame(0, true).GetFileLineNumber() + 1;
			throw new InvalidOperationException("Deliberate test failure");
		}

		private FileInfo[] ReportFiles()
		{
			var di = new DirectoryInfo(_logs);
			return di.Exists ? di.GetFiles("FCE_*") : new FileInfo[0];
		}

		private FileInfo WaitForReport()
		{
			// Reports are grouped before writing, so the file appears shortly after the call.
			var deadline = DateTime.UtcNow.AddSeconds(10);
			while (DateTime.UtcNow < deadline)
			{
				var files = ReportFiles();
				if (files.Length > 0)
					return files.OrderByDescending(f => f.LastWriteTimeUtc).First();
				Thread.Sleep(100);
			}
			return null;
		}

	}
}
