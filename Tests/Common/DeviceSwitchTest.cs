// @under-test: App.v4/Common/DInput/DeviceSwitch.cs, App.v4/Common/DInput/XInputReorderRunner.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>Switching a device off and on leaves nothing stored, and what the device says is the answer.</summary>
	/// <remarks>
	/// Switching a controller off through SetupAPI stored a disabled mark that survived unplugging it, and answered success
	/// when a program holding the controller would not let go. The controller then sat in Device Manager present and
	/// answering nobody. Here the configuration manager and the USB hub are stand-ins: nothing is switched.
	/// </remarks>
	[TestClass]
	public class DeviceSwitchTest
	{
		/// <summary>A machine of pretend devices, put in place of the configuration manager for as long as it lives.</summary>
		class Machine : IDisposable
		{
			public readonly Dictionary<string, uint?> Nodes = new Dictionary<string, uint?>();
			public readonly Dictionary<uint, Tuple<bool, uint>> State = new Dictionary<uint, Tuple<bool, uint>>();
			public readonly List<string> Calls = new List<string>();
			/// <summary>Whether a device obeys being stopped.</summary>
			public Func<uint, bool> ObeysDisable = node => true;
			/// <summary>Whether a device obeys being started.</summary>
			public Func<uint, bool> ObeysEnable = node => true;
			/// <summary>Whether clearing a stored mark starts the device.</summary>
			public Func<string, bool> ClearsMark = id => false;
			/// <summary>What cycling a device's port does: by default the device is not on a USB port, and nothing happens.</summary>
			public Func<string, bool> Cycles = id => false;
			/// <summary>What the configuration manager answers to a request.</summary>
			public int Answer;
			/// <summary>Whether a port has been cycled.</summary>
			public bool Cycled;

			readonly Func<string, uint?> _locate = DeviceSwitch.Locate;
			readonly Func<uint, int> _disable = DeviceSwitch.Disable;
			readonly Func<uint, int> _enable = DeviceSwitch.Enable;
			readonly Func<uint, Tuple<bool, uint>> _status = DeviceSwitch.Status;
			readonly Func<string, bool> _cycle = DeviceSwitch.CyclePort;
			readonly Func<string, bool, bool> _clear = DeviceSwitch.ClearStoredMark;
			readonly TimeSpan _limit = DeviceSwitch.Limit;
			readonly TimeSpan _cycleLimit = DeviceSwitch.CycleLimit;
			readonly TimeSpan _settle = DeviceSwitch.Settle;

			public Machine()
			{
				DeviceSwitch.Locate = id => { uint? node; return Nodes.TryGetValue(id, out node) ? node : null; };
				DeviceSwitch.Disable = node =>
				{
					Calls.Add("disable " + node);
					if (Answer == 0 && ObeysDisable(node))
						State[node] = Tuple.Create(false, 22u);
					return Answer;
				};
				DeviceSwitch.Enable = node =>
				{
					Calls.Add("enable " + node);
					if (Answer == 0 && ObeysEnable(node))
						State[node] = Tuple.Create(true, 0u);
					return Answer;
				};
				DeviceSwitch.Status = node => State[node];
				DeviceSwitch.CyclePort = id =>
				{
					Calls.Add("cycle " + id);
					return Cycles(id);
				};
				DeviceSwitch.ClearStoredMark = (id, on) =>
				{
					Calls.Add("clear " + id);
					if (!ClearsMark(id))
						return false;
					State[Nodes[id].Value] = Tuple.Create(true, 0u);
					return true;
				};
				DeviceSwitch.Limit = TimeSpan.FromMilliseconds(250);
				DeviceSwitch.CycleLimit = TimeSpan.FromMilliseconds(600);
				DeviceSwitch.Settle = TimeSpan.FromMilliseconds(50);
			}

			/// <summary>A device that is present, as started or as switched off.</summary>
			public uint Add(string id, bool started)
			{
				var node = (uint)(Nodes.Count + 1);
				Nodes[id] = node;
				State[node] = started ? Tuple.Create(true, 0u) : Tuple.Create(false, 22u);
				return node;
			}

			public void Dispose()
			{
				DeviceSwitch.Locate = _locate;
				DeviceSwitch.Disable = _disable;
				DeviceSwitch.Enable = _enable;
				DeviceSwitch.Status = _status;
				DeviceSwitch.CyclePort = _cycle;
				DeviceSwitch.ClearStoredMark = _clear;
				DeviceSwitch.Limit = _limit;
				DeviceSwitch.CycleLimit = _cycleLimit;
				DeviceSwitch.Settle = _settle;
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A device is asked politely to stop, so a program holding it can close it, and its port is left alone when it does")]
		public void A_device_is_switched_off_politely()
		{
			using (var m = new Machine())
			{
				m.Add("A", true);
				Assert.IsTrue(DeviceSwitch.SetState("A", false));
				CollectionAssert.AreEqual(new[] { "disable 1" }, m.Calls, "A device that stops when asked was asked more than once, or had its port cycled.");
				Assert.AreEqual(true, DeviceSwitch.IsOff("A"));
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A device a program holds open and will not let go of is taken away by cycling its port, and stopped as it arrives again")]
		public void A_device_a_program_holds_is_taken_away_by_cycling_its_port()
		{
			using (var m = new Machine())
			{
				var node = m.Add("A", true);
				m.ObeysDisable = n => m.Cycled;
				// The hub takes the device away and it arrives again, started, with nothing holding it.
				m.Cycles = id => { m.Cycled = true; m.State[node] = Tuple.Create(true, 0u); return true; };
				Assert.IsTrue(DeviceSwitch.SetState("A", false));
				CollectionAssert.AreEqual(new[] { "disable 1", "cycle A", "disable 1" }, m.Calls);
				Assert.AreEqual(true, DeviceSwitch.IsOff("A"));
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A device that will not stop, and is not on a USB port to cycle, is not said to be off")]
		public void A_device_that_does_not_stop_is_not_said_to_be_off()
		{
			using (var m = new Machine())
			{
				m.Add("A", true);
				m.ObeysDisable = node => false;
				var ex = Assert.ThrowsExactly<InvalidOperationException>(() => DeviceSwitch.SetState("A", false));
				StringAssert.Contains(ex.Message, "did not switch it off");
				Assert.AreEqual(false, DeviceSwitch.IsOff("A"), "A device that is running was read as off.");
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A device that cannot be stopped even after its port was cycled gives up in time, and is not said to be off")]
		public void A_device_that_cannot_be_stopped_after_its_port_was_cycled_gives_up()
		{
			using (var m = new Machine())
			{
				m.Add("A", true);
				m.ObeysDisable = node => false;
				m.Cycles = id => true;
				var started = DateTime.UtcNow;
				var ex = Assert.ThrowsExactly<InvalidOperationException>(() => DeviceSwitch.SetState("A", false));
				StringAssert.Contains(ex.Message, "did not switch it off");
				Assert.IsTrue(DateTime.UtcNow - started < TimeSpan.FromSeconds(20), "The wait for a device that never stops did not end.");
			}
		}

		[TestMethod, TestCategory("devices")]
		[Description("A device that is not there is said not to be, not switched")]
		public void A_device_that_is_not_there_is_said_not_to_be()
		{
			using (var m = new Machine())
			{
				var ex = Assert.ThrowsExactly<InvalidOperationException>(() => DeviceSwitch.SetState("A", false));
				StringAssert.Contains(ex.Message, "not connected");
				Assert.IsNull(DeviceSwitch.IsOff("A"));
				CollectionAssert.AreEqual(new string[0], m.Calls);
			}
		}

		[TestMethod, TestCategory("devices")]
		[Description("A refusal for want of Administrator is said at once, and the port is not cycled")]
		public void A_refusal_for_want_of_Administrator_is_said_at_once()
		{
			using (var m = new Machine())
			{
				m.Add("A", true);
				m.Answer = 0x33;
				var ex = Assert.ThrowsExactly<InvalidOperationException>(() => DeviceSwitch.SetState("A", false));
				StringAssert.Contains(ex.Message, "Administrator");
				CollectionAssert.AreEqual(new[] { "disable 1" }, m.Calls, "A refusal for want of Administrator was tried again, or cycled a port.");
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A device is switched on when it then says it is started")]
		public void A_device_is_switched_on_when_it_says_it_is_started()
		{
			using (var m = new Machine())
			{
				m.Add("A", false);
				Assert.IsTrue(DeviceSwitch.SetState("A", true));
				CollectionAssert.AreEqual(new[] { "enable 1" }, m.Calls);
				Assert.AreEqual(false, DeviceSwitch.IsOff("A"));
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A disabled mark stored by an earlier run is cleared when starting the device alone does not bring it back")]
		public void A_stored_mark_is_cleared_when_starting_alone_does_not_bring_it_back()
		{
			using (var m = new Machine())
			{
				m.Add("A", false);
				m.ObeysEnable = node => false;
				m.ClearsMark = id => true;
				Assert.IsTrue(DeviceSwitch.SetState("A", true));
				CollectionAssert.AreEqual(new[] { "enable 1", "clear A" }, m.Calls);
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A device that does not start is not said to be on")]
		public void A_device_that_does_not_start_is_not_said_to_be_on()
		{
			using (var m = new Machine())
			{
				m.Add("A", false);
				m.ObeysEnable = node => false;
				var ex = Assert.ThrowsExactly<InvalidOperationException>(() => DeviceSwitch.SetState("A", true));
				StringAssert.Contains(ex.Message, "did not switch it on");
				Assert.AreEqual(true, DeviceSwitch.IsOff("A"));
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A device this program switched off and that is still off is listed on the Issues tab as information, and one switched back on is not")]
		public void A_device_still_switched_off_is_an_information_issue()
		{
			var file = Path.Combine(Path.GetTempPath(), "x360ce.test-" + Guid.NewGuid().ToString("N") + ".txt");
			var saved = XInputReorderRunner.PendingFile;
			XInputReorderRunner.PendingFile = () => file;
			try
			{
				using (var m = new Machine())
				{
					m.Add("OFF-ONE", false);
					m.Add("ON-ONE", true);
					File.WriteAllLines(file, new[] { "OFF-ONE", "ON-ONE", "GONE" });
					CollectionAssert.AreEqual(new[] { "OFF-ONE" }, XInputReorderRunner.StillSwitchedOff(),
						"A device that is on, or not there, was listed as switched off.");
					var issue = (JocysCom.ClassLibrary.Controls.IssuesControl.IssueItem)Activator.CreateInstance(
						typeof(XInputReorderRunner).Assembly.GetType("x360ce.App.Issues.SwitchedOffControllersIssue"));
					issue.CheckTask();
					Assert.AreEqual(JocysCom.ClassLibrary.Controls.IssuesControl.IssueSeverity.Low, issue.Severity, "Switched off is not information.");
					StringAssert.Contains(issue.Description, "OFF-ONE");
					Assert.IsNull(issue.FixName, "A button was offered to switch it on.");
					File.WriteAllLines(file, new[] { "ON-ONE" });
					issue.CheckTask();
					Assert.AreEqual(JocysCom.ClassLibrary.Controls.IssuesControl.IssueSeverity.None, issue.Severity, "The issue stays after the device is on.");
				}
			}
			finally
			{
				XInputReorderRunner.PendingFile = saved;
				if (File.Exists(file))
					File.Delete(file);
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A start-up restore counts only a device that was off and is on again, and keeps the note of one that is still off")]
		public void A_restore_counts_only_a_device_that_was_off_and_is_on_again()
		{
			var file = Path.Combine(Path.GetTempPath(), "x360ce.test-" + Guid.NewGuid().ToString("N") + ".txt");
			var saved = XInputReorderRunner.PendingFile;
			// The notes every copy of the program on the machine shares are not touched.
			XInputReorderRunner.PendingFile = () => file;
			try
			{
				using (var m = new Machine())
				{
					var a = m.Add("A", false);
					m.Add("B", false);
					m.Add("C", true);
					// Only A obeys being started; "D" is not there at all.
					m.ObeysEnable = node => node == a;
					File.WriteAllLines(file, new[] { "A", "B", "C", "D" });
					string[] stillOff;
					var restored = XInputReorderRunner.RestoreAnythingLeftOff(out stillOff);
					CollectionAssert.AreEqual(new[] { "A" }, restored, "Something not switched back on by this run was counted as put back.");
					CollectionAssert.AreEqual(new[] { "B" }, stillOff, "A device that would not come back was not reported as still off.");
					CollectionAssert.AreEqual(new[] { "B" }, File.ReadAllLines(file), "Only the device still off keeps its note.");
				}
			}
			finally
			{
				XInputReorderRunner.PendingFile = saved;
				if (File.Exists(file))
					File.Delete(file);
			}
		}
	}
}
