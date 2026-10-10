// @under-test: Engine/Input/States/SourceState.cs, Engine/Data/UserDevice.cs, App.v4/Common/TestDeviceHelper.cs, App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs, App.v4/Common/DInput/DInputHelper.Step3.UpdateXiStates.cs, App.v4/Common/DInput/Recorder.cs, App.v4/Mcp/McpTools.cs, App.v4/Controls/PadTabPages/DirectInputControl.cs
// @area: engine   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.DInput;
using x360ce.App.Mcp;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>A device's state is read into states reserved for it, not into new ones.</summary>
	/// <remarks>
	/// A new state per poll would hand the collector about 1.5 KB a poll at a thousand polls a second: a collection
	/// a second, and each one stops the engine. Each device has two states the engine fills in turn, so a state is
	/// not written until one whole poll after it stops being shown, and whoever keeps a state longer takes a copy.
	/// </remarks>
	[TestClass]
	public class DeviceStateReuseTest
	{
		static readonly UserGame Game = new UserGame { FileName = "state-reuse.exe", FileProductName = "State reuse", EnableMask = (int)MapToMask.Controller1 };

		/// <summary>The test controller on Controller 1, and the engine's reading step for it.</summary>
		static EngineSteps.DiStates Reading(out UserDevice ud, out DeviceRouting routing)
		{
			ud = TestDeviceHelper.NewUserDevice();
			ud.IsOnline = true;
			var ps = new PadSetting { ButtonA = "b1", PadSettingChecksum = Guid.NewGuid() };
			var row = new UserSetting { InstanceGuid = ud.InstanceGuid, FileName = Game.FileName, MapTo = (int)MapTo.Controller1, IsEnabled = true, PadSettingChecksum = ps.PadSettingChecksum };
			routing = DeviceRouting.Build(Game, new[] { row }, new[] { ps }, new[] { ud });
			return EngineSteps.UpdateDiStates(new DInputHelper());
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A state is filled into the arrays it has, and reads the same as a new one")]
		public void A_state_is_filled_in_place()
		{
			var js = new JoystickState { X = 1, Y = 2, RotationZ = 3, TorqueZ = 4, AngularVelocityZ = 5 };
			js.Sliders[1] = 6;
			js.ForceSliders[0] = 7;
			js.VelocitySliders[1] = 8;
			js.PointOfViewControllers[3] = 9000;
			js.Buttons[127] = true;
			var made = new SourceState(js);
			var filled = new SourceState();
			var axis = filled.Axis;
			var buttons = filled.Buttons;
			filled.Load(js);
			CollectionAssert.AreEqual(made.Axis, filled.Axis);
			CollectionAssert.AreEqual(made.Sliders, filled.Sliders);
			CollectionAssert.AreEqual(made.Povs, filled.Povs);
			CollectionAssert.AreEqual(made.Buttons, filled.Buttons);
			Assert.AreSame(axis, filled.Axis, "Filling a state made new arrays.");
			Assert.AreSame(buttons, filled.Buttons, "Filling a state made new arrays.");
			var copy = filled.Clone();
			filled.Axis[0] = 99;
			Assert.AreEqual(1, copy.Axis[0], "A copy shares its arrays with the state it was taken from.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Each poll fills the state from two polls ago, so the one shown is never the one written")]
		public void The_state_shown_is_never_the_one_written()
		{
			UserDevice ud;
			DeviceRouting routing;
			var read = Reading(out ud, out routing);
			read(null, Game, null, routing);
			var first = ud.SourceState;
			var firstJo = ud.JoState;
			read(null, Game, null, routing);
			var second = ud.SourceState;
			Assert.AreNotSame(first, second, "The poll wrote into the state shown until then.");
			Assert.AreNotSame(firstJo, ud.JoState, "The poll read the device into the state shown until then.");
			Assert.AreSame(first, ud.OldSourceState);
			read(null, Game, null, routing);
			Assert.AreSame(first, ud.SourceState, "A third poll made a new state rather than filling the one from two polls ago.");
			Assert.AreSame(firstJo, ud.JoState, "A third poll made a new state rather than filling the one from two polls ago.");
			Assert.AreSame(second, ud.OldSourceState);
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("Reading a device hands nothing to the collector")]
		public void Reading_a_device_hands_nothing_to_the_collector()
		{
			UserDevice ud;
			DeviceRouting routing;
			var read = Reading(out ud, out routing);
			for (var i = 0; i < 10; i++)
				read(null, Game, null, routing);
			const int passes = 20000;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < passes; i++)
					read(null, Game, null, routing);
			});
			Console.WriteLine("Bytes per poll: " + allocated / (double)passes);
			Assert.IsTrue(allocated < passes,
				passes + " polls of one device handed the collector " + allocated + " bytes; a poll must hand the collector nothing.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("An axis that reports movement rests in the middle and follows the movement from there; every other control is shown as read, and nothing is made")]
		public void A_mouse_is_worked_out_without_making_anything()
		{
			// X, Y and the wheel report how far they moved; axis 4 reports where it is.
			var ud = new UserDevice { DiRelativeAxisMask = 0x1 | 0x2 | 0x4 };
			var read = new SourceState();
			var shown = new SourceState();
			read.Axis[0] = 1000;
			read.Axis[3] = 12345;
			read.Buttons[2] = true;
			read.Povs[0] = -1;
			shown.Povs[0] = 9000;
			DInputHelper.ToMouseState(ud, read, shown, 5);
			Assert.IsNotNull(ud.OriginSourceState);
			Assert.AreNotSame(read, ud.OriginSourceState, "The origin is the reading itself, which the next poll fills again.");
			Assert.AreEqual(1000, read.Axis[0], "The reading was written over.");
			Assert.AreEqual(-short.MinValue, shown.Axis[0], "A mouse that has not moved rests at one end, not in the middle.");
			Assert.AreEqual(-short.MinValue, shown.Axis[1], "An axis still at its first reading does not rest in the middle.");
			Assert.AreEqual(12345, shown.Axis[3], "An axis that reports where it is was changed.");
			Assert.AreEqual(-1, shown.Povs[0], "The state shown keeps a hat from before.");
			Assert.IsTrue(shown.Buttons[2], "A button held on the mouse is not shown.");
			read.Axis[0] = 1100;
			DInputHelper.ToMouseState(ud, read, shown, 6);
			Assert.AreEqual(-short.MinValue + 1600, shown.Axis[0], "The movement from the origin is not what the mouse shows.");
			// Past the end the origin moves with the mouse, so turning back answers at once.
			read.Axis[0] = 1100 - 10000;
			DInputHelper.ToMouseState(ud, read, shown, 7);
			Assert.AreEqual(0, shown.Axis[0], "A mouse moved past the end is not at the end.");
			read.Axis[0] += 100;
			DInputHelper.ToMouseState(ud, read, shown, 8);
			Assert.AreEqual(1600, shown.Axis[0], "Turning back does not answer at once.");
			const int polls = 20000;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < polls; i++)
					DInputHelper.ToMouseState(ud, read, shown, i);
			});
			Assert.IsTrue(allocated < polls, polls + " mouse polls handed the collector " + allocated + " bytes.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Axes that report movement are known by DirectInput's own flag on any device not read as a gamepad, and only they are worked out from where they were first read")]
		public void Movement_axes_are_known_by_their_flag()
		{
			// The masks need a device, so they are read from the source.
			var engine = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "Engine", "Input", "States", "SourceState.cs"));
			Assert.AreEqual(3, Ui.Count(engine, "if (item.Flags.HasFlag(DeviceObjectTypeFlags.RelativeAxis))"),
				"An axis or slider mask leaves out the controls DirectInput calls relative.");
			Assert.AreEqual(3, Ui.Count(engine, "relativeMask |= 1 << i;"));
			var step2 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step2.UpdateDiStates.cs"));
			StringAssert.Contains(step2, "ud.DiRelativeAxisMask = SourceState.TrustedRelativeMask(ud.CapType, relativeMask);");
			StringAssert.Contains(step2, "ud.DiRelativeSliderMask = SourceState.TrustedRelativeMask(ud.CapType, relativeSliderMask);");
			StringAssert.Contains(step2, "var moving = ud.Device != null && (ud.DiRelativeAxisMask | ud.DiRelativeSliderMask) != 0;");
			Assert.AreEqual(2, Ui.Count(step2, "ud.DiRelativeRestart = true;"),
				"A device acquired again keeps the origin of its moving axes, so after a reorder or a reconnect they rest at an end.");
			Assert.IsFalse(step2.Contains("ud.IsMouse"), "The engine asks whether a device is a mouse, so a trackball or a spinner rests at one end.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A slider that reports movement rests in the middle and follows the movement, as a moving axis does; a slider that reports where it is is shown as read")]
		public void A_moving_slider_rests_in_the_middle()
		{
			// Slider 2 reports how far it moved; slider 1 and the axes report where they are.
			var ud = new UserDevice { DiRelativeSliderMask = 0x2 };
			var read = new SourceState();
			var shown = new SourceState();
			read.Sliders[0] = 777;
			read.Sliders[1] = 5000;
			read.Axis[0] = 4321;
			DInputHelper.ToMouseState(ud, read, shown, 1);
			Assert.AreEqual(-short.MinValue, shown.Sliders[1], "A slider that reports movement does not rest in the middle.");
			Assert.AreEqual(777, shown.Sliders[0], "A slider that reports where it is was changed.");
			Assert.AreEqual(4321, shown.Axis[0], "An axis that reports where it is was changed.");
			read.Sliders[1] = 5100;
			DInputHelper.ToMouseState(ud, read, shown, 2);
			Assert.AreEqual(-short.MinValue + 1600, shown.Sliders[1], "The movement from the origin is not what the slider shows.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A device acquired again takes the origin of its moving axes and sliders again, in place, so they rest in the middle after a reorder or a reconnect")]
		public void A_device_acquired_again_rests_in_the_middle_again()
		{
			var ud = new UserDevice { DiRelativeAxisMask = 0x1 | 0x2, DiRelativeSliderMask = 0x1 };
			var read = new SourceState();
			var shown = new SourceState();
			read.Axis[0] = 500;
			DInputHelper.ToMouseState(ud, read, shown, 1);
			var origin = ud.OriginSourceState;
			read.Axis[0] += 10000;
			DInputHelper.ToMouseState(ud, read, shown, 2);
			Assert.AreEqual(ushort.MaxValue, shown.Axis[0], "A mouse moved past the end is not at the end.");
			// Acquired again, as after a reorder or a reconnect: DirectInput's running totals start somewhere else.
			ud.DiRelativeRestart = true;
			read.Axis[0] = 100000;
			read.Axis[1] = -100000;
			read.Sliders[0] = 100000;
			DInputHelper.ToMouseState(ud, read, shown, 3);
			Assert.AreEqual(-short.MinValue, shown.Axis[0], "An axis at an end before the device was acquired again does not rest in the middle.");
			Assert.AreEqual(-short.MinValue, shown.Axis[1], "An axis whose total moved while the device was away does not rest in the middle.");
			Assert.AreEqual(-short.MinValue, shown.Sliders[0], "A moving slider does not rest in the middle after the device was acquired again.");
			Assert.IsFalse(ud.DiRelativeRestart, "The origin is taken again on every poll, so the device never moves.");
			Assert.AreSame(origin, ud.OriginSourceState, "The origin was made again rather than filled in place.");
			read.Axis[0] += 100;
			DInputHelper.ToMouseState(ud, read, shown, 4);
			Assert.AreEqual(-short.MinValue + 1600, shown.Axis[0], "The movement from the new origin is not what the mouse shows.");
			const int restarts = 20000;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < restarts; i++)
				{
					ud.DiRelativeRestart = true;
					DInputHelper.ToMouseState(ud, read, shown, i);
				}
			});
			Assert.IsTrue(allocated < restarts, restarts + " origins taken again handed the collector " + allocated + " bytes.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("An axis or slider that reports movement, at rest in the middle, centres a stick and leaves a trigger and a button released; moved one way, it presses them")]
		public void A_moving_axis_at_rest_centres_a_stick_and_releases_a_trigger_and_a_button()
		{
			// Axis 1 and slider 1 report how far they moved; axis 2 reports where it is.
			var ud = new UserDevice { InstanceGuid = Guid.NewGuid(), DiRelativeAxisMask = 0x1, DiRelativeSliderMask = 0x1, JoState = new JoystickState(), SourceState = new SourceState() };
			ud.IsOnline = true;
			var ps = new PadSetting { LeftThumbAxisX = "a1", LeftTrigger = "a1", ButtonA = "a1", ButtonB = "a-1", ButtonX = "s1", RightTrigger = "a2", PadSettingChecksum = Guid.NewGuid() };
			var row = new UserSetting { InstanceGuid = ud.InstanceGuid, FileName = Game.FileName, MapTo = (int)MapTo.Controller1, IsEnabled = true, PadSettingChecksum = ps.PadSettingChecksum };
			var routing = DeviceRouting.Build(Game, new[] { row }, new[] { ps }, new[] { ud });
			var xi = EngineSteps.UpdateXiStates(new DInputHelper());
			// At rest: the middle, where a moving axis starts.
			ud.SourceState.Axis[0] = -short.MinValue;
			ud.SourceState.Axis[1] = -short.MinValue;
			ud.SourceState.Sliders[0] = -short.MinValue;
			xi(routing);
			Assert.AreEqual(0, row.XiState.LeftThumbX, "A stick on a moving axis at rest is not centred.");
			Assert.AreEqual(0, row.XiState.LeftTrigger, "A trigger on a moving axis at rest is pressed.");
			Assert.AreEqual(SharpDX.XInput.GamepadButtonFlags.None, row.XiState.Buttons, "A button on a moving axis or slider at rest is pressed.");
			Assert.AreEqual(127, row.XiState.RightTrigger, "A trigger on an axis that reports where it is no longer reads the whole axis.");
			// Moved one way: the stick and the trigger follow, and the buttons on that way press.
			ud.SourceState.Axis[0] = ushort.MaxValue;
			ud.SourceState.Sliders[0] = ushort.MaxValue;
			xi(routing);
			Assert.AreEqual(short.MaxValue, row.XiState.LeftThumbX);
			Assert.AreEqual(byte.MaxValue, row.XiState.LeftTrigger, "A trigger on a moving axis is not pressed by the movement.");
			Assert.AreEqual(SharpDX.XInput.GamepadButtonFlags.A | SharpDX.XInput.GamepadButtonFlags.X, row.XiState.Buttons);
			// Moved the other way: only the inverted button presses.
			ud.SourceState.Axis[0] = 0;
			ud.SourceState.Sliders[0] = 0;
			xi(routing);
			Assert.AreEqual(short.MinValue, row.XiState.LeftThumbX);
			Assert.AreEqual(0, row.XiState.LeftTrigger, "A trigger on a moving axis is pressed by the movement the other way.");
			Assert.AreEqual(SharpDX.XInput.GamepadButtonFlags.B, row.XiState.Buttons);
			const int passes = 20000;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < passes; i++)
					xi(routing);
			});
			Assert.IsTrue(allocated < passes, passes + " conversions of a moving axis handed the collector " + allocated + " bytes.");
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Waiting for input sees a press even when the engine fills the same state again")]
		public void Waiting_for_input_keeps_its_own_copy()
		{
			var ud = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Reused state" };
			ud.SourceState = new SourceState(new JoystickState());
			SettingsManager.UserDevices.Items.Add(ud);
			try
			{
				var wait = Task.Run(() => McpTools.InputWait(3));
				Thread.Sleep(300);
				// The engine fills the state it replaced two polls ago in place: the object the wait began with.
				ud.SourceState.Buttons[0] = true;
				var found = wait.Result as Dictionary<string, object>;
				Assert.IsNotNull(found, "A button pressed while the tool waited was not seen, because the state it compared with was written over.");
				Assert.AreEqual(ud.InstanceGuid.ToString(), found["InstanceGuid"]);
			}
			finally
			{
				SettingsManager.UserDevices.Items.Remove(ud);
			}
			var recorder = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "Recorder.cs"));
			StringAssert.Contains(recorder, "recordingSnapshot = state.Clone();",
				"The recording keeps the engine's own state, which the engine fills again two polls later.");
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The Direct Input tab shows a value the engine wrote into the state the tab drew last")]
		public void The_direct_input_tab_keeps_its_own_copy()
		{
			Ui.OnUiThread(() =>
			{
				using (var tab = new DirectInputUserControl())
				{
					tab.CreateControl();
					var ud = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Reused state", JoState = new JoystickState { X = 100 } };
					ud.JoState.Buttons[1] = true;
					tab.UpdateFrom(ud);
					var axis = (DataTable)typeof(DirectInputUserControl).GetField("DiAxisTable", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tab);
					var buttons = (DataTable)typeof(DirectInputUserControl).GetField("DiButtonsTable", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tab);
					Assert.AreEqual(100, axis.Rows[0][1]);
					Assert.AreEqual("01", buttons.Rows[0][0]);
					// The engine fills the state it replaced two polls ago in place: the object the tab drew last.
					ud.JoState.X = 200;
					ud.JoState.Buttons[3] = true;
					tab.UpdateFrom(ud);
					Assert.AreEqual(200, axis.Rows[0][1], "The tab kept the engine's own state as the one it drew, so a value written into it was taken as already shown.");
					Assert.AreEqual("01 03", buttons.Rows[0][0], "The tab kept the engine's own state as the one it drew, so a button written into it was not shown.");
				}
			});
		}
	}
}
