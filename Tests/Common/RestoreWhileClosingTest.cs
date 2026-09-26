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
	}
}
