// @under-test: App.v4/MainForm.cs, App.v3/MainForm.cs
// @area: startup   @layer: ui-wpf
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Windows.Automation;

namespace x360ce.Tests
{
	/// <summary>
	/// Launch smoke for both applications. These catch the failures that make an application
	/// useless before a user can do anything: it does not start, it starts without a window,
	/// or it starts showing the wrong build. Everything deeper belongs in a unit test.
	/// </summary>
	/// <remarks>
	/// The applications are driven as processes rather than referenced, because App.v3 and
	/// App.v4 both produce x360ce.exe in the namespace x360ce.App and cannot coexist in one
	/// assembly. Running the executable is also what a user does.
	/// </remarks>
	[TestClass]
	public class AppUiTest
	{

		private Process _process;

		[TestCleanup]
		public void Cleanup() => Ui.CloseApp(_process);

		[TestMethod, TestCategory("startup"), TestCategory("smoke"), TestCategory("ui-interactive")]
		[Description("Version 4 starts and shows its own version in the window title")]
		public void V4_starts_and_titles_the_window_with_its_version()
			=> App_starts_and_titles_the_window_with_its_version("App.v4");

		[TestMethod, TestCategory("startup"), TestCategory("smoke"), TestCategory("ui-interactive")]
		[Description("Version 3 starts and shows its own version in the window title")]
		public void V3_starts_and_titles_the_window_with_its_version()
			=> App_starts_and_titles_the_window_with_its_version("App.v3");

		/// <summary>
		/// The title is the cheapest end-to-end signal there is: it proves the process started,
		/// created its window, resolved its own assembly version, and rendered. An empty title
		/// was a real defect on this branch, caused by display scaling changing mid-startup.
		/// </summary>
		private void App_starts_and_titles_the_window_with_its_version(string appFolder)
		{
			// Given: a built application.
			var exe = Ui.FindApp(appFolder);
			if (exe == null)
				Assert.Inconclusive($"{appFolder} is not built. Build the solution before running UI tests.");
			var expected = FileVersionInfo.GetVersionInfo(exe).FileVersion;

			// When: it is started.
			_process = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = System.IO.Path.GetDirectoryName(exe) });
			var window = Ui.WaitForMainWindow(_process, TimeSpan.FromSeconds(45));

			// Then: the window is titled with the version that was just built.
			var title = window.Current.Name;
			StringAssert.Contains(title, expected,
				$"Window title '{title}' does not contain the built version '{expected}'.");
		}

		[TestMethod, TestCategory("startup"), TestCategory("ui-interactive")]
		[Description("Version 4 exposes its tab strip, so the interface built rather than half-loading")]
		public void V4_shows_its_main_tabs()
		{
			var exe = Ui.FindApp("App.v4");
			if (exe == null)
				Assert.Inconclusive("App.v4 is not built. Build the solution before running UI tests.");

			_process = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = System.IO.Path.GetDirectoryName(exe) });
			var window = Ui.WaitForMainWindow(_process, TimeSpan.FromSeconds(45));

			// A tab control proves the interface finished composing, not merely that a window exists.
			var tabs = Ui.WaitFor(
				() => window.FindFirst(TreeScope.Descendants,
					new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Tab)),
				TimeSpan.FromSeconds(30), "the main tab strip");

			var pages = tabs.FindAll(TreeScope.Children,
				new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));
			Assert.IsTrue(pages.Count >= 4, $"Expected at least 4 tabs, found {pages.Count}.");
		}

	}
}
