// @under-test: App.v4/Common/DInput/DInputHelper.Step6.RetrieveXiStates.cs, App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs, App.v4/Common/DInput/DInputHelper.cs, App.v4/Common/DInput/VirtualDriverInstaller.cs, App.v4/MainForm.cs
// @area: engine   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using x360ce.App.DInput;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// XInput stops answering while Windows takes controllers away and builds them again, which
	/// removing leftover controllers does. The reads must rest for a while and then go on by
	/// themselves; nothing may answer the timeout by switching the person's XInput view off.
	/// </summary>
	[TestClass]
	public class XInputReadPauseTest
	{
		[TestMethod, TestCategory("engine")]
		[Description("A pause runs until its end and not past it, across the tick counter wrapping round")]
		public void The_pause_ends_when_it_ends()
		{
			Assert.IsTrue(Paused(1000, 999));
			Assert.IsFalse(Paused(1000, 1000));
			Assert.IsFalse(Paused(1000, 5000));
			// The tick counter turns negative after 24.9 days; a pause set just before must still end just after.
			Assert.IsTrue(Paused(int.MinValue + 100, int.MaxValue - 100));
			Assert.IsFalse(Paused(int.MinValue + 100, int.MinValue + 200));
			Assert.IsTrue(DInputHelper.XiReadPauseMs >= 1000 && DInputHelper.XiReadPauseMs <= 30000,
				"The pause is seconds, not an instant and not for ever.");
		}

		[TestMethod, TestCategory("engine")]
		[Description("A pause never set does not hold the reads, whatever the uptime")]
		public void A_pause_never_set_holds_nothing_at_any_uptime()
		{
			// The pause starts at 0. Environment.TickCount is negative from 24.9 to 49.7 days after
			// Windows starts, and compared by sign alone a 0 lies ahead of it for all that time.
			Assert.IsFalse(Paused(0, 1000000), "One thousand seconds after Windows started.");
			Assert.IsFalse(Paused(0, int.MinValue + 1000000), "25 days after Windows started.");
			Assert.IsFalse(Paused(0, -1000000000), "37 days after Windows started.");
		}

		/// <summary>Whether the reads rest, asked the way <see cref="DInputHelper.XiReadsPaused"/> asks it.</summary>
		static bool Paused(int until, int now)
		{
			return DInputHelper.IsWaiting(until, now, DInputHelper.XiReadPauseMs);
		}

		[TestMethod, TestCategory("engine")]
		[Description("Nothing in the window answers a read failure by switching the XInput view off")]
		public void A_read_failure_never_switches_the_view_off()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "MainForm.cs");
			var source = File.ReadAllText(path);
			var handler = Regex.Match(source, @"void DHelper_StatesRetrieved\(.*?\n\t\t\}", RegexOptions.Singleline);
			Assert.IsTrue(handler.Success, "The handler for retrieved states was not found.");
			Assert.IsFalse(handler.Value.Contains("GetXInputStates"),
				"The handler for a failed read changes the XInput view setting. A timeout while Windows " +
				"rebuilds controllers used to switch the view off for good, with its button still showing on.");
		}

		[TestMethod, TestCategory("engine")]
		[Description("The rest, and the time a read has to answer, are both judged across the tick counter wrapping")]
		public void The_rest_is_judged_the_same_at_any_uptime()
		{
			var step6 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step6.RetrieveXiStates.cs"));
			StringAssert.Contains(step6, "IsWaiting(_xiReadPausedUntil, Environment.TickCount, XiReadPauseMs)",
				"The rest is no longer judged by IsWaiting, which holds at any uptime.");
			StringAssert.Contains(step6, "IsRecent(_displayReadAskedAt, Environment.TickCount, XiAnswerMs)",
				"The time a read has to answer is no longer judged by IsRecent, which holds at any uptime.");
		}

		[TestMethod, TestCategory("engine")]
		[Description("A read that does not come back rests the view after a second, without holding up a pass, and its answer is taken when it comes")]
		public void A_read_that_does_not_come_back_rests_the_view()
		{
			var helper = new DInputHelper();
			var retrieve = EngineSteps.RetrieveXiStates(helper);
			var game = new UserGame { FileName = "read-rest.exe", FileProductName = "Read rest" };
			var errors = new List<string>();
			helper.StatesRetrieved += (s, e) =>
			{
				if (e.Error != null)
					errors.Add(e.Error.Message);
			};
			XInputDisplayReadTest.StartClock(helper);
			Thread.Sleep(20);
			long slowest = 0;
			var paused = false;
			var connected = true;
			try
			{
				XInputDisplayReadTest.WithSystemXInput(() =>
				{
					// Another thread holds the lock the reader reads under. To the input thread that is a read that
					// does not come back.
					Assert.IsTrue(HeldLock.Finishes(() =>
					{
						var run = Stopwatch.StartNew();
						while (run.ElapsedMilliseconds < 1200)
						{
							var call = Stopwatch.StartNew();
							retrieve(game, true);
							slowest = Math.Max(slowest, call.ElapsedMilliseconds);
							Thread.Sleep(16);
						}
						paused = helper.XiReadsPaused;
						connected = helper.LiveXiConnected.Any(x => x);
					}, 3000, Controller.XInputLock), "The input thread waits for a read that does not come back.");
					// Let go of, the reader answers, and the answer is taken even while the view rests.
					XInputDisplayReadTest.WaitForAnswer(helper);
					retrieve(game, true);
					Assert.AreEqual(XInputDisplayReadTest.Idle, XInputDisplayReadTest.DisplayRead(helper), "The late answer was not taken.");
				});
			}
			finally
			{
				helper.Dispose();
			}
			Assert.IsTrue(slowest < 50, "A pass waited " + slowest + " ms for the display read.");
			Assert.AreEqual(1, errors.Count, "A read that does not come back is to be said once a rest: " + string.Join(" | ", errors));
			StringAssert.Contains(errors[0], "did not answer");
			Assert.IsTrue(paused, "The view does not rest while XInput does not answer.");
			Assert.IsFalse(connected, "The places are shown as connected while XInput does not answer.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Letting go of the controllers gives up in time while XInput does not answer, goes on, and says why")]
		public void Letting_go_gives_up_while_xinput_does_not_answer()
		{
			var helper = new DInputHelper();
			var errors = new List<string>();
			helper.StatesRetrieved += (s, e) =>
			{
				if (e.Error != null)
					errors.Add(e.Error.Message);
			};
			var released = true;
			long took = -1;
			XInputDisplayReadTest.WithSystemXInput(() =>
			{
				Assert.IsTrue(HeldLock.Finishes(() =>
				{
					var watch = Stopwatch.StartNew();
					released = helper.ReleaseForDeviceRemoval(TimeSpan.FromMilliseconds(200));
					took = watch.ElapsedMilliseconds;
				}, 3000, Controller.XInputLock), "Letting go of the controllers waits for a read that does not come back.");
				Assert.IsTrue(Controller.IsLoaded, "The library was let go of while a read held it.");
			});
			Assert.IsFalse(released, "Letting go says it worked while a read held the library.");
			Assert.IsTrue(took >= 150 && took < 1000, "Letting go gave up after " + took + " ms, not after the 200 ms it was given.");
			Assert.IsFalse(helper.Suspended, "The input thread is left stopped after letting go gave up.");
			Assert.AreEqual(1, errors.Count, "The person is not told once why nothing was let go of.");
			StringAssert.Contains(errors[0], "XInput is not answering");
			// Every caller acts on the answer rather than going on as if everything had been let go of.
			var dir = Path.Combine(Ui.RepoRoot.FullName, "App.v4");
			var calls = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
				.SelectMany(x => File.ReadAllLines(x).Select(line => new { File = Path.GetFileName(x), Line = line.Trim() }))
				.Where(x => x.Line.Contains("ReleaseForDeviceRemoval(") && !x.Line.StartsWith("public "))
				.ToArray();
			Assert.AreEqual(3, calls.Length, "Expected the three callers: " + string.Join(" | ", calls.Select(x => x.File + ": " + x.Line)));
			foreach (var call in calls)
				Assert.IsTrue(call.Line.StartsWith("if (") && call.Line.Contains("!"),
					call.File + " lets go of the controllers without asking whether it worked: " + call.Line);
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Putting controllers in order gives up within the time XInput has to answer while XInput does not answer")]
		public void Reordering_gives_up_while_xinput_does_not_answer()
		{
			var helper = new DInputHelper();
			var stopped = true;
			long took = -1;
			Assert.IsTrue(HeldLock.Finishes(() =>
			{
				var watch = Stopwatch.StartNew();
				// The runner's own limit, which is for the pass under way and the device list.
				stopped = helper.StopForReorder(TimeSpan.FromSeconds(10));
				took = watch.ElapsedMilliseconds;
			}, 3 * DInputHelper.XiAnswerMs, Controller.XInputLock), "Putting controllers in order waits past the time XInput has to answer.");
			Assert.IsFalse(stopped, "Putting controllers in order says everything was let go of while a read held the library.");
			Assert.IsTrue(took < 2 * DInputHelper.XiAnswerMs, "Putting controllers in order gave up after " + took + " ms.");
			Assert.IsFalse(helper.Suspended, "Feeding the controllers is left stopped after putting them in order gave up.");
		}
	}
}
