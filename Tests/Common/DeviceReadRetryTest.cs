// @under-test: App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs, App.v4/Common/DInput/DInputHelper.Step1.UpdateDevices.cs, Engine/Data/UserDevice.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using x360ce.App.DInput;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>A device the engine cannot read, poll after poll.</summary>
	/// <remarks>
	/// A device mid-reset answers E_FAIL to Acquire and is back on the next poll, so one failure is tried
	/// again at once and is not reported (<see cref="BenignDeviceResultTest"/>). Without a rest, a device
	/// that keeps failing is asked on every poll, up to a thousand times a second, each time through an
	/// exception and three native calls, and a fault would be reported on every one. After two failures
	/// in a row the device rests between tries, and a fault is reported once for the run.
	/// </remarks>
	[TestClass]
	public class DeviceReadRetryTest
	{
		const int Now = 1000;

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A device that fails once is read again on the next poll, and that failure is the one reported")]
		public void One_failure_is_tried_again_at_once()
		{
			var ud = new UserDevice();
			Assert.IsFalse(DInputHelper.IsDeviceReadResting(ud, Now), "A device that never failed is resting.");
			Assert.IsTrue(DInputHelper.CountDeviceReadFailure(ud, Now, false), "The first failure of a run is the one to report.");
			Assert.IsFalse(DInputHelper.IsDeviceReadResting(ud, Now + 1), "One failure stopped the next poll; a device mid-reset comes back on it.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A device that keeps failing rests between tries and is reported once")]
		public void A_device_that_keeps_failing_rests_and_is_reported_once()
		{
			var ud = new UserDevice();
			DInputHelper.CountDeviceReadFailure(ud, Now, false);
			Assert.IsFalse(DInputHelper.CountDeviceReadFailure(ud, Now + 1, false), "The second failure in a row was reported again.");
			Assert.IsTrue(DInputHelper.IsDeviceReadResting(ud, Now + 2), "A device that failed twice in a row is read again at once.");
			Assert.IsTrue(DInputHelper.IsDeviceReadResting(ud, Now + DInputHelper.DeviceReadRetryMs), "The rest ended early.");
			Assert.IsFalse(DInputHelper.IsDeviceReadResting(ud, Now + 1 + DInputHelper.DeviceReadRetryMs), "The rest never ends.");
			for (var i = 0; i < 100; i++)
				Assert.IsFalse(DInputHelper.CountDeviceReadFailure(ud, Now + i, false), "A later failure of the same run was reported.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A fault that follows a device condition in the same run is reported, once")]
		public void A_fault_after_a_device_condition_is_reported_once()
		{
			var ud = new UserDevice();
			Assert.IsFalse(DInputHelper.CountDeviceReadFailure(ud, Now, true), "A device condition was reported.");
			Assert.IsTrue(DInputHelper.CountDeviceReadFailure(ud, Now + 1, false), "A fault after a device condition in the same run was not reported.");
			Assert.IsFalse(DInputHelper.CountDeviceReadFailure(ud, Now + 2, false), "The run's fault was reported again.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A read that works ends the run, so the next failure is reported and tried again at once")]
		public void A_read_ends_the_run()
		{
			var ud = new UserDevice();
			DInputHelper.CountDeviceReadFailure(ud, Now, false);
			DInputHelper.CountDeviceReadFailure(ud, Now, false);
			// What the engine does after a good GetCurrentState, and at the end of a poll that failed nothing.
			ud.DiReadFailures = 0;
			ud.DiReadFaultReported = false;
			Assert.IsFalse(DInputHelper.IsDeviceReadResting(ud, Now + 1), "A device that was read is still resting.");
			Assert.IsTrue(DInputHelper.CountDeviceReadFailure(ud, Now + 2, false), "A new run of failures was not reported.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A rest that starts just before the tick count wraps ends on time after it")]
		public void A_rest_across_the_wrap_ends_on_time()
		{
			var ud = new UserDevice();
			var now = int.MaxValue - 100;
			DInputHelper.CountDeviceReadFailure(ud, now, false);
			DInputHelper.CountDeviceReadFailure(ud, now, false);
			Assert.IsTrue(DInputHelper.IsDeviceReadResting(ud, now + 1), "The rest ended before the wrap.");
			Assert.IsTrue(DInputHelper.IsDeviceReadResting(ud, unchecked(now + 200)), "The rest ended at the wrap.");
			Assert.IsFalse(DInputHelper.IsDeviceReadResting(ud, unchecked(now + DInputHelper.DeviceReadRetryMs)), "The rest outlived the wrap.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("performance")]
		[Description("Deciding whether to read a device hands nothing to the collector")]
		public void Deciding_hands_nothing_to_the_collector()
		{
			var ud = new UserDevice();
			const int calls = 20000;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < calls; i++)
					if (!DInputHelper.IsDeviceReadResting(ud, i))
						DInputHelper.CountDeviceReadFailure(ud, i, false);
			});
			Assert.IsTrue(allocated < calls,
				"Deciding " + calls + " times handed the collector " + allocated + " bytes; it runs for every device on every poll.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The engine skips a resting device, ends the run on a good read, and reports a fault once a run")]
		public void The_engine_reads_through_the_rule()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step2.UpdateDiStates.cs"));
			StringAssert.Contains(source, "if (device != null && !IsDeviceReadResting(ud, Environment.TickCount))",
				"The engine asks a device that keeps failing on every poll.");
			var read = source.IndexOf("device.GetCurrentState(ref reading);");
			var reset = source.IndexOf("ud.DiReadFailures = 0;");
			Assert.IsTrue(read > 0 && reset > read, "A good read does not end the run of failures.");
			var named = source.IndexOf("state = reading;");
			Assert.IsTrue(named > read && named < reset, "A read that fails would show the state from two polls ago as new.");
			StringAssert.Contains(source, "if (CountDeviceReadFailure(ud, Environment.TickCount, benign))",
				"A failure is not counted, or a fault is reported on every poll it recurs.");
			var spring = source.IndexOf("ud.FFState.UpdateSpring(");
			var clean = source.IndexOf("ud.DiReadFaultReported = false;");
			var failed = source.IndexOf("var dex = ex as SharpDXException;");
			Assert.IsTrue(spring > 0 && clean > spring && failed > clean,
				"A poll that failed nothing does not end the run's report, so a later fault goes unreported.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A device that comes back online starts with no failed reads")]
		public void A_device_that_comes_back_starts_a_new_run()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step1.UpdateDevices.cs"));
			var offline = source.IndexOf("if (!ud.IsOnline)");
			var online = source.IndexOf("ud.IsOnline = true;");
			Assert.IsTrue(offline > 0 && online > offline, "The place where a device comes back online was not found.");
			var failures = source.IndexOf("ud.DiReadFailures = 0;", offline);
			var reported = source.IndexOf("ud.DiReadFaultReported = false;", offline);
			Assert.IsTrue(failures > offline && failures < online,
				"A device that comes back keeps the failed reads from before it went, so it rests on its first failure.");
			Assert.IsTrue(reported > offline && reported < online,
				"A device that comes back keeps the report of its old run, so its first fault is not reported.");
		}
	}
}
