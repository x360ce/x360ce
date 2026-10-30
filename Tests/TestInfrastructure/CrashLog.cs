// @under-test: Tests/TestInfrastructure/TestRun.cs, Tests/TestInfrastructure/Ui.cs
// @area: diagnostics   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace x360ce.Tests
{
	/// <summary>
	/// The crashes and hangs Windows logged for the programs a test run starts, so a crash nobody saw still fails the run.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A program can end with an error and show nothing: x360ce crashed on every close for months (issue #1650) and only
	/// the Windows Application log knew. Every run reads that log for its own time when it ends (<see cref="TestRun"/>),
	/// writes what it found to <c>Tests\TestResults\WindowsCrashes.txt</c>, and fails unless each crash was raised on
	/// purpose and said so with <see cref="Expect"/>.
	/// </para>
	/// <para>
	/// The log is not enough on its own: a program started by a test runner can inherit an error mode that keeps Windows
	/// from reporting its crash at all. So each test that closes a program it started also checks its exit code
	/// (<see cref="AssertExitedCleanly"/>).
	/// </para>
	/// </remarks>
	public static class CrashLog
	{
		/// <summary>One crash or hang the Application log holds.</summary>
		public sealed class Crash
		{
			public DateTime Time;
			/// <summary>Application Error (1000), Application Hang (1002), or .NET Runtime: an internal error (1023), FailFast (1025) or an unhandled exception (1026).</summary>
			public string Source;
			public string Program;
			/// <summary>The process ID, or 0 when the event does not give it.</summary>
			public int ProcessId;
			/// <summary>The exception code, such as 0xc0020001, or empty.</summary>
			public string Code;
			public string Module;
			public string Text;

			public override string ToString()
			{
				return Time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " " + Source + ": " + Program
					+ (ProcessId == 0 ? "" : " (process " + ProcessId + ")")
					+ (string.IsNullOrEmpty(Code) ? "" : " " + Code)
					+ (string.IsNullOrEmpty(Module) ? "" : " in " + Module);
			}
		}

		/// <summary>Processes a test made crash on purpose, with why.</summary>
		static readonly ConcurrentDictionary<int, string> Expected = new ConcurrentDictionary<int, string>();

		/// <summary>Says a process is made to crash on purpose, so its crash does not fail the run.</summary>
		public static void Expect(int processId, string reason)
		{
			Expected[processId] = reason;
		}

		/// <summary>True when a test said the crash was raised on purpose.</summary>
		public static bool IsExpected(Crash crash)
		{
			return crash.ProcessId != 0 && Expected.ContainsKey(crash.ProcessId);
		}

		/// <summary>Programs whose crashes count: x360ce in any form, the test host and runner, and anything run from the repository.</summary>
		public static bool IsWatched(string program, string path)
		{
			var name = (program ?? "").ToLowerInvariant();
			if (name.StartsWith("x360ce") || name.StartsWith("testhost") || name.StartsWith("vstest") || name.StartsWith("autohotkey"))
				return true;
			return !string.IsNullOrEmpty(path) && path.StartsWith(Ui.RepoRoot.FullName, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>Every crash and hang of a watched program logged since <paramref name="since"/>.</summary>
		/// <param name="processId">Only this process, or 0 for every watched one.</param>
		public static List<Crash> Read(DateTime since, int processId = 0)
		{
			var time = since.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
			var query = "*[System[(Provider[@Name='Application Error'] and EventID=1000)"
				+ " or (Provider[@Name='Application Hang'] and EventID=1002)"
				+ " or (Provider[@Name='.NET Runtime'] and (EventID=1023 or EventID=1025 or EventID=1026))]"
				+ " and System[TimeCreated[@SystemTime>='" + time + "']]]";
			var crashes = new List<Crash>();
			using (var reader = new EventLogReader(new EventLogQuery("Application", PathType.LogName, query)))
			{
				EventRecord record;
				while ((record = reader.ReadEvent()) != null)
				{
					using (record)
					{
						var crash = ToCrash(record);
						if (crash == null)
							continue;
						if (processId != 0 ? crash.ProcessId == processId : IsWatched(crash.Program, PathOf(record)))
							crashes.Add(crash);
					}
				}
			}
			return crashes;
		}

		static string Property(EventRecord record, int index)
		{
			return record.Properties.Count > index ? Convert.ToString(record.Properties[index].Value, CultureInfo.InvariantCulture) : null;
		}

		/// <summary>The program's full path, which only Application Error gives.</summary>
		static string PathOf(EventRecord record)
		{
			return record.ProviderName == "Application Error" ? Property(record, 10) : null;
		}

		static int Hex(string value)
		{
			int id;
			if (string.IsNullOrEmpty(value))
				return 0;
			value = value.Trim();
			if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				return int.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out id) ? id : 0;
			return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id) ? id : 0;
		}

		/// <summary>The crash an event describes, read from its fields rather than its text, which Windows writes in the PC's language.</summary>
		static Crash ToCrash(EventRecord record)
		{
			var crash = new Crash { Time = record.TimeCreated ?? DateTime.MinValue, Source = record.ProviderName };
			string text;
			try
			{
				text = record.FormatDescription();
			}
			catch (EventLogException)
			{
				text = null;
			}
			crash.Text = text ?? string.Join(Environment.NewLine, record.Properties.Select(x => Convert.ToString(x.Value, CultureInfo.InvariantCulture)));
			switch (record.ProviderName)
			{
				case "Application Error":
					crash.Program = Property(record, 0);
					crash.Module = Property(record, 3);
					var code = Property(record, 6);
					crash.Code = string.IsNullOrEmpty(code) ? "" : (code.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? code : "0x" + code);
					crash.ProcessId = Hex(Property(record, 8));
					return crash;
				case "Application Hang":
					crash.Program = Property(record, 0);
					crash.Code = "hang";
					crash.ProcessId = Hex(Property(record, 2));
					return crash;
				case ".NET Runtime":
					// One text field: "Application: name.exe", then the exception and its stack. Written by the process
					// that fails, so the record names it.
					var first = (Property(record, 0) ?? "").Split('\n').FirstOrDefault() ?? "";
					crash.Program = first.Contains(":") ? first.Substring(first.IndexOf(':') + 1).Trim() : first.Trim();
					crash.Code = record.Id == 1025 ? ".NET FailFast" : record.Id == 1023 ? ".NET internal error" : ".NET unhandled exception";
					crash.ProcessId = record.ProcessId ?? 0;
					return crash;
			}
			return null;
		}

		/// <summary>
		/// The exit codes a process ends with when it crashes: an NTSTATUS error such as 0xC0000005, the CLR's unhandled
		/// exception 0xE0434352, or a failure HRESULT such as FailFast's 0x80131623. A program's own error codes are small
		/// and positive, and -1 is a kill.
		/// </summary>
		public static bool IsCrashCode(int exitCode)
		{
			return exitCode < 0 && exitCode != -1;
		}

		/// <summary>Fails when a process a test started ended with a crash code, or Windows logged a crash or hang of it.</summary>
		/// <param name="exitCode">Its exit code, or null when it was killed or is still running.</param>
		/// <param name="what">What the process was, for the message.</param>
		public static void AssertExitedCleanly(int processId, DateTime started, int? exitCode, string what)
		{
			if (Expected.ContainsKey(processId))
				return;
			var crashes = Read(started, processId);
			if (exitCode.HasValue && IsCrashCode(exitCode.Value))
				Assert.Fail(what + " ended with a crash, exit code 0x" + exitCode.Value.ToString("X8", CultureInfo.InvariantCulture)
					+ ". Windows may have logged nothing, so this is the only sign of it. " + string.Join(" ", crashes.Select(x => x.ToString())));
			if (crashes.Count > 0)
				Assert.Fail(what + " crashed or hung, as the Windows Application log says: " + string.Join(" ", crashes.Select(x => x.ToString())));
		}

		/// <summary>Writes the crashes found to a file, each with its full text, and the dump Windows kept of it if any.</summary>
		public static void Export(string path, DateTime since, IList<Crash> crashes)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			var text = new StringBuilder();
			text.AppendLine("Crashes and hangs Windows logged for x360ce, the test host and programs run from the repository");
			text.AppendLine("since " + since.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + ": " + crashes.Count);
			var dumps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrashDumps");
			foreach (var crash in crashes)
			{
				text.AppendLine();
				text.AppendLine(crash + (IsExpected(crash) ? " (on purpose: " + Expected[crash.ProcessId] + ")" : ""));
				if (crash.ProcessId != 0 && Directory.Exists(dumps))
					foreach (var dump in Directory.GetFiles(dumps, "*." + crash.ProcessId + ".dmp"))
						text.AppendLine("Dump: " + dump);
				text.AppendLine(crash.Text);
			}
			File.WriteAllText(path, text.ToString());
		}
	}
}
