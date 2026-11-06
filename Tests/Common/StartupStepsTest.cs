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
using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Mcp;
using JocysCom.ClassLibrary.Runtime;
using x360ce.App;
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
		static readonly string[] Steps = { "LoadPages: end", "UpdateForm1: end", "UpdateForm2: start", "UpdateForm2: end", "device thread start" };

		/// <summary>The drawing timer's rate in front; behind and minimised it ticks five times a second.</summary>
		const int DrawingInFront = 10;
		const int DrawingBehind = 5;

		/// <summary>The Issues list's own monitor, in the shared library, ticks twice a second for the program's life.</summary>
		const int IssuesMonitor = 2;

		/// <summary>Room for a tick that falls either side of a second's line.</summary>
		const double TimerSlack = 1;

		/// <summary>Work posted to the window now and then: the issue round's list and its tab, every five seconds while shown.</summary>
		const double Calls = 1;

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
				var info = new ProcessStartInfo(Exe, "\"/Profile=" + Profile + "\"") { WorkingDirectory = Folder, UseShellExecute = false };
				info.EnvironmentVariables["X360CE_ENGINE_LOG"] = Log;
				if (environment != null)
					foreach (var pair in environment)
						info.EnvironmentVariables[pair.Key] = pair.Value;
				Process = Process.Start(info);
				Window = new IntPtr(Ui.WaitForMainWindow(Process, TimeSpan.FromSeconds(60)).Current.NativeWindowHandle);
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
				// Settled: the engine has run for a while and the first issue round has ended.
				Ui.WaitFor(() => copy.EngineSeconds() >= 3 ? "running" : null, TimeSpan.FromSeconds(30), "three seconds of the engine");
				var from = copy.Now();
				Wait(TimeSpan.FromSeconds(6));
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
			Assert.IsTrue(at[Steps.Length] > at[1], "The first issue round ended before it was started.");
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

		#region A critical issue standing

		[TestMethod, TestCategory("startup"), TestCategory("ui-interactive"), Timeout(240000)]
		[Description("With a critical issue standing, the engine runs and a box changed on a controller page is kept")]
		public void A_critical_issue_holds_up_neither_the_engine_nor_the_settings()
		{
			using (var copy = new Copy("critical-issue"))
			{
				// The door, on a port of its own, for this test alone; its key is kept here and never shown.
				var ai = new AiOptions { AiAccessEnabled = true, AiAccess = AiAccess.Configure, AiAccessTrustLocal = true, AiAccessPort = 37364 };
				ai.EnsureAiAccessToken();
				copy.Write("AI.xml", ai);
				// The copy's own entry, with its tabs off, and one row on Controller 1 for a listed device that is not plugged in:
				// a page shows its boxes greyed out for a device the list does not hold.
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
					MapTo = (int)MapTo.Controller1,
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
				copy.Start(new Dictionary<string, string> { { "X360CE_CRITICAL_ISSUE", "A critical issue told to stand by the test." } });
				copy.WaitForMark("issue checked: ToldCriticalIssue", 60);
				copy.WaitForMark("issues checked", 60);
				var seconds = copy.EngineSeconds();
				WaitForDoor(ai.AiAccessPort);
				var tree = (Dictionary<string, object>)Json.DeserializeObject(Text(Post(ai, Call(1, "ui_read", "{}"))));
				var path = PathOf(tree, "ForceOverallTrackBar", "Pad1TabPage");
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
