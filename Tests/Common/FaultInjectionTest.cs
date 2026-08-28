// @under-test: App.v4/MainForm.cs
// @area: diagnostics   @layer: ui-interactive
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;

namespace x360ce.Tests
{
	/// <summary>
	/// The application can be told to raise a fault on purpose, so a crash report can be
	/// reproduced instead of waited for.
	///
	/// This exists because of a report that described only the failure to report a failure:
	/// something threw on the interface thread, the framework tried to build its error window,
	/// building that failed as well, and the original fault was never named. Fifteen attempts
	/// to reproduce it by hand produced nothing, because the trigger cannot be arranged from
	/// outside the process.
	///
	/// These tests keep that ability alive. Without them the switches rot quietly: someone
	/// renames a variable or moves the handler, nothing fails, and the next time a report like
	/// that arrives there is again no way to reproduce it.
	/// </summary>
	[TestClass]
	public class FaultInjectionTest
	{
		Process _process;

		[TestCleanup]
		public void Cleanup()
		{
			Ui.CloseApp(_process);
			Ui.RemoveInjectedFaultReports(Ui.FindApp("App.v4"));
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("ui-interactive")]
		[Description("A fault can be raised while the application is closing")]
		public void Fault_can_be_injected_while_closing()
		{
			// The fault escapes FormClosing, so the close does not complete. That is the point:
			// it puts the application in the state the report came from, where the framework
			// cannot create a window any more.
			_process = Start(withFault: true);
			Ui.WaitForMainWindow(_process, TimeSpan.FromSeconds(30));
			_process.CloseMainWindow();
			var exited = _process.WaitForExit(8000);
			Assert.IsFalse(exited,
				"The application closed normally with X360CE_THROW_ON_CLOSE set. The switch that "
				+ "reproduces a fault during shutdown no longer works, so the report that needed "
				+ "it cannot be reproduced.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("ui-interactive")]
		[Description("Nothing is injected unless it is asked for")]
		public void No_fault_is_injected_by_default()
		{
			// The other half, and the more important one. A switch that fires without being set
			// would break every user's shutdown.
			_process = Start(withFault: false);
			Ui.WaitForMainWindow(_process, TimeSpan.FromSeconds(30));
			_process.CloseMainWindow();
			var exited = _process.WaitForExit(15000);
			Assert.IsTrue(exited,
				"The application did not close with no fault requested. Either shutdown is broken "
				+ "or the injected fault is raised when nothing asked for it.");
		}

		/// <summary>Starts version 4, optionally asking it to fault while closing.</summary>
		static Process Start(bool withFault)
		{
			var exe = Ui.FindApp("App.v4");
			var info = new ProcessStartInfo(exe)
			{
				WorkingDirectory = Path.GetDirectoryName(exe),
				UseShellExecute = false,
			};
			// Passed through the environment so a normal build carries no switch at all.
			info.EnvironmentVariables["X360CE_THROW_ON_CLOSE"] = withFault ? "1" : "";
			return Process.Start(info);
		}
	}
}
