// @under-test: App.v4/MainForm.Tray.cs
// @area: diagnostics   @layer: integration-ui
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace x360ce.Tests
{
	/// <summary>
	/// A request to show the window that arrives while it is closing is dropped, not acted on.
	/// </summary>
	/// <remarks>
	/// A second launch, the tray icon and the command line all ask the running window to show
	/// itself. Acted on while the window was closing, the request rebuilt a window whose pages were
	/// already gone and the program ended on a fault instead of closing (two reports against
	/// 4.22.21.0: a disposed TabPage, a disposed GamesGridUserControl under RestoreFromTray).
	///
	/// Drives the built program, so it needs no other copy running.
	/// </remarks>
	[TestClass]
	public class RestoreWhileClosingTest
	{
		Process _process;

		[TestCleanup]
		public void Cleanup()
		{
			Ui.CloseApp(_process);
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("ui-interactive")]
		[Description("Show-me requests during close leave no fault report and the program still exits")]
		public void Restore_requests_during_close_are_dropped()
		{
			var exe = Ui.FindApp("App.v4");
			var started = DateTime.UtcNow;
			_process = Process.Start(new ProcessStartInfo(exe) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(exe) });
			Ui.WaitForMainWindow(_process, TimeSpan.FromSeconds(30));
			// Requests keep coming for the whole of the close, a few hundred of them.
			var asking = true;
			var asker = new Thread(() =>
			{
				while (asking)
				{
					Ui.PostRestoreRequest();
					Thread.Sleep(5);
				}
			});
			asker.IsBackground = true;
			asker.Start();
			_process.CloseMainWindow();
			var exited = _process.WaitForExit(20000);
			asking = false;
			asker.Join();
			Assert.IsTrue(exited, "The program did not close while being asked to show itself.");
			var reports = Ui.ErrorFolders(exe)
				.Where(Directory.Exists)
				.SelectMany(folder => Directory.GetFiles(folder, "*.htm"))
				.Where(file => File.GetLastWriteTimeUtc(file) >= started)
				.Where(file => File.ReadAllText(file).Contains("ObjectDisposedException"))
				.ToList();
			Assert.AreEqual(0, reports.Count, "Closing left a fault report: " + string.Join(", ", reports));
		}

		/// <remarks>
		/// Taking the window off the task bar by changing ShowInTaskbar makes Windows Forms build the window
		/// and every control on it again, at each minimise and at each restore. Four reports against
		/// 4.26.38.0 failed inside that rebuild: three in a list reading its rows afresh (738017, 737939,
		/// 737839) and one that ran out of window handles while making them (737772).
		/// </remarks>
		[TestMethod, TestCategory("diagnostics"), TestCategory("ui-interactive")]
		[Description("Minimising to the tray and back keeps the window, rather than building it and every control on it again")]
		public void Minimising_to_the_tray_keeps_the_window()
		{
			var exe = Ui.FindApp("App.v4");
			if (exe == null)
				Assert.Inconclusive("App.v4 is not built. Build the solution before running UI tests.");
			if (Process.GetProcessesByName("x360ce").Length > 0)
				Assert.Inconclusive("x360ce is running; this test starts a copy of its own.");
			// A new profile: Minimize to Tray is on by default, and nothing of the person's is touched.
			var profile = Path.Combine(Path.GetTempPath(), "x360ce-tray-" + Guid.NewGuid().ToString("N"));
			try
			{
				_process = Process.Start(new ProcessStartInfo(exe, "\"/Profile=" + profile + "\"") { WorkingDirectory = Path.GetDirectoryName(exe), UseShellExecute = false });
				Ui.WaitForMainWindow(_process, TimeSpan.FromSeconds(45));
				_process.Refresh();
				var window = _process.MainWindowHandle;
				Ui.Minimize(_process);
				Ui.WaitFor(() =>
				{
					_process.Refresh();
					return _process.MainWindowHandle == IntPtr.Zero ? "in the tray" : null;
				}, TimeSpan.FromSeconds(15), "the window to go to the tray");
				Ui.Restore(_process);
				_process.Refresh();
				Assert.AreEqual(window, _process.MainWindowHandle, "The window came back from the tray as a new window, built again.");
			}
			finally
			{
				Ui.CloseApp(_process);
				_process = null;
				if (Directory.Exists(profile))
					Directory.Delete(profile, true);
			}
		}
	}
}
