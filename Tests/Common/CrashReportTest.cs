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
			// Give this test its own folder inside the run folder that TestRun already
			// redirected to, so reports from one test cannot be seen by another.
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
		[Description("A report identifies the fault, never the person or the computer that hit it")]
		public void Report_does_not_carry_who_or_where()
		{
			// Given: a failure captured the way the application captures one.
			Exception caught = null;
			try { ThrowAtKnownLine(out _); }
			catch (Exception ex) { caught = ex; }

			// When: the body that would be sent to support is built.
			var body = LogHelper.Current.ExceptionInfo(caught, null, true);

			// Then: it does not name the person or their machine. A report travels by email to
			// people the sender has never met, so what identifies them must not ride along. None
			// of it helps to find a fault.
			var machine = Environment.MachineName;
			var user = Environment.UserName;
			Assert.IsFalse(body.IndexOf(machine, StringComparison.OrdinalIgnoreCase) >= 0,
				"The report names the computer it came from (" + machine + ").");
			Assert.IsFalse(body.IndexOf(user, StringComparison.OrdinalIgnoreCase) >= 0,
				"The report names the person who hit the fault (" + user + ").");

			// ... nor where on their disk the application lives. That folder often sits under
			// their profile, so it names them a second time, and it says nothing about the fault.
			var folder = Path.GetDirectoryName(typeof(LogHelper).Assembly.Location);
			Assert.IsFalse(body.IndexOf(folder, StringComparison.OrdinalIgnoreCase) >= 0,
				"The report carries the folder the application runs from (" + folder + ").");

			// ... while still saying what is needed to act on it.
			StringAssert.Contains(body, "CrashReportTest.cs",
				"Removing what identifies the sender also removed what identifies the fault.");
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

		[TestMethod, TestCategory("diagnostics")]
		[Description("Shipped binaries carry what a report needs to name a line")]
		public void Release_builds_carry_their_symbols()
		{
			// The tests above prove the reporting itself, running against this assembly. They
			// cannot prove the shipped build can name a line, which depends on how it was built.
			//
			// The programs ship as one file, so their symbols are built into them. The engine is
			// different: it is carried inside those programs and loaded from bytes, and symbols
			// built into it cannot be reached that way, so it needs them as a file to be carried
			// alongside. Either way the answer is the same - without it every field report arrives
			// with a stack trace and no line numbers, which is the difference between a fixable
			// report and a guess.
			var missing = new List<string>();
			var checkedAny = false;

			foreach (var binary in new[]
			{
				Tuple.Create("App.v4", "x360ce.exe", true),
				Tuple.Create("App.v3", "x360ce.exe", true),
				Tuple.Create("Engine", "x360ce.Engine.dll", false),
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
					var name = file.FullName.Substring(Ui.RepoRoot.FullName.Length + 1);
					if (binary.Item3)
					{
						if (!HasEmbeddedSymbols(file.FullName))
							missing.Add(name + " (nothing built into it)");
					}
					else if (!File.Exists(Path.ChangeExtension(file.FullName, ".pdb")))
					{
						missing.Add(name + " (no symbol file to carry)");
					}
				}
			}

			if (!checkedAny)
				Assert.Inconclusive("No Release build found. Build Release before running this test.");
			Assert.AreEqual(0, missing.Count,
				"Shipped binaries that cannot name a line in a crash report: " + string.Join(", ", missing));
		}

		/// <summary>
		/// True when the file carries its own symbols. Read from the debug directory the linker
		/// writes into every binary, where entry type 17 means the symbols are stored inside the
		/// file rather than in a .pdb beside it.
		/// </summary>
		private static bool HasEmbeddedSymbols(string path)
		{
			using (var stream = File.OpenRead(path))
			using (var reader = new BinaryReader(stream))
			{
				stream.Position = 0x3C;
				stream.Position = reader.ReadUInt32();          // start of the PE header
				if (reader.ReadUInt32() != 0x00004550)          // "PE  "
					return false;
				reader.ReadUInt16();                            // machine
				var sections = reader.ReadUInt16();
				stream.Position += 12;
				var optionalSize = reader.ReadUInt16();
				reader.ReadUInt16();                            // characteristics
				var optionalStart = stream.Position;
				var magic = reader.ReadUInt16();
				// The debug directory is the seventh data directory, and the directories start at
				// 96 bytes into the optional header for 32-bit files and 112 for 64-bit ones.
				stream.Position = optionalStart + (magic == 0x20B ? 112 : 96) + (6 * 8);
				var debugRva = reader.ReadUInt32();
				var debugSize = reader.ReadUInt32();
				if (debugRva == 0 || debugSize == 0)
					return false;

				// Addresses in the header are relative to where the file would be loaded in memory,
				// so the section table is what turns one back into a position in the file.
				stream.Position = optionalStart + optionalSize;
				long debugOffset = -1;
				for (var i = 0; i < sections; i++)
				{
					stream.Position += 8;                       // section name
					reader.ReadUInt32();                        // virtual size
					var virtualAddress = reader.ReadUInt32();
					var rawSize = reader.ReadUInt32();
					var rawPointer = reader.ReadUInt32();
					stream.Position += 16;                      // the rest of the section header
					if (debugRva >= virtualAddress && debugRva < virtualAddress + rawSize)
						debugOffset = rawPointer + (debugRva - virtualAddress);
				}
				if (debugOffset < 0)
					return false;

				for (var i = 0; i < debugSize / 28; i++)
				{
					stream.Position = debugOffset + (i * 28) + 12;
					if (reader.ReadUInt32() == 17)              // symbols stored inside this file
						return true;
				}
				return false;
			}
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
