// @under-test: App.v4/Common/DInput/XInputPlaces.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;
using x360ce.App.DInput;

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
	}
}
