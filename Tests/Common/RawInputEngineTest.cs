// @under-test: App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs, App.v4/Common/DInput/DInputHelper.Step3.UpdateXiStates.cs, App.v4/Common/DInput/DInputHelper.cs, Engine/Data/UserDevice.cs, Engine/Input/Processors/RawInputHub.cs
// @area: engine   @layer: unit
using JocysCom.ClassLibrary.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// The engine reads a Raw Input device's state from the hub into the device's own states, and the steps after the
	/// read see it as they see a DirectInput device: the same states, turn about, the same mapping, and no force.
	/// </summary>
	/// <remarks>
	/// The engine's read and conversion steps run on a recorded wheel (see <see cref="RawInputFixtures"/>) through a hub
	/// no thread runs: the test stands in for the hub thread, which the hub allows while it is stopped.
	/// </remarks>
	[TestClass]
	public class RawInputEngineTest
	{
		const string WheelPath = @"\\?\HID#VID_046D&PID_C29B#7&2B3C4D5E&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";

		static readonly UserGame Game = new UserGame { FileName = "raw-input-read.exe", FileProductName = "Raw Input read", EnableMask = (int)MapToMask.Controller1 };

		static readonly RawInputFixture Fixture = RawInputFixtures.G27;

		/// <summary>The wheel turned fully left, no button pressed.</summary>
		static readonly RawInputSample Left = Fixture.Samples.Single(x => x.Shows == "Wheel axis 1:30 = 0");

		/// <summary>The wheel near the centre, button 6 pressed.</summary>
		static readonly RawInputSample Pressed = Fixture.Samples.Single(x => x.Shows == "Button 6 9:07 pressed");

		/// <summary>The recorded wheel in the hub, read in its twin's slots, and listed as the device list lists it.</summary>
		static UserDevice Wheel(DInputHelper helper)
		{
			var wheel = Fixture.Device(0, WheelPath, null, 0x04);
			var entry = helper.RawInput.Add(wheel);
			helper.RawInput.SetLayout(wheel.InstanceGuid, RawInputLayout.FromTwin(wheel.Controls, Fixture.Objects));
			entry.ApplyLayout();
			return new UserDevice { InstanceGuid = wheel.InstanceGuid, InputSourceType = (int)InputSourceType.RawInput, IsOnline = true };
		}

		/// <summary>The wheel on Controller 1: its X axis on the left stick, button 6 on A, and force feedback on.</summary>
		static DeviceRouting Routing(UserDevice ud)
		{
			var ps = new PadSetting { LeftThumbAxisX = "a1", ButtonA = "b7", ForceEnable = "1", PadSettingChecksum = Guid.NewGuid() };
			var row = new UserSetting { InstanceGuid = ud.InstanceGuid, FileName = Game.FileName, MapTo = (int)MapTo.Controller1, IsEnabled = true, PadSettingChecksum = ps.PadSettingChecksum };
			var routing = DeviceRouting.Build(Game, new[] { row }, new[] { ps }, new[] { ud });
			Assert.AreEqual(1, routing.Rows.Length, "The wheel's row was not routed.");
			return routing;
		}

		/// <summary>What DirectInput reads for the wheel before its first report: the axes and sliders it has at the centre.</summary>
		static SourceState BeforeFirstReport()
		{
			var state = new SourceState();
			using (var wheel = Fixture.Device(0, WheelPath, null, 0x04))
				new RawInputReader(wheel, RawInputLayout.FromTwin(wheel.Controls, Fixture.Objects)).Rest(state);
			return state;
		}

		/// <summary>The state of a device the hub does not have: nothing at all, and its controller reads nothing from it.</summary>
		static SourceState Missing()
		{
			var state = new SourceState();
			RawInputReader.Reset(state);
			return state;
		}

		static void AssertSame(SourceState expected, SourceState state, string what)
		{
			CollectionAssert.AreEqual(expected.Axis, state.Axis, what + ": axes differ.");
			CollectionAssert.AreEqual(expected.Sliders, state.Sliders, what + ": sliders differ.");
			CollectionAssert.AreEqual(expected.Povs, state.Povs, what + ": hats differ.");
			CollectionAssert.AreEqual(expected.Buttons, state.Buttons, what + ": buttons differ.");
		}

		/// <summary>The engine's count of Raw Input reads, fresh states and missing devices since its last log line.</summary>
		static int[] Counts(DInputHelper helper)
		{
			return new[] { "_rawInputReads", "_rawInputFresh", "_rawInputMissing" }
				.Select(x => (int)typeof(DInputHelper).GetField(x, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(helper))
				.ToArray();
		}

		[TestMethod, TestCategory("engine"), TestCategory("devices"), TestCategory("critical")]
		[Description("A Raw Input device's state follows the hub into the states DirectInput devices use, two kept and filled in turn, reaches its controller through the same mapping, and leaves force feedback untouched")]
		public void A_Raw_Input_device_s_state_follows_the_hub_into_the_mapping()
		{
			var helper = new DInputHelper();
			try
			{
				var ud = Wheel(helper);
				var routing = Routing(ud);
				var read = EngineSteps.UpdateDiStates(helper);
				var convert = EngineSteps.UpdateXiStates(helper);
				// No report yet: as DirectInput reads a device before its first report, its axes at the centre.
				read(null, Game, null, routing);
				var first = ud.SourceState;
				Assert.IsNotNull(first, "The wheel was not read.");
				AssertSame(BeforeFirstReport(), first, "Before the first report");
				Assert.IsFalse(ud.RawInputMissing, "A wheel the hub has is taken for missing.");
				// A report: the state DirectInput read for the twin, and the controller follows it.
				Assert.AreEqual(1, helper.RawInput.ReadInput(RawInputTest.RawInput(Left.Bytes())), "Reports read.");
				read(null, Game, null, routing);
				var second = ud.SourceState;
				Assert.AreNotSame(first, second, "The read wrote into the state shown until then.");
				Assert.AreSame(first, ud.OldSourceState, "The state shown before is not kept as the one before.");
				RawInputTest.AssertState(Fixture, Left, second);
				convert(routing);
				var gp = routing.Rows[0].XiState;
				Assert.AreEqual(short.MinValue, gp.LeftThumbX, "The wheel turned fully left does not push the stick fully left.");
				Assert.AreEqual(GamepadButtonFlags.None, gp.Buttons, "A button is pressed.");
				// No new report: the same state, in the state from two reads ago.
				read(null, Game, null, routing);
				Assert.AreSame(first, ud.SourceState, "A third read made a state rather than filling the one from two reads ago.");
				Assert.AreSame(second, ud.OldSourceState);
				RawInputTest.AssertState(Fixture, Left, ud.SourceState);
				helper.RawInput.ReadInput(RawInputTest.RawInput(Pressed.Bytes()));
				read(null, Game, null, routing);
				Assert.AreSame(second, ud.SourceState, "A fourth read made a state rather than filling the one from two reads ago.");
				RawInputTest.AssertState(Fixture, Pressed, ud.SourceState);
				convert(routing);
				gp = routing.Rows[0].XiState;
				Assert.AreEqual(GamepadButtonFlags.A, gp.Buttons, "Button 6 does not press A.");
				Assert.IsTrue(gp.LeftThumbX > short.MinValue / 2 && gp.LeftThumbX < short.MaxValue / 2, "The stick does not follow the wheel back near the centre: " + gp.LeftThumbX);
				// What a DirectInput read leaves that a Raw Input read has no part in.
				Assert.IsNull(ud.JoState, "A Raw Input device was given a DirectInput state.");
				// Force feedback is on for its tab, and nothing of it is touched: no device, no hold, no force state.
				Assert.IsNull(ud.Device, "A Raw Input device was given a DirectInput device.");
				Assert.IsNull(ud.IsExclusiveMode, "A Raw Input device was held.");
				Assert.IsNull(ud.FFState, "A Raw Input device was given force feedback.");
				Assert.AreEqual(0, ud.ForceFailures, "Force feedback was tried on a Raw Input device.");
				Assert.AreEqual(0, ud.DiReadFailures, "A Raw Input read was counted as a DirectInput one.");
				// What the engine log counts: four reads, three of them finding a state the hub published since the read
				// before: the twin's layout, at rest, and two reports.
				CollectionAssert.AreEqual(new[] { 4, 3, 0 }, Counts(helper), "Reads, fresh states and missing devices counted.");
			}
			finally
			{
				helper.Dispose();
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("devices"), TestCategory("critical")]
		[Description("A button pressed and let go between two engine passes reaches its controller for one pass, whether the reports came in one input or two, and the engine's log has each change in order")]
		public void A_press_between_two_passes_reaches_the_controller()
		{
			var helper = new DInputHelper();
			try
			{
				var ud = Wheel(helper);
				var routing = Routing(ud);
				var read = EngineSteps.UpdateDiStates(helper);
				var convert = EngineSteps.UpdateXiStates(helper);
				helper.RawInput.ReadInput(RawInputTest.RawInput(Left.Bytes()));
				read(null, Game, null, routing);
				var log = helper.StartInputLog(100);
				var twoInputs = new[] { RawInputTest.RawInput(Pressed.Bytes()), RawInputTest.RawInput(Left.Bytes()) };
				var oneInput = new[] { RawInputTest.RawInput(Pressed.Bytes(), Left.Bytes()) };
				foreach (var inputs in new[] { twoInputs, oneInput })
				{
					foreach (var input in inputs)
						helper.RawInput.ReadInput(input);
					read(null, Game, null, routing);
					convert(routing);
					Assert.AreEqual(GamepadButtonFlags.A, routing.Rows[0].XiState.Buttons, "A press that came and went between two passes did not reach the controller.");
					read(null, Game, null, routing);
					convert(routing);
					Assert.AreEqual(GamepadButtonFlags.None, routing.Rows[0].XiState.Buttons, "The press was held past the one pass it is shown for.");
				}
				helper.StopInputLog();
				var lines = x360ce.App.Mcp.McpTools.DescribeChanges(log);
				var button = lines.Where(x => x.Contains("Button 7")).Select(x => x.Substring(x.LastIndexOf(' ') + 1)).ToArray();
				CollectionAssert.AreEqual(new[] { "down", "up", "down", "up" }, button, "The log does not have each press and release in order: " + string.Join("; ", lines));
				// Each time: button 6 tapped, and the wheel, which these recorded reports also move from fully left to near the centre and back.
				Assert.AreEqual(4, log.ShownRawInput, "The log does not count the two taps and two flicks the state alone would have missed.");
				Assert.AreEqual(0, log.ShownDirectInput);
				Assert.AreEqual(0, log.Dropped);
				// Turned off, a pass reads the state alone, and a tap between two passes is not shown.
				x360ce.App.SettingsManager.Options.ReadEveryChange = false;
				try
				{
					foreach (var input in oneInput)
						helper.RawInput.ReadInput(input);
					read(null, Game, null, routing);
					convert(routing);
					Assert.AreEqual(GamepadButtonFlags.None, routing.Rows[0].XiState.Buttons, "A tap between two passes was shown with reading every change off.");
				}
				finally
				{
					x360ce.App.SettingsManager.Options.ReadEveryChange = true;
				}
			}
			finally
			{
				helper.Dispose();
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("devices"), TestCategory("critical")]
		[Description("A wheel flicked fully one way and back between two engine passes reaches its controller's stick for one pass")]
		public void A_flick_between_two_passes_reaches_the_controller()
		{
			var helper = new DInputHelper();
			try
			{
				var ud = Wheel(helper);
				var routing = Routing(ud);
				var read = EngineSteps.UpdateDiStates(helper);
				var convert = EngineSteps.UpdateXiStates(helper);
				var centre = Fixture.Samples.Single(x => x.Shows == "Wheel axis 1:30 = 8193").Bytes();
				helper.RawInput.ReadInput(RawInputTest.RawInput(centre));
				read(null, Game, null, routing);
				convert(routing);
				Assert.IsTrue(Math.Abs((int)routing.Rows[0].XiState.LeftThumbX) < 100, "The wheel at the centre does not centre the stick.");
				helper.RawInput.ReadInput(RawInputTest.RawInput(Left.Bytes(), centre));
				read(null, Game, null, routing);
				convert(routing);
				Assert.AreEqual(short.MinValue, routing.Rows[0].XiState.LeftThumbX, "A flick between two passes did not reach the controller.");
				read(null, Game, null, routing);
				convert(routing);
				Assert.IsTrue(Math.Abs((int)routing.Rows[0].XiState.LeftThumbX) < 100, "The flick was held past the one pass it is shown for.");
			}
			finally
			{
				helper.Dispose();
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("devices"), TestCategory("critical")]
		[Description("A Raw Input device the hub does not have, unplugged or with the hub stopped, rests and reaches its controller as nothing; this is noted once, written nowhere, and costs nothing on later passes; when it is back it is read again")]
		public void A_device_the_hub_does_not_have_rests_and_is_noted_once()
		{
			var helper = new DInputHelper();
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
				var ud = Wheel(helper);
				var routing = Routing(ud);
				var read = EngineSteps.UpdateDiStates(helper);
				var convert = EngineSteps.UpdateXiStates(helper);
				helper.RawInput.ReadInput(RawInputTest.RawInput(Pressed.Bytes()));
				read(null, Game, null, routing);
				read(null, Game, null, routing);
				convert(routing);
				Assert.AreEqual(GamepadButtonFlags.A, routing.Rows[0].XiState.Buttons, "The wheel was not read before it left.");
				foreach (var leave in new Action[] { () => helper.RawInput.Remove(IntPtr.Zero), () => helper.RawInput.Stop() })
				{
					leave();
					read(null, Game, null, routing);
					Assert.IsTrue(ud.RawInputMissing, "A wheel the hub does not have is not noted.");
					var rested = ud.SourceState;
					AssertSame(Missing(), rested, "Missing");
					convert(routing);
					var gp = routing.Rows[0].XiState;
					Assert.AreEqual(GamepadButtonFlags.None, gp.Buttons, "A missing wheel still presses a button.");
					Assert.AreEqual(0, gp.LeftThumbX, "A missing wheel reaches its controller as something.");
					// Later passes change nothing and make nothing.
					var counts = Counts(helper);
					var allocated = Allocations.FewestBytes(5, () =>
					{
						for (var i = 0; i < 1000; i++)
							read(null, Game, null, routing);
					});
					Assert.IsTrue(allocated < 1000, "1000 passes over a missing wheel handed the collector " + allocated + " bytes.");
					Assert.AreSame(rested, ud.SourceState, "A missing wheel's state was filled again.");
					Assert.AreEqual(counts[2] + 5000, Counts(helper)[2], "Passes over a missing wheel are not counted as missing.");
					Assert.AreEqual(0, faults.Count, "A missing wheel was written to the error log: " + string.Join("; ", faults.Select(x => x.Message)));
					// Back: the hub reads it by its twin's slots from the first report on.
					helper.RawInput.Add(Fixture.Device(0, WheelPath, null, 0x04));
					helper.RawInput.ReadInput(RawInputTest.RawInput(Left.Bytes()));
					read(null, Game, null, routing);
					Assert.IsFalse(ud.RawInputMissing, "A wheel back in the hub is still noted as missing.");
					RawInputTest.AssertState(Fixture, Left, ud.SourceState);
					convert(routing);
					Assert.AreEqual(short.MinValue, routing.Rows[0].XiState.LeftThumbX, "A wheel back in the hub does not reach its controller.");
				}
			}
			finally
			{
				log.WritingException -= keep;
				helper.Dispose();
			}
		}
	}
}
