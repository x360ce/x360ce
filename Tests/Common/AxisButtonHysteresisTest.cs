// @under-test: Engine/Common/ConvertHelper.cs, Engine/Data/UserSetting.cs, App.v4/Common/DInput/DInputHelper.Step3.UpdateXiStates.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// When a button driven by an axis or a slider is pressed, and when it lets go.
	/// </summary>
	/// <remarks>
	/// A switch on a radio control transmitter, or a stick held near the press point, gives a reading
	/// that wobbles by a few steps. With one point for pressing and releasing, every wobble across it
	/// pressed and released the button, which a game reads as the arm switch flicked on and off. The
	/// button lets go a little below the point that pressed it.
	///
	/// Readings are DirectInput's, 0 to 65535. The press point is the row's dead zone, set on a 0 to
	/// 32767 scale; 8192 is its default.
	/// </remarks>
	[TestClass]
	public class AxisButtonHysteresisTest
	{
		const int PressPoint = 8192;
		const int ReleasePoint = PressPoint - ConvertHelper.AxisButtonReleaseMargin;

		static bool Next(int reading, bool wasPressed, bool inverted = false, bool half = false, int pressPoint = PressPoint)
		{
			return ConvertHelper.IsAxisButtonPressed(reading, inverted, half, pressPoint, wasPressed);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Rising, the button is pressed only once the reading passes the press point")]
		public void Rising_presses_past_the_press_point()
		{
			Assert.IsFalse(Next(0, false));
			Assert.IsFalse(Next(PressPoint, false), "At the press point itself the button is not yet pressed.");
			Assert.IsTrue(Next(PressPoint + 1, false));
			Assert.IsTrue(Next(65535, false));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Falling, the button stays pressed down to the release point and lets go at it")]
		public void Falling_releases_at_the_release_point()
		{
			Assert.AreEqual(6144, ReleasePoint, "The release point is a sixteenth of the press point's scale below it.");
			Assert.IsTrue(Next(PressPoint, true), "Back at the press point the button is still held.");
			Assert.IsTrue(Next(ReleasePoint + 1, true));
			Assert.IsFalse(Next(ReleasePoint, true), "At the release point the button lets go.");
			Assert.IsFalse(Next(ReleasePoint + 1, false), "Released, the button is not pressed again until the press point.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A reading wobbling across the press point presses the button once, not on every wobble")]
		public void A_wobble_across_the_press_point_presses_once()
		{
			var random = new Random(1);
			var pressed = false;
			var changes = 0;
			for (var i = 0; i < 1000; i++)
			{
				var now = Next(PressPoint + random.Next(-300, 301), pressed);
				if (now != pressed)
					changes++;
				pressed = now;
			}
			Assert.IsTrue(changes <= 1, "The button changed " + changes + " times while the reading wobbled 300 steps either side of the press point.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Inverted, the low end presses the button and the release point is measured from that end")]
		public void Inverted_presses_at_the_low_end()
		{
			Assert.IsTrue(Next(0, false, inverted: true));
			Assert.IsFalse(Next(65535, false, inverted: true));
			// 65535 - 57000 = 8535, past the press point.
			Assert.IsTrue(Next(57000, false, inverted: true));
			// 65535 - 58000 = 7535, between the release point and the press point: held only if it was.
			Assert.IsTrue(Next(58000, true, inverted: true));
			Assert.IsFalse(Next(58000, false, inverted: true));
			// 65535 - 59500 = 6035, below the release point.
			Assert.IsFalse(Next(59500, true, inverted: true));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Half, only the travel past the middle counts, and the middle itself is released")]
		public void Half_counts_only_past_the_middle()
		{
			Assert.IsFalse(Next(32767, false, half: true), "The middle of a three-position switch presses nothing.");
			Assert.IsFalse(Next(0, false, half: true), "The other half never presses the button.");
			// 40961 - 32768 = 8193, past the press point.
			Assert.IsTrue(Next(40961, false, half: true));
			// 40000 - 32768 = 7232, held only if it was.
			Assert.IsTrue(Next(40000, true, half: true));
			// 38000 - 32768 = 5232, below the release point.
			Assert.IsFalse(Next(38000, true, half: true));
			// Inverted half is the half below the middle.
			Assert.IsTrue(Next(0, false, inverted: true, half: true));
			Assert.IsFalse(Next(65535, false, inverted: true, half: true));
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A small press point still lets go before the control is back at rest")]
		public void A_small_press_point_still_lets_go()
		{
			// A whole margin below 1000 would be below nought, so half of the press point is used.
			Assert.IsTrue(Next(501, true, pressPoint: 1000));
			Assert.IsFalse(Next(500, true, pressPoint: 1000));
			// Nought presses at the first movement and lets go at rest.
			Assert.IsTrue(Next(1, false, pressPoint: 0));
			Assert.IsFalse(Next(0, true, pressPoint: 0));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A two-position switch presses at one end and lets go at the other, every time")]
		public void A_two_position_switch_follows_its_ends()
		{
			var pressed = false;
			foreach (var reading in new[] { 0, 65535, 0, 65535, 0 })
			{
				pressed = Next(reading, pressed);
				Assert.AreEqual(reading == 65535, pressed, "At " + reading);
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("performance")]
		[Description("Deciding hands nothing to the collector")]
		public void Deciding_hands_nothing_to_the_collector()
		{
			var pressed = Next(0, false);
			const int calls = 20000;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < calls; i++)
					pressed = ConvertHelper.IsAxisButtonPressed(i & 0xFFFF, (i & 1) == 0, (i & 2) == 0, PressPoint, pressed);
			});
			Assert.IsTrue(allocated < calls,
				"Deciding " + calls + " times handed the collector " + allocated + " bytes; it runs for every axis button on every poll.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The engine keeps what each device's axis buttons held and decides through the one rule")]
		public void The_engine_keeps_the_held_buttons_per_device()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step3.UpdateXiStates.cs");
			var source = File.ReadAllText(path);
			StringAssert.Contains(source, "var wasAxisButtons = setting.AxisButtons;", "The engine does not read what was held on the last pass.");
			StringAssert.Contains(source, "setting.AxisButtons = GamepadButtonFlags.None;", "The engine does not clear what it held at the top of the per-setting loop.");
			Assert.IsTrue(source.IndexOf("setting.AxisButtons = GamepadButtonFlags.None;") < source.IndexOf("if (ud.JoState == null)"),
				"The clear must sit at the top of the per-setting loop, before the turn can end early.");
			StringAssert.Contains(source, "axisButtons |= map.ButtonFlag;", "The engine does not record a pressed axis button as it decides one.");
			StringAssert.Contains(source, "setting.AxisButtons = axisButtons;", "The engine does not keep what it held for the next pass.");
			StringAssert.Contains(source, "ConvertHelper.IsAxisButtonPressed(v, map.IsInverted, half, map.DeadZone, wasPressed)",
				"The engine decides an axis button some other way than the tested rule.");
			Assert.IsFalse(source.Contains("if (v > map.DeadZone)"), "The single threshold is still in the engine.");
		}
	}
}
