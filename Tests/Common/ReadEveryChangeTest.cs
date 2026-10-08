// @under-test: App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs, App.v4/Common/Options.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// A press or release that comes and goes between two engine passes reaches the controller, for one pass, from a
	/// DirectInput device as from a Raw Input one: DirectInput keeps every change, and the engine reads them after the
	/// state, so however a quick tap falls between the two reads it is not lost.
	/// </summary>
	[TestClass]
	public class ReadEveryChangeTest
	{
		static SourceState State(params int[] pressed)
		{
			var state = new SourceState();
			foreach (var b in pressed)
				state.Buttons[b] = true;
			return state;
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A button that reads as before while DirectInput kept a change of it to the other value is shown changed for one pass; one that changed, or did not move, is shown as read")]
		public void A_kept_change_the_state_missed_is_shown()
		{
			using (var helper = new DInputHelper())
			{
				// Button 0: pressed and let go between two passes. Button 1: let go and pressed again while held.
				// Button 2: pressed and still held, which the state shows by itself. Button 3: did not move.
				helper._bufferedPresses[0] = true;
				helper._bufferedReleases[0] = true;
				helper._bufferedReleases[1] = true;
				helper._bufferedPresses[1] = true;
				helper._bufferedPresses[2] = true;
				var previous = State(1);
				var state = State(1, 2);
				helper.ShowBufferedChanges(previous, state, true);
				Assert.IsTrue(state.Buttons[0], "A tap between two passes was lost.");
				Assert.IsFalse(state.Buttons[1], "A release and press while held was lost.");
				Assert.IsTrue(state.Buttons[2], "A press the state shows was undone.");
				Assert.IsFalse(state.Buttons[3], "A button that did not move was shown pressed.");
				// A press the state caught after the buffer was last read: it is in the next buffer, and the state shows it already.
				previous = State(0);
				state = State(0);
				Array.Clear(helper._bufferedReleases, 0, helper._bufferedReleases.Length);
				helper.ShowBufferedChanges(previous, state, true);
				Assert.IsTrue(state.Buttons[0], "A press already shown was shown let go.");
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A hat tapped and a stick flicked between two passes, which DirectInput kept, are shown for one pass; jitter and a device whose axes report movement are not")]
		public void A_kept_hat_tap_and_stick_flick_are_shown()
		{
			using (var helper = new DInputHelper())
			{
				var previous = State();
				var state = State();
				previous.Povs[0] = state.Povs[0] = -1;
				previous.Povs[1] = state.Povs[1] = -1;
				previous.Axis[0] = state.Axis[0] = 32767;
				previous.Axis[1] = state.Axis[1] = 32767;
				// Hat 1: right and back. Hat 2: did not move.
				helper._bufferedPovMoved[0] = true;
				helper._bufferedPovLast[0] = -1;
				helper._bufferedPovOther[0] = 9000;
				helper._bufferedPovHasOther[0] = true;
				// X: flicked fully left and back. Y: jitter around the centre.
				helper._bufferedMoved[0] = true;
				helper._bufferedLow[0] = 0;
				helper._bufferedHigh[0] = 32767;
				helper._bufferedMoved[1] = true;
				helper._bufferedLow[1] = 32700;
				helper._bufferedHigh[1] = 32900;
				helper.ShowBufferedChanges(previous, state, false);
				Assert.AreEqual(9000, state.Povs[0], "A hat tap between two passes was lost.");
				Assert.AreEqual(32767, state.Axis[0], "A device whose axes report movement was shown a kept value as a place.");
				helper.ShowBufferedChanges(previous, state, true);
				Assert.AreEqual(-1, state.Povs[1], "A hat that did not move was moved.");
				Assert.AreEqual(0, state.Axis[0], "A flick between two passes was lost.");
				Assert.AreEqual(32767, state.Axis[1], "Jitter was shown as a flick.");
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A Raw Input button whose count grew by two or more while it reads the same is shown changed for one pass")]
		public void A_counted_change_the_state_missed_is_shown()
		{
			var previous = State(1);
			var state = State(1, 2);
			state.ButtonChanges[0] = 2;
			state.ButtonChanges[1] = 2;
			state.ButtonChanges[2] = 1;
			// Hat 1: right and back. X: flicked fully left and back. Y: turned back, but only a jitter's width.
			previous.Povs[0] = state.Povs[0] = -1;
			state.PovChanges[0] = 2;
			state.PovsBefore[0] = 9000;
			previous.Axis[0] = state.Axis[0] = 32767;
			state.AxisTurns[0] = 1;
			state.AxisTurnValues[0] = 0;
			previous.Axis[1] = state.Axis[1] = 32767;
			state.AxisTurns[1] = 5;
			state.AxisTurnValues[1] = 32900;
			DInputHelper.ShowChangesBetweenPasses(previous, state);
			Assert.IsTrue(state.Buttons[0], "A tap between two passes was lost.");
			Assert.IsFalse(state.Buttons[1], "A release and press while held was lost.");
			Assert.IsTrue(state.Buttons[2], "A press the state shows was undone.");
			Assert.IsFalse(state.Buttons[3], "A button that did not move was shown pressed.");
			Assert.AreEqual(9000, state.Povs[0], "A hat tap between two passes was lost.");
			Assert.AreEqual(0, state.Axis[0], "A flick between two passes was lost.");
			Assert.AreEqual(32767, state.Axis[1], "Jitter was shown as a flick.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Reading every change is on unless the person turns it off")]
		public void Reading_every_change_is_on_by_default()
		{
			Assert.IsTrue(new Options().ReadEveryChange, "A new settings file misses quick presses.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("An attached DirectInput controller keeps its changes for the engine, and reading them makes nothing")]
		public void An_attached_controller_keeps_its_changes()
		{
			Ui.OnUiThread(() =>
			{
				using (var manager = new DirectInput())
				{
					var instance = manager.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly).FirstOrDefault();
					if (instance == null)
						Assert.Inconclusive("No DirectInput controller is attached.");
					using (var window = new Form())
					using (var joystick = new Joystick(manager, instance.InstanceGuid))
					using (var helper = new DInputHelper())
					{
						DInputHelper.KeepDeviceChanges(joystick);
						Assert.AreEqual(DInputHelper.DeviceBufferSize, joystick.Properties.BufferSize, instance.InstanceName + " does not keep its changes.");
						joystick.SetCooperativeLevel(window.Handle, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
						joystick.Acquire();
						try
						{
							Assert.IsTrue(helper.ReadBufferedChanges(joystick), instance.InstanceName + ": the kept changes could not be read.");
							var allocated = Allocations.FewestBytes(5, () =>
							{
								for (var i = 0; i < 1000; i++)
									helper.ReadBufferedChanges(joystick);
							});
							Assert.AreEqual(0L, allocated, "Bytes handed to the collector by 1000 reads of kept changes.");
						}
						finally
						{
							joystick.Unacquire();
						}
					}
				}
			});
		}
	}
}
