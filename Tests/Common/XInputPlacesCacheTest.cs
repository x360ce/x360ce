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
	}
}
