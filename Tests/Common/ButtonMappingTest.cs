// @under-test: Engine/Common/ConvertHelper.cs, Engine/Maps/SettingsConverter.cs, App.v4/Common/DInput/DInputHelper.Step3.UpdateXiStates.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.XInput;
using System.IO;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// How a mapping to a device button is read: which way round, and which buttons it can reach.
	/// </summary>
	/// <remarks>
	/// The mapping menu offers every button inverted, and the stored value keeps the minus sign, so an
	/// inverted button has to count as pressed while it is released. The index a mapping names counts
	/// from one, so button 128, the last one a device's state holds, is the one whose index is 128.
	/// </remarks>
	[TestClass]
	public class ButtonMappingTest
	{
		/// <summary>A device's 128 buttons with the given ones, counted from one, held.</summary>
		static bool[] Buttons(params int[] held)
		{
			var buttons = new bool[128];
			foreach (var b in held)
				buttons[b - 1] = true;
			return buttons;
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A plain button is pressed while it is held")]
		public void A_plain_button_is_pressed_while_held()
		{
			var map = new Map(MapCode.ButtonA, "3", GamepadButtonFlags.A, "");
			Assert.IsFalse(map.IsInverted, "A plain button is marked inverted.");
			Assert.IsTrue(ConvertHelper.IsButtonPressed(Buttons(3), map.Index, map.IsInverted), "Held, a plain button must count as pressed.");
			Assert.IsFalse(ConvertHelper.IsButtonPressed(Buttons(), map.Index, map.IsInverted), "Released, a plain button must count as released.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("An inverted button is pressed while it is released")]
		public void An_inverted_button_is_pressed_while_released()
		{
			// The menu shows it as "IButton 3"; the settings keep it as "-3".
			var map = new Map(MapCode.ButtonA, "-3", GamepadButtonFlags.A, "");
			Assert.IsTrue(map.IsButton, "A minus sign in a button field still names a button.");
			Assert.IsTrue(map.IsInverted, "An inverted button is not marked inverted, so nothing turns it round.");
			Assert.IsTrue(ConvertHelper.IsButtonPressed(Buttons(), map.Index, map.IsInverted), "Released, an inverted button must count as pressed.");
			Assert.IsFalse(ConvertHelper.IsButtonPressed(Buttons(3), map.Index, map.IsInverted), "Held, an inverted button must count as released.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Button 128 can be mapped, and one past it presses nothing")]
		public void The_last_button_is_reachable()
		{
			Assert.IsTrue(ConvertHelper.IsButtonPressed(Buttons(128), 128, false), "Button 128 is held and was not read.");
			Assert.IsFalse(ConvertHelper.IsButtonPressed(Buttons(128), 129, false), "A button the device does not have must read as released.");
			Assert.IsFalse(ConvertHelper.IsButtonPressed(Buttons(), 129, true), "An inverted button the device does not have must not press anything.");
			Assert.IsFalse(ConvertHelper.IsButtonPressed(Buttons(1), 0, false), "Nought names no button.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Invert turns a button round and back, as it does an axis or a slider")]
		public void Invert_turns_a_button_round_and_back()
		{
			Assert.AreEqual(MapType.IButton, SettingsConverter.Invert(MapType.Button));
			Assert.AreEqual(MapType.Button, SettingsConverter.Invert(MapType.IButton));
			Assert.AreEqual(MapType.ISlider, SettingsConverter.Invert(MapType.Slider));
			Assert.AreEqual(MapType.POV, SettingsConverter.Invert(MapType.POV), "A D-Pad has no other way round.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The engine reads mapped buttons through the one rule and bounds every index counted from one")]
		public void The_engine_reads_buttons_through_the_rule()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step3.UpdateXiStates.cs");
			var source = File.ReadAllText(path);
			StringAssert.Contains(source, "ConvertHelper.IsButtonPressed(diState.Buttons, map.Index, map.IsInverted)",
				"The engine reads mapped buttons some other way, so an inverted or a last button can go wrong again.");
			Assert.IsFalse(source.Contains("map.Index < diState.Buttons.Length"),
				"A button is counted from one, so '<' leaves the last one out.");
			Assert.IsFalse(source.Contains("map.Index < dPadButtons.Length"),
				"A D-Pad direction is counted from one, so '<' leaves the last one out.");
			StringAssert.Contains(source, "index <= diState.Povs.Length",
				"A D-Pad the device does not have is read past the end of its directions.");
		}
	}
}
