// @under-test: App.v4/MainForm.cs, App.v4/Program.cs, App.v4/Issues/LeftoverVirtualPadsIssue.cs, App.v4/Issues/HidHideIssue.cs
// @area: startup   @layer: ui-interactive
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Automation;
using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Mcp;
using JocysCom.ClassLibrary.Runtime;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>How the program starts: each step once and in order, from the event that makes it due, and nothing polling once it has started.</summary>
	/// <remarks>
	/// A 50 ms timer drove the steps and went on ticking for as long as the program ran, minimised included, and the
	/// controllers and the settings waited for the first round of issue checks to find nothing critical: with a critical
	/// issue standing, no controller was read and no box change was kept. Each test starts a copy of the program in a folder
	/// of its own with a profile of its own, so no settings of the person's are read or written, and no tab is switched on,
	/// so no virtual controller is made.
	/// </remarks>
	[TestClass]
	public class StartupStepsTest
	{
		/// <summary>The steps of the start-up as the trace marks them, in the order they run, each once.</summary>
		static readonly string[] Steps = { "LoadPages: end", "UpdateForm2: start", "UpdateForm2: end", "UpdateForm1: end", "device thread start" };

		/// <summary>The drawing timer's rate in front; behind and minimised it ticks five times a second.</summary>
		const int DrawingInFront = 10;
		const int DrawingBehind = 5;

		/// <summary>The Issues list's own monitor, in the shared library, ticks twice a second for the program's life.</summary>
		const int IssuesMonitor = 2;

		/// <summary>Room for a tick that falls either side of a second's line.</summary>
		const double TimerSlack = 1;

		/// <summary>Work posted to the window now and then: the issue round's list and its tab, five to eight calls every five seconds while shown.</summary>
		/// <remarks>Measured on the builds before and after the tabs were made ready behind the words. A window woken by a poll is woken many times this.</remarks>
		const double Calls = 2;

		#region A copy of the program

		/// <summary>A copy of the program in a folder of its own, with a profile of its own and its start-up trace.</summary>
		sealed class Copy : IDisposable
		{
			public readonly string Folder;
			public readonly string Profile;
			public readonly string Exe;
			public readonly string Log;
			public Process Process;
			/// <summary>The main window, as it first appeared; its handle stays its own while it is hidden in the tray.</summary>
			public IntPtr Window;

			public Copy(string name)
			{
				var exe = Ui.FindApp("App.v4");
				if (exe == null)
					Assert.Inconclusive("App.v4 is not built.");
				if (Process.GetProcessesByName("x360ce").Length > 0)
					Assert.Inconclusive("x360ce is running; this test starts a copy of its own.");
				Folder = Path.Combine(Path.GetTempPath(), "x360ce.Tests", name + "-" + Guid.NewGuid().ToString("N"));
				Profile = Path.Combine(Folder, "profile");
				Directory.CreateDirectory(Path.Combine(Profile, "Settings"));
				Exe = Path.Combine(Folder, "x360ce.exe");
				File.Copy(exe, Exe);
				Log = Path.Combine(Folder, "startup.log");
			}

			public void Write<T>(string name, params T[] items)
			{
				var data = new XSettingsData<T> { Items = new SortableBindingList<T>(items.ToList()) };
				File.WriteAllBytes(Path.Combine(Profile, "Settings", "x360ce." + name), Serializer.SerializeToXmlBytes(data, Encoding.UTF8, true));
			}

			public T[] Read<T>(string name)
			{
				var data = Serializer.DeserializeFromXmlFile<XSettingsData<T>>(Path.Combine(Profile, "Settings", "x360ce." + name));
				return data.Items.ToArray();
			}

			public void Start(IDictionary<string, string> environment = null)
			{
				Launch("", environment);
				Window = new IntPtr(Ui.WaitForMainWindow(Process, TimeSpan.FromSeconds(60)).Current.NativeWindowHandle);
			}

			/// <summary>Starts the program and waits for nothing.</summary>
			public void Launch(string arguments = "", IDictionary<string, string> environment = null)
			{
				var info = new ProcessStartInfo(Exe, "\"/Profile=" + Profile + "\" " + arguments) { WorkingDirectory = Folder, UseShellExecute = false };
				info.EnvironmentVariables["X360CE_ENGINE_LOG"] = Log;
				if (environment != null)
					foreach (var pair in environment)
						info.EnvironmentVariables[pair.Key] = pair.Value;
				Process = Process.Start(info);
			}

			/// <summary>The trace as it stands, while the program is still appending to it.</summary>
			public string[] Lines()
			{
				if (!File.Exists(Log))
					return new string[0];
				using (var stream = new FileStream(Log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				using (var reader = new StreamReader(stream))
					return reader.ReadToEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
			}

			/// <summary>The start-up marks, without the milliseconds: "issues checked", "device thread start" and so on.</summary>
			public string[] Marks()
			{
				return Lines().Where(x => x.StartsWith("startup,", StringComparison.Ordinal))
					.Select(x => x.Substring(x.IndexOf(',', "startup,".Length) + 1)).ToArray();
			}

			/// <summary>The engine's lines, one for each second it ran.</summary>
			public int EngineSeconds()
			{
				return Lines().Count(x => !x.StartsWith("startup,", StringComparison.Ordinal) && x.Contains("read="));
			}

			public void WaitForMark(string mark, int seconds)
			{
				Ui.WaitFor(() => Marks().Contains(mark) ? mark : null, TimeSpan.FromSeconds(seconds), "the start-up mark '" + mark + "'");
			}

			/// <summary>Milliseconds since the program started, the clock its trace is written in.</summary>
			public long Now()
			{
				return (long)(DateTime.Now - Process.StartTime).TotalMilliseconds;
			}

			/// <summary>How many times a second the window's thread was woken between two moments, by its timers or by work posted to it.</summary>
			/// <remarks>From the program's "interface,{ms},timers={n},calls={n}" lines, the counts since it began.</remarks>
			/// <param name="what">"timers" or "calls".</param>
			public double WakesPerSecond(long from, long to, string what)
			{
				var lines = Lines().Where(x => x.StartsWith("interface,", StringComparison.Ordinal))
					.Select(x => x.Split(','))
					.Select(x => new { Ms = long.Parse(x[1]), Count = long.Parse(x.Single(y => y.StartsWith(what + "=", StringComparison.Ordinal)).Substring(what.Length + 1)) })
					.ToArray();
				var first = lines.LastOrDefault(x => x.Ms <= from);
				var last = lines.LastOrDefault(x => x.Ms <= to);
				Assert.IsTrue(first != null && last != null && last.Ms - first.Ms >= 3000,
					"The trace has too few lines of what woke the window between " + from + " and " + to + " ms to count it.");
				return (last.Count - first.Count) * 1000.0 / (last.Ms - first.Ms);
			}

			public void Dispose()
			{
				if (Process != null)
					Ui.CloseApp(Process);
				Process = null;
				try { Directory.Delete(Folder, true); }
				catch (IOException) { }
				catch (UnauthorizedAccessException) { }
			}
		}

		/// <summary>Waits a while. Polling lives in Ui, so a test body never sleeps.</summary>
		static void Wait(TimeSpan span)
		{
			var until = DateTime.UtcNow + span;
			Ui.WaitFor(() => DateTime.UtcNow >= until ? "reached" : null, span + TimeSpan.FromSeconds(5), "the clock");
		}

		#endregion

		#region The steps and the wakes, from one start

		static string[] _marks;
		static double _shownTimers;
		static double _shownCalls;
		static double _minimisedTimers;
		static double _minimisedCalls;
		static bool _minimisedThroughout;

		/// <summary>Starts a copy once for both tests: its trace, and what woke its window's thread shown and minimised.</summary>
		static void StartOnce()
		{
			if (_marks != null)
				return;
			using (var copy = new Copy("startup-steps"))
			{
				copy.Start();
				copy.WaitForMark("issues checked", 60);
				copy.WaitForMark("device thread start", 60);
				copy.WaitForMark(TabsReady, 60);
				copy.WaitForMark(ControllerTabsReady, 120);
				// Settled: the engine has run for a while and the first issue round has ended.
				Ui.WaitFor(() => copy.EngineSeconds() >= 3 ? "running" : null, TimeSpan.FromSeconds(30), "three seconds of the engine");
				var from = copy.Now();
				// Several issue rounds: their calls come in bursts, so six seconds holds one round or two and reads as half or double.
				Wait(TimeSpan.FromSeconds(16));
				var to = copy.Now();
				Ui.Minimize(copy.Window);
				Ui.WaitFor(() => Ui.IsMinimized(copy.Window) ? "minimised" : null, TimeSpan.FromSeconds(10), "the window to minimise");
				Wait(TimeSpan.FromSeconds(2));
				var minimisedFrom = copy.Now();
				Wait(TimeSpan.FromSeconds(6));
				var minimisedTo = copy.Now();
				// Shown again while it was counted, as a person or a second launch can show it, the window was counted drawing.
				_minimisedThroughout = Ui.IsMinimized(copy.Window);
				_shownTimers = copy.WakesPerSecond(from, to, "timers");
				_shownCalls = copy.WakesPerSecond(from, to, "calls");
				_minimisedTimers = copy.WakesPerSecond(minimisedFrom, minimisedTo, "timers");
				_minimisedCalls = copy.WakesPerSecond(minimisedFrom, minimisedTo, "calls");
				Console.WriteLine("A second, shown: " + _shownTimers.ToString("0.0") + " timer ticks and " + _shownCalls.ToString("0.0")
					+ " posted calls; minimised: " + _minimisedTimers.ToString("0.0") + " and " + _minimisedCalls.ToString("0.0") + ".");
				_marks = copy.Marks();
			}
		}

		[TestMethod, TestCategory("startup"), TestCategory("ui-interactive"), Timeout(240000)]
		[Description("The start-up steps run once each, in order, and the first issue round is checked once")]
		public void Start_up_steps_run_once_each_in_order()
		{
			StartOnce();
			foreach (var line in _marks)
				Console.WriteLine(line);
			var at = new List<int>();
			foreach (var step in Steps.Concat(new[] { "issues checked" }))
			{
				var count = _marks.Count(x => x == step);
				Assert.AreEqual(1, count, "The start-up step '" + step + "' ran " + count + " times.");
				at.Add(Array.IndexOf(_marks, step));
			}
			for (var i = 1; i < Steps.Length; i++)
				Assert.IsTrue(at[i] > at[i - 1], "'" + Steps[i] + "' ran before '" + Steps[i - 1] + "'.");
			Assert.IsTrue(at[Steps.Length] > Array.IndexOf(_marks, "UpdateForm1: end"), "The first issue round ended before it was started.");
		}

		/// <remarks>
		/// Counted from the program's own trace of what its window's thread takes from its queue, not from how often Windows
		/// switches the thread in: another program that asks the window something, as an accessibility tool asks a tab strip
		/// for its selection, wakes it too, and was measured at about forty times a second on one machine.
		/// </remarks>
		[TestMethod, TestCategory("startup"), TestCategory("performance"), TestCategory("ui-interactive"), Timeout(240000)]
		[Description("Once started, the window's thread is woken only by the drawing timer, shown and minimised")]
		public void Once_started_nothing_polls_the_window()
		{
			StartOnce();
			// Minimised, the drawing timer still ticks five times a second and draws nothing.
			Assert.IsTrue(_shownTimers <= DrawingInFront + IssuesMonitor + TimerSlack,
				"Shown, " + _shownTimers.ToString("0.0") + " timer ticks a second woke the window; the drawing timer and the Issues list's monitor account for "
				+ (DrawingInFront + IssuesMonitor) + ".");
			Assert.IsTrue(_shownCalls <= Calls, "Shown, " + _shownCalls.ToString("0.0") + " posted calls a second woke the window.");
			if (!_minimisedThroughout)
				Assert.Inconclusive("The window was shown again before its minimised count ended, by a person or another program, so it was "
					+ "counted drawing (" + _minimisedTimers.ToString("0.0") + " timer ticks a second). Run it again with the desktop left alone.");
			Assert.IsTrue(_minimisedTimers <= DrawingBehind + IssuesMonitor + TimerSlack,
				"Minimised, " + _minimisedTimers.ToString("0.0") + " timer ticks a second woke the window; the drawing timer and the Issues list's monitor account for "
				+ (DrawingBehind + IssuesMonitor) + ".");
			Assert.IsTrue(_minimisedCalls <= Calls, "Minimised, " + _minimisedCalls.ToString("0.0") + " posted calls a second woke the window.");
		}

		#endregion

		#region The window first, then the main control, and the tabs made ready after

		/// <summary>The trace's mark of the windows of the tabs not open being made.</summary>
		const string TabsReady = "tabs ready";

		/// <summary>The trace's mark of the main control joining the window.</summary>
		const string ControlAdded = "MainControl: added";

		/// <summary>The mark for the words that say the window is loading giving way to the control, once its tabs are ready.</summary>
		const string ControlShown = "MainControl: shown";

		/// <summary>The mark for the windows of the tabs inside the controller pages being made, in the background once the control is shown.</summary>
		const string ControllerTabsReady = "controller tabs ready";

		/// <summary>
		/// Opens a controller tab and reads every control on its page, each with its place within the page. A page settles
		/// after it is shown, so it is read until two readings agree.
		/// </summary>
		static string[] PageLayout(AutomationElement window, int pad)
		{
			var tab = window.FindFirst(TreeScope.Descendants, new AndCondition(
				new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
				new PropertyCondition(AutomationElement.NameProperty, "Controller " + pad)));
			Assert.IsNotNull(tab, "The window has no tab for Controller " + pad + ".");
			((SelectionItemPattern)tab.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
			var id = new PropertyCondition(AutomationElement.AutomationIdProperty, "ControlPad" + pad);
			var page = Ui.WaitFor(() => window.FindFirst(TreeScope.Descendants, id), TimeSpan.FromSeconds(30), "Controller " + pad + "'s page");
			string[] last = null;
			Ui.WaitFor(() =>
			{
				var origin = page.Current.BoundingRectangle;
				var layout = page.FindAll(TreeScope.Descendants, Condition.TrueCondition).Cast<AutomationElement>().Select(x =>
				{
					var at = x.Current.BoundingRectangle;
					// A column heading's id is a number Windows makes up for it, different on every list.
					var name = x.Current.AutomationId.All(char.IsDigit) ? "" : x.Current.AutomationId;
					return string.Format("{0} {1} {2},{3} {4}x{5}", x.Current.ControlType.ProgrammaticName, name,
						at.Left - origin.Left, at.Top - origin.Top, at.Width, at.Height);
				}).ToArray();
				var settled = last != null && layout.SequenceEqual(last);
				last = layout;
				return settled ? "settled" : null;
			}, TimeSpan.FromSeconds(30), "Controller " + pad + "'s page to settle");
			return last;
		}

		[TestMethod, TestCategory("startup"), TestCategory("ui-interactive"), Timeout(240000)]
		[Description("Every controller page is built before the main control joins the window, and the tabs are made ready once, after it")]
		public void Every_page_is_built_before_the_control_joins_the_window_and_the_tabs_made_ready_after()
		{
			StartOnce();
			var added = Array.IndexOf(_marks, ControlAdded);
			Assert.IsTrue(added >= 0, "The trace has no mark of the main control joining the window.");
			Assert.IsTrue(Array.IndexOf(_marks, "StartApp: main form built") < Array.IndexOf(_marks, "MainControl: before InitializeComponent"),
				"The main control was begun before the window was built.");
			foreach (var pad in new[] { 1, 2, 3, 4 })
			{
				var built = Array.IndexOf(_marks, "BuildPages: pad " + pad + " built");
				Assert.IsTrue(built >= 0 && built < added, "Controller " + pad + "'s page was not built before the control joined the window.");
			}
			Assert.AreEqual(1, _marks.Count(x => x == TabsReady), "The tabs were not made ready once.");
			Assert.IsTrue(Array.IndexOf(_marks, "device thread start") > Array.IndexOf(_marks, ControlShown), "The engine ran before the main control showed, with nothing yet to show it.");
			Assert.AreEqual(1, _marks.Count(x => x == ControllerTabsReady), "The tabs inside the controller pages were not made ready once.");
			Assert.IsTrue(Array.IndexOf(_marks, ControllerTabsReady) > Array.IndexOf(_marks, ControlShown), "The window waited for the tabs inside the controller pages before it showed the control.");
		}

		[TestMethod, TestCategory("startup"), TestCategory("ui-interactive"), Timeout(240000)]
		[Description("The window appears first and says it is loading, and the Game row and the tabs take the place of those words once the tabs are ready")]
		public void The_window_appears_first_and_the_main_control_joins_it()
		{
			using (var copy = new Copy("window-first"))
			{
				copy.Launch();
				var window = WaitForWindow(copy);
				var words = new PropertyCondition(AutomationElement.AutomationIdProperty, nameof(MainForm.LoadingLabel));
				// Read as long as the window is still loading: a slow computer may reach it only after the control has joined.
				var loading = window.FindFirst(TreeScope.Descendants, words);
				if (loading == null)
					Console.WriteLine("The window had finished loading by the time it was read, so its words were not checked.");
				else
				{
					Assert.AreEqual("Loading. Please wait...", loading.Current.Name, "The window does not say it is loading.");
					var subject = window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, nameof(MainForm.HelpSubjectLabel))).Current.Name;
					var spinner = window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, nameof(x360ce.App.Controls.BaseFormWithHeader.BusyLoadingCircle)));
					var counters = new[] { "UI Hz: 0", "HW Hz: 0", "D: 0" }
						.Where(x => window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, x)) == null).ToArray();
					// Read after both, so a window that finished meanwhile is not held to what a loading one shows.
					if (window.FindFirst(TreeScope.Descendants, words) != null)
					{
						Assert.AreEqual("", subject, "The header names a page while the window is still loading.");
						Assert.IsTrue(spinner != null && !spinner.Current.IsOffscreen, "The header's spinner is not shown while the window is loading.");
						Assert.AreEqual(0, counters.Length, "The status bar counts while the window is loading: " + string.Join(", ", counters) + " is not shown.");
					}
					foreach (var placeholder in new[] { "Help Subject", nameof(MainForm.StatusTimerLabel), nameof(MainForm.StatusEventsLabel), nameof(MainForm.StatusSaveLabel), nameof(MainForm.StatusIsAdminLabel), nameof(MainForm.StatusDllLabel) })
						Assert.IsNull(window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, placeholder)),
							"The window shows the designer's words '" + placeholder + "' while it loads.");
				}
				Ui.WaitFor(() => window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, nameof(MainControl))),
					TimeSpan.FromSeconds(60), "the main control to join the window");
				copy.WaitForMark(ControlShown, 60);
				Ui.WaitFor(() => window.FindFirst(TreeScope.Descendants, words) == null ? "gone" : null, TimeSpan.FromSeconds(10), "the words to go");
				Assert.AreEqual("Controller 1", window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, nameof(MainForm.HelpSubjectLabel))).Current.Name,
					"The header does not name the page shown.");
				var stopped = window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, nameof(x360ce.App.Controls.BaseFormWithHeader.BusyLoadingCircle)));
				Assert.IsTrue(stopped == null || stopped.Current.IsOffscreen, "The header's spinner still shows after the window has loaded.");
				Ui.WaitFor(() => window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "UI Hz: 0")) == null
					&& window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "HW Hz: 0")) == null ? "counting" : null,
					TimeSpan.FromSeconds(10), "the status bar to show the rates once the window has loaded");
				var marks = copy.Marks();
				Assert.IsTrue(Array.IndexOf(marks, ControlAdded) < Array.IndexOf(marks, TabsReady) && Array.IndexOf(marks, TabsReady) < Array.IndexOf(marks, ControlShown),
					"The window stopped saying it is loading before the tabs were ready.");
				Assert.IsTrue(Array.IndexOf(marks, "MainForm: shown") >= 0 && Array.IndexOf(marks, "MainForm: shown") < Array.IndexOf(marks, "MainControl: before InitializeComponent"),
					"The main control was begun before the window had appeared.");
			}
		}

		[TestMethod, TestCategory("startup"), TestCategory("ui-interactive"), Timeout(240000)]
		[Description("A step of loading that fails says so on the window, rather than leaving it loading for ever")]
		public void A_step_that_fails_says_so_on_the_window()
		{
			using (var copy = new Copy("load-fails"))
			{
				copy.Launch("", new Dictionary<string, string> { { "X360CE_THROW_WHILE_LOADING", "1" } });
				var window = WaitForWindow(copy);
				copy.WaitForMark("loading failed", 60);
				var words = window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, nameof(MainForm.LoadingLabel)));
				StringAssert.StartsWith(words.Current.Name, "Loading failed: ", "The window does not say that loading failed.");
				Assert.IsFalse(copy.Process.HasExited, "The program ended rather than saying so.");
			}
		}

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern bool IsWindowVisible(IntPtr window);

		[TestMethod, TestCategory("startup"), TestCategory("ui-interactive"), Timeout(240000)]
		[Description("Closing the window takes it off the screen at once, keeps a recording that runs, and ends the program")]
		public void Closing_the_window_keeps_a_running_recording_and_ends_the_program()
		{
			using (var copy = new Copy("close-recording"))
			{
				copy.Start();
				var window = AutomationElement.FromHandle(copy.Window);
				var tab = window.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem))
					.Cast<AutomationElement>().First(x => x.Current.Name.EndsWith("Issues", StringComparison.Ordinal) || x.Current.Name.EndsWith("Issue", StringComparison.Ordinal));
				((SelectionItemPattern)tab.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
				var button = Ui.WaitFor(() => window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, nameof(RecorderUserControl.RecordButton))),
					TimeSpan.FromSeconds(30), "the recording button");
				((InvokePattern)button.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
				var folder = Path.Combine(copy.Profile, "Recordings");
				Ui.WaitFor(() => Directory.Exists(folder) && Directory.GetFiles(folder).Length > 0 ? "started" : null, TimeSpan.FromSeconds(10), "the recording to start");
				Wait(TimeSpan.FromSeconds(2));
				var clock = Stopwatch.StartNew();
				copy.Process.CloseMainWindow();
				Ui.WaitFor(() => IsWindowVisible(copy.Window) ? null : "gone", TimeSpan.FromSeconds(10), "the window to leave the screen");
				var gone = clock.ElapsedMilliseconds;
				Assert.IsTrue(copy.Process.WaitForExit(30000), "The program did not end after its window was closed.");
				Console.WriteLine("The window left the screen after " + gone + " ms, and the program ended after " + clock.ElapsedMilliseconds + " ms.");
				Assert.IsTrue(gone < 5000, "The window stayed on the screen for " + gone + " ms after it was closed.");
				Assert.IsTrue(copy.Marks().Contains("loops stopped"), "Closing did not stop the loops.");
				StringAssert.Contains(File.ReadAllText(Directory.GetFiles(folder).Single()), "recording ended", "Closing the window lost the end of the recording.");
			}
		}

		/// <summary>The window as it first appears, with or without its tabs.</summary>
		static AutomationElement WaitForWindow(Copy copy)
		{
			return Ui.WaitFor(() =>
			{
				copy.Process.Refresh();
				return copy.Process.MainWindowHandle == IntPtr.Zero ? null : AutomationElement.FromHandle(copy.Process.MainWindowHandle);
			}, TimeSpan.FromSeconds(30), "the window");
		}

		/// <remarks>
		/// A window that has read no settings holds empty lists, and closing saves what the window holds, so a close while it
		/// loads would replace the person's settings with nothing. Closed at the first moment it can be, and again further on.
		/// What matters is what the files hold, so that is what is compared: the program may write a file again in its own format.
		/// </remarks>
		[TestMethod, TestCategory("startup"), TestCategory("ui-interactive"), Timeout(240000)]
		[Description("Closing the window while it loads loses no settings")]
		public void Closing_the_window_while_it_loads_keeps_the_settings()
		{
			var closedBeforeTheEngine = 0;
			foreach (var mark in new[] { null, "BuildPages: pad 2 built", "MainControl: themed", ControlAdded })
				using (var copy = new Copy("close-while-loading"))
				{
					var row = WriteRow(copy, MapTo.Controller2);
					var folder = Path.Combine(copy.Profile, "Settings");
					var before = Directory.GetFiles(folder).ToDictionary(x => x, File.ReadAllBytes);
					copy.Launch();
					var window = WaitForWindow(copy);
					if (mark != null)
						copy.WaitForMark(mark, 60);
					// Reached late on a busy computer. Closing then must keep the settings as well, so the round still counts for that.
					if (!copy.Marks().Contains("device thread start"))
						closedBeforeTheEngine++;
					copy.Process.CloseMainWindow();
					Assert.IsTrue(copy.Process.WaitForExit(30000), "The window closed at " + (mark ?? "its appearing") + " did not end the program.");
					var when = "Closing the window at " + (mark ?? "its appearing");
					foreach (var pair in before)
						if (!pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key)))
							Console.WriteLine(when + " rewrote " + Path.GetFileName(pair.Key) + ":" + Environment.NewLine + File.ReadAllText(pair.Key));
					Assert.AreEqual(row.SettingId, copy.Read<UserSetting>("UserSettings.xml").Single().SettingId, when + " lost the device's row.");
					Assert.AreEqual(row.PadSettingChecksum, copy.Read<PadSetting>("PadSettings.xml").Single().PadSettingChecksum, when + " lost the row's settings.");
					Assert.AreEqual(1, copy.Read<UserDevice>("UserDevices.xml").Length, when + " lost the device.");
					Assert.IsTrue(copy.Read<UserGame>("UserGames.xml").Any(x => x.FileProductName == "X360CE startup test"), when + " lost the game.");
				}
			Assert.IsTrue(closedBeforeTheEngine > 0, "No round closed the window before its settings were read, so that close was not tested.");
		}

		[TestMethod, TestCategory("startup"), TestCategory("ui-interactive"), Timeout(240000)]
		[Description("A start in the tray shows no loading window and makes no tab ready until the window is shown")]
		public void A_start_in_the_tray_makes_no_tab_ready_until_the_window_is_shown()
		{
			using (var copy = new Copy("start-in-tray"))
			{
				copy.Launch("/WindowState=Minimized");
				// Nobody is waiting for the window, so the controllers do not wait for it.
				copy.WaitForMark("device thread start", 60);
				Wait(TimeSpan.FromSeconds(3));
				Assert.IsFalse(copy.Marks().Contains(TabsReady), "The tabs were made ready while the window was in the tray.");
				Ui.Restore(copy.Process);
				copy.WaitForMark(TabsReady, 30);
			}
		}

		[TestMethod, TestCategory("startup"), TestCategory("ui-interactive"), Timeout(240000)]
		[Description("A tab made ready unseen is laid out as the tab shown from the start, in Windows' font and in a chosen one")]
		public void A_tab_made_ready_unseen_is_laid_out_as_the_first()
		{
			foreach (var options in new[] { new Options(), new Options { InterfaceFont = AppFonts.BuiltInName } })
				using (var copy = new Copy("tab-made-ready"))
				{
					copy.Write("Options.xml", options);
					copy.Start();
					copy.WaitForMark(TabsReady, 60);
					var window = AutomationElement.FromHandle(copy.Window);
					var first = PageLayout(window, 1);
					var second = PageLayout(window, 2);
					var font = string.IsNullOrEmpty(options.InterfaceFont) ? "Windows' font" : options.InterfaceFont;
					Assert.IsTrue(first.Length > 50, "Controller 1's page shows too few controls to compare, in " + font + ": " + first.Length + ".");
					var different = first.Zip(second, (a, b) => a == b ? null : a + "  <>  " + b).Where(x => x != null).ToArray();
					Assert.IsTrue(first.Length == second.Length && different.Length == 0, "In " + font + ", Controller 2's page has "
						+ second.Length + " controls and Controller 1's " + first.Length + ", and " + different.Length + " stand elsewhere:"
						+ Environment.NewLine + string.Join(Environment.NewLine, different.Take(20)));
				}
		}

		[TestMethod, TestCategory("startup"), TestCategory("ui-interactive"), Timeout(240000)]
		[Description("The door's first call reads a controller page nobody has opened with its real values, and a change made there is kept")]
		public void The_door_reads_and_changes_a_page_nobody_has_opened()
		{
			using (var copy = new Copy("page-through-door"))
			{
				var ai = WriteDoor(copy, 37366);
				var row = WriteRow(copy, MapTo.Controller4);
				copy.Start();
				WaitForDoor(ai.AiAccessPort);
				var tree = (Dictionary<string, object>)Json.DeserializeObject(Text(Post(ai, Call(1, "ui_read", "{}"))));
				var path = PathOf(tree, nameof(PadControl.ForceOverallTrackBar), nameof(MainControl.Pad4TabPage));
				Assert.IsNotNull(path, "The overall force strength slider is not in the door's first reading under Controller 4.");
				var read = ValueOf(tree, path);
				Assert.AreEqual("80", read, "The door's first reading of Controller 4 did not show the row's setting.");
				StringAssert.Contains(Text(Post(ai, Call(2, "ui_set", "{\"path\":\"" + path + "\",\"value\":\"35\"}"))), "Done");
				Text(Post(ai, Call(3, "settings_save", "{}")));
				var saved = copy.Read<UserSetting>("UserSettings.xml").Single(x => x.SettingId == row.SettingId);
				var kept = copy.Read<PadSetting>("PadSettings.xml").FirstOrDefault(x => x.PadSettingChecksum == saved.PadSettingChecksum);
				Assert.AreEqual("35", kept?.ForceOverall, "The slider changed on Controller 4 through the door was not kept.");
			}
		}

		/// <summary>The value of the node at a path, as the door reads it.</summary>
		static string ValueOf(Dictionary<string, object> node, string path)
		{
			object value;
			if (node.TryGetValue("Path", out value) && (string)value == path)
				return node.TryGetValue("Value", out value) ? string.Format("{0}", value) : null;
			object items;
			if (!node.TryGetValue("Items", out items) || !(items is object[]))
				return null;
			return ((object[])items).Select(x => ValueOf((Dictionary<string, object>)x, path)).FirstOrDefault(x => x != null);
		}

		#endregion

		#region A critical issue standing

		[TestMethod, TestCategory("startup"), TestCategory("ui-interactive"), Timeout(240000)]
		[Description("With a critical issue standing, the engine runs and a box changed on a controller page is kept")]
		public void A_critical_issue_holds_up_neither_the_engine_nor_the_settings()
		{
			using (var copy = new Copy("critical-issue"))
			{
				var ai = WriteDoor(copy, 37364);
				var row = WriteRow(copy, MapTo.Controller1);
				copy.Start(new Dictionary<string, string> { { "X360CE_CRITICAL_ISSUE", "A critical issue told to stand by the test." } });
				copy.WaitForMark("issue checked: ToldCriticalIssue", 60);
				copy.WaitForMark("issues checked", 60);
				var seconds = copy.EngineSeconds();
				WaitForDoor(ai.AiAccessPort);
				var tree = (Dictionary<string, object>)Json.DeserializeObject(Text(Post(ai, Call(1, "ui_read", "{}"))));
				var path = PathOf(tree, nameof(PadControl.ForceOverallTrackBar), nameof(MainControl.Pad1TabPage));
				Assert.IsNotNull(path, "The overall force strength slider is not in the tree under controller 1.");
				StringAssert.Contains(Text(Post(ai, Call(2, "ui_set", "{\"path\":\"" + path + "\",\"value\":\"35\"}"))), "Done");
				Text(Post(ai, Call(3, "settings_save", "{}")));
				var saved = copy.Read<UserSetting>("UserSettings.xml").Single(x => x.SettingId == row.SettingId);
				var kept = copy.Read<PadSetting>("PadSettings.xml").FirstOrDefault(x => x.PadSettingChecksum == saved.PadSettingChecksum);
				var running = true;
				try
				{
					Ui.WaitFor(() => copy.EngineSeconds() >= seconds + 2 ? "running" : null, TimeSpan.FromSeconds(15), "the engine");
				}
				catch (TimeoutException)
				{
					running = false;
				}
				var faults = new List<string>();
				if (!running)
					faults.Add("the engine did not run (" + copy.EngineSeconds() + " seconds of it in the trace)");
				if (kept == null || kept.ForceOverall != "35")
					faults.Add("the slider changed on Controller 1 was not kept (saved: " + (kept == null ? "nothing" : kept.ForceOverall) + ")");
				Assert.AreEqual(0, faults.Count, "With a critical issue standing, " + string.Join(", and ", faults) + ".");
			}
		}

		/// <summary>Opens the door, on a port of its own, for one test alone; its key is kept here and never shown.</summary>
		static AiOptions WriteDoor(Copy copy, int port)
		{
			var ai = new AiOptions { AiAccessEnabled = true, AiAccess = AiAccess.Configure, AiAccessTrustLocal = true, AiAccessPort = port };
			ai.EnsureAiAccessToken();
			copy.Write("AI.xml", ai);
			return ai;
		}

		/// <summary>
		/// The copy's own entry, with its tabs off, and one row on a controller page for a listed device that is not plugged
		/// in, with a force strength of 80: a page shows its boxes greyed out for a device the list does not hold.
		/// </summary>
		static UserSetting WriteRow(Copy copy, MapTo page)
		{
			var device = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Startup test pad", InstanceName = "Startup test pad", IsEnabled = true };
			var game = new UserGame
			{
				FileName = "x360ce.exe",
				FileProductName = "X360CE startup test",
				FullPath = copy.Exe,
				EnableMask = 0,
				EmulationType = (int)EmulationType.None,
			};
			var settings = new PadSetting { ForceOverall = "80" };
			settings.PadSettingChecksum = settings.CleanAndGetCheckSum();
			var row = new UserSetting
			{
				SettingId = Guid.NewGuid(),
				MapTo = (int)page,
				FileName = game.FileName,
				FileProductName = game.FileProductName,
				InstanceGuid = device.InstanceGuid,
				InstanceName = device.InstanceName,
				ProductName = device.ProductName,
				IsEnabled = true,
				PadSettingChecksum = settings.PadSettingChecksum,
			};
			copy.Write("UserDevices.xml", device);
			copy.Write("UserGames.xml", game);
			copy.Write("UserSettings.xml", row);
			copy.Write("PadSettings.xml", settings);
			return row;
		}

		static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

		/// <summary>Waits for the door to take connections: it opens once the window has its pages.</summary>
		static void WaitForDoor(int port)
		{
			Ui.WaitFor(() =>
			{
				using (var client = new System.Net.Sockets.TcpClient())
				{
					try
					{
						client.Connect("localhost", port);
						return "open";
					}
					catch (System.Net.Sockets.SocketException)
					{
						return null;
					}
				}
			}, TimeSpan.FromSeconds(60), "the AI door on port " + port);
		}

		static string Post(AiOptions ai, string body)
		{
			return McpClient.Post(ai.AiAccessPort, ai.AiAccessToken, body);
		}

		static string Call(int id, string tool, string arguments)
		{
			return "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"method\":\"tools/call\",\"params\":{\"name\":\"" + tool + "\",\"arguments\":" + arguments + "}}";
		}

		static string Text(string response)
		{
			var answer = (Dictionary<string, object>)Json.DeserializeObject(response);
			Assert.IsFalse(answer.ContainsKey("error"), "The program answered with an error: " + response);
			return (string)((Dictionary<string, object>)((object[])((Dictionary<string, object>)answer["result"])["content"])[0])["text"];
		}

		/// <summary>The Path of the node with the given Id, searched only under the node with the other Id.</summary>
		static string PathOf(Dictionary<string, object> node, string id, string under, bool inside = false)
		{
			object value;
			var here = node.TryGetValue("Id", out value) && (string)value == under;
			if ((inside || here) && node.TryGetValue("Id", out value) && (string)value == id)
				return (string)node["Path"];
			object items;
			if (!node.TryGetValue("Items", out items) || !(items is object[]))
				return null;
			foreach (var child in (object[])items)
			{
				var found = PathOf((Dictionary<string, object>)child, id, under, inside || here);
				if (found != null)
					return found;
			}
			return null;
		}

		#endregion
	}
}
