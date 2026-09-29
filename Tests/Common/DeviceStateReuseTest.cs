// @under-test: Engine/Common/CustomDiState.cs, Engine/Data/UserDevice.cs, App.v4/Common/TestDeviceHelper.cs, App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs, App.v4/Common/DInput/Recorder.cs, App.v4/Mcp/McpTools.cs, App.v4/Controls/PadTabPages/DirectInputControl.cs
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
			var made = new CustomDiState(js);
			var filled = new CustomDiState();
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
			var first = ud.DiState;
			var firstJo = ud.JoState;
			read(null, Game, null, routing);
			var second = ud.DiState;
			Assert.AreNotSame(first, second, "The poll wrote into the state shown until then.");
			Assert.AreNotSame(firstJo, ud.JoState, "The poll read the device into the state shown until then.");
			Assert.AreSame(first, ud.OldDiState);
			read(null, Game, null, routing);
			Assert.AreSame(first, ud.DiState, "A third poll made a new state rather than filling the one from two polls ago.");
			Assert.AreSame(firstJo, ud.JoState, "A third poll made a new state rather than filling the one from two polls ago.");
			Assert.AreSame(second, ud.OldDiState);
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
		[Description("A mouse's first reading is kept as its own copy, and each poll works the state out without making anything")]
		public void A_mouse_is_worked_out_without_making_anything()
		{
			var ud = new UserDevice();
			var read = new CustomDiState();
			var shown = new CustomDiState();
			read.Axis[0] = 1000;
			read.Buttons[2] = true;
			shown.Povs[0] = 9000;
			DInputHelper.ToMouseState(ud, read, shown, 5);
			Assert.IsNotNull(ud.OrgDiState);
			Assert.AreNotSame(read, ud.OrgDiState, "The origin is the reading itself, which the next poll fills again.");
			Assert.AreEqual(-short.MinValue, ud.OrgDiState.Axis[0], "The first reading is not centred.");
			Assert.AreEqual(0, shown.Axis[0], "A mouse that has not moved shows movement.");
			Assert.AreEqual(0, shown.Povs[0], "The state shown keeps a hat from before.");
			Assert.IsTrue(shown.Buttons[2], "A button held on the mouse is not shown.");
			read.Axis[0] = -short.MinValue + 100;
			DInputHelper.ToMouseState(ud, read, shown, 6);
			Assert.AreEqual(1600, shown.Axis[0], "The movement from the origin is not what the mouse shows.");
			const int polls = 20000;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < polls; i++)
					DInputHelper.ToMouseState(ud, read, shown, i);
			});
			Assert.IsTrue(allocated < polls, polls + " mouse polls handed the collector " + allocated + " bytes.");
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Waiting for input sees a press even when the engine fills the same state again")]
		public void Waiting_for_input_keeps_its_own_copy()
		{
			var ud = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Reused state" };
			ud.DiState = new CustomDiState(new JoystickState());
			SettingsManager.UserDevices.Items.Add(ud);
			try
			{
				var wait = Task.Run(() => McpTools.InputWait(3));
				Thread.Sleep(300);
				// The engine fills the state it replaced two polls ago in place: the object the wait began with.
				ud.DiState.Buttons[0] = true;
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
