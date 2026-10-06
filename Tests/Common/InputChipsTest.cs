// @under-test: App.v4/Controls/PadTabPages/General/InputChips.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System.Linq;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The input panel shows the controls a device has and lights the ones being used, before
	/// anything is mapped. Which chips exist and when they light are decided here, away from
	/// the drawing, so both can be checked without a device in hand.
	/// </summary>
	[TestClass]
	public class InputChipsTest
	{
		static SourceState Rest()
		{
			var state = new SourceState(new JoystickState());
			for (var i = 0; i < state.Axis.Length; i++)
				state.Axis[i] = InputChips.AxisCentre;
			for (var i = 0; i < state.Povs.Length; i++)
				state.Povs[i] = InputChips.PovRest;
			return state;
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A stick with X, Y and RZ shows axes 1, 2 and 6, as the mapping list does")]
		public void Axes_are_numbered_by_slot_not_by_count()
		{
			var chips = InputChips.Create(12, 0x1 | 0x2 | 0x20, 0, 1);
			var axes = chips.Where(c => c.Kind == InputChipKind.Axis).Select(c => c.Caption).ToArray();
			CollectionAssert.AreEqual(new[] { "1", "2", "6" }, axes);
			Assert.AreEqual("Axis 6", chips.First(c => c.Kind == InputChipKind.Axis && c.Index == 5).Payload);
			Assert.AreEqual(12, chips.Count(c => c.Kind == InputChipKind.Button));
			Assert.AreEqual(0, chips.Count(c => c.Kind == InputChipKind.Slider), "No slider bit, no slider chip.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A wheel with two sliders shows Slider 1 and Slider 2 from its slider mask")]
		public void Sliders_come_from_the_slider_mask()
		{
			var chips = InputChips.Create(8, 0x1, 0x3, 0);
			var sliders = chips.Where(c => c.Kind == InputChipKind.Slider).Select(c => c.Payload).ToArray();
			CollectionAssert.AreEqual(new[] { "Slider 1", "Slider 2" }, sliders);
			Assert.AreEqual(0, chips.Count(c => c.Kind == InputChipKind.Pov || c.Kind == InputChipKind.PovDirection));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Each POV is followed by its four directions, and their payloads name the direction")]
		public void Each_pov_carries_its_four_directions()
		{
			var chips = InputChips.Create(0, 0, 0, 2);
			var povs = chips.Where(c => c.Kind == InputChipKind.Pov || c.Kind == InputChipKind.PovDirection).ToList();
			CollectionAssert.AreEqual(new[] { "1", "U", "R", "D", "L", "2", "U", "R", "D", "L" }, povs.Select(c => c.Caption).ToArray());
			Assert.AreEqual("POV 2 Left", povs.Last().Payload);
			Assert.AreEqual("POV 1", povs.First().Payload);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Every payload is a mapping the settings parser accepts back")]
		public void Payloads_round_trip_through_the_parser()
		{
			var chips = InputChips.Create(3, 0x1 | 0x20, 0x2, 2);
			foreach (var chip in chips)
			{
				MapType type;
				int index;
				Assert.IsTrue(SettingsConverter.TryParseTextValue(chip.Payload, out type, out index), chip.Payload + " is not a mapping.");
				Assert.AreEqual(chip.Payload, SettingsConverter.ToTextValue(type, index), "Parsing and writing back changed the text.");
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A chip lights when its control is pressed or moved, and reports the change once")]
		public void Chips_light_when_their_control_is_used()
		{
			var chips = InputChips.Create(4, 0x1 | 0x2, 0x1, 1);
			var state = Rest();
			Assert.IsTrue(InputChips.Update(chips, state, 0), "The first reading is itself a change from nothing.");
			Assert.IsFalse(InputChips.Update(chips, state, 10), "Nothing moved, nothing changed.");
			Assert.IsFalse(chips.Any(c => c.Lit), "Everything at rest lights nothing.");

			state.Buttons[2] = true;
			state.Axis[0] = InputChips.AxisCentre + ConvertHelper.AxisButtonReleaseMargin + 1;
			state.Axis[1] = InputChips.AxisCentre - ConvertHelper.AxisButtonReleaseMargin;
			state.Sliders[0] = ConvertHelper.AxisButtonReleaseMargin + 1;
			state.Povs[0] = 9000;
			Assert.IsTrue(InputChips.Update(chips, state, 20));
			Assert.IsTrue(chips.Single(c => c.Kind == InputChipKind.Button && c.Index == 2).Lit);
			Assert.IsFalse(chips.Single(c => c.Kind == InputChipKind.Button && c.Index == 1).Lit);
			Assert.IsTrue(chips.Single(c => c.Kind == InputChipKind.Axis && c.Index == 0).Lit, "A move past the margin lights.");
			Assert.IsFalse(chips.Single(c => c.Kind == InputChipKind.Axis && c.Index == 1).Lit, "A move within the margin is wobble.");
			Assert.AreEqual(InputChips.AxisCentre + ConvertHelper.AxisButtonReleaseMargin + 1, chips.Single(c => c.Kind == InputChipKind.Axis && c.Index == 0).Value);
			Assert.IsTrue(chips.Single(c => c.Kind == InputChipKind.Slider).Lit);
			Assert.IsTrue(chips.Single(c => c.Kind == InputChipKind.Pov).Lit);
			var directions = chips.Where(c => c.Kind == InputChipKind.PovDirection).ToList();
			CollectionAssert.AreEqual(new[] { false, true, false, false }, directions.Select(c => c.Lit).ToArray(), "9000 is right.");
			Assert.AreEqual(9000, directions[1].Value);
			Assert.AreEqual(InputChips.PovRest, directions[0].Value);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("No state puts every chip at rest rather than throwing")]
		public void No_state_is_everything_at_rest()
		{
			var chips = InputChips.Create(2, 0x1, 0x1, 1);
			InputChips.Update(chips, Rest(), 0);
			chips[0].Lit = true;
			Assert.IsTrue(InputChips.Update(chips, null, 10));
			Assert.IsFalse(chips.Any(c => c.Lit));
			Assert.AreEqual(InputChips.AxisCentre, chips.Single(c => c.Kind == InputChipKind.Axis).Value);
			Assert.AreEqual(InputChips.PovRest, chips.Single(c => c.Kind == InputChipKind.Pov).Value);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Counts past what the state arrays hold are capped rather than indexed out of range")]
		public void Counts_are_capped_to_the_state_arrays()
		{
			var chips = InputChips.Create(500, 0, 0, 9);
			Assert.AreEqual(DirectInputLayout.ButtonOffsets.Count, chips.Count(c => c.Kind == InputChipKind.Button));
			Assert.AreEqual(DirectInputLayout.PovOffsets.Count, chips.Count(c => c.Kind == InputChipKind.Pov));
			// Reading a rest state into chips that start at rest changes nothing, and throws nothing.
			Assert.IsFalse(InputChips.Update(chips, Rest(), 0));
			Assert.IsFalse(chips.Any(c => c.Lit));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A moved axis stays lit while it moves and for a second after, wherever it stops")]
		public void A_moved_axis_goes_dark_a_second_after_it_stops()
		{
			var chips = InputChips.Create(0, 0x1, 0, 0);
			var axis = chips.Single();
			var state = Rest();
			InputChips.Update(chips, state, 0);
			state.Axis[0] = 65535;
			InputChips.Update(chips, state, 100);
			Assert.IsTrue(axis.Lit, "A moved axis is dark.");
			InputChips.Update(chips, state, 100 + InputChips.HoldMs - 1);
			Assert.IsTrue(axis.Lit, "A moved axis goes dark before the second is up.");
			InputChips.Update(chips, state, 100 + InputChips.HoldMs);
			Assert.IsFalse(axis.Lit, "An axis left away from the middle stays lit.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A pedal or a throttle resting at its end, and a control that wobbles, stay dark")]
		public void Controls_resting_anywhere_stay_dark()
		{
			var chips = InputChips.Create(0, 0x1, 0x1, 0);
			var state = Rest();
			state.Sliders[0] = 65535;
			for (var t = 0; t <= 5000; t += 100)
			{
				// A pedal resting at full, wobbling by 500 steps.
				state.Axis[0] = 65535 - (t / 100 % 2) * 500;
				InputChips.Update(chips, state, t);
				Assert.IsFalse(chips.Any(c => c.Lit), "Lit at rest at " + t + " ms.");
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A slider moved slowly, a little each tick, still lights")]
		public void A_slow_slide_lights()
		{
			var chips = InputChips.Create(0, 0, 0x1, 0);
			var slider = chips.Single();
			var state = Rest();
			InputChips.Update(chips, state, 0);
			var lit = false;
			for (var i = 1; i <= 10 && !lit; i++)
			{
				state.Sliders[0] = i * 300;
				InputChips.Update(chips, state, i * 100);
				lit = slider.Lit;
			}
			Assert.IsTrue(lit, "A slider moved 300 steps a tick never lights.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A diagonal lights the two directions beside it, as the virtual D-pad presses them")]
		public void A_diagonal_lights_two_directions()
		{
			var chips = InputChips.Create(0, 0, 0, 1);
			var directions = chips.Where(c => c.Kind == InputChipKind.PovDirection).ToList();
			var state = Rest();
			foreach (var reading in new[] { -1, 0, 2249, 2250, 4500, 9000, 13500, 18000, 22500, 27000, 31500, 35999 })
			{
				state.Povs[0] = reading;
				InputChips.Update(chips, state, 0);
				for (var d = 0; d < 4; d++)
					Assert.AreEqual(ConvertHelper.IsPovDirectionPressed(reading, d), directions[d].Lit,
						reading + " lights " + directions[d].Caption + " differently from the D-pad.");
			}
			state.Povs[0] = 4500;
			InputChips.Update(chips, state, 0);
			CollectionAssert.AreEqual(new[] { true, true, false, false }, directions.Select(c => c.Lit).ToArray(), "4500 is up and right.");
			state.Povs[0] = 31500;
			InputChips.Update(chips, state, 0);
			CollectionAssert.AreEqual(new[] { true, false, false, true }, directions.Select(c => c.Lit).ToArray(), "31500 is up and left.");
			state.Povs[0] = 2249;
			InputChips.Update(chips, state, 0);
			CollectionAssert.AreEqual(new[] { true, false, false, false }, directions.Select(c => c.Lit).ToArray(), "Just right of up is up.");
		}
	}
}
