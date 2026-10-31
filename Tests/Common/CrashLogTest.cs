// @under-test: Tests/TestInfrastructure/CrashLog.cs
// @area: diagnostics   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace x360ce.Tests
{
	/// <summary>
	/// The run's crash check finds a crash Windows logged, lets one raised on purpose through, and tells a crash exit
	/// code from an ordinary one, so a crash nobody looked at still fails the run.
	/// </summary>
	[TestClass]
	public class CrashLogTest
	{
		/// <summary>Runs Windows PowerShell with a command and gives its process, ended.</summary>
		static Process RunPowerShell(string command)
		{
			var info = new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -Command \"" + command + "\"")
			{
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			var process = Process.Start(info);
			Assert.IsTrue(process.WaitForExit(60000), "PowerShell did not end.");
			return process;
		}

		[TestMethod, TestCategory("diagnostics")]
		[Description("A process made to crash on purpose is found in the Windows Application log, and counts as expected")]
		public void A_crash_on_purpose_is_found_and_expected()
		{
			var started = DateTime.Now.AddSeconds(-1);
			using (var process = RunPowerShell("[Environment]::FailFast('x360ce tests: a crash on purpose')"))
			{
				CrashLog.Expect(process.Id, "CrashLogTest proves the crash check reads the Windows log");
				Assert.IsTrue(CrashLog.IsCrashCode(process.ExitCode), "FailFast did not end PowerShell with a crash code: " + process.ExitCode);
				// Windows writes the event a moment after the process has gone.
				var crashes = Enumerable.Empty<CrashLog.Crash>().ToList();
				var deadline = DateTime.UtcNow.AddSeconds(20);
				while (crashes.Count == 0 && DateTime.UtcNow < deadline)
				{
					Thread.Sleep(500);
					crashes = CrashLog.Read(started, process.Id);
				}
				Assert.IsTrue(crashes.Count > 0, "The crash is not in the Windows Application log, or its process was not read from the event.");
				Assert.IsTrue(crashes.All(CrashLog.IsExpected), "A crash said to be on purpose is not counted as expected.");
				StringAssert.Contains(crashes[0].Program.ToLowerInvariant(), "powershell", "The event names another program.");
				// Not one of the programs the run watches, so the run's own check leaves it alone either way.
				Assert.IsFalse(CrashLog.IsWatched(crashes[0].Program, null), "PowerShell is counted as a program of the run.");
			}
		}

		[TestMethod, TestCategory("diagnostics")]
		[Description("A process that ends with a crash code fails the close check, even when Windows logged nothing")]
		public void A_crash_exit_code_fails_the_close_check()
		{
			var started = DateTime.Now.AddSeconds(-1);
			// An access violation's code, given as the exit code: nothing crashed, so nothing is logged, as for a crash
			// Windows was kept from reporting.
			using (var process = RunPowerShell("exit -1073741819"))
			{
				Assert.AreEqual(unchecked((int)0xC0000005), process.ExitCode, "PowerShell did not end with the code asked for.");
				var failure = Assert.ThrowsExactly<AssertFailedException>(() => CrashLog.AssertExitedCleanly(process.Id, started, process.ExitCode, "PowerShell"));
				StringAssert.Contains(failure.Message, "0xC0000005");
			}
		}

		[TestMethod, TestCategory("diagnostics")]
		[Description("Crash codes are told from an ordinary exit, an error code a program returns, and a kill")]
		public void Crash_codes_are_told_from_ordinary_exits()
		{
			Assert.IsFalse(CrashLog.IsCrashCode(0), "A clean exit.");
			Assert.IsFalse(CrashLog.IsCrashCode(1), "An error code a program returns.");
			Assert.IsFalse(CrashLog.IsCrashCode(-1), "A kill.");
			Assert.IsTrue(CrashLog.IsCrashCode(unchecked((int)0xC0000005)), "An access violation.");
			Assert.IsTrue(CrashLog.IsCrashCode(unchecked((int)0xC000041D)), "A crash in a Windows callback, the #1650 exit crash.");
			Assert.IsTrue(CrashLog.IsCrashCode(unchecked((int)0xE0434352)), "An unhandled .NET exception.");
			Assert.IsTrue(CrashLog.IsCrashCode(unchecked((int)0x80131623)), "A .NET FailFast.");
		}
	}
}
