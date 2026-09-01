// @under-test: App.v4/Common/DInput/VirtualDriverInstaller.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// Measures whether XInput places can be reordered on purpose, before anything is built on the
	/// idea that they can.
	/// </summary>
	/// <remarks>
	/// Reordering rests on one belief: that taking a controller away and bringing it back puts it at
	/// the end of the queue, because a place is handed out when a device arrives and there is no
	/// other way to ask for one. The belief is reasonable and it is not written down anywhere by the
	/// people who wrote XInput. Three things would have to be true, and this measures all three
	/// rather than assuming them:
	///
	///   1. A place that is given up is still free a moment later. With more controllers than places,
	///      some are waiting without one, and if Windows hands the empty place straight to a waiting
	///      controller then reordering loses a race it never knew it was in.
	///   2. Taking one away and bringing it back takes a knowable length of time. Every later decision
	///      about how long to wait comes from this number.
	///   3. Watching which place fills up says which controller filled it. That is the whole mapping
	///      from controller to place, for nothing, if it holds.
	///
	/// This is a measurement, not a test. It asserts almost nothing and prints what it saw. It runs
	/// only when somebody asks it to, because it switches a real controller off and on again.
	/// </remarks>
	[TestClass]
	public class SlotReorderSpike
	{
		/// <summary>Where the intent to disable a device is written before the device is touched.</summary>
		/// <remarks>
		/// A controller switched off by a program that then stops running is a controller the person
		/// has to find and switch on again themselves, in a window they never opened, with nothing
		/// anywhere saying who did it. Writing the intent down first means the next run can put it
		/// back even if this one never reaches its own tidying up.
		/// </remarks>
		static string PendingFile
		{
			get { return Path.Combine(Path.GetTempPath(), "x360ce-slot-spike-pending.txt"); }
		}

		static void RequireOptIn()
		{
			if (Environment.GetEnvironmentVariable("QA_ALLOW_ELEVATION") != "1")
				Assert.Inconclusive("Skipped: switches a real controller off and on, and needs "
					+ "elevation. Set QA_ALLOW_ELEVATION=1 to opt in.");
			if (!WinAPI.IsElevated())
				Assert.Inconclusive("Skipped: switching a device off needs Administrator.");
		}

		/// <summary>Which of the four places report a controller right now.</summary>
		static bool[] Occupied()
		{
			var places = new bool[4];
			for (var i = 0; i < 4; i++)
			{
				State state;
				places[i] = Controller.XInputGetState(i, out state) == 0;
			}
			return places;
		}

		static string Show(bool[] places)
		{
			return string.Join(" ", Enumerable.Range(0, 4)
				.Select(i => string.Format("{0}:{1}", i + 1, places[i] ? "taken" : "free")).ToArray());
		}

		/// <summary>Waits until the places look different, and says how long that took.</summary>
		static TimeSpan WaitForChange(bool[] from, TimeSpan limit, out bool[] to)
		{
			var started = DateTime.UtcNow;
			while (DateTime.UtcNow - started < limit)
			{
				to = Occupied();
				if (!to.SequenceEqual(from))
					return DateTime.UtcNow - started;
				Thread.Sleep(100);
			}
			to = Occupied();
			return limit;
		}

		[TestMethod, TestCategory("devices"), TestCategory("requires-elevation")]
		[Description("Measures whether a freed XInput place stays free, and for how long")]
		public void Can_a_place_be_given_up_and_taken_back_on_purpose()
		{
			RequireOptIn();
			RestoreAnythingLeftDisabled();

			var all = DeviceDetector.GetDevices(null, DIGCF.DIGCF_ALLCLASSES | DIGCF.DIGCF_PRESENT);
			var byId = all.ToDictionary(x => x.DeviceId, x => x, StringComparer.OrdinalIgnoreCase);
			// Only controllers XInput can see, and only ones this program did not make. A device the
			// bus made can be taken away without asking anybody, so it proves nothing about the case
			// that matters, which is a controller somebody plugged in.
			var real = all
				.Where(x => VirtualDriverInstaller.CarriesInputGroup(x.HardwareIds)
					|| VirtualDriverInstaller.CarriesInputGroup(x.DeviceId))
				.Where(x => !VirtualDriverInstaller.IsVirtualPad(x, byId))
				.ToList();

			var before = Occupied();
			Console.WriteLine("places at rest      : {0}", Show(before));
			Console.WriteLine("real controllers    : {0}", real.Count);
			foreach (var d in real)
				Console.WriteLine("    {0}", d.DeviceId);

			if (real.Count == 0)
				Assert.Inconclusive("No real XInput controller is plugged in, so there is nothing to move.");
			if (!before.Any(x => x))
				Assert.Inconclusive("XInput reports no controller at all, so there is no place to give up.");

			var victim = real[0];
			Console.WriteLine();
			Console.WriteLine("switching off       : {0}", victim.DeviceId);

			bool[] afterOff;
			var offTook = TimeSpan.Zero;
			Remember(victim.DeviceId);
			try
			{
				DeviceDetector.SetDeviceState(victim.DeviceId, false);
				offTook = WaitForChange(before, TimeSpan.FromSeconds(15), out afterOff);
				Console.WriteLine("places after off    : {0}   after {1:0.0}s", Show(afterOff), offTook.TotalSeconds);

				// Question one. A place given up should still be free a moment later. If something
				// else has taken it, reordering cannot work on a machine with controllers waiting.
				Thread.Sleep(3000);
				var settled = Occupied();
				Console.WriteLine("places 3s later     : {0}", Show(settled));
				var stolen = Enumerable.Range(0, 4).Where(i => before[i] && !afterOff[i] && settled[i]).ToList();
				Console.WriteLine("place taken by something else while free: {0}",
					stolen.Count == 0 ? "no" : string.Join(",", stolen.Select(i => (i + 1).ToString()).ToArray()));
			}
			finally
			{
				Console.WriteLine();
				Console.WriteLine("switching back on   : {0}", victim.DeviceId);
				DeviceDetector.SetDeviceState(victim.DeviceId, true);
				bool[] afterOn;
				var onTook = WaitForChange(Occupied(), TimeSpan.FromSeconds(30), out afterOn);
				Thread.Sleep(2000);
				afterOn = Occupied();
				Console.WriteLine("places after on     : {0}   after {1:0.0}s", Show(afterOn), onTook.TotalSeconds);
				Forget();

				// Question three. Exactly one place should have changed, and it says where the
				// controller went. More than one and watching cannot tell them apart.
				var gained = Enumerable.Range(0, 4).Where(i => afterOn[i]).ToList();
				Console.WriteLine();
				Console.WriteLine("SUMMARY");
				Console.WriteLine("  giving up a place took   : {0:0.0}s", offTook.TotalSeconds);
				Console.WriteLine("  getting it back took     : {0:0.0}s", onTook.TotalSeconds);
				Console.WriteLine("  places taken at the end  : {0}", string.Join(",",
					gained.Select(i => (i + 1).ToString()).ToArray()));
				Console.WriteLine("  same as at the start     : {0}", Occupied().SequenceEqual(before) ? "yes" : "NO");
			}

			// Nothing is asserted about the ordering. The numbers above are the point, and a machine
			// that answers differently is telling the truth about itself rather than failing.
			Assert.IsTrue(Occupied().Any(x => x),
				"The controller was switched off and never came back. Check Device Manager: it may "
				+ "still be disabled, and the next run of this test will switch it back on.");
		}

		static void Remember(string deviceId)
		{
			try { File.WriteAllText(PendingFile, deviceId); }
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
		}

		static void Forget()
		{
			try { if (File.Exists(PendingFile)) File.Delete(PendingFile); }
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
		}

		/// <summary>Switches back on anything an earlier run switched off and never restored.</summary>
		static void RestoreAnythingLeftDisabled()
		{
			string deviceId = null;
			try { if (File.Exists(PendingFile)) deviceId = File.ReadAllText(PendingFile).Trim(); }
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
			if (string.IsNullOrEmpty(deviceId))
				return;
			Console.WriteLine("an earlier run left {0} switched off; switching it back on", deviceId);
			DeviceDetector.SetDeviceState(deviceId, true);
			Forget();
		}
	}
}
