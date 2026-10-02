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
					released = helper.ReleaseForDeviceRemoval(TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(200));
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

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Putting controllers in order waits for a plug under way: it refuses while the plug runs past its limit, and goes ahead once the plug has finished")]
		public void Putting_controllers_in_order_waits_for_a_plug_under_way()
		{
			var helper = new DInputHelper();
			var plugging = (System.Threading.Tasks.Task<x360ce.App.VirtualError>[])typeof(DInputHelper)
				.GetField("_plugging", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(helper);
			var underWay = new System.Threading.Tasks.TaskCompletionSource<x360ce.App.VirtualError>();
			plugging[0] = underWay.Task;
			var stopped = true;
			long took = -1;
			// No bus client, so letting go unplugs nothing.
			var client = Nefarius.ViGEm.Client.ViGEmClient.Current;
			Nefarius.ViGEm.Client.ViGEmClient.Current = null;
			try
			{
				BusRefusalFixtures.Logged(() =>
				{
					var watch = Stopwatch.StartNew();
					stopped = helper.StopForReorder(TimeSpan.FromMilliseconds(300));
					took = watch.ElapsedMilliseconds;
				});
				Assert.IsFalse(stopped, "The controllers were let go of while a plug was under way, so its controller can arrive after them.");
				Assert.IsTrue(took >= 250 && took < 2000, "Putting controllers in order gave up after " + took + " ms, not after the 300 ms it was given.");
				Assert.IsFalse(helper.Suspended, "Feeding the controllers is left stopped after putting them in order gave up.");
				Assert.AreSame(underWay.Task, plugging[0], "The plug under way was forgotten.");
				// Repair and removing leftover controllers let go through the same step, and the person is told why it did not.
				var told = new List<string>();
				EventHandler<DInputEventArgs> tell = (sender, e) => told.Add(e.Error.Message);
				helper.StatesRetrieved += tell;
				var released = true;
				BusRefusalFixtures.Logged(() => released = helper.ReleaseForDeviceRemoval(TimeSpan.FromMilliseconds(300), TimeSpan.FromSeconds(1)));
				helper.StatesRetrieved -= tell;
				Assert.IsFalse(released, "Repair or removal let go of the controllers while a plug was under way.");
				Assert.AreEqual(1, told.Count, "The person is not told once why nothing was let go of.");
				StringAssert.Contains(told[0], "XInput is not answering");
				// It finishes while the order waits for it.
				System.Threading.Tasks.Task.Delay(300).ContinueWith(x => underWay.SetResult(x360ce.App.VirtualError.None));
				BusRefusalFixtures.Logged(() => stopped = helper.StopForReorder(TimeSpan.FromSeconds(5)));
				Assert.IsTrue(stopped, "Putting controllers in order did not go ahead once the plug had finished.");
				Assert.IsNull(plugging[0], "The finished plug was not taken in.");
			}
			finally
			{
				Nefarius.ViGEm.Client.ViGEmClient.Current = client;
			}
		}

		/// <summary>How much longer a thousand passes may take while another thread holds the lock, in milliseconds: 20 µs a pass.</summary>
		/// <remarks>A failed try of a lock another thread holds spins for about 800 µs on .NET Framework before it gives up.</remarks>
		internal const double HeldSlackMs = 20;

		/// <summary>The fastest of three runs of <paramref name="calls"/> calls, in milliseconds.</summary>
		internal static double Fastest(int calls, Action call)
		{
			var fastest = double.MaxValue;
			for (var run = 0; run < 3; run++)
			{
				var watch = Stopwatch.StartNew();
				for (var i = 0; i < calls; i++)
					call();
				fastest = Math.Min(fastest, watch.Elapsed.TotalMilliseconds);
			}
			return fastest;
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("While a read of the places holds the load lock, a pass with a refused force pending costs what a pass costs with the lock free, and makes nothing")]
		public void A_pass_passes_by_the_load_lock_a_read_holds()
		{
			const int passes = 1000;
			var routing = SharedForcePassThroughTest.PassingToPlaceOne();
			var helper = new DInputHelper();
			var last = SharedForcePassThroughTest.LastPassedForce(helper);
			Assert.IsNotNull(DInputHelper.OccupiedPlaces(), "XInput does not answer, so the library is not loaded before the test.");
			// The motors at place 1 are running, and the game has asked for them to stop.
			var running = (100 << 8) | 50;
			last[0] = running;
			double held = -1;
			long bytes = -1;
			// Held the way a read of the places holds it, saying so.
			Assert.IsTrue(HeldLock.Finishes(() =>
			{
				// Refused, so every pass tries it again.
				helper.PassForcesThrough(routing, 1 << 0);
				held = Fastest(passes, () => helper.PassForcesThrough(routing, 0));
				bytes = Allocations.FewestBytes(5, () =>
				{
					for (var i = 0; i < passes; i++)
						helper.PassForcesThrough(routing, 0);
				});
			}, 30000, HeldLock.Enter(typeof(SystemXInput), "EnterLoadLock"), HeldLock.Exit(typeof(SystemXInput), "ExitLoadLock")),
				"The passes did not finish while a read held the load lock.");
			Assert.AreEqual(running, last[0], "The stop was sent while a read held the load lock.");
			// Free: the stop goes on the first pass, and the rest have nothing to send.
			var free = Fastest(passes, () => helper.PassForcesThrough(routing, 0));
			Assert.AreEqual(0, last[0], "The stop was not sent once the load lock was free.");
			Console.WriteLine(passes + " passes: " + held + " ms with the load lock held, " + free + " ms with it free; " + bytes + " bytes held.");
			Assert.IsTrue(held <= free + HeldSlackMs, passes + " passes took " + held + " ms with the load lock held and " + free + " ms with it free.");
			Assert.IsTrue(bytes < passes, passes + " passes with the load lock held handed the collector " + bytes + " bytes.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("While the display reader holds the XInput lock, the passes that load or let go of the library cost what they cost with the lock free, and make nothing")]
		public void Loading_and_letting_go_pass_by_the_xinput_lock_a_read_holds()
		{
			const int passes = 1000;
			var helper = new DInputHelper { UpdateDevicesEnabled = false };
			var game = new UserGame { FileName = "held-xinput-lock.exe", FileProductName = "Held XInput lock", EmulationType = (int)x360ce.Engine.EmulationType.Virtual };
			// Held the way the display reader holds it, saying so.
			var enter = HeldLock.Enter(typeof(DInputHelper), "EnterXInputLock");
			var exit = HeldLock.Exit(typeof(DInputHelper), "ExitXInputLock");
			double unloadHeld = -1, loadHeld = -1, unloadFree = -1, loadFree = -1;
			long unloadBytes = -1, loadBytes = -1;
			XInputDisplayReadTest.WithSystemXInput(() =>
			{
				// Loaded and not wanted: every pass tries to let go of it.
				Assert.IsTrue(HeldLock.Finishes(() =>
				{
					unloadHeld = Fastest(passes, () => helper.CheckAndUnloadXInputLibrarry(game, false));
					unloadBytes = Allocations.FewestBytes(5, () => { for (var i = 0; i < passes; i++) helper.CheckAndUnloadXInputLibrarry(game, false); });
				}, 30000, enter, exit), "The passes did not finish while the display reader held the XInput lock.");
				Assert.IsTrue(Controller.IsLoaded, "The library was let go of while the display reader held it.");
				helper.CheckAndUnloadXInputLibrarry(game, false);
				Assert.IsFalse(Controller.IsLoaded, "The library was not let go of once the lock was free.");
				unloadFree = Fastest(passes, () => helper.CheckAndUnloadXInputLibrarry(game, false));
				// Not loaded and wanted: every pass tries to load it.
				Assert.IsTrue(HeldLock.Finishes(() =>
				{
					loadHeld = Fastest(passes, () => helper.CheckAndLoadXInputLibrary(game, true));
					loadBytes = Allocations.FewestBytes(5, () => { for (var i = 0; i < passes; i++) helper.CheckAndLoadXInputLibrary(game, true); });
				}, 30000, enter, exit), "The passes did not finish while the display reader held the XInput lock.");
				Assert.IsFalse(Controller.IsLoaded, "The library was loaded while the display reader held the lock.");
				helper.CheckAndLoadXInputLibrary(game, true);
				Assert.IsTrue(Controller.IsLoaded, "The library was not loaded once the lock was free.");
				loadFree = Fastest(passes, () => helper.CheckAndLoadXInputLibrary(game, true));
			});
			Console.WriteLine(passes + " passes letting go: " + unloadHeld + " ms held, " + unloadFree + " ms free; loading: " + loadHeld + " ms held, " + loadFree + " ms free.");
			Assert.IsTrue(unloadHeld <= unloadFree + HeldSlackMs, passes + " passes letting go took " + unloadHeld + " ms with the lock held and " + unloadFree + " ms with it free.");
			Assert.IsTrue(loadHeld <= loadFree + HeldSlackMs, passes + " passes loading took " + loadHeld + " ms with the lock held and " + loadFree + " ms with it free.");
			Assert.IsTrue(unloadBytes < passes && loadBytes < passes, "Passes with the lock held handed the collector " + unloadBytes + " and " + loadBytes + " bytes.");
			// The third try, for a new device's effects, needs a DirectInput device; it goes through the same try.
			var dir = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput");
			var library = File.ReadAllText(Path.Combine(dir, "DInputHelper.XInputLibrarry.cs"));
			var step2 = File.ReadAllText(Path.Combine(dir, "DInputHelper.Step2.UpdateDiStates.cs"));
			Assert.AreEqual(2, Ui.Count(library, "if (!TryEnterXInputLockWhenFree())"), "Loading or letting go of the library tries the lock without asking whether it is held.");
			StringAssert.Contains(step2, "ud.DeviceEffects == null && TryEnterXInputLockWhenFree()", "Reading a new device's effects tries the lock without asking whether it is held.");
			Assert.IsFalse((library + step2).Contains("Monitor.TryEnter(Controller.XInputLock"), "The input thread tries the XInput lock directly.");
		}
	}
}
