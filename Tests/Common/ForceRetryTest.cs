// @under-test: App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs, Engine/Data/UserDevice.cs, App.v4/Issues/ForceFeedbackIssue.cs, Engine/Common/ForceFeedbackState.cs
// @area: force-feedback   @layer: unit
using JocysCom.ClassLibrary.Controls.IssuesControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.App.Issues;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>A device whose force feedback keeps failing while the device itself reads.</summary>
	/// <remarks>
	/// The force is sent after the device is read, on the same poll. A failure there that is not a refusal, such as
	/// DIERR_DEVICEFULL, used to count as a failed read: the device was held again on the next poll and read, which
	/// ended the run of failures, and was sent its force again because it was held again. So it never rested, and
	/// cost an exception and three native calls on every poll. A force failure now leaves the read and the hold alone
	/// unless the hold was lost, is sent again on the next poll, and after a second failure in a row rests for
	/// <see cref="DInputHelper.ForceRetryMs"/>. It is reported once a run, and named on the Issues tab until an
	/// attempt succeeds.
	/// </remarks>
	[TestClass]
	public class ForceRetryTest
	{
		const int Now = 1000;

		/// <summary>DIERR_DEVICEFULL: no room on the device for the effect. Neither a refusal nor a device condition.</summary>
		static readonly int DeviceFull = unchecked((int)0x80040201);

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A force that fails once is sent again on the next poll, and that failure is the one reported")]
		public void One_force_failure_is_tried_again_at_once()
		{
			var ud = new UserDevice();
			Assert.IsFalse(DInputHelper.IsForceResting(ud, Now), "A device whose force never failed is resting.");
			Assert.IsTrue(DInputHelper.CountForceFailure(ud, Now, DeviceFull, false), "The first fault of a run is not reported.");
			Assert.AreEqual(Now + DInputHelper.ForceRetryMs, ud.ForceRetryAt, "The failure does not set when the force is tried again.");
			Assert.IsFalse(DInputHelper.IsForceResting(ud, Now + 1), "One failure stopped the next poll's force; a device that lost its hold for a moment takes it on the next poll.");
			Assert.AreEqual(DeviceFull, ud.ForceFault, "The Issues tab is not given the run's fault.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A force that fails twice in a row rests until it is due, and is reported once")]
		public void A_force_that_keeps_failing_rests_and_is_reported_once()
		{
			var ud = new UserDevice();
			DInputHelper.CountForceFailure(ud, Now, DeviceFull, false);
			Assert.IsFalse(DInputHelper.CountForceFailure(ud, Now + 1, DeviceFull, false), "The second failure in a row was reported again.");
			Assert.IsTrue(DInputHelper.IsForceResting(ud, Now + 2), "A force that failed twice in a row is sent again on the next poll, where it fails again.");
			Assert.IsTrue(DInputHelper.IsForceResting(ud, Now + DInputHelper.ForceRetryMs), "The rest ended early.");
			Assert.IsFalse(DInputHelper.IsForceResting(ud, Now + 1 + DInputHelper.ForceRetryMs), "The force is not tried again when it is due.");
			for (var i = 2; i < 100; i++)
				Assert.IsFalse(DInputHelper.CountForceFailure(ud, Now + i * DInputHelper.ForceRetryMs, DeviceFull, false), "A later failure of the same run was reported.");
			Assert.AreEqual(DeviceFull, ud.ForceFault, "The Issues tab is not given the run's fault.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("Force that keeps failing neither rests the device's reading nor counts as a failed read")]
		public void A_force_failure_leaves_the_read_alone()
		{
			var ud = new UserDevice();
			for (var i = 0; i < 10; i++)
				DInputHelper.CountForceFailure(ud, Now + i, DeviceFull, false);
			Assert.IsFalse(DInputHelper.IsDeviceReadResting(ud, Now + 10), "Force that keeps failing stops the device being read, so its input is lost.");
			Assert.AreEqual(0, ud.DiReadFailures, "A force failure is counted as a failed read.");
			Assert.AreEqual(0, ud.DiReadRetryAt, "A force failure sets when the device is read again.");
			Assert.IsFalse(ud.DiReadFaultReported, "A force failure takes the report of the device's next failed read.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A device condition counts towards the rest with no report and no Issues line, and a fault after it in the same run is reported once")]
		public void A_device_condition_counts_towards_the_rest_and_is_not_reported()
		{
			var ud = new UserDevice();
			Assert.IsFalse(DInputHelper.CountForceFailure(ud, Now, SharpDX.DirectInput.ResultCode.InputLost.Code, true), "A device condition was reported.");
			Assert.IsFalse(DInputHelper.IsForceResting(ud, Now + 1), "One device condition stopped the next poll's force.");
			Assert.AreEqual(0, ud.ForceFault, "A device condition is named on the Issues tab.");
			Assert.IsTrue(DInputHelper.CountForceFailure(ud, Now + 1, DeviceFull, false), "A fault after a device condition in the same run was not reported.");
			Assert.IsTrue(DInputHelper.IsForceResting(ud, Now + 2), "Two failures in a row, a device condition among them, do not rest the force.");
			Assert.IsFalse(DInputHelper.CountForceFailure(ud, Now + 2 + DInputHelper.ForceRetryMs, DeviceFull, false), "The run's fault was reported again.");
		}

		[TestMethod, TestCategory("force-feedback")]
		[Description("An attempt that fails nothing ends the run, so the force is sent at once and the next fault is news")]
		public void A_success_ends_the_run()
		{
			var ud = new UserDevice();
			DInputHelper.CountForceFailure(ud, Now, DeviceFull, false);
			DInputHelper.CountForceFailure(ud, Now + 1, DeviceFull, false);
			// What the engine does at the end of a force attempt that failed nothing.
			ud.ForceFailures = 0;
			ud.ForceFault = 0;
			Assert.IsFalse(DInputHelper.IsForceResting(ud, Now + 2), "Force that played is still resting.");
			Assert.IsTrue(DInputHelper.CountForceFailure(ud, Now + 3, DeviceFull, false), "A new run of failures was not reported.");
			Assert.IsFalse(DInputHelper.IsForceResting(ud, Now + 4), "The first failure of a new run rests the force, as if the old run went on.");
		}

		[TestMethod, TestCategory("force-feedback")]
		[Description("A rest that starts just before the tick count wraps ends on time after it, and a force that never failed never rests")]
		public void A_rest_across_the_wrap_ends_on_time()
		{
			var ud = new UserDevice();
			var now = int.MaxValue - 100;
			DInputHelper.CountForceFailure(ud, now, DeviceFull, false);
			DInputHelper.CountForceFailure(ud, now, DeviceFull, false);
			Assert.IsTrue(DInputHelper.IsForceResting(ud, now + 1), "The rest ended before the wrap.");
			Assert.IsTrue(DInputHelper.IsForceResting(ud, unchecked(now + 200)), "The rest ended at the wrap.");
			Assert.IsFalse(DInputHelper.IsForceResting(ud, unchecked(now + DInputHelper.ForceRetryMs)), "The rest outlived the wrap.");
			// A retry time never set is nought, which reads as ahead just before the count comes round to nought.
			Assert.IsFalse(DInputHelper.IsForceResting(new UserDevice(), -100), "A force that never failed rests just before the tick count comes round.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("Only a failure that says the hold was lost lets go of the device to take it again")]
		public void Only_a_lost_hold_is_taken_again()
		{
			foreach (var code in new[]
			{
				SharpDX.DirectInput.ResultCode.InputLost.Code,
				SharpDX.DirectInput.ResultCode.NotAcquired.Code,
				SharpDX.DirectInput.ResultCode.Unplugged.Code,
				SharpDX.DirectInput.ResultCode.NotExclusiveAcquired.Code,
			})
				Assert.IsTrue(DInputHelper.IsDeviceHoldLost(new Result(code)), string.Format("0x{0:X8} says the hold was lost, yet the device is not held again.", code));
			foreach (var code in new[]
			{
				DeviceFull,
				unchecked((int)0x80004005), // E_FAIL.
				unchecked((int)0x80070005), // E_ACCESSDENIED.
				unchecked((int)0x80040203), // DIERR_NOTDOWNLOADED.
				unchecked((int)0x80004001), // E_NOTIMPL.
				unchecked((int)0x80070057), // E_INVALIDARG.
			})
				Assert.IsFalse(DInputHelper.IsDeviceHoldLost(new Result(code)), string.Format("0x{0:X8} lets go of a device that is still held, and takes it again.", code));
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("performance")]
		[Description("Deciding whether to send a device's force hands nothing to the collector")]
		public void Deciding_hands_nothing_to_the_collector()
		{
			var ud = new UserDevice();
			const int calls = 20000;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < calls; i++)
					if (!DInputHelper.IsForceResting(ud, i))
						DInputHelper.CountForceFailure(ud, i, DeviceFull, false);
			});
			Assert.IsTrue(allocated < calls,
				"Deciding " + calls + " times handed the collector " + allocated + " bytes; it runs for every force feedback device on every poll.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("The engine sends force in a try of its own after the read, skips it while it rests, and leaves the read's failures alone")]
		public void The_engine_sends_force_through_the_rule()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step2.UpdateDiStates.cs"));
			var reset = source.IndexOf("ud.DiReadFailures = 0;");
			var gate = source.IndexOf("if (hasForceFeedback && !IsForceResting(ud, Environment.TickCount))");
			Assert.IsTrue(reset > source.IndexOf("device.GetCurrentState(ref reading);") && gate > reset,
				"The force is not skipped while it rests, or is sent before the device is read.");
			var send = source.IndexOf("ud.FFState.SetDeviceForces(ud, device, ps, v);", gate);
			Assert.IsTrue(send > gate && SharedForceTest.LastTry(source, send) > gate,
				"The force is sent in the read's try, so a force failure counts as a failed read and the device is held again.");
			// From the gate to the end of the poll's read: the force's own try and catch.
			var block = Ui.Between(source, "if (hasForceFeedback && !IsForceResting(ud, Environment.TickCount))", "ud.DiReadFaultReported = false;");
			foreach (var read in new[] { "DiReadFailures", "DiReadRetryAt", "DiReadFaultReported", "CountDeviceReadFailure(" })
				Assert.IsFalse(block.Contains(read), "The force block touches the read's failures (" + read + "), so a force failure would rest the device's input.");
			StringAssert.Contains(block, "if (forceArrived || held || ud.ForceFailures > 0 || ud.FFState.Changed(ps))",
				"Force that failed is sent again only when a game asks for a new one.");
			var spring = block.IndexOf("ud.FFState.UpdateSpring(device,");
			var played = block.IndexOf("ud.ForceFailures = 0;");
			var caught = block.IndexOf("catch (Exception");
			Assert.IsTrue(spring > 0 && played > spring && caught > played && block.IndexOf("ud.ForceFault = 0;") > spring,
				"An attempt that failed nothing does not end the run, so a device whose force plays again stays named on the Issues tab.");
			var failure = block.Substring(caught);
			StringAssert.Contains(failure, "if (CountForceFailure(ud, Environment.TickCount, ex.HResult, ",
				"A force failure is not counted apart from the read, or is reported on every attempt.");
			Assert.IsTrue(Regex.IsMatch(failure, @"if \(fault != null && IsDeviceHoldLost\(fault\.ResultCode\)\)\s*ud\.IsExclusiveMode = null;"),
				"A force failure lets go of the device whatever the error, so a device still held is held again.");
			Assert.AreEqual(1, Ui.Count(failure, "ud.IsExclusiveMode = null;"), "A force failure lets go of the device in more than one place.");
			// Every force call in the block sits in the force's own try, and its catch does not throw on.
			var forceTry = SharedForceTest.LastTry(source, send);
			var end = source.IndexOf("ud.DiReadFaultReported = false;", gate);
			foreach (var call in new[] { "ud.FFState.StopDeviceForces(device);", "ud.FFState.UpdateSpring(device," })
				for (var at = source.IndexOf(call, gate); at >= 0 && at < end; at = source.IndexOf(call, at + 1))
					Assert.AreEqual(forceTry, SharedForceTest.LastTry(source, at), call + " is outside the force's own try, so its failure counts as a failed read.");
			Assert.IsFalse(Regex.IsMatch(failure, @"\bthrow\b"), "The force's catch throws on, so a force failure reaches the read's catch.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A force update that threw is made again whole by the next one, so the retry sends what the device never took, a new effect type or motor swap among it")]
		public void A_retry_sends_every_setting_again_after_an_update_that_threw()
		{
			// The state records each setting before the device takes it, the effect type and the motor swap among them, so a
			// retry of the same force would find nothing changed: a failed stop would leave the motor running, and a failed
			// type change would leave the old effect on the device, playing the strength it had when the change failed.
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "Engine", "Common", "ForceFeedbackState.cs"));
			var start = source.IndexOf("public bool SetDeviceForces(");
			var noActuator = source.IndexOf("if (actuatorL == null)", start);
			var carried = source.IndexOf("var resend = interrupted;", start);
			var marked = source.IndexOf("interrupted = true;", start);
			var resume = source.IndexOf("Resume(effectL);", start);
			Assert.IsTrue(start > 0 && noActuator > start && carried > noActuator && marked > carried && resume > marked,
				"An update is not marked as under way before its first device call, so one that throws is not made again by the next.");
			// Every effect made again with every setting, as for a new type or new motors: a make sends them all.
			Assert.IsTrue(source.IndexOf("PlanEffects(motors, held, motorsRefused, motorsChanged || forceChanged || resend, out drop, out make);", start) > resume,
				"An update after one that threw keeps the effects it holds, so a new type or motor swap that threw is never applied.");
			var sentRight = source.IndexOf("SetParamaters(effectR, paramsR, flagsR);", start);
			var spring = source.IndexOf("if (motorsChanged || resend)", start);
			var finished = source.IndexOf("interrupted = false;", start);
			Assert.IsTrue(sentRight > resume && spring > sentRight && finished > spring,
				"The spring stays on the actuator an update that threw left it on.");
			Assert.IsTrue(finished < source.IndexOf("return true;", sentRight), "An update is marked finished before its settings reached the device.");
		}

		[TestMethod, TestCategory("force-feedback")]
		[Description("A device the engine lets go of forgets its force failure, so the Issues tab does not name force it is no longer sent")]
		public void A_device_let_go_of_forgets_its_force_failure()
		{
			var game = DeviceRoutingFixtures.Game;
			var ps = DeviceRoutingFixtures.Force(true);
			// Not connected, so no pass calls into it: what the pass leaves on it is read back.
			var wheel = new UserDevice { InstanceGuid = Guid.NewGuid() };
			var row = DeviceRoutingFixtures.Row(wheel.InstanceGuid, MapTo.Controller1, ps);
			var ticked = DeviceRouting.Build(game, new[] { row }, new[] { ps }, new[] { wheel });
			row.IsEnabled = false;
			var unticked = DeviceRouting.Build(game, new[] { row }, new[] { ps }, new[] { wheel });
			var di = EngineSteps.UpdateDiStates(new DInputHelper());
			di(null, game, null, ticked);
			// As a pass leaves a device whose force keeps failing.
			wheel.ForceFailures = 2;
			wheel.ForceRetryAt = Now + DInputHelper.ForceRetryMs;
			wheel.ForceFault = DeviceFull;
			di(null, game, null, unticked);
			Assert.AreEqual(0, wheel.ForceFailures, "A device let go of keeps resting force it is no longer sent.");
			Assert.AreEqual(0, wheel.ForceFault, "A device let go of stays named on the Issues tab for force it is no longer sent.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A device whose force feedback keeps failing is named on the Issues tab with its error, said once, until an attempt succeeds")]
		public void The_Issues_tab_names_a_failing_device_until_it_plays()
		{
			var name = "Force retry pad " + Guid.NewGuid().ToString("N");
			var ud = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = name };
			ud.IsOnline = true;
			// An effect lost to the failure: its settings made, the effect not. It reads as refused as well.
			var state = new ForceFeedbackState();
			typeof(ForceFeedbackState).GetField("paramsL", BindingFlags.NonPublic | BindingFlags.Instance)
				.SetValue(state, new SharpDX.DirectInput.EffectParameters());
			ud.FFState = state;
			var issue = new ForceFeedbackIssue();
			try
			{
				SettingsManager.UserDevices.Items.Add(ud);
				DInputHelper.CountForceFailure(ud, Now, DeviceFull, false);
				var lines = Lines(issue, name);
				Assert.AreEqual(1, lines.Length, "A device whose force feedback fails is not named once on the Issues tab: " + string.Join(" | ", lines));
				StringAssert.Contains(lines[0], "0x80040201", "The Issues tab does not name the error.");
				Assert.IsFalse(lines[0].Contains("refused"), "An effect lost to the failure is said to be refused as well.");
				// Unplugged, the device is not tried, so it is not said to be, and its lost effect is still not a refusal.
				ud.IsOnline = false;
				Assert.AreEqual(0, Lines(issue, name).Length, "A device that is not connected is said to be tried again, or to refuse an effect it lost.");
				ud.IsOnline = true;
				// What the engine does after an attempt that failed nothing.
				ud.ForceFailures = 0;
				ud.ForceFault = 0;
				lines = Lines(issue, name);
				Assert.AreEqual(1, lines.Length, "The refusal is not said once the failure is over.");
				Assert.IsFalse(lines[0].Contains("0x80040201"), "A device whose force feedback plays is still named for its failure.");
				ud.FFState = null;
				Assert.AreEqual(0, Lines(issue, name).Length, "The Issues tab names a device with nothing wrong.");
			}
			finally
			{
				SettingsManager.UserDevices.Items.Remove(ud);
			}
		}

		/// <summary>The Issues tab's lines about the device named <paramref name="name"/>, after a check.</summary>
		static string[] Lines(ForceFeedbackIssue issue, string name)
		{
			issue.Check();
			Assert.IsNull(issue.LastException, "The check failed: " + issue.LastException);
			if (issue.Severity == IssueSeverity.None)
				return new string[0];
			return issue.Description.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Where(x => x.Contains(name)).ToArray();
		}
	}
}
