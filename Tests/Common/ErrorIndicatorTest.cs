// @under-test: App.v4/MainForm.cs, App.v4/Program.cs
// @area: diagnostics   @layer: ui-interactive
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

namespace x360ce.Tests
{
	/// <summary>
	/// A failure is only useful to us if the user can send it, and the only route they have is
	/// the status bar: something fails, the error count rises, they click it, the report window
	/// opens, they press Send. This test walks that route.
	///
	/// It is worth a test because the route broke silently. The handler that records a failure
	/// was attached while the Options page was being built, so a user who never opened Options
	/// had no handler at all: the framework tried to show its own error window instead, failed
	/// to create it, and the original failure was never named or counted. Nothing about that
	/// looked wrong from the outside - the count simply stayed at zero.
	/// </summary>
	[TestClass]
	public class ErrorIndicatorTest
	{
		/// <summary>
		/// What the error count calls itself to assistive software. It says "no error reports"
		/// or "N error reports waiting to be sent", so this matches either.
		/// </summary>
		const string ReportWindowAction = "error report";

		Process _process;

		[TestCleanup]
		public void Cleanup()
		{
			Ui.CloseApp(_process);
			Ui.RemoveInjectedFaultReports(Ui.FindApp("App.v4"));
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("ui-interactive")]
		[Description("A failure raises the error count, and clicking it opens the report window")]
		public void Failure_raises_the_count_and_clicking_it_opens_the_report_window()
		{
			// Given: the application running, told to fail on purpose a few seconds in.
			_process = Start(faultAfterSeconds: 5);
			var window = Ui.WaitForMainWindow(_process, TimeSpan.FromSeconds(30));
			var indicator = Ui.WaitFor(() => FindIndicator(window), TimeSpan.FromSeconds(20),
				"the error count in the status bar");

			// When: the failure happens, the count must rise. It reads "Errors: {files} | {seen}".
			Ui.WaitFor(() => Counted(indicator) ? indicator : null, TimeSpan.FromSeconds(40),
				"the error count to rise after the injected failure. The failure was not recorded, "
				+ "so nothing appears in the status bar and the user has nothing to send.");

			// Then: clicking the count opens the window they send the report from.
			// Activated through the pattern it publishes, which is the same route a screen
			// reader or any automation tool takes. Needing to aim a mouse at it would mean the
			// control is not reachable by anything but a pointer.
			var invoke = (InvokePattern)indicator.GetCurrentPattern(InvokePattern.Pattern);
			// On its own thread: the window it opens is modal, so the call does not return until
			// the window is closed again.
			var opening = new Thread(() => { try { invoke.Invoke(); } catch (Exception) { } });
			opening.IsBackground = true;
			opening.Start();
			var report = Ui.WaitFor(() => FindReportWindow(), TimeSpan.FromSeconds(30),
				"the report window after activating the error count in the status bar");
			Assert.IsNotNull(report, "The report window did not open.");
		}

		/// <summary>
		/// Finds the report window among the windows this application owns. Asked of the window
		/// manager rather than of automation: the window is modal and the thread that owns it is
		/// occupied, so automation queries against it time out.
		/// </summary>
		string FindReportWindow()
		{
			foreach (var title in NativeMethods.VisibleWindowTitles(_process.Id))
			{
				if (title.IndexOf("Error Report", StringComparison.OrdinalIgnoreCase) >= 0)
					return title;
			}
			return null;
		}

		/// <summary>
		/// Finds the error count in the status bar the way assistive software would: by what it
		/// says it does, rather than by the text a sighted user reads.
		/// </summary>
		static AutomationElement FindIndicator(AutomationElement window)
		{
			foreach (AutomationElement e in window.FindAll(TreeScope.Descendants,
				Condition.TrueCondition))
			{
				if ((e.Current.Name ?? "").IndexOf(ReportWindowAction, StringComparison.OrdinalIgnoreCase) >= 0)
					return e;
			}
			return null;
		}

		/// <summary>True once the indicator says reports are waiting rather than that none are.</summary>
		static bool Counted(AutomationElement indicator)
		{
			return (indicator.Current.Name ?? "").IndexOf("waiting to be sent",
				StringComparison.OrdinalIgnoreCase) >= 0;
		}

		/// <summary>Starts version 4, asking it to fail on purpose after the given delay.</summary>
		static Process Start(int faultAfterSeconds)
		{
			var exe = Ui.FindApp("App.v4");
			var info = new ProcessStartInfo(exe)
			{
				WorkingDirectory = Path.GetDirectoryName(exe),
				UseShellExecute = false,
			};
			info.EnvironmentVariables["X360CE_THROW_AFTER"] = faultAfterSeconds.ToString();
			return Process.Start(info);
		}

		static class NativeMethods
		{
			delegate bool EnumProc(IntPtr window, IntPtr param);

			[DllImport("user32.dll")]
			static extern bool EnumWindows(EnumProc callback, IntPtr param);

			[DllImport("user32.dll", CharSet = CharSet.Unicode)]
			static extern int GetWindowText(IntPtr window, StringBuilder text, int count);

			[DllImport("user32.dll")]
			static extern bool IsWindowVisible(IntPtr window);

			[DllImport("user32.dll")]
			static extern int GetWindowThreadProcessId(IntPtr window, out int processId);

			/// <summary>Titles of the visible top-level windows belonging to one process.</summary>
			public static List<string> VisibleWindowTitles(int processId)
			{
				var titles = new List<string>();
				EnumWindows((window, param) =>
				{
					GetWindowThreadProcessId(window, out var owner);
					if (owner != processId || !IsWindowVisible(window))
						return true;
					var text = new StringBuilder(300);
					if (GetWindowText(window, text, text.Capacity) > 0)
						titles.Add(text.ToString());
					return true;
				}, IntPtr.Zero);
				return titles;
			}
		}

	}
}
