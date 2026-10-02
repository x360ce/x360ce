// @under-test: App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs, App.v4/ViGEm/Client/Targets/Xbox360Controller.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using x360ce.App;
using x360ce.App.DInput;
using static x360ce.Tests.BusRefusalFixtures;

namespace x360ce.Tests
{
	/// <summary>A refused report or a plug that throws is recorded once for its controller and never thrown.</summary>
	/// <remarks>
	/// Feeding a controller and plugging it in each take the bus's answer without making an object for it,
	/// and a send or a plug that fails is caught once, around the call that can throw, so a refusal never
	/// reaches the pass that drives every controller.
	/// </remarks>
	[TestClass]
	public class BusRefusalFaultRecordingTest
	{
		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A report refused with a sick answer is recorded for its controller, written to the log once, and not thrown")]
		public void A_refused_report_is_recorded_once_and_not_thrown()
		{
			var errors = NoErrors();
			var faults = new Type[4];
			var codes = NoErrors();
			var taken = 0;
			var lines = Logged(() =>
			{
				for (var i = 0; i < 10000; i++)
					if (DInputHelper.ReportTaken(errors, faults, codes, 2, Unnamed))
						taken++;
			});
			Assert.AreEqual(0, taken, "A refused report was taken as delivered, so the controller is never put back.");
			Assert.AreEqual(Unnamed, errors[1], "The refusal is not recorded, so the Issues tab cannot name it.");
			Assert.AreEqual(VIGEM_ERROR.VIGEM_ERROR_NONE, errors[0], "Another controller's record changed.");
			Assert.AreEqual(1, lines.Count, "10,000 refusals with one answer wrote " + lines.Count + " log lines.");
			StringAssert.Contains(lines[0].Key, "Virtual controller 2");
			StringAssert.Contains(lines[0].Key, "0xE0000015", "The log quotes the code otherwise than the Issues tab does.");
			Assert.AreEqual(EventLogEntryType.Warning, lines[0].Value);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A different answer is written again, a controller gone is written quietly, and a report taken keeps the record")]
		public void Each_change_of_answer_is_written_once()
		{
			var errors = NoErrors();
			var faults = new Type[4];
			var codes = NoErrors();
			var fine = false;
			var lines = Logged(() =>
			{
				DInputHelper.ReportTaken(errors, faults, codes, 1, Unnamed);
				DInputHelper.ReportTaken(errors, faults, codes, 1, VIGEM_ERROR.VIGEM_ERROR_INVALID_TARGET);
				DInputHelper.ReportTaken(errors, faults, codes, 1, VIGEM_ERROR.VIGEM_ERROR_INVALID_TARGET);
				fine = DInputHelper.ReportTaken(errors, faults, codes, 1, VIGEM_ERROR.VIGEM_ERROR_NONE);
			});
			Assert.IsTrue(fine, "A report the bus took was read as refused.");
			Assert.AreEqual(VIGEM_ERROR.VIGEM_ERROR_INVALID_TARGET, errors[0],
				"A report taken wiped the last refusal, which the Issues tab names while refusals keep coming.");
			Assert.AreEqual(2, lines.Count, string.Join(Environment.NewLine, lines.Select(x => x.Key)));
			Assert.AreEqual(EventLogEntryType.Information, lines[1].Value,
				"A controller that went away is made again by itself; it is no warning.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A refusal after reports the bus took is written again, and the Issues tab keeps the last refusal")]
		public void A_refusal_after_reports_taken_is_written_again()
		{
			var errors = NoErrors();
			var faults = new Type[4];
			var codes = NoErrors();
			var lines = Logged(() =>
			{
				DInputHelper.ReportTaken(errors, faults, codes, 1, Unnamed);
				DInputHelper.ReportTaken(errors, faults, codes, 1, VIGEM_ERROR.VIGEM_ERROR_NONE);
				DInputHelper.ReportTaken(errors, faults, codes, 1, Unnamed);
			});
			Assert.AreEqual(2, lines.Count, "A bus that refused, took reports, then refused alike wrote " + lines.Count +
				" lines; the refusal that came back is not written.");
			Assert.AreEqual(Unnamed, errors[0], "A report taken wiped the refusal the Issues tab names.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("A changed report and the bus's answer hand nothing to the collector")]
		public void A_changed_report_hands_nothing_to_the_collector()
		{
			var errors = NoErrors();
			var faults = new Type[4];
			var codes = NoErrors();
			const int calls = 20000;
			long allocated = 0;
			Logged(() =>
			{
				// Written before the count starts: controller 2's refusal is written when the answer changes, not per report.
				DInputHelper.ReportTaken(errors, faults, codes, 2, Unnamed);
				var state = new Gamepad();
				var sum = 0L;
				allocated = Allocations.FewestBytes(5, () =>
				{
					for (var i = 0; i < calls; i++)
					{
						state.Buttons = unchecked((GamepadButtonFlags)(short)i);
						state.LeftThumbX = unchecked((short)i);
						state.RightTrigger = unchecked((byte)i);
						var report = DInputHelper.ToReport(state);
						sum += report.wButtons + report.sThumbLX + report.bRightTrigger;
						DInputHelper.ReportTaken(errors, faults, codes, 1, VIGEM_ERROR.VIGEM_ERROR_NONE);
						DInputHelper.ReportTaken(errors, faults, codes, 2, Unnamed);
					}
				});
				Assert.IsTrue(sum != 0);
			});
			Console.WriteLine("Changed reports: " + calls + ", bytes handed to the collector: " + allocated + ".");
			Assert.IsTrue(allocated < calls,
				"Building " + calls + " changed reports and taking the answers handed the collector " + allocated +
				" bytes; this runs for every controller on every pass its state changes.");
		}

		/// <summary>The body of <c>FeedDevice</c>, read from its source.</summary>
		static string[] FeedDeviceLines()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs");
			var text = File.ReadAllText(path);
			var start = text.IndexOf("public bool FeedDevice(uint i)");
			Assert.IsTrue(start >= 0, "FeedDevice is no longer where this test looks for it.");
			var end = text.IndexOf("\n\t\t}", start);
			Assert.IsTrue(end > start, "The end of FeedDevice was not found.");
			return text.Substring(start, end - start).Split('\n')
				.Select(x => x.Trim())
				.Where(x => !x.StartsWith("//"))
				.ToArray();
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Feeding a controller takes the bus's answer and makes no objects: no report object, no boxed flag, one catch around the send alone")]
		public void Feeding_takes_the_answer_and_makes_nothing()
		{
			var lines = FeedDeviceLines();
			var body = string.Join("\n", lines);
			StringAssert.Contains(body, "ReportTaken(", "FeedDevice no longer takes the bus's answer, so a refusal goes unnoticed.");
			Assert.IsFalse(body.Contains("HasFlag("), "HasFlag boxes its value and its flag on every call.");
			Assert.IsFalse(lines.Any(x => x.Contains(" new ")), "FeedDevice makes an object on every changed report.");
			// A refusal is an answer to read. Only a send that cannot be made at all throws, and only the send is guarded.
			Assert.AreEqual(1, lines.Count(x => x.StartsWith("catch")), "FeedDevice should catch around the send, once.");
			var at = Array.IndexOf(lines, "try");
			Assert.IsTrue(at >= 0 && lines[at + 1] == "{" && lines[at + 3] == "}" && lines[at + 4].StartsWith("catch"),
				"The try in FeedDevice holds more than one statement.");
			StringAssert.Contains(lines[at + 2], "SendReport(", "The try in FeedDevice is not around the send.");
			// A send that throws and a refused report keep one record, which a report taken clears, so the same failure
			// after it is written again.
			StringAssert.Contains(body, "NoteFault(FeedFaults, FeedFaultCodes, i, ex)",
				"A send that throws is not written once per change.");
			StringAssert.Contains(body, "ReportTaken(BusErrors, FeedFaults, FeedFaultCodes, i, answer)",
				"A refused report keeps a record of its own, so a report taken does not end a failed send's.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A send that throws is a refusal: false on every call, written once, and nothing reaches the loop")]
		public void A_send_that_throws_is_written_once_and_not_thrown()
		{
			var helper = new DInputHelper();
			helper.CombinedXiStates[0].Gamepad.Buttons = GamepadButtonFlags.A;
			var faults = new List<Exception>();
			EventHandler<LogHelperEventArgs> keep = (sender, e) =>
			{
				faults.Add(e.Exception);
				e.Cancel = true;
			};
			var client = ViGEmClient.Current;
			var log = LogHelper.Current;
			var fed = 0;
			// With no bus client the send throws on every call.
			ViGEmClient.Current = null;
			log.WritingException += keep;
			try
			{
				for (var i = 0; i < 10000; i++)
					if (helper.FeedDevice(1))
						fed++;
				Assert.AreEqual(1, faults.Count, "10,000 failed sends wrote " + faults.Count + " fault reports.");
				// Forgetting what the bus did judges the next failure afresh.
				helper.ResumeAfterDeviceRemoval();
				helper.FeedDevice(1);
				Assert.AreEqual(2, faults.Count, "A failure after the record was forgotten was not written.");
			}
			finally
			{
				log.WritingException -= keep;
				ViGEmClient.Current = client;
			}
			Assert.AreEqual(0, fed, "A send that failed was taken as delivered, so the controller is never let go of.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A plug that faults is written once per change for its controller, and afresh after a plug that did not fault or after Repair")]
		public void A_plug_that_faults_is_written_once_per_change()
		{
			var helper = new DInputHelper();
			var faulted = Task.FromException<VirtualError>(new InvalidOperationException());
			var refused = Task.FromException<VirtualError>(new ViGEmException(Unnamed));
			var finished = Task.FromResult(VirtualError.Other);
			var faults = new List<Exception>();
			EventHandler<LogHelperEventArgs> keep = (sender, e) =>
			{
				faults.Add(e.Exception);
				e.Cancel = true;
			};
			var log = LogHelper.Current;
			log.WritingException += keep;
			try
			{
				for (var i = 0; i < 10000; i++)
					Assert.AreEqual(VirtualError.Other, helper.PlugOutcome(2, faulted), "A plug that faulted was read as made.");
				Assert.AreEqual(1, faults.Count, "10,000 plugs that faulted alike wrote " + faults.Count + " fault reports.");
				helper.PlugOutcome(2, refused);
				Assert.AreEqual(2, faults.Count, "A different fault was not written.");
				helper.PlugOutcome(1, faulted);
				Assert.AreEqual(3, faults.Count, "One controller's record held back another's fault.");
				Assert.AreEqual(VirtualError.Other, helper.PlugOutcome(2, finished), "A finished plug's answer was not handed back.");
				helper.PlugOutcome(2, refused);
				Assert.AreEqual(4, faults.Count, "A fault after a plug that did not fault was not written.");
				// Repair forgets what the bus did, so the next fault is judged afresh.
				helper.ResumeAfterDeviceRemoval();
				helper.PlugOutcome(1, faulted);
				Assert.AreEqual(5, faults.Count, "A fault after Repair was not written.");
			}
			finally
			{
				log.WritingException -= keep;
			}
		}
	}
}
