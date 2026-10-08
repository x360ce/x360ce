// @under-test: Engine/Input/Processors/DirectInputChanges.cs, Engine/Input/Devices/DirectInputDevice.cs, Engine/Input/States/SourceState.cs, App.v4/Common/Options.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
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

		/// <summary>The size of DIDEVICEOBJECTDATA in a 64-bit process, which the test laid entries out as.</summary>
		const int EntrySize = 24;

		/// <summary>What DirectInput kept, as it hands it over: each change as the control's offset in DirectInput's joystick state, then its value.</summary>
		static DirectInputChanges Kept(params int[] offsetsAndValues)
		{
			var count = offsetsAndValues.Length / 2;
			var data = new byte[Math.Max(1, count) * EntrySize];
			for (var i = 0; i < count; i++)
			{
				BitConverter.GetBytes(offsetsAndValues[i * 2]).CopyTo(data, i * EntrySize);
				BitConverter.GetBytes(offsetsAndValues[i * 2 + 1]).CopyTo(data, i * EntrySize + 4);
			}
			var kept = new DirectInputChanges();
			kept.Load(data, count, EntrySize);
			return kept;
		}

		const int Button0 = (int)JoystickOffset.Buttons0;
		const int Hat0 = (int)JoystickOffset.PointOfViewControllers0;
		const int Pressed = 0x80;

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A button that reads as before while DirectInput kept a change of it to the other value is shown changed for one pass; one that changed, or did not move, is shown as read")]
		public void A_kept_change_the_state_missed_is_shown()
		{
			// Button 0: pressed and let go between two passes. Button 1: let go and pressed again while held.
			// Button 2: pressed and still held, which the state shows by itself. Button 3: did not move.
			var kept = Kept(Button0, Pressed, Button0, 0, Button0 + 1, 0, Button0 + 1, Pressed, Button0 + 2, Pressed);
			var previous = State(1);
			var state = State(1, 2);
			Assert.AreEqual(2, kept.ShowIn(previous, state, true), "The two changes the state missed are not counted.");
			Assert.IsTrue(state.Buttons[0], "A tap between two passes was lost.");
			Assert.IsFalse(state.Buttons[1], "A release and press while held was lost.");
			Assert.IsTrue(state.Buttons[2], "A press the state shows was undone.");
			Assert.IsFalse(state.Buttons[3], "A button that did not move was shown pressed.");
			// A press the state caught after the buffer was last read: it is in the next buffer, and the state shows it already.
			previous = State(0);
			state = State(0);
			kept = Kept(Button0, Pressed);
			Assert.AreEqual(0, kept.ShowIn(previous, state, true), "A press the state caught was counted as missed.");
			Assert.IsTrue(state.Buttons[0], "A press already shown was shown let go.");
			// A pass on which DirectInput kept nothing, as most are: nothing is left of the read before.
			kept.Load(new byte[EntrySize], 0, EntrySize);
			Assert.IsFalse(kept.Any);
			Assert.IsFalse(kept.Presses[0], "A change of the read before was kept into this one.");
			Assert.AreEqual(0, kept.ShowIn(State(), State(), true));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A hat tapped and a stick flicked between two passes, which DirectInput kept, are shown for one pass; jitter and a device whose axes report movement are not")]
		public void A_kept_hat_tap_and_stick_flick_are_shown()
		{
			var previous = State();
			var state = State();
			previous.Povs[0] = state.Povs[0] = -1;
			previous.Povs[1] = state.Povs[1] = -1;
			previous.Axis[0] = state.Axis[0] = 32767;
			previous.Axis[1] = state.Axis[1] = 32767;
			// Hat 1: right and back; hat 2 did not move. X: flicked fully left and back. Y: jitter around the centre.
			var kept = Kept(Hat0, 9000, Hat0, -1, (int)JoystickOffset.X, 0, (int)JoystickOffset.X, 32767,
				(int)JoystickOffset.Y, 32700, (int)JoystickOffset.Y, 32900);
			kept.ShowIn(previous, state, false);
			Assert.AreEqual(9000, state.Povs[0], "A hat tap between two passes was lost.");
			Assert.AreEqual(32767, state.Axis[0], "A device whose axes report movement was shown a kept value as a place.");
			kept.ShowIn(previous, state, true);
			Assert.AreEqual(-1, state.Povs[1], "A hat that did not move was moved.");
			Assert.AreEqual(0, state.Axis[0], "A flick between two passes was lost.");
			Assert.AreEqual(32767, state.Axis[1], "Jitter was shown as a flick.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A Raw Input control whose count grew while it reads the same is shown changed for one pass: a button tapped, a hat tapped, a stick flicked; jitter is not")]
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
			Assert.AreEqual(4, state.ShowChangesSince(previous), "Two buttons, a hat and a stick the newest values missed are not counted.");
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
		[Description("An attached DirectInput controller names itself as the other sources do, keeps its changes for the engine, and reading them makes nothing")]
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
					using (var device = new DirectInputDevice(manager, instance.InstanceGuid))
					{
						IInputSourceDevice named = device;
						Assert.AreEqual(InputSourceType.DirectInput, named.Source);
						Assert.AreEqual(instance.InstanceGuid, named.InstanceGuid);
						Assert.AreEqual(instance.ProductGuid, named.ProductGuid);
						if (instance.IsHumanInterfaceDevice)
						{
							StringAssert.StartsWith(named.InterfacePath, @"\\?\", instance.InstanceName + " has no interface path.");
							Assert.AreEqual(instance.ProductGuid, RawInputDevice.GetProductGuid(named.VendorId, named.ProductId), instance.InstanceName + ": vendor and product IDs are not those of its product GUID.");
						}
						device.KeepChanges();
						Assert.AreEqual(DirectInputDevice.BufferSize, device.Joystick.Properties.BufferSize, instance.InstanceName + " does not keep its changes.");
						device.Joystick.SetCooperativeLevel(window.Handle, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
						device.Joystick.Acquire();
						try
						{
							Assert.IsTrue(device.ReadChanges(), instance.InstanceName + ": the kept changes could not be read.");
							var allocated = Allocations.FewestBytes(5, () =>
							{
								for (var i = 0; i < 1000; i++)
									device.ReadChanges();
							});
							Assert.AreEqual(0L, allocated, "Bytes handed to the collector by 1000 reads of kept changes.");
						}
						finally
						{
							device.Joystick.Unacquire();
						}
					}
				}
			});
		}
	}
}
