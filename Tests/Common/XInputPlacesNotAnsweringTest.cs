// @under-test: App.v4/Common/DInput/SystemXInput.cs, App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs, App.v4/Common/DInput/XInputReorderRunner.cs, App.v4/Common/DInput/XInputPlaces.cs
// @area: engine   @layer: unit
using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using x360ce.App;
using x360ce.App.DInput;
using static x360ce.Tests.BusRefusalFixtures;

namespace x360ce.Tests
{
	/// <summary>Asking Windows' XInput which places are taken waits no longer than XInput has to answer.</summary>
	/// <remarks>
	/// XInput stops answering while Windows takes controllers away and builds them again, which putting controllers in
	/// order does on purpose. A question that waits for ever stops the order half way, with the controllers unfed and real
	/// ones switched off. The places are read under the library's load lock, so holding that lock on another thread is a
	/// read that does not come back.
	/// </remarks>
	[TestClass]
	public class XInputPlacesNotAnsweringTest
	{
		internal static object LoadLock()
		{
			return typeof(SystemXInput).GetField("LoadLock", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A read of the places gives up after the time it was given while XInput does not answer, and answers once it does")]
		public void A_places_read_gives_up_while_xinput_does_not_answer()
		{
			var places = new bool[4];
			var answered = new bool[2];
			var took = new long[2];
			Assert.IsTrue(HeldLock.Finishes(() =>
			{
				// The second question finds the first read still out, and waits for it rather than starting another.
				for (var i = 0; i < 2; i++)
				{
					var watch = Stopwatch.StartNew();
					answered[i] = SystemXInput.ReadPlaces(places, TimeSpan.FromMilliseconds(200));
					took[i] = watch.ElapsedMilliseconds;
				}
			}, 3000, LoadLock()), "Asking which places are taken waits for a read that does not come back.");
			for (var i = 0; i < 2; i++)
			{
				Assert.IsFalse(answered[i], "A read that did not come back was taken as an answer.");
				Assert.IsTrue(took[i] >= 150 && took[i] < 1000, "Asking gave up after " + took[i] + " ms, not after the 200 ms it was given.");
			}
			Assert.IsTrue(SystemXInput.ReadPlaces(places, TimeSpan.FromSeconds(1)), "The places are not read once XInput answers again.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A question asked while another waits for a read that has not come back gives up within its own time, not after both")]
		public void A_second_question_waits_no_longer_than_it_was_given()
		{
			var limit = TimeSpan.FromMilliseconds(400);
			long took = -1;
			Assert.IsTrue(HeldLock.Finishes(() =>
			{
				var first = System.Threading.Tasks.Task.Run(() => SystemXInput.ReadPlaces(new bool[4], limit));
				System.Threading.Thread.Sleep(100);
				var watch = Stopwatch.StartNew();
				SystemXInput.ReadPlaces(new bool[4], limit);
				took = watch.ElapsedMilliseconds;
				first.Wait();
			}, 3000, LoadLock()), "Asking which places are taken waits for a read that does not come back.");
			Assert.IsTrue(took < 550, "A question asked behind another gave up after " + took + " ms, not after the 400 ms it was given.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A load of the library that throws while the places are read is caught narrowly, written once, and read as XInput not answering")]
		public void A_failed_load_in_the_places_reader_reads_as_not_answering()
		{
			// Loading lists the system folder, which can fail, and nothing a test may do makes it fail: the shape is read.
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "SystemXInput.cs"));
			var loop = Ui.Between(source, "static void ReadPlacesLoop()", "static void NotePlacesLoadFault(");
			StringAssert.Contains(loop, "catch (IOException ex) { NotePlacesLoadFault(ex); }", "A failed load ends the places reader.");
			StringAssert.Contains(loop, "catch (UnauthorizedAccessException ex) { NotePlacesLoadFault(ex); }", "A failed load ends the places reader.");
			Assert.IsTrue(loop.LastIndexOf("catch (") < loop.IndexOf("PlacesAnswered.Set();"),
				"A read that ends in a fault leaves every later question waiting.");
			StringAssert.Contains(Ui.Between(source, "public static bool ReadPlaces(", "static void ReadPlacesLoop()"),
				"if (_placesLoadFault != null)", "A read that could not load the library is taken as an answer.");
			StringAssert.Contains(Ui.Between(source, "static void NotePlacesLoadFault(", "#endregion"),
				"if (_placesLoadFault == null)", "A failed load is written on every read, not once.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A read of the places that throws something unexpected is written once, read as XInput not answering, and read again by the next question")]
		public void An_unexpected_fault_in_the_places_reader_reads_as_not_answering()
		{
			var places = new bool[4];
			var limit = TimeSpan.FromSeconds(1);
			Assert.IsTrue(SystemXInput.ReadPlaces(places, limit), "XInput does not answer before the test.");
			// A driver that faults inside the read stands in for anything the reader does not expect.
			var field = typeof(SystemXInput).GetField("_GetState", BindingFlags.NonPublic | BindingFlags.Static);
			var loaded = field.GetValue(null);
			var faulting = Expression.Lambda(field.FieldType,
				Expression.Throw(Expression.New(typeof(InvalidOperationException)), typeof(int)),
				field.FieldType.GetMethod("Invoke").GetParameters().Select(x => Expression.Parameter(x.ParameterType))).Compile();
			var faults = new List<Exception>();
			EventHandler<LogHelperEventArgs> keep = (sender, e) => { faults.Add(e.Exception); e.Cancel = true; };
			var answered = true;
			LogHelper.Current.WritingException += keep;
			try
			{
				lock (LoadLock())
					field.SetValue(null, faulting);
				answered = SystemXInput.ReadPlaces(places, limit);
			}
			finally
			{
				LogHelper.Current.WritingException -= keep;
				lock (LoadLock())
					if (field.GetValue(null) == faulting)
						field.SetValue(null, loaded);
			}
			Assert.IsFalse(answered, "A read that threw was taken as an answer.");
			Assert.AreEqual(1, faults.Count, "A read that threw wrote " + faults.Count + " fault reports, not one.");
			Assert.IsInstanceOfType(faults[0], typeof(InvalidOperationException), "What the read threw was not written.");
			// The reader lived through it: a reader that ended answers nothing again.
			Assert.IsTrue(SystemXInput.ReadPlaces(places, limit), "The places are not read after a read that threw.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A plug whose place XInput never says during the whole wait is put down to XInput, not to the bus")]
		public void A_place_wait_xinput_never_answers_is_not_blamed_on_the_bus()
		{
			var wait = typeof(DInputHelper).GetMethod("WaitForPlace", BindingFlags.NonPublic | BindingFlags.Static, null,
				new[] { typeof(bool[]), typeof(bool).MakeByRefType() }, null);
			Assert.IsNotNull(wait, "The wait for a place does not say whether XInput answered.");
			var silent = new object[] { new bool[4], true };
			object place = null;
			Logged(() => Assert.IsTrue(HeldLock.Finishes(() => place = wait.Invoke(null, silent), 8000, LoadLock()),
				"The wait for a place waits for a read that does not come back."));
			Assert.AreEqual(-1, place, "A place was found while XInput did not answer.");
			Assert.AreEqual(false, silent[1], "A wait XInput never answered was taken as answered.");
			var heard = new object[] { new bool[4], false };
			wait.Invoke(null, heard);
			Assert.AreEqual(true, heard[1], "A wait XInput answered was taken as not answered.");
			// Taken away again, and XInput named instead of the bus, before the bus is blamed for a place not given.
			var step5 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			var enable = Ui.Between(step5, "public VirtualError EnableFeeding(", "public VirtualError DisableFeeding(");
			var notAnswered = enable.IndexOf("!answered ? VirtualError.NotAnswering : place < 0 ? VirtualError.PlaceNotGiven");
			Assert.IsTrue(notAnswered > enable.IndexOf("client.UnPlug(userIndex);"), "A plug whose place XInput never said is blamed on the bus.");
			Assert.AreEqual(0, DInputHelper.NextPlugFailures(0, VirtualError.NotAnswering), "XInput not answering is counted against the bus.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Reading the places makes nothing")]
		public void Each_places_read_makes_nothing()
		{
			var places = new bool[4];
			var limit = TimeSpan.FromSeconds(1);
			// The first question makes the reader.
			Assert.IsTrue(SystemXInput.ReadPlaces(places, limit));
			const int reads = 1000;
			var allocated = Allocations.FewestBytes(3, () =>
			{
				for (var i = 0; i < reads; i++)
					SystemXInput.ReadPlaces(places, limit);
			});
			Assert.IsTrue(allocated < reads, reads + " reads of the places handed the collector " + allocated + " bytes.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("XInput not saying which places are taken is written to the log once, and again after it has answered")]
		public void Places_not_answering_is_written_once_per_change()
		{
			// Answered first, so what an earlier test left behind does not decide what this one sees.
			Assert.IsNotNull(DInputHelper.OccupiedPlaces(), "XInput does not answer before the test.");
			bool[] first = null, second = null;
			var lines = Logged(() => Assert.IsTrue(HeldLock.Finishes(() =>
			{
				first = DInputHelper.OccupiedPlaces();
				second = DInputHelper.OccupiedPlaces();
			}, 5000, LoadLock()), "Asking which places are taken waits for a read that does not come back."));
			Assert.IsNull(first, "A read that did not come back was taken as an answer.");
			Assert.IsNull(second, "A read that did not come back was taken as an answer.");
			Assert.AreEqual(1, lines.Count, "Two unanswered questions wrote " + lines.Count + " log lines.");
			StringAssert.Contains(lines[0].Key, "XInput");
			Assert.AreEqual(TraceLevel.Warning, lines[0].Value);
			Assert.IsNotNull(DInputHelper.OccupiedPlaces(), "The places are not read once XInput answers again.");
			lines = Logged(() => Assert.IsTrue(HeldLock.Finishes(() => DInputHelper.OccupiedPlaces(), 5000, LoadLock()),
				"Asking which places are taken waits for a read that does not come back."));
			Assert.AreEqual(1, lines.Count, "XInput not answering after it had answered was not written again.");
			// Left answering, so the next test starts from the same place this one did.
			Assert.IsNotNull(DInputHelper.OccupiedPlaces(), "The places are not read once XInput answers again.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Nothing that makes or orders controllers asks XInput without a limit, and the order stops before touching anything when XInput does not answer")]
		public void Putting_controllers_in_order_never_waits_for_xinput()
		{
			var app = Path.Combine(Ui.RepoRoot.FullName, "App.v4");
			var direct = Directory.GetFiles(app, "*.cs", SearchOption.AllDirectories)
				.Where(x => File.ReadAllText(x).Contains("SystemXInput.IsConnected("))
				.Select(Path.GetFileName).ToArray();
			// Every read goes through the reader, which holds the load lock, so nothing is let go of under one.
			Assert.AreEqual(0, direct.Length, "These ask XInput which places are taken outside the load lock: " + string.Join(", ", direct));
			var runner = File.ReadAllText(Path.Combine(app, "Common", "DInput", "XInputReorderRunner.cs"));
			var run = Ui.Between(runner, "public bool Run(XInputReorderPlan plan)", "bool Fail(string why)");
			var start = run.IndexOf("var start = DInputHelper.OccupiedPlaces(StopLimit);");
			var refused = run.IndexOf("if (start == null)", Math.Max(start, 0));
			var stop = run.IndexOf("helper.StopForReorder(StopLimit)");
			Assert.IsTrue(start > 0 && refused > start && stop > refused,
				"The order switches controllers off before it knows XInput answers.");
			var step5 = File.ReadAllText(Path.Combine(app, "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			StringAssert.Contains(Ui.Between(step5, "public VirtualError EnableFeeding(", "public VirtualError DisableFeeding("),
				"return VirtualError.NotAnswering;", "A controller is plugged in with no knowledge of which places are taken.");
			var pass = Ui.Between(step5, "void UpdateVirtualDevices(UserGame game)", "public System.Threading.Tasks.Task<VirtualError> BeginPlug(");
			Assert.IsFalse(pass.Contains("OccupiedPlaces") || pass.Contains("ReadPlaces"), "The input thread asks XInput which places are taken.");
			StringAssert.Contains(Ui.Between(step5, "public VirtualError EnableFeeding(", "public VirtualError DisableFeeding("),
				"PlacesMask(before) == misplacedWith", "A controller put in the wrong place is made again with no change in the places.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Putting controllers in order while XInput does not answer is not started, touches nothing, and says only that")]
		public void An_order_xinput_does_not_answer_is_not_started()
		{
			if (Global.DHelper == null)
				Global.InitDHelperHelper();
			var helper = Global.DHelper;
			// Only a step that takes this program's own away, so no controller has to be made and the read of the places comes
			// first. Nothing past the read switches a real controller or starts a copy as Administrator, even if it were reached.
			var plan = new XInputReorderPlan();
			plan.Steps.Add(new XInputReorderPlan.Step { Kind = XInputReorderPlan.StepKind.RemoveVirtual, Pad = 1 });
			var runner = new XInputReorderRunner();
			var ran = true;
			// The read before anything is touched waits as long as letting go of the controllers does: ten seconds.
			Logged(() => Assert.IsTrue(HeldLock.Finishes(() => ran = runner.Run(plan), 15000, LoadLock()),
				"Putting controllers in order waits for a read that does not come back."));
			Assert.IsNotNull(DInputHelper.OccupiedPlaces(), "The places are not read once XInput answers again.");
			Assert.IsFalse(ran, "Controllers were put in order while XInput did not answer.");
			Assert.IsFalse(runner.Started, "Controllers were touched while XInput did not answer.");
			Assert.IsFalse(helper.Suspended, "Reading the controllers was stopped while XInput did not answer.");
			Assert.AreEqual("XInput is not answering, so nothing was changed. Try again in a moment.", runner.ToString().TrimEnd(),
				"The report of an order that was not started says more than why.");
			var page = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Controls", "XInputDevicesUserControl.cs"));
			StringAssert.Contains(page, "runner.Started ? \"Stopped part way\" : \"Not started\"",
				"An order that was not started is shown as stopped part way.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("While the loop is suspended for an order nothing asks XInput which places are taken, so nothing loads it again")]
		public void Reads_of_the_places_are_not_made_while_the_loop_is_suspended()
		{
			if (Global.DHelper == null)
				Global.InitDHelperHelper();
			var helper = Global.DHelper;
			helper.Suspended = true;
			try
			{
				Assert.IsNull(DInputHelper.OccupiedPlaces(), "XInput was asked which places are taken while controllers were put in order.");
			}
			finally
			{
				helper.Suspended = false;
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("The pass never enters the check for a controller put in the wrong place: only the plug worker runs it")]
		public void The_misplaced_check_runs_only_on_the_plug_worker()
		{
			var app = Path.Combine(Ui.RepoRoot.FullName, "App.v4");
			var step5 = File.ReadAllText(Path.Combine(app, "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			// What the pass runs of this file, up to the plug it only starts.
			var pass = Ui.Between(step5, "void UpdateVirtualDevices(UserGame game)", "public System.Threading.Tasks.Task<VirtualError> BeginPlug(");
			foreach (var part in new[] { "PlacesUnchanged", "PlacesMask(", "OccupiedPlaces", "ReadPlaces", "IsConnected(", "EnableFeeding(" })
				Assert.IsFalse(pass.Contains(part), "The pass itself reaches " + part);
			// Starting a plug hands the check to a worker, with the places the controller was put elsewhere with.
			var plug = Ui.Between(step5, "public System.Threading.Tasks.Task<VirtualError> BeginPlug(", "bool PlugUnderWay(");
			StringAssert.Contains(plug, "Task.Run(() => EnableFeeding(userIndex, misplacedWith))",
				"The check for a controller put in the wrong place is not run on the plug worker.");
			Assert.IsFalse(plug.Contains("OccupiedPlaces") || plug.Contains("PlacesMask("), "Starting a plug asks XInput which places are taken.");
			Assert.IsFalse(step5.Contains("PlacesUnchanged"), "A second way of checking the places is left for the pass to call.");
			// The check is made in one place, which only the plug worker reaches.
			Assert.AreEqual(1, Ui.Count(step5, "PlacesMask(before) == misplacedWith"), "The check for a controller put in the wrong place is not made once.");
			var callers = Directory.GetFiles(app, "*.cs", SearchOption.AllDirectories)
				.Where(x => File.ReadAllText(x).Contains("EnableFeeding("))
				.Select(Path.GetFileName).ToArray();
			CollectionAssert.AreEquivalent(new[] { "DInputHelper.Step5.VirtualDevices.cs" }, callers,
				"These make a controller: " + string.Join(", ", callers));
			Assert.AreEqual(2, Ui.Count(step5, "EnableFeeding("), "Something other than the plug worker makes a controller.");
			// The order made by the reorder worker asks XInput nothing while it plugs.
			var plugForOrder = Ui.Between(step5, "public VirtualError PlugForOrder(", "public VirtualError DisableFeeding(");
			Assert.IsFalse(plugForOrder.Contains("OccupiedPlaces") || plugForOrder.Contains("ReadPlaces"),
				"A controller made for the order asks XInput which places are taken.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A controller put in the wrong place is held back before the bus is asked whether it still holds it")]
		public void A_misplaced_controller_the_bus_kept_is_not_fed()
		{
			// The bus can refuse to let go of a controller put in the wrong place. It then still reads as connected, and taken as
			// made it would be fed in another tab's place. Deciding needs the bus, which no test may reach: the order is read.
			var step5 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			var enable = Ui.Between(step5, "public VirtualError EnableFeeding(", "public VirtualError DisableFeeding(");
			var kept = enable.IndexOf("if (client.IsControllerConnected(userIndex))");
			var check = enable.IndexOf("PlacesMask(before) == misplacedWith");
			Assert.IsTrue(kept > 0 && check > 0 && check < kept,
				"A controller put in the wrong place that the bus would not let go of is fed in another tab's place.");
			var unknown = enable.IndexOf("return VirtualError.NotAnswering;");
			Assert.IsTrue(unknown > 0 && unknown < kept,
				"A controller put in the wrong place is fed while XInput does not say whether the places changed.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("The place table reads XInput only under the load lock, and gives up within the time XInput has to answer while it is held")]
		public void The_place_table_reads_only_under_the_load_lock()
		{
			Dictionary<string, int> table = new Dictionary<string, int>();
			long took = -1;
			// Held here as the reorder holds it while it lets go of the library.
			Logged(() => Assert.IsTrue(HeldLock.Finishes(() =>
			{
				var watch = Stopwatch.StartNew();
				table = XInputPlaces.Resolve(new DeviceInfo[0], new Dictionary<string, DeviceInfo>());
				took = watch.ElapsedMilliseconds;
			}, 5000, LoadLock()), "The place table waits for a library being let go of."));
			Assert.IsNull(table, "The place table read XInput while the library was being let go of.");
			Assert.IsTrue(took < 3 * DInputHelper.XiAnswerMs, "The place table gave up after " + took + " ms.");
			Assert.IsNotNull(XInputPlaces.Resolve(new DeviceInfo[0], new Dictionary<string, DeviceInfo>()),
				"The place table does not read XInput once the lock is free.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Vibration is sent only under the load lock: a sender gives up within its limit while the lock is held, and the input thread does not wait at all")]
		public void Vibration_is_sent_only_under_the_load_lock()
		{
			var helper = new DInputHelper();
			var passForceTo = typeof(DInputHelper).GetMethod("PassForceTo", BindingFlags.NonPublic | BindingFlags.Instance);
			var last = (int[])typeof(DInputHelper).GetField("_lastPassedForce", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(helper);
			bool? sent = true;
			long took = -1;
			long engineTook = -1;
			object engineSent = null;
			// Not loaded, so a send that loaded the library before it asked for the lock would wait for the lock the load takes.
			Assert.IsTrue(SystemXInput.Release(TimeSpan.FromSeconds(1)), "The library could not be let go of before the test.");
			Assert.IsTrue(HeldLock.Finishes(() =>
			{
				var watch = Stopwatch.StartNew();
				sent = SystemXInput.SetVibration(0, 0, 0, TimeSpan.FromMilliseconds(200));
				took = watch.ElapsedMilliseconds;
				watch.Restart();
				engineSent = passForceTo.Invoke(helper, new object[] { 0, (byte)0, (byte)0, TimeSpan.Zero });
				engineTook = watch.ElapsedMilliseconds;
			}, 3000, LoadLock()), "Sending vibration waits for a library being let go of.");
			Assert.IsNull(sent, "Vibration was sent while the library was being let go of.");
			Assert.IsTrue(took >= 150 && took < 1000, "Sending gave up after " + took + " ms, not after the 200 ms it was given.");
			Assert.AreEqual(false, engineSent, "The input thread says it sent a force it could not send.");
			Assert.IsTrue(engineTook < 50, "The input thread waited " + engineTook + " ms for the load lock.");
			Assert.AreEqual(-1, last[0], "A force not sent is recorded as sent, so it is never sent.");
			// Free again, but let go of: the input thread leaves loading the library to the places reader.
			Assert.AreEqual(false, passForceTo.Invoke(helper, new object[] { 0, (byte)0, (byte)0, TimeSpan.Zero }), "The input thread sent a force it had to load the library for.");
			Assert.IsNull(SystemXInput.LibraryPath, "The input thread loaded the library.");
			Assert.IsNotNull(DInputHelper.OccupiedPlaces(), "The places reader did not load the library.");
			// Loaded: sent, and recorded, so the same force is not sent again.
			Assert.AreEqual(true, passForceTo.Invoke(helper, new object[] { 0, (byte)0, (byte)0, TimeSpan.Zero }));
			Assert.AreEqual(0, last[0], "A force sent is not recorded.");
		}
	}
}
