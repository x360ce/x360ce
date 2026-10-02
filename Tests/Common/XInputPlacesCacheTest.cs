// @under-test: App.v4/Common/DInput/XInputPlaces.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;
using x360ce.App.DInput;
using static x360ce.Tests.BusRefusalFixtures;
using static x360ce.Tests.XInputPlacesNotAnsweringTest;

namespace x360ce.Tests
{
	/// <summary>
	/// The interface asks which XInput place a controller holds many times a second, while painting
	/// rows. Answering by reading the device tree from that thread froze the program: Windows asks
	/// the main window whether a device may be removed, and a thread inside the device tree cannot
	/// answer, so the removal this program itself asked for never finished. The rule pinned here is
	/// that a question never reads the machine; only an explicit read does, and only when stale.
	/// </summary>
	[TestClass]
	public class XInputPlacesCacheTest
	{
		int _reads;
		Func<DeviceInfo[]> _real;

		[TestInitialize]
		public void Count()
		{
			_reads = 0;
			_real = XInputPlaces.ReadMachine;
			XInputPlaces.ReadMachine = () => { _reads++; return new DeviceInfo[0]; };
			XInputPlaces.Invalidate();
		}

		[TestCleanup]
		public void Restore()
		{
			XInputPlaces.ReadMachine = _real;
			XInputPlaces.Invalidate();
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Asking about a place never reads the machine, however stale the answer")]
		public void A_question_never_reads_the_machine()
		{
			Assert.IsTrue(XInputPlaces.IsStale);
			XInputPlaces.PlaceFor("HID\\VID_045E&PID_028E&IG_00\\1");
			XInputPlaces.IsMadeNotPluggedIn("USB\\VID_045E&PID_028E\\1");
			XInputPlaces.IsOneOfOurs("USB\\VID_045E&PID_028E\\1");
			Assert.AreEqual(0, _reads, "A lookup read the device tree, which the interface thread must never do.");
			Assert.IsTrue(XInputPlaces.IsStale, "A lookup does not count as a read.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A stale answer is read once on request, and not again until something changes")]
		public void The_machine_is_read_once_per_change()
		{
			XInputPlaces.ReadIfStale();
			Assert.AreEqual(1, _reads);
			Assert.IsFalse(XInputPlaces.IsStale);
			XInputPlaces.ReadIfStale();
			XInputPlaces.ReadIfStale();
			Assert.AreEqual(1, _reads, "Nothing changed, so nothing is read again.");
			XInputPlaces.Invalidate();
			Assert.IsTrue(XInputPlaces.IsStale);
			XInputPlaces.ReadIfStale();
			Assert.AreEqual(2, _reads);
			XInputPlaces.Read();
			Assert.AreEqual(3, _reads, "An explicit read always reads.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The device thread's request returns at once, reads once on a worker, and keeps an invalidation that arrived meanwhile")]
		public void The_device_thread_never_waits_for_a_read()
		{
			var started = new ManualResetEventSlim();
			var release = new ManualResetEventSlim();
			XInputPlaces.ReadMachine = () => { _reads++; started.Set(); release.Wait(5000); return new DeviceInfo[0]; };
			var watch = Stopwatch.StartNew();
			XInputPlaces.ReadWhenStale();
			XInputPlaces.ReadWhenStale();
			Assert.IsTrue(watch.ElapsedMilliseconds < 200, "The request must not wait for the machine.");
			Assert.IsTrue(started.Wait(5000), "A read must have started on a worker.");
			Assert.AreEqual(1, _reads, "Two requests while one read is under way start one read.");
			// The machine changed while it was being read: the answer being built is already old.
			XInputPlaces.Invalidate();
			release.Set();
			var deadline = DateTime.UtcNow.AddSeconds(5);
			while (DateTime.UtcNow < deadline && !XInputPlaces.IsStale) Thread.Sleep(10);
			Thread.Sleep(50);
			Assert.IsTrue(XInputPlaces.IsStale, "An invalidation during a read survives it.");
			release.Set();
			XInputPlaces.ReadWhenStale();
			deadline = DateTime.UtcNow.AddSeconds(5);
			while (DateTime.UtcNow < deadline && XInputPlaces.IsStale) Thread.Sleep(10);
			Assert.IsFalse(XInputPlaces.IsStale, "The next request reads again and the answers are current.");
			Assert.AreEqual(2, _reads);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Each place table a read publishes comes with a new version, and nothing else changes the version")]
		public void Each_published_table_has_a_new_version()
		{
			XInputPlaces.ReadIfStale();
			var version = XInputPlaces.Version;
			var table = XInputPlaces.Current;
			XInputPlaces.ReadIfStale();
			Assert.AreEqual(version, XInputPlaces.Version, "The version changed with no new table.");
			XInputPlaces.Invalidate();
			Assert.AreEqual(version, XInputPlaces.Version, "The version changed when the places were marked out of date, before a new table was read.");
			Assert.AreSame(table, XInputPlaces.Current);
			XInputPlaces.ReadIfStale();
			Assert.AreEqual(version + 1, XInputPlaces.Version,
				"A new table was published with the old version, so the engine goes on passing force to where a controller was.");
			Assert.AreNotSame(table, XInputPlaces.Current, "The table read was not published.");
			// Raised after the table is replaced, so a reader that sees the new version sees the new table.
			var source = System.IO.File.ReadAllText(System.IO.Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "XInputPlaces.cs"));
			var read = source.IndexOf("public static void Read()");
			var replaced = source.IndexOf("_cache = cache;", read);
			var raised = source.IndexOf("Interlocked.Increment(ref _version)", read);
			Assert.IsTrue(read > 0 && replaced > read && raised > replaced,
				"A new version can be read with the old table, and the engine keeps the old place until the next change.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("performance")]
		[Description("The engine's lookup tries both of a device's ids, and hands nothing to the collector")]
		public void The_engine_lookup_tries_both_ids_and_makes_nothing()
		{
			var places = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
			{
				{ "HID\\VID_045E&PID_028E&IG_00\\1", 2 },
				{ "USB\\VID_045E&PID_028E\\2", 1 },
				{ "HID\\VID_045E&PID_028E&IG_00\\3", XInputPlaces.Unknown },
			};
			Assert.AreEqual(2, XInputPlaces.PlaceOf(places, "HID\\VID_045E&PID_028E&IG_00\\1", null), "The HID face's place is not found.");
			Assert.AreEqual(1, XInputPlaces.PlaceOf(places, "HID\\VID_045E&PID_028E&IG_00\\9", "USB\\VID_045E&PID_028E\\2"),
				"The device's own id is not tried when its HID face is not in the table.");
			Assert.AreEqual(1, XInputPlaces.PlaceOf(places, "HID\\VID_045E&PID_028E&IG_00\\3", "USB\\VID_045E&PID_028E\\2"),
				"A face whose place is not known hides the place the other id holds.");
			Assert.AreEqual(XInputPlaces.Unknown, XInputPlaces.PlaceOf(places, null, ""), "A device with no ids has a place.");
			const int calls = 20000;
			var sum = 0;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < calls; i++)
					sum += XInputPlaces.PlaceOf(places, "HID\\VID_045E&PID_028E&IG_00\\9", "USB\\VID_045E&PID_028E\\2");
			});
			Assert.AreEqual(5 * calls, sum);
			Assert.IsTrue(allocated < calls, calls + " lookups handed the collector " + allocated + " bytes.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("performance")]
		[Description("With the table current, the engine's check hands nothing to the collector")]
		public void A_current_table_costs_a_pass_nothing()
		{
			XInputPlaces.ReadIfStale();
			const int passes = 20000;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < passes; i++)
					XInputPlaces.ReadWhenStale();
			});
			Assert.AreEqual(1, _reads, "A current table was read again.");
			Assert.IsTrue(allocated < passes, passes + " checks handed the collector " + allocated + " bytes.");
			var source = System.IO.File.ReadAllText(System.IO.Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "XInputPlaces.cs"));
			var check = source.Substring(source.IndexOf("public static void ReadWhenStale()"));
			check = check.Substring(0, check.IndexOf("\n\t\t}"));
			Assert.IsFalse(check.Contains("lock ("), "The engine's check takes a lock on every pass.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("performance")]
		[Description("While a read is under way, the pass's repeated check also hands the collector nothing")]
		public void A_read_under_way_costs_a_pass_nothing()
		{
			var started = new ManualResetEventSlim();
			var release = new ManualResetEventSlim();
			XInputPlaces.ReadMachine = () => { _reads++; started.Set(); release.Wait(5000); return new DeviceInfo[0]; };
			try
			{
				XInputPlaces.ReadWhenStale();
				Assert.IsTrue(started.Wait(5000), "A read must have started on a worker.");
				const int passes = 20000;
				var allocated = Allocations.FewestBytes(5, () =>
				{
					for (var i = 0; i < passes; i++)
						XInputPlaces.ReadWhenStale();
				});
				Assert.AreEqual(1, _reads, "A read already under way was started again.");
				Assert.IsTrue(allocated < passes, passes + " checks handed the collector " + allocated + " bytes.");
			}
			finally
			{
				// Let the blocked read finish before the fixture swaps the machine back for the next test.
				release.Set();
				var deadline = DateTime.UtcNow.AddSeconds(5);
				while (DateTime.UtcNow < deadline && XInputPlaces.IsStale) Thread.Sleep(10);
			}
		}

		/// <summary>Asks as the device thread does on every pass, for this long or until <paramref name="done"/> answers true.</summary>
		static void Passes(int milliseconds, Func<bool> done = null)
		{
			var watch = Stopwatch.StartNew();
			while (watch.ElapsedMilliseconds < milliseconds && (done == null || !done()))
			{
				XInputPlaces.ReadWhenStale();
				Thread.Sleep(1);
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A read XInput does not answer publishes nothing and leaves the answers out of date, so they are read again")]
		public void A_read_xinput_does_not_answer_leaves_the_answers_out_of_date()
		{
			Assert.IsNotNull(DInputHelper.OccupiedPlaces(), "XInput does not answer before the test.");
			XInputPlaces.ReadIfStale();
			var version = XInputPlaces.Version;
			XInputPlaces.Invalidate();
			// Held here as a read of the places that has not come back holds it.
			Logged(() => Assert.IsTrue(HeldLock.Finishes(XInputPlaces.Read, 5000, LoadLock()),
				"Reading the machine waits for XInput past the time it has to answer."));
			Assert.AreEqual(version, XInputPlaces.Version, "A table was published while XInput did not answer.");
			Assert.IsTrue(XInputPlaces.IsStale, "A read XInput did not answer counts as current, so it is not read again until something else changes.");
			XInputPlaces.ReadIfStale();
			Assert.AreEqual(version + 1, XInputPlaces.Version, "The places are not read again once XInput answers.");
			Assert.IsFalse(XInputPlaces.IsStale);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("While XInput does not answer, the device thread's requests read the machine once per the time XInput has to answer, and it is written once")]
		public void Reads_while_xinput_does_not_answer_are_paced()
		{
			Assert.IsNotNull(DInputHelper.OccupiedPlaces(), "XInput does not answer before the test.");
			XInputPlaces.ReadIfStale();
			XInputPlaces.Invalidate();
			_reads = 0;
			var faults = 0;
			EventHandler<LogHelperEventArgs> keep = (sender, e) => { Interlocked.Increment(ref faults); e.Cancel = true; };
			LogHelper.Current.WritingException += keep;
			try
			{
				// A read of the places that has not come back holds the lock for 3.5 s. Each read gives up after 1 s, and the
				// next starts 1 s after that: at 0 and 2 s. Asked on every pass, they would start at 0, 1, 2 and 3 s.
				var lines = Logged(() => Assert.IsTrue(HeldLock.Finishes(() => Passes(3500), 6000, LoadLock()),
					"The device thread's request waits for XInput."));
				Assert.IsTrue(XInputPlaces.IsStale, "A read XInput did not answer counts as current, so it is not read again.");
				Assert.AreEqual(2, _reads, "In 3.5 s of XInput not answering the machine was read " + _reads + " times, not twice.");
				Assert.AreEqual(1, lines.Count, "Reads XInput did not answer wrote " + lines.Count + " log lines, not one.");
				StringAssert.Contains(lines[0].Key, "XInput");
				Assert.AreEqual(0, faults, "Reads XInput did not answer wrote " + faults + " fault reports.");
				// Answering again: the next paced read publishes a table.
				Passes(5000, () => !XInputPlaces.IsStale);
				Assert.IsFalse(XInputPlaces.IsStale, "The places are not read again once XInput answers.");
			}
			finally
			{
				LogHelper.Current.WritingException -= keep;
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A read that fails at once is tried again once per the time XInput has to answer, and its fault is written once until a read works")]
		public void Reads_that_fail_at_once_are_paced_and_written_once()
		{
			// A load of the library that throws fails the same way, at once, and no test can make it throw: a machine read
			// that throws stands in for it. Both publish nothing.
			const string fault = "The machine could not be read.";
			Func<DeviceInfo[]> failing = () => { _reads++; throw new InvalidOperationException(fault); };
			var faults = 0;
			EventHandler<LogHelperEventArgs> keep = (sender, e) =>
			{
				if (e.Exception == null || e.Exception.Message != fault)
					return;
				Interlocked.Increment(ref faults);
				e.Cancel = true;
			};
			LogHelper.Current.WritingException += keep;
			try
			{
				XInputPlaces.ReadMachine = failing;
				// Asked on every pass for 2.5 s: at 0, 1 and 2 s, not thousands of times.
				Passes(2500);
				Assert.IsTrue(XInputPlaces.IsStale, "A read that failed counts as current, so it is not tried again.");
				Assert.IsTrue(_reads >= 2 && _reads <= 3, "In 2.5 s of reads failing at once the machine was read " + _reads + " times, not once a second.");
				Assert.AreEqual(1, faults, _reads + " failed reads wrote " + faults + " fault reports.");
				// Read once it can be, and a failure after that is written again.
				XInputPlaces.ReadMachine = () => { _reads++; return new DeviceInfo[0]; };
				Passes(5000, () => !XInputPlaces.IsStale);
				Assert.IsFalse(XInputPlaces.IsStale, "The machine is not read again once it can be.");
				XInputPlaces.ReadMachine = failing;
				XInputPlaces.Invalidate();
				XInputPlaces.Read();
				Assert.AreEqual(2, faults, "A failure after a read that worked was not written.");
			}
			finally
			{
				LogHelper.Current.WritingException -= keep;
				// A table published, so the next test's first request is not held back.
				XInputPlaces.ReadMachine = () => new DeviceInfo[0];
				XInputPlaces.Read();
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A device tree that cannot be read still has Windows' XInput loaded by the read, so a force passed on to a real controller is sent")]
		public void A_tree_that_cannot_be_read_still_loads_xinput()
		{
			// A place no controller holds, found while the library is loaded, so the stop sent below reaches nothing.
			var free = SharedForcePassThroughTest.FreePlace();
			// A table published, so the fault below is the first since one was.
			XInputPlaces.Read();
			var helper = new DInputHelper();
			var passForceTo = typeof(DInputHelper).GetMethod("PassForceTo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			const string fault = "The device tree could not be read.";
			var faults = 0;
			EventHandler<LogHelperEventArgs> keep = (sender, e) =>
			{
				if (e.Exception == null || e.Exception.Message != fault)
					return;
				Interlocked.Increment(ref faults);
				e.Cancel = true;
			};
			// Not loaded, as at start or after a reorder. The input thread never loads it: a read of the places does.
			Assert.IsTrue(SystemXInput.Release(TimeSpan.FromSeconds(1)), "The library could not be let go of before the test.");
			LogHelper.Current.WritingException += keep;
			try
			{
				XInputPlaces.ReadMachine = () => { _reads++; throw new InvalidOperationException(fault); };
				XInputPlaces.Read();
				XInputPlaces.Read();
				Assert.IsNotNull(SystemXInput.LibraryPath, "Windows' XInput is never loaded while the device tree cannot be read.");
				Assert.AreEqual(true, passForceTo.Invoke(helper, new object[] { free, (byte)0, (byte)0, TimeSpan.Zero }),
					"The input thread never sends a force passed on to a real controller while the device tree cannot be read.");
				Assert.AreEqual(1, faults, "Two reads of a device tree that cannot be read wrote " + faults + " fault reports, not one.");
			}
			finally
			{
				LogHelper.Current.WritingException -= keep;
				// A table published, so the next test's first request is not held back.
				XInputPlaces.ReadMachine = () => new DeviceInfo[0];
				XInputPlaces.Read();
			}
			// Each read asks for the places once, before the tree, and works the table out from that answer.
			var source = System.IO.File.ReadAllText(System.IO.Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "XInputPlaces.cs"));
			var read = Ui.Between(source, "public static void Read()", "lock (SyncRoot)");
			Assert.AreEqual(1, Ui.Count(read, "OccupiedPlaces("), "A read asks XInput which places are taken more than once, or not at all.");
			Assert.IsTrue(read.IndexOf("OccupiedPlaces(") < read.IndexOf("ReadMachine()"),
				"The places are asked only once the device tree is read, so a tree that cannot be read leaves XInput never loaded.");
		}
	}
}
