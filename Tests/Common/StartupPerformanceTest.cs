// @under-test: App.v4/Program.cs, App.v4/MainForm.cs, App.v4/Common/DInput/DInputHelper.Step1.UpdateDevices.cs
// @area: engine   @layer: ui-interactive
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace x360ce.Tests
{
	/// <summary>
	/// What starting the program costs, read from the program's own trace rather than guessed at.
	/// </summary>
	/// <remarks>
	/// The device list used to be read on the device thread, and it read every device on the
	/// machine: two to five seconds during which no controller was polled, paid again on every
	/// arrival and removal. It is now read on a worker, started while the controller panels are
	/// still being built, and asks only for the interfaces the DirectInput devices name and for
	/// those devices' own chains. The device thread takes the result in, which costs milliseconds.
	///
	/// The bounds below are about two to three times what this was measured at, so a busy machine
	/// passes and a return to the old behaviour does not. They are held against the trace the
	/// program writes when X360CE_ENGINE_LOG names a file, because a number read off the window
	/// one glance at a time can be made to say whatever the reader hoped.
	/// </remarks>
	[TestClass]
	public class StartupPerformanceTest
	{
		/// <summary>Measured 1850 to 2250 ms on a laptop in Debug; a collapse back to a whole-machine read is seconds more.</summary>
		const int DeviceListTakenInMs = 8000;

		/// <summary>
		/// Measured 10 to 30 ms. The read is started before the panels are built and the panels take
		/// longer, so the list is already waiting when the device thread asks for it. If this grows the
		/// read is no longer being prefetched and the device thread is waiting for the machine again.
		/// </summary>
		const int PrefetchGapMs = 700;

		/// <summary>Measured 38 ms of interfaces and devices; DirectInput's own enumeration is a floor this cannot move.</summary>
		const int ReadOwnWorkMs = 400;

		/// <summary>Measured 410 ms, nearly all of it DirectInput. It used to be 2600 to 5000.</summary>
		const int ReadTotalMs = 1500;

		/// <summary>Measured 78 to 109 ms on the first second and 1 to 7 ms after. It used to be whole seconds.</summary>
		const int TakeInMs = 400;

		/// <summary>Working set around 114 MB and private bytes around 82 MB once settled.</summary>
		const long PrivateBytesCeilingMb = 300;

		/// <summary>Settled means settled: what it holds at ten seconds is what it holds at thirty.</summary>
		const long GrowthCeilingMb = 50;

		[TestMethod, TestCategory("performance"), TestCategory("ui-interactive")]
		[Description("Starting reads the device list once, quickly, on a worker, and settles without growing")]
		public void Program_starts_reads_devices_and_settles_within_bounds()
		{
			var exe = Ui.FindApp("App.v4");
			if (exe == null)
				Assert.Inconclusive("App.v4 is not built.");
			if (Process.GetProcessesByName("x360ce").Length > 0)
				Assert.Inconclusive("Another x360ce instance is already running, so this test could not drive its own. "
					+ "Close it and run again.");
			// Named after this process and this run, so two test hosts never write into one trace, and
			// set for the program alone rather than for this one: a variable left behind in the test
			// host makes every later test write a trace nobody reads.
			var log = Path.Combine(Path.GetTempPath(),
				"x360ce.startup-performance." + Process.GetCurrentProcess().Id + "." + Guid.NewGuid().ToString("N") + ".log");
			var info = new ProcessStartInfo(exe)
			{
				WorkingDirectory = Path.GetDirectoryName(exe),
				UseShellExecute = false,
			};
			info.EnvironmentVariables["X360CE_ENGINE_LOG"] = log;
			var process = Process.Start(info);
			var startedAt = process.StartTime;
			try
			{
				Ui.WaitForMainWindow(process, TimeSpan.FromSeconds(45));
				var takenLine = Ui.WaitFor(() => FirstMilestone(log, "device list taken in"),
					TimeSpan.FromSeconds(30), "the device list to be taken in");
				// Ten and thirty seconds after the process began, so the two samples are a fixed
				// distance apart whatever the machine did in between.
				var atTen = SampleMemory(process, startedAt.AddSeconds(10), "10s");
				var atThirty = SampleMemory(process, startedAt.AddSeconds(30), "30s");
				// Thirty seconds of ordinary running after the list arrived, so the engine lines cover
				// a settled program and not only its start-up.
				WaitUntil(startedAt.AddMilliseconds(MillisecondsOf(takenLine)).AddSeconds(30));
				// Closed before the trace is read, so the last second's line is written and flushed.
				// CloseApp restores the window from the tray and asks it to close, which is the clean
				// route: killing it strands the controllers it plugged in, and skips the runtime's own
				// tidying up on the way out.
				Ui.CloseApp(process);
				process = null;

				var milestones = ReadLines(log).Where(x => x.StartsWith("startup,", StringComparison.Ordinal)).ToArray();
				foreach (var line in milestones)
					Console.WriteLine(line);
				var engineLines = ReadLines(log)
					.Where(x => !x.StartsWith("startup,", StringComparison.Ordinal) && x.Contains("read="))
					.ToArray();

				var takenIn = MillisecondsOf(takenLine);
				Assert.IsTrue(takenIn <= DeviceListTakenInMs,
					"The device list was taken in " + takenIn + " ms after the process started, over " + DeviceListTakenInMs
					+ " ms. Reading every device on the machine is what used to cost that.");

				var formBuilt = FirstMilestone(log, "UpdateForm2: end");
				Assert.IsNotNull(formBuilt, "The trace has no 'UpdateForm2: end' milestone, so the prefetch cannot be measured.");
				var gap = takenIn - MillisecondsOf(formBuilt);
				Assert.IsTrue(gap <= PrefetchGapMs,
					"The device list arrived " + gap + " ms after the panels were built (list at " + takenIn + " ms, panels at "
					+ MillisecondsOf(formBuilt) + " ms), over " + PrefetchGapMs + " ms. The read is meant to run on a worker "
					+ "while the panels are built, so it is finished before the device thread asks for it.");

				var read = engineLines.Select(Parse).FirstOrDefault(x => x.Read > 0);
				Assert.IsNotNull(read, "No engine line reported a device read, so the read cannot be measured. "
					+ engineLines.Length + " engine lines were written.");
				Console.WriteLine("read line: " + read.Text);
				var ownWork = read.Interfaces + read.Devices;
				Assert.IsTrue(ownWork <= ReadOwnWorkMs,
					"The read spent " + ownWork + " ms on interfaces and devices (int " + read.Interfaces + ", dev "
					+ read.Devices + ") out of " + read.Read + " ms, over " + ReadOwnWorkMs + " ms. That is the part the "
					+ "program chooses; asking for the whole machine is what used to make it seconds.");
				Assert.IsTrue(read.Read <= ReadTotalMs,
					"The read took " + read.Read + " ms (" + read.Phases + "), over " + ReadTotalMs + " ms.");

				var worst = engineLines.Select(Parse).OrderByDescending(x => x.TakeIn).FirstOrDefault();
				Assert.IsNotNull(worst, "No engine lines were written, so the take-in cannot be measured.");
				Assert.IsTrue(worst.TakeIn <= TakeInMs,
					"Taking the device list in cost the device thread " + worst.TakeIn + " ms in one second, over " + TakeInMs
					+ " ms, across " + engineLines.Length + " seconds. The worst line was: " + worst.Text);

				var thirtyMb = atThirty[0] / 1024 / 1024;
				var growthMb = (atThirty[0] - atTen[0]) / 1024 / 1024;
				Assert.IsTrue(thirtyMb <= PrivateBytesCeilingMb,
					"Private bytes were " + thirtyMb + " MB thirty seconds in, over " + PrivateBytesCeilingMb + " MB.");
				Assert.IsTrue(growthMb <= GrowthCeilingMb,
					"Private bytes grew " + growthMb + " MB between ten and thirty seconds (" + atTen[0] / 1024 / 1024
					+ " MB to " + thirtyMb + " MB), over " + GrowthCeilingMb + " MB. A settled program holds what it holds.");
			}
			finally
			{
				if (process != null)
					Ui.CloseApp(process);
				try { File.Delete(log); }
				catch (IOException) { }
			}
		}

		/// <summary>Private bytes and working set at a moment, printed as they are read.</summary>
		static long[] SampleMemory(Process process, DateTime when, string label)
		{
			WaitUntil(when);
			// The counters are cached on the object, so an unrefreshed sample is whatever was true
			// when it was first asked and both samples then read the same number.
			process.Refresh();
			var sample = new[] { process.PrivateMemorySize64, process.WorkingSet64 };
			Console.WriteLine("memory at {0}: private {1} MB, working set {2} MB",
				label, sample[0] / 1024 / 1024, sample[1] / 1024 / 1024);
			return sample;
		}

		/// <summary>Waits for a moment to arrive. Polling lives in Ui, so a test body never sleeps.</summary>
		static void WaitUntil(DateTime when)
		{
			var remaining = when - DateTime.Now;
			if (remaining <= TimeSpan.Zero)
				return;
			Ui.WaitFor(() => DateTime.Now >= when ? "reached" : null,
				remaining + TimeSpan.FromSeconds(5), "the clock to reach " + when.ToString("HH:mm:ss"));
		}

		/// <summary>The first line marking a milestone, or null while the program has not reached it.</summary>
		static string FirstMilestone(string path, string milestone)
		{
			return ReadLines(path).FirstOrDefault(x =>
				x.StartsWith("startup,", StringComparison.Ordinal)
				&& x.EndsWith("," + milestone, StringComparison.Ordinal));
		}

		/// <summary>Milliseconds since the process started, the second field of every line.</summary>
		static long MillisecondsOf(string line)
		{
			return long.Parse(line.Split(',')[1]);
		}

		/// <summary>The trace as it stands, while the program is still appending to it.</summary>
		static IEnumerable<string> ReadLines(string path)
		{
			if (!File.Exists(path))
				return new string[0];
			try
			{
				using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (var reader = new StreamReader(stream))
					return reader.ReadToEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			}
			catch (IOException)
			{
				return new string[0];
			}
		}

		/// <summary>One second of engine log: what the worker read cost, and what taking it in cost.</summary>
		class EngineLine
		{
			public string Text;
			public long Read;
			public string Phases;
			public long Interfaces;
			public long Devices;
			public long TakeIn;
		}

		static EngineLine Parse(string line)
		{
			var read = Regex.Match(line, @"read=(\d+)\(([^)]*)\)");
			var phases = read.Success ? read.Groups[2].Value : "";
			return new EngineLine
			{
				Text = line,
				Read = read.Success ? long.Parse(read.Groups[1].Value) : 0,
				Phases = phases,
				Interfaces = Phase(phases, "int"),
				Devices = Phase(phases, "dev"),
				TakeIn = Field(line, "UpdateDiDevices"),
			};
		}

		/// <summary>One phase of the worker read, from the bracket after read=.</summary>
		static long Phase(string phases, string name)
		{
			var m = Regex.Match(phases, @"\b" + name + @":(\d+)");
			return m.Success ? long.Parse(m.Groups[1].Value) : 0;
		}

		/// <summary>One named step of the update, in milliseconds spent during that second.</summary>
		static long Field(string line, string name)
		{
			var m = Regex.Match(line, @"," + name + @"=(\d+)");
			return m.Success ? long.Parse(m.Groups[1].Value) : 0;
		}
	}
}
